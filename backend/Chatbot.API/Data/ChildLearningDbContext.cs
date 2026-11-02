using Chatbot.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace Chatbot.API.Data;

public class ChildLearningDbContext : DbContext
{
    public ChildLearningDbContext(DbContextOptions<ChildLearningDbContext> options)
        : base(options)
    {
    }

    public DbSet<LearningLevel> LearningLevels => Set<LearningLevel>();
    public DbSet<ChildProfile> ChildProfiles => Set<ChildProfile>();
    public DbSet<LearningProgress> LearningProgress => Set<LearningProgress>();
    public DbSet<ConversationSession> ConversationSessions => Set<ConversationSession>();
    public DbSet<ConversationMessage> ConversationMessages => Set<ConversationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ChildLearningDbContext).Assembly);
    }
}
