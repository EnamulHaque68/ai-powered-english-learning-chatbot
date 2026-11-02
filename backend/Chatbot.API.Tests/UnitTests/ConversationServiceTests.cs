using Chatbot.API.DTOs.ChildProfile;
using Chatbot.API.DTOs.Conversation;
using Chatbot.API.Entities;
using Chatbot.API.Exceptions;
using Chatbot.API.Services;
using Chatbot.API.Tests.Helpers;
using Xunit;

namespace Chatbot.API.Tests.UnitTests;

public class ConversationServiceTests
{
    [Fact]
    public async Task StartSession_And_AddMessages_TracksConversationAccurately()
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();
        var profileService = new ChildProfileService(db);
        var convService = new ConversationService(db);

        var child = await profileService.CreateAsync(new CreateChildProfileRequest(
            "Mia", null, new DateOnly(2019, 8, 20), 1, "en", null, "mia@test.com"));

        // Act - Start Session
        var session = await convService.StartSessionAsync(new StartSessionRequest(
            child.Id, "Dinosaur Questions", "Curious"));

        Assert.True(session.IsActive);
        Assert.Equal("Dinosaur Questions", session.SessionTitle);
        Assert.Equal(0, session.TotalMessages);

        // Act - Child message
        var msg1 = await convService.AddMessageAsync(session.Id, new AddMessageRequest(
            SenderRole.Child, "Why was T-Rex so big?", null, null, null, 0.6));

        // Act - AI message
        var msg2 = await convService.AddMessageAsync(session.Id, new AddMessageRequest(
            SenderRole.AI, "T-Rex needed to be big to hunt large herbivorous dinosaurs!", null, 42, 120, 0.8));

        // Act - End session
        var endedSession = await convService.EndSessionAsync(session.Id, new EndSessionRequest(
            "Happy", "Learned about dinosaur sizes and diets."));

        // Act - Get detail
        var detail = await convService.GetSessionDetailAsync(session.Id);

        // Assert
        Assert.False(endedSession.IsActive);
        Assert.NotNull(endedSession.EndedAt);
        Assert.Equal("Happy", endedSession.ChildMood);
        Assert.Equal(2, detail.TotalMessages);
        Assert.Equal(2, detail.Messages.Count);
        Assert.Equal("Child", detail.Messages[0].SenderRoleName);
        Assert.Equal("AI", detail.Messages[1].SenderRoleName);
    }

    [Fact]
    public async Task AddMessageAsync_NonExistentSession_ThrowsNotFoundException()
    {
        // Arrange
        using var db = TestDbContextFactory.CreateInMemoryDbContext();
        var convService = new ConversationService(db);

        var request = new AddMessageRequest(SenderRole.Child, "Hello?", null, null, null, null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => convService.AddMessageAsync(Guid.NewGuid(), request));
    }
}
