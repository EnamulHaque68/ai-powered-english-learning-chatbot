namespace Chatbot.API.Entities;

public class ChildProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public string PreferredLanguage { get; set; } = "en";
    public string? Interests { get; set; }
    public string ParentEmail { get; set; } = string.Empty;
    public int CurrentLevelId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public LearningLevel LearningLevel { get; set; } = null!;
    public ICollection<LearningProgress> ProgressRecords { get; set; } = new List<LearningProgress>();
    public ICollection<ConversationSession> ConversationSessions { get; set; } = new List<ConversationSession>();

    // Helper computation
    public int CalculateAge()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var age = today.Year - DateOfBirth.Year;
        if (DateOfBirth > today.AddYears(-age)) age--;
        return age;
    }
}
