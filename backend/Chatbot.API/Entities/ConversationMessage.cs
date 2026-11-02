namespace Chatbot.API.Entities;

public class ConversationMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ConversationSessionId { get; set; }
    public SenderRole SenderRole { get; set; } = SenderRole.Child;
    public string MessageText { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? MediaUrl { get; set; }
    public int? TokensCount { get; set; }
    public int? ResponseTimeMs { get; set; }
    public double? SentimentScore { get; set; } // -1.0 to 1.0

    // Navigation property
    public ConversationSession Session { get; set; } = null!;
}
