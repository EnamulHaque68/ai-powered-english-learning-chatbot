namespace Chatbot.API.DTOs.LearningLevel;

public record LearningLevelResponse(
    int Id,
    string Code,
    string Title,
    string Description,
    int MinAge,
    int MaxAge,
    int SortOrder,
    bool IsActive
);
