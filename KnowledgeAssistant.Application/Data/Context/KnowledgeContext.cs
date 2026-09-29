using Microsoft.EntityFrameworkCore;
using KnowledgeAssistant.Application.Entities.Users;
using KnowledgeAssistant.Application.Entities.Emails;

namespace KnowledgeAssistant.Application.Data.Context
{
    public class KnowledgeContext : DbContext
    {
        public KnowledgeContext(DbContextOptions<KnowledgeContext> options) : base(options)
        {

        }

        public DbSet<User> Users => Set<User>();
        public DbSet<UserCredential> UserCredentials => Set<UserCredential>();
        public DbSet<UserDocument> UserDocuments => Set<UserDocument>();
        public DbSet<Email> Emails => Set<Email>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations seperately - can be found in the Configs subfolder.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(KnowledgeContext).Assembly);
        }
    }
}
