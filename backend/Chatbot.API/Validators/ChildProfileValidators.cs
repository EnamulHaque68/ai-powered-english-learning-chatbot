using Chatbot.API.DTOs.ChildProfile;
using FluentValidation;

namespace Chatbot.API.Validators;

public class CreateChildProfileValidator : AbstractValidator<CreateChildProfileRequest>
{
    public CreateChildProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .Length(2, 50).WithMessage("First name must be between 2 and 50 characters.")
            .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("First name contains invalid characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.")
            .Matches(@"^[a-zA-Z\s\-']*$").WithMessage("Last name contains invalid characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.LastName));

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .Must(dob => dob <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.")
            .Must(dob =>
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var age = today.Year - dob.Year;
                if (dob > today.AddYears(-age)) age--;
                return age is >= 1 and <= 18;
            })
            .WithMessage("Child age must be between 1 and 18 years old.");

        RuleFor(x => x.CurrentLevelId)
            .GreaterThan(0).WithMessage("A valid learning level must be selected.");

        RuleFor(x => x.ParentEmail)
            .NotEmpty().WithMessage("Parent email address is required.")
            .EmailAddress().WithMessage("Please provide a valid parent email address.")
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").WithMessage("Please provide a valid parent email address with a valid domain.")
            .MaximumLength(150).WithMessage("Parent email cannot exceed 150 characters.");

        RuleFor(x => x.PreferredLanguage)
            .MaximumLength(20).WithMessage("Preferred language code cannot exceed 20 characters.");

        RuleFor(x => x.Interests)
            .MaximumLength(500).WithMessage("Interests cannot exceed 500 characters.");
    }
}

public class UpdateChildProfileValidator : AbstractValidator<UpdateChildProfileRequest>
{
    public UpdateChildProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .Length(2, 50).WithMessage("First name must be between 2 and 50 characters.")
            .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("First name contains invalid characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(50).WithMessage("Last name cannot exceed 50 characters.")
            .Matches(@"^[a-zA-Z\s\-']*$").WithMessage("Last name contains invalid characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.LastName));

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .Must(dob => dob <= DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("Date of birth cannot be in the future.")
            .Must(dob =>
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);
                var age = today.Year - dob.Year;
                if (dob > today.AddYears(-age)) age--;
                return age is >= 1 and <= 18;
            })
            .WithMessage("Child age must be between 1 and 18 years old.");

        RuleFor(x => x.CurrentLevelId)
            .GreaterThan(0).WithMessage("A valid learning level must be selected.");

        RuleFor(x => x.ParentEmail)
            .NotEmpty().WithMessage("Parent email address is required.")
            .EmailAddress().WithMessage("Please provide a valid parent email address.")
            .Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").WithMessage("Please provide a valid parent email address with a valid domain.")
            .MaximumLength(150).WithMessage("Parent email cannot exceed 150 characters.");

        RuleFor(x => x.PreferredLanguage)
            .MaximumLength(20).WithMessage("Preferred language code cannot exceed 20 characters.");

        RuleFor(x => x.Interests)
            .MaximumLength(500).WithMessage("Interests cannot exceed 500 characters.");
    }
}
