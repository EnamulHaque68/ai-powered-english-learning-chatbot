using Chatbot.API.DTOs.Conversation;
using Chatbot.API.DTOs.LearningProgress;
using FluentValidation;

namespace Chatbot.API.Validators;

public class RecordProgressValidator : AbstractValidator<RecordProgressRequest>
{
    public RecordProgressValidator()
    {
        RuleFor(x => x.Topic)
            .NotEmpty().WithMessage("Topic name is required.")
            .MaximumLength(100).WithMessage("Topic name cannot exceed 100 characters.");

        RuleFor(x => x.ScoreDeltaOrValue)
            .GreaterThanOrEqualTo(0).WithMessage("Proficiency score or delta cannot be negative.")
            .Must((req, val) => req.IsIncremental || val <= 100.0)
            .WithMessage("Absolute proficiency score must not exceed 100.");

        RuleFor(x => x.ActivitiesCompleted)
            .GreaterThanOrEqualTo(0).WithMessage("Activities completed count cannot be negative.");

        RuleFor(x => x.TimeSpentMinutes)
            .GreaterThanOrEqualTo(0).WithMessage("Time spent minutes cannot be negative.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters.");
    }
}

public class StartSessionValidator : AbstractValidator<StartSessionRequest>
{
    public StartSessionValidator()
    {
        RuleFor(x => x.ChildProfileId)
            .NotEmpty().WithMessage("ChildProfileId is required.");

        RuleFor(x => x.SessionTitle)
            .MaximumLength(150).WithMessage("Session title cannot exceed 150 characters.");

        RuleFor(x => x.InitialMood)
            .MaximumLength(50).WithMessage("Initial mood cannot exceed 50 characters.");
    }
}

public class AddMessageValidator : AbstractValidator<AddMessageRequest>
{
    public AddMessageValidator()
    {
        RuleFor(x => x.SenderRole)
            .IsInEnum().WithMessage("A valid sender role must be specified.");

        RuleFor(x => x.MessageText)
            .NotEmpty().WithMessage("Message text is required.")
            .MaximumLength(4000).WithMessage("Message cannot exceed 4000 characters.");

        RuleFor(x => x.MediaUrl)
            .MaximumLength(500).WithMessage("Media URL cannot exceed 500 characters.");

        RuleFor(x => x.TokensCount)
            .GreaterThanOrEqualTo(0).WithMessage("Tokens count cannot be negative.")
            .When(x => x.TokensCount.HasValue);

        RuleFor(x => x.ResponseTimeMs)
            .GreaterThanOrEqualTo(0).WithMessage("Response time cannot be negative.")
            .When(x => x.ResponseTimeMs.HasValue);

        RuleFor(x => x.SentimentScore)
            .InclusiveBetween(-1.0, 1.0).WithMessage("Sentiment score must be between -1.0 and 1.0.")
            .When(x => x.SentimentScore.HasValue);
    }
}
