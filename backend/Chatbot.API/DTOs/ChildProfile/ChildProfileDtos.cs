namespace Chatbot.API.DTOs.ChildProfile;

public record CreateChildProfileRequest(
    string FirstName,
    string? LastName,
    DateOnly DateOfBirth,
    int CurrentLevelId,
    string? PreferredLanguage,
    string? Interests,
    string ParentEmail
);

public record UpdateChildProfileRequest(
    string FirstName,
    string? LastName,
    DateOnly DateOfBirth,
    int CurrentLevelId,
    string? PreferredLanguage,
    string? Interests,
    string ParentEmail
);

public record ChildProfileResponse(
    Guid Id,
    string FirstName,
    string? LastName,
    string FullName,
    DateOnly DateOfBirth,
    int Age,
    int CurrentLevelId,
    string LevelTitle,
    string PreferredLanguage,
    string? Interests,
    string ParentEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);

public record ChildProfileDetailResponse(
    Guid Id,
    string FirstName,
    string? LastName,
    string FullName,
    DateOnly DateOfBirth,
    int Age,
    int CurrentLevelId,
    string LevelTitle,
    string PreferredLanguage,
    string? Interests,
    string ParentEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<LearningProgress.LearningProgressResponse> Progress,
    IReadOnlyList<Conversation.ConversationSessionResponse> RecentSessions
);
