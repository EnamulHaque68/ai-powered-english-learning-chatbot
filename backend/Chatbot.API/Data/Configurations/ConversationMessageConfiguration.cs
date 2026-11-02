using Chatbot.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chatbot.API.Data.Configurations;

public class ConversationMessageConfiguration : IEntityTypeConfiguration<ConversationMessage>
{
    public void Configure(EntityTypeBuilder<ConversationMessage> builder)
    {
        builder.ToTable("ConversationMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SenderRole)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(x => x.MessageText)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(x => x.Timestamp)
            .IsRequired();

        builder.Property(x => x.MediaUrl)
            .HasMaxLength(500);

        builder.HasIndex(x => x.ConversationSessionId);
        builder.HasIndex(x => x.Timestamp);
    }
}
