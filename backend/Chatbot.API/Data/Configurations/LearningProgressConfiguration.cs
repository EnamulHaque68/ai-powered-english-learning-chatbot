using Chatbot.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chatbot.API.Data.Configurations;

public class LearningProgressConfiguration : IEntityTypeConfiguration<LearningProgress>
{
    public void Configure(EntityTypeBuilder<LearningProgress> builder)
    {
        builder.ToTable("LearningProgress");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Topic)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ProficiencyScore)
            .IsRequired();

        builder.Property(x => x.CompletedActivitiesCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.TotalTimeSpentMinutes)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(x => x.MasteryStatus)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        // A child has one progress tracker per topic
        builder.HasIndex(x => new { x.ChildProfileId, x.Topic })
            .IsUnique();
    }
}
