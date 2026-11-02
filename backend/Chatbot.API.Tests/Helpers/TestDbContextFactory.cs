using Chatbot.API.Data;
using Chatbot.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.API.Tests.Helpers;

public static class TestDbContextFactory
{
    public static ChildLearningDbContext CreateInMemoryDbContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ChildLearningDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new ChildLearningDbContext(options);

        // Ensure database is created and seed levels are present
        context.Database.EnsureCreated();

        if (!context.LearningLevels.Any())
        {
            context.LearningLevels.AddRange(
                new LearningLevel
                {
                    Id = 1,
                    Code = "LEVEL_1_TODDLER",
                    Title = "Toddler Explorer",
                    Description = "Sensory play and phonics",
                    MinAge = 3,
                    MaxAge = 5,
                    SortOrder = 1,
                    IsActive = true
                },
                new LearningLevel
                {
                    Id = 2,
                    Code = "LEVEL_2_EARLY_READER",
                    Title = "Early Reader",
                    Description = "Sight words and counting",
                    MinAge = 6,
                    MaxAge = 7,
                    SortOrder = 2,
                    IsActive = true
                }
            );
            context.SaveChanges();
        }

        return context;
    }
}
