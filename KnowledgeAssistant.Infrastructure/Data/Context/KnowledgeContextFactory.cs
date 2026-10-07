using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace KnowledgeAssistant.Infrastructure.Data.Context
{
    public class KnowledgeContextFactory : IDesignTimeDbContextFactory<KnowledgeContext>
    {
        public KnowledgeContext CreateDbContext(string[] args)
        {
            // Point at the .Api project's folder so it can find appsettings.json
            var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "KnowledgeAssistant.API");

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<KnowledgeContext>();
            optionsBuilder.UseSqlServer(config.GetConnectionString("KnowledgeDatabase"));

            return new KnowledgeContext(optionsBuilder.Options);
        }
    }
}