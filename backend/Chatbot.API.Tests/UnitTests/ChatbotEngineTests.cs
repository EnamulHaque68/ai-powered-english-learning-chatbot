using Chatbot.API.Data;
using Chatbot.API.DTOs.ChildProfile;
using Chatbot.API.DTOs.Conversation;
using Chatbot.API.Entities;
using Chatbot.API.Services;
using Chatbot.API.Services.Chatbot;
using Chatbot.API.Services.ChildSafety;
using Chatbot.API.Tests.Helpers;
using Xunit;

namespace Chatbot.API.Tests.UnitTests;

public class ChatbotEngineTests
{
    private static (RuleBasedChatbotEngine Engine, ChildProfile Child, ChildLearningDbContext Db) CreateTestEnvironment()
    {
        var db = TestDbContextFactory.CreateInMemoryDbContext();
        var safetyService = new ChildSafetyService();
        var contextService = new ChatbotContextService();
        var progressionService = new LevelProgressionService(db);
        var progressService = new LearningProgressService(db);
        var profileService = new ChildProfileService(db);

        var childDto = profileService.CreateAsync(new CreateChildProfileRequest(
            FirstName: "Leo",
            LastName: null,
            DateOfBirth: new DateOnly(2022, 6, 1), // 4 years old -> L1 Toddler
            CurrentLevelId: 1,
            PreferredLanguage: "en",
            Interests: "Animals, Cars",
            ParentEmail: "parent@test.com"
        )).GetAwaiter().GetResult();

        var child = db.ChildProfiles.Find(childDto.Id)!;

        var engine = new RuleBasedChatbotEngine(
            db,
            safetyService,
            contextService,
            progressionService,
            progressService
        );

        return (engine, child, db);
    }

    [Theory]
    [InlineData("I want to learn words", ChatbotMode.Learning)]
    [InlineData("Let's talk and chat", ChatbotMode.Conversation)]
    [InlineData("Can we play a game riddle?", ChatbotMode.Engagement)]
    [InlineData("I feel sad and need help", ChatbotMode.Support)]
    public async Task ProcessMessage_DetectsAndSwitchesModesAccurately(string userMessage, ChatbotMode expectedMode)
    {
        // Arrange
        var (engine, child, _) = CreateTestEnvironment();

        var request = new ChatbotMessageRequest(
            ChildId: child.Id,
            SessionId: null,
            Message: userMessage,
            ModeOverride: null,
            LevelOverride: null
        );

        // Act
        var response = await engine.ProcessMessageAsync(request);

        // Assert
        Assert.Equal(expectedMode, response.ActiveMode);
        Assert.False(string.IsNullOrWhiteSpace(response.ReplyText));
        Assert.NotNull(response.SuggestedReplies);
        Assert.True(response.SuggestedReplies.Count > 0);
    }

    [Fact]
    public async Task ProcessMessage_MeasuresResponseLatency()
    {
        // Arrange
        var (engine, child, _) = CreateTestEnvironment();

        var request = new ChatbotMessageRequest(
            ChildId: child.Id,
            SessionId: null,
            Message: "Hello Buddy!",
            ModeOverride: ChatbotMode.Conversation,
            LevelOverride: null
        );

        // Act
        var response = await engine.ProcessMessageAsync(request);

        // Assert
        Assert.True(response.ResponseTimeMs >= 0, "Response latency must be measured in milliseconds.");
        Assert.True(response.ResponseTimeMs < 2000, "Rule-based engine should respond within reasonable limits (< 2000ms).");
    }

    [Fact]
    public async Task LearningMode_ConsecutiveCorrectAnswers_TriggersLevelProgressionFromL1ToL2()
    {
        // Arrange
        var (engine, child, db) = CreateTestEnvironment();

        // 1. Initial greeting to receive first question
        var startReply = await engine.ProcessMessageAsync(new ChatbotMessageRequest(
            child.Id, null, "Let's learn colors", ChatbotMode.Learning, ChildLearningLevel.L1_Toddler));

        var sessionId = startReply.SessionId;
        Assert.Equal(ChildLearningLevel.L1_Toddler, startReply.Level);

        // 2. Answer Question 1 correctly ("red" for strawberry)
        var reply1 = await engine.ProcessMessageAsync(new ChatbotMessageRequest(
            child.Id, sessionId, "The strawberry is red", ChatbotMode.Learning, null));
        Assert.Equal(1, reply1.StarsAwarded);

        // 3. Answer Question 2 correctly ("moo" for cow)
        var reply2 = await engine.ProcessMessageAsync(new ChatbotMessageRequest(
            child.Id, sessionId, "The cow says moo!", ChatbotMode.Learning, null));
        Assert.Equal(1, reply2.StarsAwarded);

        // 4. Answer Question 3 correctly ("a" for apple) -> 3rd in a row!
        var reply3 = await engine.ProcessMessageAsync(new ChatbotMessageRequest(
            child.Id, sessionId, "A for apple", ChatbotMode.Learning, null));

        // Assert
        Assert.Equal(1, reply3.StarsAwarded);
        Assert.NotNull(reply3.LevelProgressionAlert);
        Assert.Contains("LEVEL UP", reply3.LevelProgressionAlert);
        Assert.Contains("Level 2", reply3.LevelProgressionAlert);

        // Verify database persistence of new level
        var reloadedChild = await db.ChildProfiles.FindAsync(child.Id);
        Assert.NotNull(reloadedChild);
        Assert.Equal((int)ChildLearningLevel.L2_EarlyLearner, reloadedChild.CurrentLevelId);
    }

    [Fact]
    public async Task LearningMode_PersistsLearningProgressInDatabase()
    {
        // Arrange
        var (engine, child, db) = CreateTestEnvironment();

        // Start session & get first prompt
        var turn1 = await engine.ProcessMessageAsync(new ChatbotMessageRequest(
            child.Id, null, "Start", ChatbotMode.Learning, ChildLearningLevel.L1_Toddler));

        // Answer correctly
        await engine.ProcessMessageAsync(new ChatbotMessageRequest(
            child.Id, turn1.SessionId, "red", ChatbotMode.Learning, null));

        // Assert - LearningProgress records exist in DB
        var progressRecords = db.LearningProgress.Where(p => p.ChildProfileId == child.Id).ToList();
        Assert.NotEmpty(progressRecords);
        Assert.Contains(progressRecords, p => p.CompletedActivitiesCount > 0);
        Assert.Contains(progressRecords, p => p.ProficiencyScore > 0);
    }

    [Theory]
    [InlineData("I want to punch and fight you!")]
    [InlineData("What is your phone number and address?")]
    [InlineData("I am scared and crying")]
    public async Task ChildSafety_UnsafeOrDistressInputs_RedirectsPositivelyWithoutScolding(string unsafeInput)
    {
        // Arrange
        var (engine, child, _) = CreateTestEnvironment();

        var request = new ChatbotMessageRequest(
            ChildId: child.Id,
            SessionId: null,
            Message: unsafeInput,
            ModeOverride: null,
            LevelOverride: null
        );

        // Act
        var response = await engine.ProcessMessageAsync(request);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(response.ReplyText));
        Assert.DoesNotContain("punish", response.ReplyText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bad boy", response.ReplyText, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("forbidden", response.ReplyText, StringComparison.OrdinalIgnoreCase);
        // Reply must contain friendly reassuring elements (emoji, puppy/hug/colors)
        Assert.True(response.SentimentScore >= 0.8);
    }

    [Fact]
    public async Task ContextService_MaintainsRecentDialogueHistory()
    {
        // Arrange
        var (engine, child, _) = CreateTestEnvironment();

        var reply1 = await engine.ProcessMessageAsync(new ChatbotMessageRequest(
            child.Id, null, "Hello!", ChatbotMode.Conversation, null));

        await engine.ProcessMessageAsync(new ChatbotMessageRequest(
            child.Id, reply1.SessionId, "I love blue", ChatbotMode.Conversation, null));

        // Act
        var context = await engine.GetSessionContextAsync(reply1.SessionId);

        // Assert
        Assert.NotNull(context);
        Assert.Equal(reply1.SessionId, context.SessionId);
        Assert.Equal(child.Id, context.ChildId);
        Assert.True(context.RecentTurns.Count >= 2);
        Assert.Equal("I love blue", context.RecentTurns.Last().UserMessage);
    }
}
