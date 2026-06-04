using Application.Common.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Repositories
{
    public class TaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;

        public TaskRepository(ApplicationDbContext context) => _context = context;

        public async Task<IReadOnlyList<UserTask>> GetAllAsync(CancellationToken ct = default)
            => await _context.UserTasks
                .Include(t => t.TaskAccounts)
                    .ThenInclude(ta => ta.Account)
                .Include(t => t.Logs)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync(ct);

        public async Task<UserTask?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => await _context.UserTasks
                .Include(t => t.TaskAccounts)
                    .ThenInclude(ta => ta.Account)
                .Include(t => t.Logs)
                .FirstOrDefaultAsync(t => t.Id == id, ct);
    }
}
