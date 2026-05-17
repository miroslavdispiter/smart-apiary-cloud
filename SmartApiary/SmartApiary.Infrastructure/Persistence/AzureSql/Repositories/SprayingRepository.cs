using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Enums.Farmer;
using SmartApiary.Domain.Models;
using SmartApiary.Domain.ValueObjects;
using SmartApiary.Infrastructure.Common.Options;

namespace SmartApiary.Infrastructure.Persistence.AzureSql.Repositories
{
    internal class SprayingRepository(
        IOptions<AzureSqlOptions> options,
        ILogger<SprayingRepository> logger) : ISprayingRepository
    {
        private readonly string _connectionString = options.Value.ConnectionString;

        public async Task<SprayingSchedule?> GetByIdAsync(EntityId id, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT s.Id, s.PlotId, p.Name AS PlotName, s.ScheduledAt, s.DurationHours, s.Preparation,
                       s.Status, s.NotifiedBeekeeperCount, s.CreatedAt
                FROM SprayingSchedules s
                INNER JOIN Plots p ON p.Id = s.PlotId
                WHERE s.Id = @Id";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id.Value);

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            if (!await reader.ReadAsync(ct))
                return null;

            return MapToSchedule(reader);
        }

        public async Task<IReadOnlyCollection<SprayingSchedule>> GetByPlotIdAsync(
            EntityId plotId,
            CancellationToken ct = default)
        {
            const string sql = @"
                SELECT s.Id, s.PlotId, p.Name AS PlotName, s.ScheduledAt, s.DurationHours, s.Preparation,
                       s.Status, s.NotifiedBeekeeperCount, s.CreatedAt
                FROM SprayingSchedules s
                INNER JOIN Plots p ON p.Id = s.PlotId
                WHERE s.PlotId = @PlotId
                ORDER BY s.ScheduledAt DESC";

            var list = new List<SprayingSchedule>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@PlotId", plotId.Value);

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                list.Add(MapToSchedule(reader));
            }

            return list.AsReadOnly();
        }

        public async Task<IReadOnlyCollection<(SprayingSchedule Schedule, string PlotName)>>
            GetUpcomingNearLocationAsync(
                double latitude,
                double longitude,
                double radiusMeters,
                CancellationToken ct = default)
        {
            const string sql = @"
                SELECT s.Id, s.PlotId, p.Name AS PlotName, s.ScheduledAt, s.DurationHours, s.Preparation,
                       s.Status, s.NotifiedBeekeeperCount, s.CreatedAt
                FROM SprayingSchedules s
                INNER JOIN Plots p ON p.Id = s.PlotId
                WHERE p.Location.STDistance(geography::Point(@Lat, @Lng, 4326)) <= @RadiusMeters
                  AND s.Status = @Status
                  AND s.ScheduledAt >= SYSUTCDATETIME()
                ORDER BY s.ScheduledAt ASC";

            var list = new List<(SprayingSchedule, string)>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Lat", latitude);
            cmd.Parameters.AddWithValue("@Lng", longitude);
            cmd.Parameters.AddWithValue("@RadiusMeters", radiusMeters);
            cmd.Parameters.AddWithValue("@Status", (int)SprayingStatus.Scheduled);

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var schedule = MapToSchedule(reader);
                var plotName = reader.GetString(reader.GetOrdinal("PlotName"));
                list.Add((schedule, plotName));
            }

            return list.AsReadOnly();
        }

        public async Task<IReadOnlyCollection<SprayingSchedule>> GetExpiredScheduledAsync(
            DateTime now,
            CancellationToken ct = default)
        {
            const string sql = @"
                SELECT s.Id, s.PlotId, p.Name AS PlotName, s.ScheduledAt, s.DurationHours, s.Preparation,
                       s.Status, s.NotifiedBeekeeperCount, s.CreatedAt
                FROM SprayingSchedules s
                INNER JOIN Plots p ON p.Id = s.PlotId
                WHERE s.Status = @Status
                  AND DATEADD(HOUR, s.DurationHours, s.ScheduledAt) <= @Now";

            var list = new List<SprayingSchedule>();

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", (int)SprayingStatus.Scheduled);
            cmd.Parameters.AddWithValue("@Now", now);

            await using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                list.Add(MapToSchedule(reader));
            }

            return list.AsReadOnly();
        }

        public async Task AddAsync(SprayingSchedule schedule, CancellationToken ct = default)
        {
            const string sql = @"
                INSERT INTO SprayingSchedules
                    (Id, PlotId, ScheduledAt, DurationHours, Preparation, Status, NotifiedBeekeeperCount, CreatedAt)
                VALUES
                    (@Id, @PlotId, @ScheduledAt, @DurationHours, @Preparation, @Status, @NotifiedBeekeeperCount, @CreatedAt)";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            MapParameters(cmd, schedule);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task UpdateAsync(SprayingSchedule schedule, CancellationToken ct = default)
        {
            const string sql = @"
                UPDATE SprayingSchedules
                SET ScheduledAt = @ScheduledAt,
                    DurationHours = @DurationHours,
                    Preparation = @Preparation,
                    Status = @Status,
                    NotifiedBeekeeperCount = @NotifiedBeekeeperCount
                WHERE Id = @Id";

            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            await using var cmd = new SqlCommand(sql, conn);
            MapParameters(cmd, schedule);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        private static SprayingSchedule MapToSchedule(SqlDataReader r)
        {
            var result = SprayingSchedule.Load(
                id: r.GetString(r.GetOrdinal("Id")),
                plotId: r.GetString(r.GetOrdinal("PlotId")),
                scheduledAt: r.GetDateTime(r.GetOrdinal("ScheduledAt")),
                durationHours: r.GetInt32(r.GetOrdinal("DurationHours")),
                preparation: r.IsDBNull(r.GetOrdinal("Preparation"))
                    ? null
                    : r.GetString(r.GetOrdinal("Preparation")),
                status: (SprayingStatus)r.GetInt32(r.GetOrdinal("Status")),
                notifiedBeekeeperCount: r.GetInt32(r.GetOrdinal("NotifiedBeekeeperCount")),
                createdAt: r.GetDateTime(r.GetOrdinal("CreatedAt"))
            );

            if (result.IsFailure)
                throw new InvalidOperationException(result.Error!.Message);

            return result.Value;
        }

        private static void MapParameters(SqlCommand cmd, SprayingSchedule s)
        {
            cmd.Parameters.AddWithValue("@Id", s.Id.Value);
            cmd.Parameters.AddWithValue("@PlotId", s.PlotId.Value);
            cmd.Parameters.AddWithValue("@ScheduledAt", s.ScheduledAt);
            cmd.Parameters.AddWithValue("@DurationHours", s.DurationHours);
            cmd.Parameters.AddWithValue("@Preparation", (object?)s.Preparation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Status", (int)s.Status);
            cmd.Parameters.AddWithValue("@NotifiedBeekeeperCount", s.NotifiedBeekeeperCount);
            cmd.Parameters.AddWithValue("@CreatedAt", s.CreatedAt);
        }
    }
}