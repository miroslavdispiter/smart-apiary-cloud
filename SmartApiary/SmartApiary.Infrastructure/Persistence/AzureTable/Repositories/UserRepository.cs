using Azure.Data.Tables;
using Microsoft.Extensions.Options;
using SmartApiary.Domain.Models;
using SmartApiary.Domain.ValueObjects;
using SmartApiary.Infrastructure.Common.Options;
using SmartApiary.Infrastructure.Persistence.AzureTable.Common;
using SmartApiary.Infrastructure.Persistence.AzureTable.Entities;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Application.Interfaces;

namespace SmartApiary.Infrastructure.Persistence.AzureTable.Repositories;

internal class UserRepository(
    TableServiceClient tableServiceClient,
    ITableKeyProvider<User> keyProvider,
    ITableMapper<User, UserEntity> mapper,
    IOptions<AzureTableOptions> options,
    IDomainEventDispatcher dispatcher)
    : AzureTableRepository<User, UserEntity>(
        tableServiceClient.GetTableClient(options.Value.UsersTable),
        keyProvider,
        mapper), IUserRepository
{
    
    public async Task SaveAsync(User user, CancellationToken ct = default)
    {
        await base.UpsertAsync(user, ct);

        if (user.DomainEvents.Any())
        {
            await dispatcher.DispatchAsync(user.DomainEvents, ct);
            user.ClearDomainEvents();
        }
    }

    
    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var partitionKey = _keyProvider.GetPartitionKey(null!); 
        var rowKey = email.ToLower().Trim();

        return await base.GetByIdAsync(partitionKey, rowKey, ct);
    }

    
    public async Task<User?> GetByIdAsync(EntityId id, CancellationToken ct = default)
    {
       
        var results = await base.QueryAsync($"Id eq '{id.Value}'", ct);
        return results.FirstOrDefault();
    }

    
    public async Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken ct = default)
    {
        return await base.QueryAsync(string.Empty, ct);
    }

    
    public async Task DeleteAsync(EntityId id, CancellationToken ct = default)
    {
        var user = await GetByIdAsync(id, ct);
        if (user != null)
        {
            await base.DeleteAsync(user, ct);
        }
    }

    public async Task<IReadOnlyCollection<User>> GetByIdsAsync(IEnumerable<EntityId> ids, CancellationToken ct = default)
    {
        
        if (ids == null || !ids.Any())
        {
            return Array.Empty<User>();
        }

        
        var idValues = ids.Select(id => id.Value).Distinct().ToList();

        
        var filterConditions = idValues.Select(id => $"Id eq '{id}'");
        var filter = string.Join(" or ", filterConditions);

        try
        {
            
            var results = await base.QueryAsync(filter, ct);
            return results;
        }
        catch (Exception ex)
        {
            
            throw;
        }
    }
}