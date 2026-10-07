using KnowledgeAssistant.Domain.Entities.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.Data.Configs.Documents
{
    public class DocumentChunkConfig : IEntityTypeConfiguration<DocumentChunk>
    {
        public void Configure(EntityTypeBuilder<DocumentChunk> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.ChunkIndex)
                .IsRequired();

            builder.Property(x => x.Content)
                .IsRequired();

            builder.Property(x => x.Embedding)
                .IsRequired();

            builder.HasOne(x => x.Document)
                .WithMany(x => x.Chunks)
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.DocumentId, x.ChunkIndex })
                .IsUnique();
        }
    }
}
