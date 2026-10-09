using KnowledgeAssistant.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace KnowledgeAssistant.Infrastructure.Data.Configs.Notifications
{
    public class NotificationRecipientConfig : IEntityTypeConfiguration<NotificationRecipient>
    {
        public void Configure(EntityTypeBuilder<NotificationRecipient> builder)
        {
            builder.HasKey(r => new { r.NotificationId, r.UserId });

            builder.Property(r => r.IsRead)
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(r => r.DeliveredAt)
                .IsRequired();

            builder.HasOne(r => r.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => new { r.UserId, r.IsRead, r.NotificationId });
        }
    }
}
