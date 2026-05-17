using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SmartApiary.Application.Interfaces.Repositories;
using SmartApiary.Domain.Enums.Farmer;
using SmartApiary.Domain.Models;
using SmartApiary.Domain.ValueObjects;
using SmartApiary.Infrastructure.Common.Options;
using SmartApiary.Infrastructure.Persistence.AzureSql.Entities;

namespace SmartApiary.Infrastructure.Persistence.AzureSql.Repositories
{
    internal class PlotRepository(
        IOptions<AzureSqlOptions> options,
        ILogger<PlotRepository> logger) : IPlotRepository
    {
        private readonly string _connectionString = options.Value.ConnectionString;

        public async Task<Plot?> GetByIdAsync(EntityId id, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT p.Id, p.OwnerId, p.Name, p.Latitude, p.Longitude, p.CreatedAt,
                       c.Id AS CultureId, c.Type, c.BloomingStart, c.BloomingEnd,
                       c.AdditionalInfo, c.CreatedAt AS CultureCreatedAt
                FROM Plots p
                LEFT JOIN Cultures c ON c.PlotId = p.Id
                WHERE p.Id = @Id";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id.Value);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                if (!await reader.ReadAsync(ct)) return null;

                return MapToPlot(reader);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SQL] Error fetching plot {Id}.", id.Value);
                throw;
            }
        }

        public async Task<Plot?> GetByIdWithSprayingsAsync(EntityId id, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT p.Id, p.OwnerId, p.Name, p.Latitude, p.Longitude, p.CreatedAt,
                       c.Id AS CultureId, c.Type, c.BloomingStart, c.BloomingEnd,
                       c.AdditionalInfo, c.CreatedAt AS CultureCreatedAt,
                       s.Id AS ScheduleId, s.ScheduledAt, s.DurationHours, s.Preparation,
                       s.Status, s.NotifiedBeekeeperCount, s.CreatedAt AS ScheduleCreatedAt
                FROM Plots p
                LEFT JOIN Cultures c ON c.PlotId = p.Id
                LEFT JOIN SprayingSchedules s ON s.PlotId = p.Id
                WHERE p.Id = @Id";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id.Value);

                await using var reader = await cmd.ExecuteReaderAsync(ct);

                Plot? plot = null;
                Culture? culture = null;
                var schedules = new List<SprayingSchedule>();

                while (await reader.ReadAsync(ct))
                {
                    if (plot is null)
                    {
                        culture = reader.IsDBNull(reader.GetOrdinal("CultureId"))
                            ? null
                            : MapToCulture(reader);

                        plot = Plot.Load(
                            id: reader.GetString(reader.GetOrdinal("Id")),
                            ownerId: reader.GetString(reader.GetOrdinal("OwnerId")),
                            name: reader.GetString(reader.GetOrdinal("Name")),
                            latitude: reader.GetDouble(reader.GetOrdinal("Latitude")),
                            longitude: reader.GetDouble(reader.GetOrdinal("Longitude")),
                            createdAt: reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            culture: culture
                        ).Value;
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("ScheduleId")))
                        schedules.Add(MapToSprayingSchedule(reader));
                }

                if (plot is null) return null;

                return Plot.Load(
                    id: plot.Id.Value,
                    ownerId: plot.OwnerId,
                    name: plot.Name,
                    latitude: plot.Latitude,
                    longitude: plot.Longitude,
                    createdAt: plot.CreatedAt,
                    culture: culture,
                    sprayingSchedules: schedules
                ).Value;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SQL] Error fetching plot {Id} with sprayings.", id.Value);
                throw;
            }
        }

        public async Task<IReadOnlyCollection<Plot>> GetAllByOwnerAsync(string ownerId, CancellationToken ct = default)
        {
            const string sql = @"
                SELECT p.Id, p.OwnerId, p.Name, p.Latitude, p.Longitude, p.CreatedAt,
                       c.Id AS CultureId, c.Type, c.BloomingStart, c.BloomingEnd,
                       c.AdditionalInfo, c.CreatedAt AS CultureCreatedAt
                FROM Plots p
                LEFT JOIN Cultures c ON c.PlotId = p.Id
                WHERE p.OwnerId = @OwnerId
                ORDER BY p.CreatedAt DESC";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@OwnerId", ownerId);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                var plots = new List<Plot>();

                while (await reader.ReadAsync(ct))
                    plots.Add(MapToPlot(reader));

                return plots.AsReadOnly();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SQL] Error fetching plots for owner {OwnerId}.", ownerId);
                throw;
            }
        }

        public async Task<IReadOnlyCollection<string>> GetBeekeeperIdsNearAsync(
            double latitude,
            double longitude,
            double radiusMeters,
            CancellationToken ct = default)
        {
            
            const string sql = @"
        SELECT DISTINCT a.OwnerId
        FROM Apiaries a
        WHERE a.Location.STDistance(geography::Point(@Lat, @Lng, 4326)) <= @RadiusMeters";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Lat", latitude);
                cmd.Parameters.AddWithValue("@Lng", longitude);
                cmd.Parameters.AddWithValue("@RadiusMeters", radiusMeters);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                var ownerIds = new List<string>();

                while (await reader.ReadAsync(ct))
                {
                    ownerIds.Add(reader.GetString(0));
                }

                return ownerIds.AsReadOnly();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SQL] Error fetching beekeeper IDs near ({Lat}, {Lng}).", latitude, longitude);
                throw;
            }
        }

        public async Task<IReadOnlyCollection<Plot>> GetPlotsWithCultureNearAsync(
            double latitude,
            double longitude,
            double radiusMeters,
            CancellationToken ct = default)
        {
            // Used by beekeeper map view — show nearby plots that have an active culture
            const string sql = @"
                SELECT p.Id, p.OwnerId, p.Name, p.Latitude, p.Longitude, p.CreatedAt,
                       c.Id AS CultureId, c.Type, c.BloomingStart, c.BloomingEnd,
                       c.AdditionalInfo, c.CreatedAt AS CultureCreatedAt
                FROM Plots p
                INNER JOIN Cultures c ON c.PlotId = p.Id
                WHERE p.Location.STDistance(geography::Point(@Lat, @Lng, 4326)) <= @RadiusMeters";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Lat", latitude);
                cmd.Parameters.AddWithValue("@Lng", longitude);
                cmd.Parameters.AddWithValue("@RadiusMeters", radiusMeters);

                await using var reader = await cmd.ExecuteReaderAsync(ct);
                var plots = new List<Plot>();

                while (await reader.ReadAsync(ct))
                    plots.Add(MapToPlot(reader));

                return plots.AsReadOnly();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SQL] Error fetching plots with culture near ({Lat}, {Lng}).", latitude, longitude);
                throw;
            }
        }

        public async Task AddAsync(Plot plot, CancellationToken ct = default)
        {
            const string sql = @"
                INSERT INTO Plots (Id, OwnerId, Name, Latitude, Longitude, CreatedAt, Location)
                VALUES (@Id, @OwnerId, @Name, @Latitude, @Longitude, @CreatedAt,
                        geography::Point(@Latitude, @Longitude, 4326))";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                await using var cmd = new SqlCommand(sql, conn);
                MapPlotParameters(cmd, plot);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SQL] Error inserting plot {Id}.", plot.Id.Value);
                throw;
            }
        }

        public async Task UpdateAsync(Plot plot, CancellationToken ct = default)
        {
            // Update plot + upsert culture in one transaction
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync(ct);
            await using var tx = (SqlTransaction)await conn.BeginTransactionAsync(ct);

            try
            {
                const string updatePlot = @"
                    UPDATE Plots
                    SET Name = @Name, Latitude = @Latitude, Longitude = @Longitude,
                        Location = geography::Point(@Latitude, @Longitude, 4326)
                    WHERE Id = @Id";

                await using var cmd = new SqlCommand(updatePlot, conn, tx);
                MapPlotParameters(cmd, plot);
                await cmd.ExecuteNonQueryAsync(ct);

                // Delete old culture
                await using var delCmd = new SqlCommand(
                    "DELETE FROM Cultures WHERE PlotId = @PlotId", conn, tx);
                delCmd.Parameters.AddWithValue("@PlotId", plot.Id.Value);
                await delCmd.ExecuteNonQueryAsync(ct);

                // Insert new culture if present
                if (plot.Culture is not null)
                {
                    const string insertCulture = @"
                        INSERT INTO Cultures (Id, PlotId, Type, BloomingStart, BloomingEnd, AdditionalInfo, CreatedAt)
                        VALUES (@Id, @PlotId, @Type, @BloomingStart, @BloomingEnd, @AdditionalInfo, @CreatedAt)";

                    await using var cCmd = new SqlCommand(insertCulture, conn, tx);
                    MapCultureParameters(cCmd, plot.Culture);
                    await cCmd.ExecuteNonQueryAsync(ct);
                }

                await tx.CommitAsync(ct);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync(ct);
                logger.LogError(ex, "[SQL] Error updating plot {Id}.", plot.Id.Value);
                throw;
            }
        }

        public async Task DeleteAsync(EntityId id, CancellationToken ct = default)
        {
            // Cascades to Cultures and SprayingSchedules via FK ON DELETE CASCADE
            const string sql = "DELETE FROM Plots WHERE Id = @Id";

            try
            {
                await using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync(ct);
                await using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Id", id.Value);
                await cmd.ExecuteNonQueryAsync(ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[SQL] Error deleting plot {Id}.", id.Value);
                throw;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static Plot MapToPlot(SqlDataReader r)
        {
            Culture? culture = null;
            if (!r.IsDBNull(r.GetOrdinal("CultureId")))
                culture = MapToCulture(r);

            return Plot.Load(
                id: r.GetString(r.GetOrdinal("Id")),
                ownerId: r.GetString(r.GetOrdinal("OwnerId")),
                name: r.GetString(r.GetOrdinal("Name")),
                latitude: r.GetDouble(r.GetOrdinal("Latitude")),
                longitude: r.GetDouble(r.GetOrdinal("Longitude")),
                createdAt: r.GetDateTime(r.GetOrdinal("CreatedAt")),
                culture: culture
            ).Value;
        }

        private static Culture MapToCulture(SqlDataReader r)
        {
            return Culture.Load(
                id: r.GetString(r.GetOrdinal("CultureId")),
                plotId: r.GetString(r.GetOrdinal("Id")),
                type: (CultureType)r.GetInt32(r.GetOrdinal("Type")),
                bloomingStart: r.GetDateTime(r.GetOrdinal("BloomingStart")),
                bloomingEnd: r.GetDateTime(r.GetOrdinal("BloomingEnd")),
                additionalInfo: r.IsDBNull(r.GetOrdinal("AdditionalInfo")) ? null : r.GetString(r.GetOrdinal("AdditionalInfo")),
                createdAt: r.GetDateTime(r.GetOrdinal("CultureCreatedAt"))
            ).Value;
        }

        private static SprayingSchedule MapToSprayingSchedule(SqlDataReader r)
        {
            return SprayingSchedule.Load(
                id: r.GetString(r.GetOrdinal("ScheduleId")),
                plotId: r.GetString(r.GetOrdinal("Id")),
                scheduledAt: r.GetDateTime(r.GetOrdinal("ScheduledAt")),
                durationHours: r.GetInt32(r.GetOrdinal("DurationHours")),
                preparation: r.IsDBNull(r.GetOrdinal("Preparation")) ? null : r.GetString(r.GetOrdinal("Preparation")),
                status: (SprayingStatus)r.GetInt32(r.GetOrdinal("Status")),
                notifiedBeekeeperCount: r.GetInt32(r.GetOrdinal("NotifiedBeekeeperCount")),
                createdAt: r.GetDateTime(r.GetOrdinal("ScheduleCreatedAt"))
            ).Value;
        }

        private static void MapPlotParameters(SqlCommand cmd, Plot plot)
        {
            cmd.Parameters.AddWithValue("@Id", plot.Id.Value);
            cmd.Parameters.AddWithValue("@OwnerId", plot.OwnerId);
            cmd.Parameters.AddWithValue("@Name", plot.Name);
            cmd.Parameters.AddWithValue("@Latitude", plot.Latitude);
            cmd.Parameters.AddWithValue("@Longitude", plot.Longitude);
            cmd.Parameters.AddWithValue("@CreatedAt", plot.CreatedAt);
        }

        private static void MapCultureParameters(SqlCommand cmd, Culture culture)
        {
            cmd.Parameters.AddWithValue("@Id", culture.Id.Value);
            cmd.Parameters.AddWithValue("@PlotId", culture.PlotId.Value);
            cmd.Parameters.AddWithValue("@Type", (int)culture.Type);
            cmd.Parameters.AddWithValue("@BloomingStart", culture.BloomingStart);
            cmd.Parameters.AddWithValue("@BloomingEnd", culture.BloomingEnd);
            cmd.Parameters.AddWithValue("@AdditionalInfo", (object?)culture.AdditionalInfo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@CreatedAt", culture.CreatedAt);
        }
    }
}
