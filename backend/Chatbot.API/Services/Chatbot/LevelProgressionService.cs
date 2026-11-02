using Chatbot.API.Data;
using Chatbot.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.API.Services.Chatbot;

public record LevelProgressionResult(
    bool DidLevelUp,
    ChildLearningLevel PreviousLevel,
    ChildLearningLevel NewLevel,
    string? CelebrationMessage
);

public interface ILevelProgressionService
{
    Task<LevelProgressionResult> CheckAndApplyProgressionAsync(ChildProfile child, int consecutiveCorrectCount);
    ChildLearningLevel ResolveLevel(ChildProfile child, ChildLearningLevel? overrideLevel = null);
}

public class LevelProgressionService : ILevelProgressionService
{
    private readonly ChildLearningDbContext _db;
    public const int CorrectStreakRequiredForLevelUp = 3;

    public LevelProgressionService(ChildLearningDbContext db)
    {
        _db = db;
    }

    public ChildLearningLevel ResolveLevel(ChildProfile child, ChildLearningLevel? overrideLevel = null)
    {
        if (overrideLevel.HasValue)
            return overrideLevel.Value;

        return child.CurrentLevelId switch
        {
            1 => ChildLearningLevel.L1_Toddler,
            2 => ChildLearningLevel.L2_EarlyLearner,
            3 => ChildLearningLevel.L3_KinderExplorer,
            _ => (child.CalculateAge() <= 4) ? ChildLearningLevel.L1_Toddler :
                 (child.CalculateAge() == 5) ? ChildLearningLevel.L2_EarlyLearner :
                 ChildLearningLevel.L3_KinderExplorer
        };
    }

    public async Task<LevelProgressionResult> CheckAndApplyProgressionAsync(ChildProfile child, int consecutiveCorrectCount)
    {
        var currentLevel = ResolveLevel(child);

        if (consecutiveCorrectCount < CorrectStreakRequiredForLevelUp)
        {
            return new LevelProgressionResult(false, currentLevel, currentLevel, null);
        }

        ChildLearningLevel nextLevel;
        string celebration;

        switch (currentLevel)
        {
            case ChildLearningLevel.L1_Toddler:
                nextLevel = ChildLearningLevel.L2_EarlyLearner;
                celebration = "🎉 LEVEL UP! You earned 3 stars in a row and unlocked Level 2 (Early Learner)! You are a superstar! 🌟🚀";
                break;

            case ChildLearningLevel.L2_EarlyLearner:
                nextLevel = ChildLearningLevel.L3_KinderExplorer;
                celebration = "🏆 SUPERSTAR LEVEL UP! You unlocked Level 3 (Kinder Explorer)! You are reading and talking like a champion! 🌟🎈";
                break;

            default:
                // Already at highest level for under-7
                return new LevelProgressionResult(false, currentLevel, currentLevel, "🌟 MASTER EXPLORER! You completed a 3-star streak in Level 3! Amazing job! 🏆");
        }

        // Persist level update to child profile
        child.CurrentLevelId = (int)nextLevel;
        child.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();

        return new LevelProgressionResult(true, currentLevel, nextLevel, celebration);
    }
}
