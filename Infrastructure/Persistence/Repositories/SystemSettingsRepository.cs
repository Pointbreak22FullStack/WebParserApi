using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class SystemSettingsRepository : ISystemSettingsRepository
    {
        private readonly ApplicationDbContext _context;
        public SystemSettingsRepository(ApplicationDbContext context) => _context = context;

        public async Task<SystemSettings?> GetAsync(CancellationToken ct)
            => await _context.SystemSettings.FirstOrDefaultAsync(x => x.Id == 1, ct);

        public async Task UpdateAsync(SystemSettings settings, CancellationToken ct)
        {
            _context.SystemSettings.Update(settings);
            await _context.SaveChangesAsync(ct);
        }
    }
}
