namespace Chatbot.API.Entities;

public class ConversationSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChildProfileId { get; set; }
    public string SessionTitle { get; set; } = "Learning Session";
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EndedAt { get; set; }
    public int TotalMessages { get; set; }
    public string? ChildMood { get; set; } // e.g. "Curious", "Happy", "Energetic", "Focused"
    public string? Summary { get; set; }
    public bool IsActive { get; set; } = true;

    // Chatbot MVP & Context state
    public ChatbotMode ActiveMode { get; set; } = ChatbotMode.Learning;
    public string? CurrentTopic { get; set; } = "Colors";
    public int ConsecutiveCorrectCount { get; set; } = 0;
    public int StarsEarned { get; set; } = 0;
    public string? RecentContextJson { get; set; }

    // Navigation properties
    public ChildProfile ChildProfile { get; set; } = null!;
    public ICollection<ConversationMessage> Messages { get; set; } = new List<ConversationMessage>();
}
