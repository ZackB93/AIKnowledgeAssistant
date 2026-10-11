using KnowledgeAssistant.Domain.Entities.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeAssistant.Infrastructure.Data.Configs.Audit
{
    public class ActivityAuditConfiguration : IEntityTypeConfiguration<ActivityAudit>
    {
        public void Configure(EntityTypeBuilder<ActivityAudit> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.DateTime)
                .IsRequired();

            builder.Property(x => x.UserId)
                .HasMaxLength(128);

            builder.Property(x => x.UserName)
                .HasMaxLength(256);

            builder.Property(x => x.Action)
                .HasConversion<string>()
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.Description)
                .HasMaxLength(1000);

            builder.Property(x => x.EntityName)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.EntityId)
                .HasMaxLength(128);

            builder.Property(x => x.OldValues)
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.NewValues)
                .HasColumnType("nvarchar(max)");

            builder.Property(x => x.ChangedProperties)
                .HasColumnType("nvarchar(max)");

            builder.HasIndex(x => new { x.EntityName, x.EntityId });
            builder.HasIndex(x => new { x.UserId, x.DateTime });
            builder.HasIndex(x => x.DateTime);
        }
    }
}
