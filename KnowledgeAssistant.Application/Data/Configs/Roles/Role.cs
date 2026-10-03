using KnowledgeAssistant.Application.Entities.Roles;
using KnowledgeAssistant.Application.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.Data.Configs.Roles
{
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .ValueGeneratedOnAdd();

            builder.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(r => r.Name)
                .IsUnique();

            builder.Property(r => r.Description)
                .HasMaxLength(250);

            builder.Property(x => x.CreatedDateTime)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.HasData(new Role {
                Id = 1,
                Name = "Admin",
                Description = "Administrator with full system access",
                CreatedDateTime = new DateTime(2026, 10, 3)
            },
            new Role
            {
                Id = 2,
                Name = "User",
                Description = "Standard system user",
                CreatedDateTime = new DateTime(2026, 10, 3)
            });
        }
    }
}
