using SmartApiary.Domain.Models;
using SmartApiary.Domain.ValueObjects;

namespace SmartApiary.Application.Interfaces.Repositories
{
    public interface IUserRepository
    {
        Task SaveAsync(User user, CancellationToken ct = default);

        Task<User?> GetByIdAsync(EntityId id, CancellationToken ct = default);

        Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

        Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken ct = default);

        Task DeleteAsync(EntityId id, CancellationToken ct = default);

        Task<IReadOnlyCollection<User>> GetByIdsAsync(IEnumerable<EntityId> ids, CancellationToken ct = default);
    }
}
