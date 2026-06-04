using Domain.Entities;

namespace Domain.Interfaces
{
    public interface ISystemSettingsRepository
    {
        Task<SystemSettings?> GetAsync(CancellationToken ct);
        Task UpdateAsync(SystemSettings settings, CancellationToken ct);
    }
}
