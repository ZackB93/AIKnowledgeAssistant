using KnowledgeAssistant.Application.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeAssistant.Application.Data.Configs.Users
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FirstName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.LastName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.AddressLine1)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.AddressLine2)
                .HasMaxLength(200);

            builder.Property(x => x.AddressLine3)
                .HasMaxLength(200);

            builder.Property(x => x.Postcode)
                .HasMaxLength(200);

            builder.Property(x => x.Location)
                .HasMaxLength(200);

            builder.Property(x => x.Enabled)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(x => x.CreatedDateTime)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(x => x.IsDeleted)
              .IsRequired()
              .HasDefaultValue(0);

            builder.HasMany(x => x.Documents)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId);

            // Individual indexes for single-column searches (e.g. search by last name only)
            builder.HasIndex(x => x.FirstName)
                .HasDatabaseName("IX_Users_FirstName");

            builder.HasIndex(x => x.LastName)
                .HasDatabaseName("IX_Users_LastName");

            // Composite index for searching/sorting by last name then first name together
            // (e.g. "Smith, John" style lookups) - also covers LastName-only queries
            builder.HasIndex(x => new { x.LastName, x.FirstName })
                .HasDatabaseName("IX_Users_LastName_FirstName");

            builder.HasData(
                new User { Id = 1, FirstName = "Zack", LastName = "Bucci", AddressLine1 = "5 Grove Nook", Postcode = "HD3 4UD", Location = "Huddersfield" },
                new User { Id = 2, FirstName = "Bob", LastName = "Smith", AddressLine1 = "123 Main Street", Postcode = "LN1 2AB", Location = "Lincoln" },
                new User { Id = 3, FirstName = "John", LastName = "Duggy", AddressLine1 = "456 Oak Avenue", Postcode = "M1 3CD", Location = "Manchester" }
            );
        }
    }
}
