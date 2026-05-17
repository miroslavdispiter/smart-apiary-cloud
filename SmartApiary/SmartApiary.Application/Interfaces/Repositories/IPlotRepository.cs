using SmartApiary.Domain.Models;
using SmartApiary.Domain.ValueObjects;

namespace SmartApiary.Application.Interfaces.Repositories
{
    public interface IPlotRepository
    {
        Task<Plot?> GetByIdAsync(EntityId id, CancellationToken ct = default);
        Task<Plot?> GetByIdWithSprayingsAsync(EntityId id, CancellationToken ct = default);
        Task<IReadOnlyCollection<Plot>> GetAllByOwnerAsync(string ownerId, CancellationToken ct = default);

        // Spatial: returns beekeeper emails for all apiaries within radiusMeters of the given point
        Task<IReadOnlyCollection<string>> GetBeekeeperIdsNearAsync(
            double latitude,
            double longitude,
            double radiusMeters,
            CancellationToken ct = default);

        // For pcelar map view — plots with active cultures near an apiary
        Task<IReadOnlyCollection<Plot>> GetPlotsWithCultureNearAsync(
            double latitude,
            double longitude,
            double radiusMeters,
            CancellationToken ct = default);

        Task AddAsync(Plot plot, CancellationToken ct = default);
        Task UpdateAsync(Plot plot, CancellationToken ct = default);
        Task DeleteAsync(EntityId id, CancellationToken ct = default);
    }
}
