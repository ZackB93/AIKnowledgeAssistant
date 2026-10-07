using KnowledgeAssistant.Domain.Entities.Emails;
using KnowledgeAssistant.Application.Data.Configs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.Data.Configs.Emails
{
    public class EmailConfiguration : IEntityTypeConfiguration<Email>
    {
        public void Configure(EntityTypeBuilder<Email> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.UserId)
                .IsRequired();

            builder.Property(x => x.To)
                .IsRequired()
                .HasMaxLength(320);

            builder.Property(x => x.Subject)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.Body)
                .IsRequired();

            builder.Property(x => x.IsHtml)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.Property(x => x.SentAt)
                .IsRequired(false);

            builder.Property(x => x.FailedAt)
                .IsRequired(false);

            builder.Property(x => x.ErrorMessage)
                .HasMaxLength(4000)
                .IsRequired(false);

            builder.Property(x => x.RetryCount)
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithMany(x => x.Emails)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new
            {
                x.Status,
                x.CreatedAt,
                x.UserId
            });
        }
    }
}
