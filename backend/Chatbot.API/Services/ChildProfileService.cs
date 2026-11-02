using Chatbot.API.Data;
using Chatbot.API.DTOs.ChildProfile;
using Chatbot.API.DTOs.Common;
using Chatbot.API.DTOs.Conversation;
using Chatbot.API.DTOs.LearningProgress;
using Chatbot.API.Entities;
using Chatbot.API.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.API.Services;

public interface IChildProfileService
{
    Task<PagedResult<ChildProfileResponse>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<ChildProfileDetailResponse> GetByIdAsync(Guid id);
    Task<ChildProfileResponse> CreateAsync(CreateChildProfileRequest request);
    Task<ChildProfileResponse> UpdateAsync(Guid id, UpdateChildProfileRequest request);
    Task<bool> DeleteAsync(Guid id);
}

public class ChildProfileService : IChildProfileService
{
    private readonly ChildLearningDbContext _db;

    public ChildProfileService(ChildLearningDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ChildProfileResponse>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.ChildProfiles
            .Include(c => c.LearningLevel)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(c => c.FirstName.ToLower().Contains(cleanSearch) ||
                                     (c.LastName != null && c.LastName.ToLower().Contains(cleanSearch)) ||
                                     c.ParentEmail.ToLower().Contains(cleanSearch));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => ToResponse(c))
            .ToListAsync();

        return new PagedResult<ChildProfileResponse>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<ChildProfileDetailResponse> GetByIdAsync(Guid id)
    {
        var child = await _db.ChildProfiles
            .Include(c => c.LearningLevel)
            .Include(c => c.ProgressRecords)
            .Include(c => c.ConversationSessions.OrderByDescending(s => s.StartedAt).Take(5))
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (child == null)
            throw new NotFoundException(nameof(ChildProfile), id);

        var fullName = string.IsNullOrWhiteSpace(child.LastName)
            ? child.FirstName
            : $"{child.FirstName} {child.LastName}";

        var progressList = child.ProgressRecords
            .OrderByDescending(p => p.LastActivityAt)
            .Select(p => new LearningProgressResponse(
                p.Id,
                p.ChildProfileId,
                p.Topic,
                p.ProficiencyScore,
                p.CompletedActivitiesCount,
                p.TotalTimeSpentMinutes,
                p.MasteryStatus,
                p.MasteryStatus.ToString(),
                p.LastActivityAt,
                p.Notes
            ))
            .ToList();

        var recentSessions = child.ConversationSessions
            .Select(s => new ConversationSessionResponse(
                s.Id,
                s.ChildProfileId,
                s.SessionTitle,
                s.StartedAt,
                s.EndedAt,
                s.TotalMessages,
                s.ChildMood,
                s.Summary,
                s.IsActive
            ))
            .ToList();

        return new ChildProfileDetailResponse(
            child.Id,
            child.FirstName,
            child.LastName,
            fullName,
            child.DateOfBirth,
            child.CalculateAge(),
            child.CurrentLevelId,
            child.LearningLevel?.Title ?? "Unknown Level",
            child.PreferredLanguage,
            child.Interests,
            child.ParentEmail,
            child.CreatedAt,
            child.UpdatedAt,
            progressList,
            recentSessions
        );
    }

    public async Task<ChildProfileResponse> CreateAsync(CreateChildProfileRequest request)
    {
        var levelExists = await _db.LearningLevels.AnyAsync(l => l.Id == request.CurrentLevelId && l.IsActive);
        if (!levelExists)
            throw new NotFoundException("LearningLevel", request.CurrentLevelId);

        var child = new ChildProfile
        {
            Id = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName?.Trim(),
            DateOfBirth = request.DateOfBirth,
            CurrentLevelId = request.CurrentLevelId,
            PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage) ? "en" : request.PreferredLanguage.Trim(),
            Interests = request.Interests?.Trim(),
            ParentEmail = request.ParentEmail.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.ChildProfiles.Add(child);
        await _db.SaveChangesAsync();

        // Load level for response
        await _db.Entry(child).Reference(c => c.LearningLevel).LoadAsync();

        return ToResponse(child);
    }

    public async Task<ChildProfileResponse> UpdateAsync(Guid id, UpdateChildProfileRequest request)
    {
        var child = await _db.ChildProfiles
            .Include(c => c.LearningLevel)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (child == null)
            throw new NotFoundException(nameof(ChildProfile), id);

        if (child.CurrentLevelId != request.CurrentLevelId)
        {
            var levelExists = await _db.LearningLevels.AnyAsync(l => l.Id == request.CurrentLevelId && l.IsActive);
            if (!levelExists)
                throw new NotFoundException("LearningLevel", request.CurrentLevelId);
            child.CurrentLevelId = request.CurrentLevelId;
        }

        child.FirstName = request.FirstName.Trim();
        child.LastName = request.LastName?.Trim();
        child.DateOfBirth = request.DateOfBirth;
        child.PreferredLanguage = string.IsNullOrWhiteSpace(request.PreferredLanguage) ? "en" : request.PreferredLanguage.Trim();
        child.Interests = request.Interests?.Trim();
        child.ParentEmail = request.ParentEmail.Trim();
        child.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync();
        await _db.Entry(child).Reference(c => c.LearningLevel).LoadAsync();

        return ToResponse(child);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var child = await _db.ChildProfiles.FirstOrDefaultAsync(c => c.Id == id);
        if (child == null)
            throw new NotFoundException(nameof(ChildProfile), id);

        _db.ChildProfiles.Remove(child);
        await _db.SaveChangesAsync();
        return true;
    }

    private static ChildProfileResponse ToResponse(ChildProfile c)
    {
        var fullName = string.IsNullOrWhiteSpace(c.LastName)
            ? c.FirstName
            : $"{c.FirstName} {c.LastName}";

        return new ChildProfileResponse(
            c.Id,
            c.FirstName,
            c.LastName,
            fullName,
            c.DateOfBirth,
            c.CalculateAge(),
            c.CurrentLevelId,
            c.LearningLevel?.Title ?? "Unknown Level",
            c.PreferredLanguage,
            c.Interests,
            c.ParentEmail,
            c.CreatedAt,
            c.UpdatedAt
        );
    }
}
