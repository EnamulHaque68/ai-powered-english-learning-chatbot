using Chatbot.API.Data;
using Chatbot.API.DTOs.LearningProgress;
using Chatbot.API.Entities;
using Chatbot.API.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.API.Services;

public interface ILearningProgressService
{
    Task<IReadOnlyList<LearningProgressResponse>> GetChildProgressAsync(Guid childId);
    Task<ChildProgressSummaryResponse> GetSummaryAsync(Guid childId);
    Task<LearningProgressResponse> RecordProgressAsync(Guid childId, RecordProgressRequest request);
}

public class LearningProgressService : ILearningProgressService
{
    private readonly ChildLearningDbContext _db;

    public LearningProgressService(ChildLearningDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LearningProgressResponse>> GetChildProgressAsync(Guid childId)
    {
        var childExists = await _db.ChildProfiles.AnyAsync(c => c.Id == childId);
        if (!childExists)
            throw new NotFoundException(nameof(ChildProfile), childId);

        var list = await _db.LearningProgress
            .Where(p => p.ChildProfileId == childId)
            .OrderByDescending(p => p.LastActivityAt)
            .AsNoTracking()
            .ToListAsync();

        return list.Select(ToResponse).ToList();
    }

    public async Task<ChildProgressSummaryResponse> GetSummaryAsync(Guid childId)
    {
        var child = await _db.ChildProfiles
            .Include(c => c.ProgressRecords)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == childId);

        if (child == null)
            throw new NotFoundException(nameof(ChildProfile), childId);

        var fullName = string.IsNullOrWhiteSpace(child.LastName)
            ? child.FirstName
            : $"{child.FirstName} {child.LastName}";

        var records = child.ProgressRecords.ToList();
        var totalTopics = records.Count;
        var avgScore = totalTopics > 0 ? Math.Round(records.Average(r => r.ProficiencyScore), 1) : 0.0;
        var totalActivities = records.Sum(r => r.CompletedActivitiesCount);
        var totalMinutes = records.Sum(r => r.TotalTimeSpentMinutes);

        return new ChildProgressSummaryResponse(
            child.Id,
            fullName,
            totalTopics,
            avgScore,
            totalActivities,
            totalMinutes,
            records.OrderByDescending(r => r.LastActivityAt).Select(ToResponse).ToList()
        );
    }

    public async Task<LearningProgressResponse> RecordProgressAsync(Guid childId, RecordProgressRequest request)
    {
        var child = await _db.ChildProfiles.FirstOrDefaultAsync(c => c.Id == childId);
        if (child == null)
            throw new NotFoundException(nameof(ChildProfile), childId);

        var normalizedTopic = request.Topic.Trim();

        var progress = await _db.LearningProgress
            .FirstOrDefaultAsync(p => p.ChildProfileId == childId && p.Topic.ToLower() == normalizedTopic.ToLower());

        var now = DateTimeOffset.UtcNow;

        if (progress == null)
        {
            var initialScore = request.IsIncremental
                ? Math.Clamp(request.ScoreDeltaOrValue, 0.0, 100.0)
                : Math.Clamp(request.ScoreDeltaOrValue, 0.0, 100.0);

            progress = new LearningProgress
            {
                Id = Guid.NewGuid(),
                ChildProfileId = childId,
                Topic = normalizedTopic,
                ProficiencyScore = initialScore,
                CompletedActivitiesCount = Math.Max(0, request.ActivitiesCompleted),
                TotalTimeSpentMinutes = Math.Max(0, request.TimeSpentMinutes),
                MasteryStatus = CalculateMastery(initialScore),
                LastActivityAt = now,
                Notes = request.Notes?.Trim(),
                CreatedAt = now
            };

            _db.LearningProgress.Add(progress);
        }
        else
        {
            var newScore = request.IsIncremental
                ? Math.Clamp(progress.ProficiencyScore + request.ScoreDeltaOrValue, 0.0, 100.0)
                : Math.Clamp(request.ScoreDeltaOrValue, 0.0, 100.0);

            progress.ProficiencyScore = newScore;
            progress.CompletedActivitiesCount += Math.Max(0, request.ActivitiesCompleted);
            progress.TotalTimeSpentMinutes += Math.Max(0, request.TimeSpentMinutes);
            progress.MasteryStatus = CalculateMastery(newScore);
            progress.LastActivityAt = now;
            progress.UpdatedAt = now;

            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                progress.Notes = request.Notes.Trim();
            }
        }

        await _db.SaveChangesAsync();

        return ToResponse(progress);
    }

    private static MasteryStatus CalculateMastery(double score) => score switch
    {
        < 40.0 => MasteryStatus.Novice,
        < 70.0 => MasteryStatus.Developing,
        < 90.0 => MasteryStatus.Proficient,
        _ => MasteryStatus.Mastered
    };

    private static LearningProgressResponse ToResponse(LearningProgress p) =>
        new(
            p.Id,
            p.ChildProfileId,
            p.Topic,
            Math.Round(p.ProficiencyScore, 1),
            p.CompletedActivitiesCount,
            p.TotalTimeSpentMinutes,
            p.MasteryStatus,
            p.MasteryStatus.ToString(),
            p.LastActivityAt,
            p.Notes
        );
}
