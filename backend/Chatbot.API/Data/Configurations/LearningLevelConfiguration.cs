using Chatbot.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chatbot.API.Data.Configurations;

public class LearningLevelConfiguration : IEntityTypeConfiguration<LearningLevel>
{
    public void Configure(EntityTypeBuilder<LearningLevel> builder)
    {
        builder.ToTable("LearningLevels");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.MinAge)
            .IsRequired();

        builder.Property(x => x.MaxAge)
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .IsRequired();

        // Seed core learning levels
        builder.HasData(
            new LearningLevel
            {
                Id = 1,
                Code = "LEVEL_1_TODDLER",
                Title = "Toddler Explorer",
                Description = "Sensory play, simple phonics, basic shapes, and color recognition.",
                MinAge = 3,
                MaxAge = 5,
                SortOrder = 1,
                IsActive = true,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new LearningLevel
            {
                Id = 2,
                Code = "LEVEL_2_EARLY_READER",
                Title = "Early Reader & Counter",
                Description = "Sight words, phonics blending, basic addition, and curiosity-driven Q&A.",
                MinAge = 6,
                MaxAge = 7,
                SortOrder = 2,
                IsActive = true,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new LearningLevel
            {
                Id = 3,
                Code = "LEVEL_3_ELEMENTARY",
                Title = "Elementary Discoverer",
                Description = "Story comprehension, multiplication, introductory science, and logic puzzles.",
                MinAge = 8,
                MaxAge = 10,
                SortOrder = 3,
                IsActive = true,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            },
            new LearningLevel
            {
                Id = 4,
                Code = "LEVEL_4_JUNIOR_SCHOLAR",
                Title = "Junior Scholar",
                Description = "Critical thinking, pre-algebra, scientific inquiry, and creative writing.",
                MinAge = 11,
                MaxAge = 13,
                SortOrder = 4,
                IsActive = true,
                CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
            }
        );
    }
}
