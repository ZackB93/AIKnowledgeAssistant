using KnowledgeAssistant.Application.DTOs.Dashboard;

namespace KnowledgeAssistant.Application.Interfaces.Repositories
{
    public interface IDashboardRepository
    {
        Task<UserDashboardVM> GetUserDashboardDataAsync(int userId, CancellationToken ct);
        Task<AdminDashboardVM> GetAdminDashboardDataAsync(CancellationToken ct);
    }
}
