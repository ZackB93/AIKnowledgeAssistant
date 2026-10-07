using KnowledgeAssistant.Domain.Entities.Chat;
using KnowledgeAssistant.Domain.Entities.Documents;
using KnowledgeAssistant.Domain.Entities.Emails;
using KnowledgeAssistant.Domain.Entities.Roles;
using KnowledgeAssistant.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

namespace KnowledgeAssistant.Application.Data.Context
{
    public class KnowledgeContext : DbContext
    {
        public KnowledgeContext(DbContextOptions<KnowledgeContext> options) : base(options)
        {

        }

        public DbSet<User> Users => Set<User>();
        public DbSet<UserCredential> UserCredentials => Set<UserCredential>();
        public DbSet<Email> Emails => Set<Email>();
        public DbSet<ChatSession> ChatSessions => Set<ChatSession>();
        public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();
        public DbSet<Domain.Entities.Documents.Document> Documents => Set<Domain.Entities.Documents.Document>();
        public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configurations seperately - can be found in the Configs subfolder.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(KnowledgeContext).Assembly);
        }
    }
}
