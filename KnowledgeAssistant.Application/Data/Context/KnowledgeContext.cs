using KnowledgeAssistant.Application.Entities.Chat;
using KnowledgeAssistant.Application.Entities.Emails;
using KnowledgeAssistant.Application.Entities.Users;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations seperately - can be found in the Configs subfolder.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(KnowledgeContext).Assembly);
        }
    }
}
