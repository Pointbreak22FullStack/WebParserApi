using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Account> Accounts => Set<Account>();
        public DbSet<Proxy> Proxies => Set<Proxy>();
        public DbSet<UserTask> UserTasks => Set<UserTask>();
        public DbSet<TaskAccount> TaskAccounts => Set<TaskAccount>();
        public DbSet<TaskLog> TaskLogs => Set<TaskLog>();
        public DbSet<ParsingResult> ParsingResults => Set<ParsingResult>();
        public DbSet<SystemSettings> SystemSettings => Set<SystemSettings>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Account>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(a => a.Proxy)
                      .WithMany(p => p.Accounts)
                      .HasForeignKey(a => a.ProxyId);
            });

            modelBuilder.Entity<UserTask>()
                .HasOne(t => t.Result)
                .WithOne()
                .HasForeignKey<ParsingResult>(r => r.ParserTaskId);

            modelBuilder.Entity<TaskAccount>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(ta => ta.UserTask)
                      .WithMany(t => t.TaskAccounts)
                      .HasForeignKey(ta => ta.UserTaskId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(ta => ta.Account)
                      .WithMany()
                      .HasForeignKey(ta => ta.AccountId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<TaskLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne<UserTask>()
                      .WithMany(t => t.Logs)
                      .HasForeignKey(l => l.ParserTaskId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.ToTable("TaskLogs");
            });

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
