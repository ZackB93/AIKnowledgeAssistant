using KnowledgeAssistant.Application.Interfaces.Repositories;
using KnowledgeAssistant.Domain.Entities.UserMemory;
using KnowledgeAssistant.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace KnowledgeAssistant.Infrastructure.Repositories
{
    public class UserMemoryRepository(KnowledgeContext context) : IUserMemoryRepository
    {
        private const int MaxTerms = 20;
        private const int MaxResults = 20;
        private const int PerTermLimit = 100;
        private const int MinTermLength = 3;
        private static readonly Regex WordRegex = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);

        public async Task<List<UserMemory>> GetByUserIdAsync(int userId, CancellationToken ct)
        {
            return await context.UserMemory
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.UpdatedAt)
                .ToListAsync(ct);
        }

        // Ok for up to a few thousand memories per user. But would need to improve for larger amounts.
        public async Task<List<UserMemory>> GetRelevantAsync(int userId, string query, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return new();
            }

            var terms = WordRegex
                .Matches(query)
                .Select(m => m.Value.ToLowerInvariant())
                .Where(x => x.Length >= MinTermLength)
                .Distinct()
                .Take(MaxTerms)
                .ToArray();

            if (terms.Length == 0)
            {
                return new();
            }

            // Memory id -> (memory, score)
            var hits = new Dictionary<int, (UserMemory Memory, double Score)>();

            foreach (var term in terms)
            {
                var t = term;

                var found = await context.UserMemory
                    .AsNoTracking()
                    .Where(x => x.UserId == userId)
                    .Where(x => x.Content.ToLower().Contains(t) || x.NormalizedKey.ToLower().Contains(t))
                    .OrderByDescending(x => x.UpdatedAt)
                    .Take(PerTermLimit)
                    .ToListAsync(ct);

                if (found.Count == 0)
                {
                    continue;
                }

                var weight = 1.0 / found.Count;

                foreach (var m in found)
                {
                    hits[m.Id] = hits.TryGetValue(m.Id, out var existing)
                        ? (existing.Memory, existing.Score + weight)
                        : (m, weight);
                }
            }

            return hits.Values
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Memory.UpdatedAt)
                .Take(MaxResults)
                .Select(x => x.Memory)
                .ToList();
        }

        public async Task<UserMemory?> GetByKeyAsync(int userId, string normalizedKey, CancellationToken ct)
        {
            return await context.UserMemory
                .FirstOrDefaultAsync(x => x.UserId == userId && x.NormalizedKey == normalizedKey, ct);
        }

        public async Task<UserMemory?> GetByIdAsync(int userId, int memoryId, CancellationToken ct)
        {
            return await context.UserMemory
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Id == memoryId, ct);
        }

        public async Task AddAsync(UserMemory memory, CancellationToken ct)
        {
            await context.UserMemory.AddAsync(memory, ct);
            await context.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(UserMemory memory, CancellationToken ct)
        {
            context.UserMemory.Update(memory);
            await context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(UserMemory memory, CancellationToken ct)
        {
            context.UserMemory.Remove(memory);
            await context.SaveChangesAsync(ct);
        }
    }
}