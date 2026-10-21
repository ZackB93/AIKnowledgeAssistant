using KnowledgeAssistant.Domain.Entities.UserMemory;
using System;
using System.Collections.Generic;
using System.Text;

namespace KnowledgeAssistant.Application.Interfaces.Repositories
{
    public interface IUserMemoryRepository
    {
        Task<List<UserMemory>> GetByUserIdAsync(int userId, CancellationToken ct);

        Task<List<UserMemory>> GetRelevantAsync(int userId, string query, CancellationToken ct);

        Task<UserMemory?> GetByKeyAsync(int userId, string normalizedKey, CancellationToken ct);

        Task<UserMemory?> GetByIdAsync(int userId, int memoryId, CancellationToken ct);

        Task AddAsync(UserMemory memory, CancellationToken ct);

        Task UpdateAsync(UserMemory memory, CancellationToken ct);

        Task DeleteAsync(UserMemory memory, CancellationToken ct);
    }
}
