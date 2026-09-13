using Cadence.Core.Interfaces;
using Cadence.Core.Scheduling;
using Cadence.Infrastructure.Notifications;
using Cadence.Infrastructure.Persistence;
using Cadence.Infrastructure.Routines;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cadence.Infrastructure
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCadenceInfrastructure(this IServiceCollection services)
        {
            services.AddSingleton<CadenceDbContext>(sp =>
            {
                CadencePaths.EnsureDatabaseMigrated();
                var dbPath = CadencePaths.GetDbPath();
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
                var options = new DbContextOptionsBuilder<CadenceDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;
                return new CadenceDbContext(options);
            });
            services.AddSingleton<ICadenceStore, SqliteCadenceStore>();
            services.AddSingleton<IRoutineSource>(sp =>
            {
                var blocks = new JsonRoutineLoader().LoadDefault();
                return new RoutineClock(blocks);
            });
            services.AddSingleton<ConsoleNotificationSender>();
            services.AddSingleton<IClock>(sp => new SystemClock());
            services.AddSingleton<INotificationSender, WindowsToastNotificationSender>();

            return services;
        }
        [Obsolete("Use CadencePaths.GetDataDirectory() instead. Kept for compatibility.")]
        public static string GetCadenceDbDirectory() => CadencePaths.GetDataDirectory();
    }
}