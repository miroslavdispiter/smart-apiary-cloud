using SmartApiary.Domain.Models;
using SmartApiary.Domain.ValueObjects;

namespace SmartApiary.Application.Interfaces.Repositories
{
    public interface ISprayingRepository
    {
        Task<SprayingSchedule?> GetByIdAsync(EntityId id, CancellationToken ct = default);

        Task<IReadOnlyCollection<SprayingSchedule>> GetByPlotIdAsync(
            EntityId plotId,
            CancellationToken ct = default);

        Task<IReadOnlyCollection<(SprayingSchedule Schedule, string PlotName)>>
            GetUpcomingNearLocationAsync(
                double latitude,
                double longitude,
                double radiusMeters,
                CancellationToken ct = default);

        Task<IReadOnlyCollection<SprayingSchedule>> GetExpiredScheduledAsync(
            DateTime now,
            CancellationToken ct = default);

        Task AddAsync(SprayingSchedule schedule, CancellationToken ct = default);

        Task UpdateAsync(SprayingSchedule schedule, CancellationToken ct = default);
    }
}