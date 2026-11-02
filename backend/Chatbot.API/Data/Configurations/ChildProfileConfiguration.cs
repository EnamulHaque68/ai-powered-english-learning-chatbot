using Chatbot.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chatbot.API.Data.Configurations;

public class ChildProfileConfiguration : IEntityTypeConfiguration<ChildProfile>
{
    public void Configure(EntityTypeBuilder<ChildProfile> builder)
    {
        builder.ToTable("ChildProfiles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.LastName)
            .HasMaxLength(50);

        builder.Property(x => x.DateOfBirth)
            .IsRequired();

        builder.Property(x => x.PreferredLanguage)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("en");

        builder.Property(x => x.Interests)
            .HasMaxLength(500);

        builder.Property(x => x.ParentEmail)
            .IsRequired()
            .HasMaxLength(150);

        builder.HasIndex(x => x.ParentEmail);

        // Relationships
        builder.HasOne(x => x.LearningLevel)
            .WithMany(l => l.ChildProfiles)
            .HasForeignKey(x => x.CurrentLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.ProgressRecords)
            .WithOne(p => p.ChildProfile)
            .HasForeignKey(p => p.ChildProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.ConversationSessions)
            .WithOne(c => c.ChildProfile)
            .HasForeignKey(c => c.ChildProfileId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
