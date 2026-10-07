using KnowledgeAssistant.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KnowledgeAssistant.Infrastructure.Data.Configs.Users
{
    public class UserCredentialConfig : IEntityTypeConfiguration<UserCredential>
    {
        public void Configure(EntityTypeBuilder<UserCredential> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.EmailAddress)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasOne(x => x.User)
                .WithOne(x => x.Credentials)
                .HasForeignKey<UserCredential>(x => x.UserId);

            builder.HasIndex(x => x.EmailAddress)
                .IsUnique()
                .HasDatabaseName("IX_UserCredentials_EmailAddress");

            // Password = test
            builder.HasData(
                new UserCredential { Id = 1, UserId = 1, EmailAddress = "zack.bucci@example.com", PasswordHash = "AQAAAAEAACcQAAAAENtrqYs0Oo8hJHojnX/bBRVRX1l9ExQGfMOScHNkp1JKfSvwHxJeBXvDpgLAVcHZVA==" },
                new UserCredential { Id = 2, UserId = 2, EmailAddress = "john.smith@example.com", PasswordHash = "AQAAAAEAACcQAAAAENtrqYs0Oo8hJHojnX/bBRVRX1l9ExQGfMOScHNkp1JKfSvwHxJeBXvDpgLAVcHZVA==" },
                new UserCredential { Id = 3, UserId = 3, EmailAddress = "john.duggy@example.com", PasswordHash = "AQAAAAEAACcQAAAAENtrqYs0Oo8hJHojnX/bBRVRX1l9ExQGfMOScHNkp1JKfSvwHxJeBXvDpgLAVcHZVA==" }
            );
        }
    }
}
