namespace Chatbot.API.Entities;

public class LearningProgress
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildProfileId { get; set; }
    public string Topic { get; set; } = string.Empty; // e.g. "Mathematics", "Phonics", "Science", "Vocabulary"
    public double ProficiencyScore { get; set; } // 0.0 - 100.0
    public int CompletedActivitiesCount { get; set; }
    public int TotalTimeSpentMinutes { get; set; }
    public MasteryStatus MasteryStatus { get; set; } = MasteryStatus.Novice;
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation property
    public ChildProfile ChildProfile { get; set; } = null!;
}
