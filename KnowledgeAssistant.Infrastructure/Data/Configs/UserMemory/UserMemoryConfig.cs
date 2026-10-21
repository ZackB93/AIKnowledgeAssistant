using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserMemoryModel = KnowledgeAssistant.Domain.Entities.UserMemory.UserMemory;

namespace KnowledgeAssistant.Infrastructure.Data.Configs.UserMemory
{
    public class UserMemoryConfig : IEntityTypeConfiguration<UserMemoryModel>
    {
        public void Configure(EntityTypeBuilder<UserMemoryModel> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.MemoryType)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(x => x.NormalizedKey)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Content)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(x => x.SourceChatSessionId);

            builder.Property(x => x.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.UpdatedAt)
                .IsRequired();

            builder.HasIndex(x => new
            {
                x.UserId,
                x.NormalizedKey
            });

            builder.HasIndex(x => new
            {
                x.UserId,
                x.MemoryType
            });

            builder.HasIndex(x => x.SourceChatSessionId);
        }
    }
}
