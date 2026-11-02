using System.Diagnostics;
using System.Text.RegularExpressions;
using Chatbot.API.Data;
using Chatbot.API.DTOs.Conversation;
using Chatbot.API.DTOs.LearningProgress;
using Chatbot.API.Entities;
using Chatbot.API.Exceptions;
using Chatbot.API.Services.ChildSafety;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.API.Services.Chatbot;

public interface IRuleBasedChatbotEngine
{
    Task<ChatbotReplyResponse> ProcessMessageAsync(ChatbotMessageRequest request);
    Task<ChatbotMode> SwitchModeAsync(Guid sessionId, ChatbotMode newMode);
    Task<SessionContextDto> GetSessionContextAsync(Guid sessionId);
}

public class RuleBasedChatbotEngine : IRuleBasedChatbotEngine
{
    private readonly ChildLearningDbContext _db;
    private readonly IChildSafetyService _safetyService;
    private readonly IChatbotContextService _contextService;
    private readonly ILevelProgressionService _progressionService;
    private readonly ILearningProgressService _progressService;

    public RuleBasedChatbotEngine(
        ChildLearningDbContext db,
        IChildSafetyService safetyService,
        IChatbotContextService contextService,
        ILevelProgressionService progressionService,
        ILearningProgressService progressService)
    {
        _db = db;
        _safetyService = safetyService;
        _contextService = contextService;
        _progressionService = progressionService;
        _progressService = progressService;
    }

    public async Task<ChatbotReplyResponse> ProcessMessageAsync(ChatbotMessageRequest request)
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. Resolve Child Profile
        var child = await _db.ChildProfiles
            .Include(c => c.LearningLevel)
            .FirstOrDefaultAsync(c => c.Id == request.ChildId);

        if (child == null)
            throw new NotFoundException(nameof(ChildProfile), request.ChildId);

        // 2. Resolve or create active session
        var session = await GetOrCreateActiveSessionAsync(child, request.SessionId);

        // 3. Resolve Level & Mode
        var level = _progressionService.ResolveLevel(child, request.LevelOverride);
        var activeMode = request.ModeOverride ?? DetectModeIntent(request.Message, session.ActiveMode);
        session.ActiveMode = activeMode;

        // 4. Child-Safety Filter Evaluation
        var safety = _safetyService.EvaluateInput(request.Message, level);
        if (!safety.IsSafe)
        {
            if (safety.SuggestSupportMode)
            {
                session.ActiveMode = ChatbotMode.Support;
            }

            stopwatch.Stop();
            var safetyLatency = stopwatch.ElapsedMilliseconds;

            var safetyReply = safety.SafeResponse ?? "Let's explore happy and bright colors! 🎨 What is your favorite color?";
            var safetyMsgId = await PersistTurnAsync(session, request.Message, safetyReply, safetyLatency, 0.9);

            return new ChatbotReplyResponse(
                SessionId: session.Id,
                MessageId: safetyMsgId,
                ReplyText: safetyReply,
                ActiveMode: session.ActiveMode,
                ActiveModeName: session.ActiveMode.ToString(),
                Level: level,
                LevelName: level.ToString(),
                StarsAwarded: 0,
                TotalSessionStars: session.StarsEarned,
                SuggestedReplies: GetSuggestedReplies(session.ActiveMode, level),
                LevelProgressionAlert: null,
                ResponseTimeMs: safetyLatency,
                ContextTopic: session.CurrentTopic,
                SentimentScore: 0.9
            );
        }

        // 5. Load Session Context
        var state = _contextService.GetState(session);
        state.ActiveMode = activeMode;

        // 6. Generate Mode-Specific Response
        var (botReplyText, starsAwarded, progressionAlert, newTopic) = await GenerateReplyAsync(
            child, session, state, request.Message, level, activeMode);

        if (!string.IsNullOrWhiteSpace(newTopic))
        {
            state.CurrentTopic = newTopic;
            session.CurrentTopic = newTopic;
        }

        // 7. Update Dialogue Context
        _contextService.AddTurn(state, request.Message, botReplyText, state.ExpectedAnswer);
        _contextService.UpdateState(session, state);

        stopwatch.Stop();
        var totalLatency = stopwatch.ElapsedMilliseconds;

        // 8. Persist Messages & State to DB
        var aiMsgId = await PersistTurnAsync(session, request.Message, botReplyText, totalLatency, 0.95);

        return new ChatbotReplyResponse(
            SessionId: session.Id,
            MessageId: aiMsgId,
            ReplyText: botReplyText,
            ActiveMode: session.ActiveMode,
            ActiveModeName: session.ActiveMode.ToString(),
            Level: level,
            LevelName: level.ToString(),
            StarsAwarded: starsAwarded,
            TotalSessionStars: session.StarsEarned,
            SuggestedReplies: GetSuggestedReplies(session.ActiveMode, level),
            LevelProgressionAlert: progressionAlert,
            ResponseTimeMs: totalLatency,
            ContextTopic: session.CurrentTopic,
            SentimentScore: 0.95
        );
    }

    public async Task<ChatbotMode> SwitchModeAsync(Guid sessionId, ChatbotMode newMode)
    {
        var session = await _db.ConversationSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        if (session == null)
            throw new NotFoundException(nameof(ConversationSession), sessionId);

        session.ActiveMode = newMode;
        var state = _contextService.GetState(session);
        state.ActiveMode = newMode;
        _contextService.UpdateState(session, state);

        await _db.SaveChangesAsync();
        return newMode;
    }

    public async Task<SessionContextDto> GetSessionContextAsync(Guid sessionId)
    {
        var session = await _db.ConversationSessions
            .Include(s => s.ChildProfile)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            throw new NotFoundException(nameof(ConversationSession), sessionId);

        var child = session.ChildProfile;
        var state = _contextService.GetState(session);
        var level = _progressionService.ResolveLevel(child);

        var childName = string.IsNullOrWhiteSpace(child.LastName)
            ? child.FirstName
            : $"{child.FirstName} {child.LastName}";

        return new SessionContextDto(
            SessionId: session.Id,
            ChildId: child.Id,
            ChildName: childName,
            ActiveMode: session.ActiveMode,
            Level: level,
            CurrentTopic: session.CurrentTopic ?? "Colors",
            ConsecutiveCorrectCount: session.ConsecutiveCorrectCount,
            StarsEarned: session.StarsEarned,
            RecentTurns: state.RecentTurns
        );
    }

    private async Task<(string Reply, int Stars, string? Alert, string? Topic)> GenerateReplyAsync(
        ChildProfile child,
        ConversationSession session,
        SessionContextState state,
        string userMessage,
        ChildLearningLevel level,
        ChatbotMode mode)
    {
        switch (mode)
        {
            case ChatbotMode.Learning:
                return await HandleLearningModeAsync(child, session, state, userMessage, level);

            case ChatbotMode.Engagement:
                return HandleEngagementMode(userMessage, level);

            case ChatbotMode.Support:
                return HandleSupportMode(userMessage, level);

            case ChatbotMode.Conversation:
            default:
                return HandleConversationMode(child, state, userMessage, level);
        }
    }

    private async Task<(string Reply, int Stars, string? Alert, string? Topic)> HandleLearningModeAsync(
        ChildProfile child,
        ConversationSession session,
        SessionContextState state,
        string input,
        ChildLearningLevel level)
    {
        var cleanInput = input.Trim().ToLowerInvariant();
        var currentExpected = state.ExpectedAnswer?.Trim().ToLowerInvariant();
        string? alert = null;
        int stars = 0;

        // Check if answering pending question
        if (!string.IsNullOrEmpty(currentExpected))
        {
            bool isCorrect = cleanInput.Contains(currentExpected) ||
                             (currentExpected == "cat" && cleanInput.Contains("cat")) ||
                             (currentExpected == "yellow" && cleanInput.Contains("yellow")) ||
                             (currentExpected == "blue" && cleanInput.Contains("blue")) ||
                             (currentExpected == "red" && cleanInput.Contains("red")) ||
                             (currentExpected == "bird" && cleanInput.Contains("bird")) ||
                             (currentExpected == "dog" && cleanInput.Contains("dog")) ||
                             (currentExpected == "moo" && cleanInput.Contains("moo")) ||
                             (currentExpected == "hat" && cleanInput.Contains("hat"));

            if (isCorrect)
            {
                stars = 1;
                state.ConsecutiveCorrectCount++;
                state.TotalStars++;
                session.StarsEarned = state.TotalStars;
                session.ConsecutiveCorrectCount = state.ConsecutiveCorrectCount;

                // Persist learning progress in LearningProgress table
                await _progressService.RecordProgressAsync(child.Id, new RecordProgressRequest(
                    Topic: state.CurrentTopic,
                    ScoreDeltaOrValue: 10.0,
                    IsIncremental: true,
                    ActivitiesCompleted: 1,
                    TimeSpentMinutes: 2,
                    Notes: $"Answered {state.CurrentTopic} exercise correctly."
                ));

                // Check Level Progression
                var progressionResult = await _progressionService.CheckAndApplyProgressionAsync(
                    child, state.ConsecutiveCorrectCount);

                if (progressionResult.DidLevelUp)
                {
                    alert = progressionResult.CelebrationMessage;
                    level = progressionResult.NewLevel;
                    state.ConsecutiveCorrectCount = 0; // Reset streak upon graduating
                }

                var praise = level switch
                {
                    ChildLearningLevel.L1_Toddler => "⭐ YAY! Super job! That is right! 🎉",
                    ChildLearningLevel.L2_EarlyLearner => "🌟 Fantastic! Spot on answer! You earned a shiny star! ⭐",
                    _ => "🏆 Brilliant thinking, explorer! Spot on! You are so clever! 🌟"
                };

                // Pick next exercise
                var nextExercise = GetNextExercise(level, state.CurrentTopic);
                state.PromptQuestion = nextExercise.Question;
                state.ExpectedAnswer = nextExercise.Expected;
                state.CurrentTopic = nextExercise.Topic;

                var replyWithNext = $"{praise}\n\nNext fun question:\n{nextExercise.Question}";
                return (replyWithNext, stars, alert, nextExercise.Topic);
            }
            else
            {
                // Positive encouragement on near match or try
                state.ConsecutiveCorrectCount = 0;
                session.ConsecutiveCorrectCount = 0;

                var encouragingClue = level switch
                {
                    ChildLearningLevel.L1_Toddler =>
                        $"Good try! 🌟 Let's say it together: '{state.ExpectedAnswer}'! 😊 Can you say {state.ExpectedAnswer}?",
                    ChildLearningLevel.L2_EarlyLearner =>
                        $"Super try! 🎈 Here is a happy hint: it sounds like '{state.ExpectedAnswer}'. Let's try again: {state.PromptQuestion}",
                    _ =>
                        $"Great thinking! 🚀 You are so close. Hint: '{state.ExpectedAnswer}'. Give it another shot: {state.PromptQuestion}"
                };

                return (encouragingClue, 0, null, state.CurrentTopic);
            }
        }

        // If no pending question, initiate exercise
        var initialExercise = GetNextExercise(level, state.CurrentTopic);
        state.PromptQuestion = initialExercise.Question;
        state.ExpectedAnswer = initialExercise.Expected;
        state.CurrentTopic = initialExercise.Topic;

        var intro = level switch
        {
            ChildLearningLevel.L1_Toddler => "Let's learn and play! 🎈\n" + initialExercise.Question,
            ChildLearningLevel.L2_EarlyLearner => "Welcome to Word Quest! 🌟\n" + initialExercise.Question,
            _ => "Let's explore English together! 🚀\n" + initialExercise.Question
        };

        return (intro, 0, null, initialExercise.Topic);
    }

    private static (string Reply, int Stars, string? Alert, string? Topic) HandleConversationMode(
        ChildProfile child,
        SessionContextState state,
        string input,
        ChildLearningLevel level)
    {
        var clean = input.ToLowerInvariant();

        // Check for greetings
        if (clean.Contains("hi") || clean.Contains("hello") || clean.Contains("hey"))
        {
            var greeting = level switch
            {
                ChildLearningLevel.L1_Toddler =>
                    $"Hello {child.FirstName}! ⭐ I am your friendly buddy! Are you happy today? 😊",
                ChildLearningLevel.L2_EarlyLearner =>
                    $"Hi there {child.FirstName}! 🎈 It is so nice to chat with you today! What did you play with earlier?",
                _ =>
                    $"Hello {child.FirstName}, my awesome explorer! 🌟 I'm so happy to chat with you today! What is the most exciting thing you saw today?"
            };
            return (greeting, 0, null, "Conversation");
        }

        // Check for favorites
        if (clean.Contains("favorite") || clean.Contains("like") || clean.Contains("love"))
        {
            var reply = level switch
            {
                ChildLearningLevel.L1_Toddler =>
                    "Ooh, that is so cool! 🌟 I love cute puppies and bright stars! ⭐ What is your favorite animal?",
                ChildLearningLevel.L2_EarlyLearner =>
                    "That sounds wonderful! 🎈 I love hearing about the things you like! Do you like swimming in the pool or playing in the park?",
                _ =>
                    "That's fantastic! It is so fun having special things we enjoy. 🚀 If you could have any superpower for one day, what would you choose?"
            };
            return (reply, 0, null, "Conversation");
        }

        // Context-aware reference to previous turns
        if (state.RecentTurns.Count > 0)
        {
            var lastTurn = state.RecentTurns.Last();
            if (clean.Contains("yes") || clean.Contains("yeah"))
            {
                return ("Yay! That makes me smile so big! 🎉 What shall we talk about next: animals or space? 🐶🚀", 0, null, "Conversation");
            }
        }

        // Default friendly conversational reply
        var defaultChat = level switch
        {
            ChildLearningLevel.L1_Toddler =>
                "You are so fun! ⭐ Look at the smiling sun! ☀️ Can you say 'sunny day'?",
            ChildLearningLevel.L2_EarlyLearner =>
                "I love talking with you! 🎈 English is full of magic words. What is your favorite color in the whole world?",
            _ =>
                "You tell such interesting stories! 🌟 Tell me, what is your favorite storybook or cartoon character?"
        };

        return (defaultChat, 0, null, "Conversation");
    }

    private static (string Reply, int Stars, string? Alert, string? Topic) HandleEngagementMode(
        string input,
        ChildLearningLevel level)
    {
        var clean = input.ToLowerInvariant();

        // Check for riddle answers
        if (clean.Contains("rabbit") || clean.Contains("bunny"))
        {
            return ("🎉 BINGO! ⭐ It is a cute hopping bunny! Hop, hop, hop! 🐰 You won a game star! Want another riddle?", 1, null, "Games");
        }
        if (clean.Contains("lion"))
        {
            return ("🎉 YES! 🦁 A mighty, golden lion with a big roar! Rrrroar! ⭐ Terrific guess!", 1, null, "Games");
        }
        if (clean.Contains("star"))
        {
            return ("✨ TWINKLE TWINKLE LITTLE STAR! ⭐ You sang the rhyme perfectly! High five! ✋", 1, null, "Rhymes");
        }

        // New riddle or game prompt
        var gamePrompt = level switch
        {
            ChildLearningLevel.L1_Toddler =>
                "Animal guessing game! 🐾 I say 'Woof, woof' and wag my tail! Who am I? 🐶",
            ChildLearningLevel.L2_EarlyLearner =>
                "Riddle time! 🥕 I have long soft ears, I love crunchy carrots, and I hop around! Who am I? 🐰",
            _ =>
                "Mystery game! 🦁 I have a golden mane, sharp paws, and a thunderous roar! Who am I?"
        };

        return (gamePrompt, 0, null, "Games");
    }

    private static (string Reply, int Stars, string? Alert, string? Topic) HandleSupportMode(
        string input,
        ChildLearningLevel level)
    {
        var clean = input.ToLowerInvariant();

        if (clean.Contains("joke"))
        {
            return ("😄 Why did the teddy bear say no to dessert? Because it was already stuffed! 🧸🧁 Haha! Does that make you smile?", 0, null, "Support");
        }

        if (clean.Contains("story"))
        {
            return ("📖 Once upon a time, a tiny bluebird wanted to fly to the rainbow. Its mommy said, 'You are strong and brave!' The little bird flapped its little wings and touched the violet rainbow! 🌈 You are brave just like the little bird. 💖", 0, null, "Support");
        }

        var comfort = level switch
        {
            ChildLearningLevel.L1_Toddler =>
                "Warm hugs for you! 🧸 You are doing so well. Everything is okay. Would you like a silly joke? 😊",
            ChildLearningLevel.L2_EarlyLearner =>
                "It is okay if things feel hard or you feel tired! 💖 Take a big deep breath with me: in... and out... 🫧 You are doing wonderful. Would you like a tiny story or a funny joke?",
            _ =>
                "Remember, making mistakes is just how our brains grow stronger! 🌟 You are doing fantastic. Take a gentle breath and relax. Would you like a sweet story or an easy animal riddle?"
        };

        return (comfort, 0, null, "Support");
    }

    private static (string Question, string Expected, string Topic) GetNextExercise(
        ChildLearningLevel level,
        string currentTopic)
    {
        // Simple rotating curriculum tailored to L1, L2, L3
        return level switch
        {
            ChildLearningLevel.L1_Toddler => currentTopic switch
            {
                "Colors" => ("What color is a red strawberry? 🍓 (Say Red or Blue)", "red", "Animals"),
                "Animals" => ("What does a little cow say? 🐮 (Moo or Meow)", "moo", "Phonics"),
                _ => ("What letter does Apple start with? 🍎 (Letter A or Letter B)", "a", "Colors")
            },
            ChildLearningLevel.L2_EarlyLearner => currentTopic switch
            {
                "Colors" => ("What color is a banana: Yellow or Purple? 🍌", "yellow", "Phonics"),
                "Phonics" => ("Spell the word C-A-T! What animal is that? 🐱", "cat", "Animals"),
                _ => ("Which animal can fly high in the sky: a bird or a dog? 🐦🐕", "bird", "Colors")
            },
            _ => currentTopic switch // L3_KinderExplorer
            {
                "Phonics" => ("What word rhymes with CAT: Hat or Dog? 🎩", "hat", "Vocabulary"),
                "Vocabulary" => ("What is the opposite of Hot: Cold or Warm? ❄️", "cold", "Grammar"),
                _ => ("Fill in the blank: Birds build nests in ... (Trees or Water)? 🌳", "trees", "Phonics")
            }
        };
    }

    private static IReadOnlyList<string> GetSuggestedReplies(ChatbotMode mode, ChildLearningLevel level)
    {
        return mode switch
        {
            ChatbotMode.Learning => level switch
            {
                ChildLearningLevel.L1_Toddler => new[] { "Red 🍓", "Moo 🐮", "Yellow ☀️" },
                ChildLearningLevel.L2_EarlyLearner => new[] { "Yellow 🍌", "Cat 🐱", "Bird 🐦" },
                _ => new[] { "Hat 🎩", "Cold ❄️", "Trees 🌳" }
            },
            ChatbotMode.Engagement => new[] { "A bunny! 🐰", "A lion! 🦁", "A puppy! 🐶" },
            ChatbotMode.Support => new[] { "Tell me a joke! 😄", "Tell me a story! 📖", "I feel better! 💖" },
            _ => new[] { "I like dogs! 🐶", "My favorite is blue! 💙", "Let's play a game! 🎈" }
        };
    }

    private static ChatbotMode DetectModeIntent(string message, ChatbotMode currentMode)
    {
        var clean = message.ToLowerInvariant();
        if (clean.Contains("learn") || clean.Contains("lesson") || clean.Contains("study") || clean.Contains("quiz") || clean.Contains("abc"))
            return ChatbotMode.Learning;
        if (clean.Contains("game") || clean.Contains("play") || clean.Contains("riddle") || clean.Contains("rhyme") || clean.Contains("joke"))
            return ChatbotMode.Engagement;
        if (clean.Contains("help") || clean.Contains("sad") || clean.Contains("stuck") || clean.Contains("tired") || clean.Contains("cry") || clean.Contains("hard"))
            return ChatbotMode.Support;
        if (clean.Contains("chat") || clean.Contains("talk") || clean.Contains("conversation"))
            return ChatbotMode.Conversation;

        return currentMode;
    }

    private async Task<ConversationSession> GetOrCreateActiveSessionAsync(ChildProfile child, Guid? sessionId)
    {
        if (sessionId.HasValue)
        {
            var existing = await _db.ConversationSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId.Value && s.ChildProfileId == child.Id);
            if (existing != null)
                return existing;
        }

        // Find recent active session or create new
        var active = await _db.ConversationSessions
            .Where(s => s.ChildProfileId == child.Id && s.IsActive)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync();

        if (active != null)
            return active;

        var newSession = new ConversationSession
        {
            Id = Guid.NewGuid(),
            ChildProfileId = child.Id,
            SessionTitle = $"English Play - {DateTimeOffset.UtcNow:MMM dd, yyyy}",
            StartedAt = DateTimeOffset.UtcNow,
            IsActive = true,
            ActiveMode = ChatbotMode.Learning,
            CurrentTopic = "Colors"
        };

        _db.ConversationSessions.Add(newSession);
        await _db.SaveChangesAsync();
        return newSession;
    }

    private async Task<Guid> PersistTurnAsync(
        ConversationSession session,
        string userMessage,
        string botReply,
        long latencyMs,
        double sentiment)
    {
        var now = DateTimeOffset.UtcNow;

        var userMsg = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationSessionId = session.Id,
            SenderRole = SenderRole.Child,
            MessageText = userMessage,
            Timestamp = now.AddMilliseconds(-latencyMs),
            ResponseTimeMs = null,
            SentimentScore = sentiment
        };

        var botMsg = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationSessionId = session.Id,
            SenderRole = SenderRole.AI,
            MessageText = botReply,
            Timestamp = now,
            ResponseTimeMs = (int)latencyMs,
            SentimentScore = sentiment
        };

        _db.ConversationMessages.AddRange(userMsg, botMsg);
        session.TotalMessages += 2;
        await _db.SaveChangesAsync();

        return botMsg.Id;
    }
}
