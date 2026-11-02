using Chatbot.API.Data;
using Chatbot.API.DTOs.LearningLevel;
using Chatbot.API.Entities;
using Chatbot.API.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.API.Services;

public interface ILearningLevelService
{
    Task<IReadOnlyList<LearningLevelResponse>> GetAllLevelsAsync();
    Task<LearningLevelResponse> GetLevelByIdAsync(int id);
}

public class LearningLevelService : ILearningLevelService
{
    private readonly ChildLearningDbContext _db;

    public LearningLevelService(ChildLearningDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LearningLevelResponse>> GetAllLevelsAsync()
    {
        var levels = await _db.LearningLevels
            .Where(l => l.IsActive)
            .OrderBy(l => l.SortOrder)
            .AsNoTracking()
            .ToListAsync();

        return levels.Select(l => new LearningLevelResponse(
            l.Id,
            l.Code,
            l.Title,
            l.Description,
            l.MinAge,
            l.MaxAge,
            l.SortOrder,
            l.IsActive
        )).ToList();
    }

    public async Task<LearningLevelResponse> GetLevelByIdAsync(int id)
    {
        var level = await _db.LearningLevels
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id);

        if (level == null)
            throw new NotFoundException(nameof(LearningLevel), id);

        return new LearningLevelResponse(
            level.Id,
            level.Code,
            level.Title,
            level.Description,
            level.MinAge,
            level.MaxAge,
            level.SortOrder,
            level.IsActive
        );
    }
}
