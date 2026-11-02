using Chatbot.API.DTOs.ChildProfile;
using Chatbot.API.DTOs.Conversation;
using Chatbot.API.DTOs.LearningProgress;
using Chatbot.API.Entities;
using Chatbot.API.Validators;
using Xunit;

namespace Chatbot.API.Tests.UnitTests;

public class ValidatorTests
{
    private readonly CreateChildProfileValidator _childValidator = new();
    private readonly RecordProgressValidator _progressValidator = new();
    private readonly AddMessageValidator _messageValidator = new();

    [Fact]
    public void CreateChildProfileValidator_ValidRequest_Passes()
    {
        var request = new CreateChildProfileRequest(
            FirstName: "Oliver",
            LastName: "Brown",
            DateOfBirth: new DateOnly(2018, 5, 10),
            CurrentLevelId: 2,
            PreferredLanguage: "en",
            Interests: "Space",
            ParentEmail: "parent@example.com"
        );

        var result = _childValidator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "First name is required.")]
    [InlineData("A", "First name must be between 2 and 50 characters.")]
    [InlineData("Oliver123", "First name contains invalid characters.")]
    public void CreateChildProfileValidator_InvalidFirstName_Fails(string name, string expectedErrorSubstr)
    {
        var request = new CreateChildProfileRequest(
            FirstName: name,
            LastName: null,
            DateOfBirth: new DateOnly(2018, 5, 10),
            CurrentLevelId: 1,
            PreferredLanguage: "en",
            Interests: null,
            ParentEmail: "parent@example.com"
        );

        var result = _childValidator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains(expectedErrorSubstr));
    }

    [Fact]
    public void CreateChildProfileValidator_FutureBirthDate_Fails()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var request = new CreateChildProfileRequest(
            FirstName: "Future",
            LastName: null,
            DateOfBirth: futureDate,
            CurrentLevelId: 1,
            PreferredLanguage: "en",
            Interests: null,
            ParentEmail: "parent@example.com"
        );

        var result = _childValidator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChildProfileRequest.DateOfBirth));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing@domain")]
    public void CreateChildProfileValidator_InvalidEmail_Fails(string email)
    {
        var request = new CreateChildProfileRequest(
            FirstName: "Lucas",
            LastName: null,
            DateOfBirth: new DateOnly(2019, 1, 1),
            CurrentLevelId: 1,
            PreferredLanguage: "en",
            Interests: null,
            ParentEmail: email
        );

        var result = _childValidator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateChildProfileRequest.ParentEmail));
    }

    [Fact]
    public void RecordProgressValidator_AbsoluteScoreOver100_Fails()
    {
        var request = new RecordProgressRequest("Math", 105.0, false, 1, 10, null);
        var result = _progressValidator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RecordProgressRequest.ScoreDeltaOrValue));
    }

    [Fact]
    public void RecordProgressValidator_ValidRequest_Passes()
    {
        var request = new RecordProgressRequest("Phonics", 85.5, false, 3, 20, "Great focus");
        var result = _progressValidator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void AddMessageValidator_EmptyMessage_Fails()
    {
        var request = new AddMessageRequest(SenderRole.Child, "", null, null, null, null);
        var result = _messageValidator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddMessageRequest.MessageText));
    }

    [Fact]
    public void AddMessageValidator_InvalidSentimentScore_Fails()
    {
        var request = new AddMessageRequest(SenderRole.Child, "Hello!", null, null, null, 2.5); // Outside [-1.0, 1.0]
        var result = _messageValidator.Validate(request);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddMessageRequest.SentimentScore));
    }
}
