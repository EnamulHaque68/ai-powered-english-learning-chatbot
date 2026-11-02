using Chatbot.API.Entities;

namespace Chatbot.API.DTOs.LearningProgress;

public record RecordProgressRequest(
    string Topic,
    double ScoreDeltaOrValue, // If isIncremental=false, absolute proficiency (0-100)
    bool IsIncremental,       // If true, add to current score/activities/time
    int ActivitiesCompleted,
    int TimeSpentMinutes,
    string? Notes
);

public record LearningProgressResponse(
    Guid Id,
    Guid ChildProfileId,
    string Topic,
    double ProficiencyScore,
    int CompletedActivitiesCount,
    int TotalTimeSpentMinutes,
    MasteryStatus MasteryStatus,
    string MasteryLabel,
    DateTimeOffset LastActivityAt,
    string? Notes
);

public record ChildProgressSummaryResponse(
    Guid ChildProfileId,
    string ChildName,
    int TotalTopicsTracked,
    double AverageProficiency,
    int TotalActivitiesCompleted,
    int TotalTimeSpentMinutes,
    IReadOnlyList<LearningProgressResponse> Topics
);
