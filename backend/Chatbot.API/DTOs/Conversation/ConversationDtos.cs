using Chatbot.API.Entities;

namespace Chatbot.API.DTOs.Conversation;

public record StartSessionRequest(
    Guid ChildProfileId,
    string? SessionTitle,
    string? InitialMood,
    ChatbotMode? InitialMode = ChatbotMode.Learning
);

public record EndSessionRequest(
    string? ChildMood,
    string? Summary
);

public record AddMessageRequest(
    SenderRole SenderRole,
    string MessageText,
    string? MediaUrl,
    int? TokensCount,
    int? ResponseTimeMs,
    double? SentimentScore
);

public record ConversationMessageResponse(
    Guid Id,
    Guid ConversationSessionId,
    SenderRole SenderRole,
    string SenderRoleName,
    string MessageText,
    DateTimeOffset Timestamp,
    string? MediaUrl,
    int? TokensCount,
    int? ResponseTimeMs,
    double? SentimentScore
);

public record ConversationSessionResponse(
    Guid Id,
    Guid ChildProfileId,
    string SessionTitle,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int TotalMessages,
    string? ChildMood,
    string? Summary,
    bool IsActive,
    ChatbotMode ActiveMode = ChatbotMode.Learning,
    string? CurrentTopic = "Colors",
    int ConsecutiveCorrectCount = 0,
    int StarsEarned = 0
);

public record SessionDetailResponse(
    Guid Id,
    Guid ChildProfileId,
    string ChildName,
    string SessionTitle,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int TotalMessages,
    string? ChildMood,
    string? Summary,
    bool IsActive,
    ChatbotMode ActiveMode,
    string? CurrentTopic,
    int ConsecutiveCorrectCount,
    int StarsEarned,
    IReadOnlyList<ConversationMessageResponse> Messages
);

public record ChatbotMessageRequest(
    Guid ChildId,
    Guid? SessionId,
    string Message,
    ChatbotMode? ModeOverride,
    ChildLearningLevel? LevelOverride
);

public record ChatbotReplyResponse(
    Guid SessionId,
    Guid MessageId,
    string ReplyText,
    ChatbotMode ActiveMode,
    string ActiveModeName,
    ChildLearningLevel Level,
    string LevelName,
    int StarsAwarded,
    int TotalSessionStars,
    IReadOnlyList<string> SuggestedReplies,
    string? LevelProgressionAlert,
    long ResponseTimeMs,
    string? ContextTopic,
    double SentimentScore
);

public record SwitchModeRequest(
    Guid SessionId,
    ChatbotMode NewMode
);

public record RecentTurnDto(
    string UserMessage,
    string BotReply,
    DateTimeOffset Timestamp,
    string? PendingQuestionAnswer = null
);

public record SessionContextDto(
    Guid SessionId,
    Guid ChildId,
    string ChildName,
    ChatbotMode ActiveMode,
    ChildLearningLevel Level,
    string CurrentTopic,
    int ConsecutiveCorrectCount,
    int StarsEarned,
    IReadOnlyList<RecentTurnDto> RecentTurns
);
