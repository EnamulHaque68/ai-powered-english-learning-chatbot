using Chatbot.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chatbot.API.Data.Configurations;

public class ConversationSessionConfiguration : IEntityTypeConfiguration<ConversationSession>
{
    public void Configure(EntityTypeBuilder<ConversationSession> builder)
    {
        builder.ToTable("ConversationSessions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SessionTitle)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.StartedAt)
            .IsRequired();

        builder.Property(x => x.ChildMood)
            .HasMaxLength(50);

        builder.Property(x => x.Summary)
            .HasMaxLength(2000);

        builder.Property(x => x.ActiveMode)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(ChatbotMode.Learning);

        builder.Property(x => x.CurrentTopic)
            .HasMaxLength(100)
            .HasDefaultValue("Colors");

        builder.Property(x => x.ConsecutiveCorrectCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.StarsEarned)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.RecentContextJson)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.ChildProfileId);
        builder.HasIndex(x => x.StartedAt);

        builder.HasMany(x => x.Messages)
            .WithOne(m => m.Session)
            .HasForeignKey(m => m.ConversationSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
