using System.Text.Json;
using BluewardSentinel.Core.Interfaces;
using BluewardSentinel.Core.Models;
using Microsoft.Data.Sqlite;

namespace BluewardSentinel.Infrastructure.Services;

public class SentinelSqliteRepository : ISentinelRepository
{
    private readonly string _connectionString;

    public SentinelSqliteRepository(string? dbPath = null)
    {
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var sentinelDir = Path.Combine(appData, "BluewardSentinelAgent");
            Directory.CreateDirectory(sentinelDir);
            dbPath = Path.Combine(sentinelDir, "sentinel_agent.db");
        }
        _connectionString = $"Data Source={dbPath};Cache=Shared;";
    }

    public async Task InitializeAsync()
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        using var pragmaCmd = conn.CreateCommand();
        pragmaCmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA busy_timeout=5000;";
        await pragmaCmd.ExecuteNonQueryAsync();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS SentinelProjects (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT,
    ApiKey TEXT UNIQUE NOT NULL,
    Environment TEXT NOT NULL,
    CreatedAtUtc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS IncidentClusters (
    Id TEXT PRIMARY KEY,
    ProjectId TEXT NOT NULL,
    Fingerprint TEXT NOT NULL,
    Title TEXT NOT NULL,
    ExceptionType TEXT NOT NULL,
    Severity TEXT NOT NULL,
    Status TEXT NOT NULL,
    OccurrenceCount INTEGER NOT NULL,
    FirstSeenUtc TEXT NOT NULL,
    LastSeenUtc TEXT NOT NULL,
    SampleStackTrace TEXT,
    SampleMessage TEXT,
    DiagnosisJson TEXT,
    TagsJson TEXT,
    IsAnomalySpike INTEGER DEFAULT 0,
    FOREIGN KEY(ProjectId) REFERENCES SentinelProjects(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_incidents_proj ON IncidentClusters(ProjectId);
CREATE INDEX IF NOT EXISTS idx_incidents_fp ON IncidentClusters(ProjectId, Fingerprint);
CREATE INDEX IF NOT EXISTS idx_incidents_severity ON IncidentClusters(Severity);

CREATE TABLE IF NOT EXISTS SentinelLogs (
    Id TEXT PRIMARY KEY,
    ProjectId TEXT NOT NULL,
    IncidentId TEXT,
    LogLevel TEXT NOT NULL,
    Message TEXT NOT NULL,
    ExceptionType TEXT,
    StackTrace TEXT,
    Source TEXT,
    HostName TEXT,
    Environment TEXT,
    Fingerprint TEXT,
    MetadataJson TEXT,
    TimestampUtc TEXT NOT NULL,
    FOREIGN KEY(ProjectId) REFERENCES SentinelProjects(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_logs_proj ON SentinelLogs(ProjectId, TimestampUtc);
CREATE INDEX IF NOT EXISTS idx_logs_incident ON SentinelLogs(IncidentId);
CREATE INDEX IF NOT EXISTS idx_logs_level ON SentinelLogs(LogLevel);
";
        await cmd.ExecuteNonQueryAsync();

        // Safe migration for existing tables
        try
        {
            using var alterCmd1 = conn.CreateCommand();
            alterCmd1.CommandText = "ALTER TABLE IncidentClusters ADD COLUMN TagsJson TEXT;";
            await alterCmd1.ExecuteNonQueryAsync();
        }
        catch { }

        try
        {
            using var alterCmd2 = conn.CreateCommand();
            alterCmd2.CommandText = "ALTER TABLE IncidentClusters ADD COLUMN IsAnomalySpike INTEGER DEFAULT 0;";
            await alterCmd2.ExecuteNonQueryAsync();
        }
        catch { }

        await SeedDefaultDataAsync(conn);
    }

    private async Task SeedDefaultDataAsync(SqliteConnection conn)
    {
        using var countCmd = conn.CreateCommand();
        countCmd.CommandText = "SELECT COUNT(*) FROM SentinelProjects;";
        var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
        if (count == 0)
        {
            var p1 = new SentinelProject
            {
                Id = "proj_nexus_rag",
                Name = "NexusRAG Studio",
                Description = "AI 문서 검색 및 색인 RAG 시스템 관제",
                ApiKey = "sec_nexus_rag_key_2026",
                Environment = "Production"
            };
            var p2 = new SentinelProject
            {
                Id = "proj_sap_dx",
                Name = "SAP Cloud DX Engine",
                Description = "ERP 클라우드 트랜잭션 및 배치 파이프라인 관제",
                ApiKey = "sec_sap_cloud_key_2026",
                Environment = "Production"
            };
            var p3 = new SentinelProject
            {
                Id = "proj_sto_platform",
                Name = "STO Digital Asset Platform",
                Description = "토큰증권 블록체인 노드 및 스마트 컨트랙트 관제",
                ApiKey = "sec_sto_asset_key_2026",
                Environment = "Staging"
            };

            foreach (var p in new[] { p1, p2, p3 })
            {
                using var ins = conn.CreateCommand();
                ins.CommandText = @"INSERT INTO SentinelProjects (Id, Name, Description, ApiKey, Environment, CreatedAtUtc)
                                    VALUES (@Id, @Name, @Description, @ApiKey, @Environment, @CreatedAtUtc);";
                ins.Parameters.AddWithValue("@Id", p.Id);
                ins.Parameters.AddWithValue("@Name", p.Name);
                ins.Parameters.AddWithValue("@Description", p.Description);
                ins.Parameters.AddWithValue("@ApiKey", p.ApiKey);
                ins.Parameters.AddWithValue("@Environment", p.Environment);
                ins.Parameters.AddWithValue("@CreatedAtUtc", p.CreatedAtUtc.ToString("o"));
                await ins.ExecuteNonQueryAsync();
            }
        }
    }

    public async Task<List<SentinelProject>> GetProjectsAsync()
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Description, ApiKey, Environment, CreatedAtUtc FROM SentinelProjects ORDER BY CreatedAtUtc ASC;";
        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<SentinelProject>();
        while (await reader.ReadAsync())
        {
            list.Add(new SentinelProject
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                ApiKey = reader.GetString(3),
                Environment = reader.GetString(4),
                CreatedAtUtc = DateTime.Parse(reader.GetString(5))
            });
        }
        return list;
    }

    public async Task<SentinelProject?> GetProjectByIdAsync(string projectId)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Description, ApiKey, Environment, CreatedAtUtc FROM SentinelProjects WHERE Id = @Id;";
        cmd.Parameters.AddWithValue("@Id", projectId);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new SentinelProject
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                ApiKey = reader.GetString(3),
                Environment = reader.GetString(4),
                CreatedAtUtc = DateTime.Parse(reader.GetString(5))
            };
        }
        return null;
    }

    public async Task<SentinelProject?> GetProjectByApiKeyAsync(string apiKey)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name, Description, ApiKey, Environment, CreatedAtUtc FROM SentinelProjects WHERE ApiKey = @ApiKey;";
        cmd.Parameters.AddWithValue("@ApiKey", apiKey);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new SentinelProject
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1),
                Description = reader.IsDBNull(2) ? "" : reader.GetString(2),
                ApiKey = reader.GetString(3),
                Environment = reader.GetString(4),
                CreatedAtUtc = DateTime.Parse(reader.GetString(5))
            };
        }
        return null;
    }

    public async Task<SentinelProject> CreateProjectAsync(string name, string description, string environment)
    {
        var project = new SentinelProject
        {
            Name = name,
            Description = description,
            Environment = environment
        };
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT INTO SentinelProjects (Id, Name, Description, ApiKey, Environment, CreatedAtUtc)
                            VALUES (@Id, @Name, @Description, @ApiKey, @Environment, @CreatedAtUtc);";
        cmd.Parameters.AddWithValue("@Id", project.Id);
        cmd.Parameters.AddWithValue("@Name", project.Name);
        cmd.Parameters.AddWithValue("@Description", project.Description);
        cmd.Parameters.AddWithValue("@ApiKey", project.ApiKey);
        cmd.Parameters.AddWithValue("@Environment", project.Environment);
        cmd.Parameters.AddWithValue("@CreatedAtUtc", project.CreatedAtUtc.ToString("o"));
        await cmd.ExecuteNonQueryAsync();
        return project;
    }

    public async Task DeleteProjectAsync(string projectId)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM SentinelProjects WHERE Id = @Id;";
        cmd.Parameters.AddWithValue("@Id", projectId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task IngestLogsAsync(List<LogEntry> logs)
    {
        if (logs.Count == 0) return;
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var tx = conn.BeginTransaction();

        foreach (var log in logs)
        {
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"INSERT INTO SentinelLogs (Id, ProjectId, IncidentId, LogLevel, Message, ExceptionType, StackTrace, Source, HostName, Environment, Fingerprint, MetadataJson, TimestampUtc)
                                VALUES (@Id, @ProjectId, @IncidentId, @LogLevel, @Message, @ExceptionType, @StackTrace, @Source, @HostName, @Environment, @Fingerprint, @MetadataJson, @TimestampUtc);";
            cmd.Parameters.AddWithValue("@Id", log.Id);
            cmd.Parameters.AddWithValue("@ProjectId", log.ProjectId);
            cmd.Parameters.AddWithValue("@IncidentId", (object?)log.IncidentId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LogLevel", log.LogLevel);
            cmd.Parameters.AddWithValue("@Message", log.Message);
            cmd.Parameters.AddWithValue("@ExceptionType", (object?)log.ExceptionType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@StackTrace", (object?)log.StackTrace ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Source", (object?)log.Source ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@HostName", (object?)log.HostName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Environment", (object?)log.Environment ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Fingerprint", (object?)log.Fingerprint ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@MetadataJson", (object?)log.MetadataJson ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@TimestampUtc", log.TimestampUtc.ToString("o"));
            await cmd.ExecuteNonQueryAsync();
        }
        await tx.CommitAsync();
    }

    public async Task<List<LogEntry>> GetLogsAsync(string? projectId, string? logLevel, int limit = 100)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();

        var query = @"SELECT l.Id, l.ProjectId, p.Name as ProjectName, l.IncidentId, l.LogLevel, l.Message, l.ExceptionType, l.StackTrace, l.Source, l.HostName, l.Environment, l.Fingerprint, l.MetadataJson, l.TimestampUtc
                      FROM SentinelLogs l
                      LEFT JOIN SentinelProjects p ON l.ProjectId = p.Id
                      WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(projectId))
        {
            query += " AND l.ProjectId = @ProjectId";
            cmd.Parameters.AddWithValue("@ProjectId", projectId);
        }
        if (!string.IsNullOrWhiteSpace(logLevel))
        {
            query += " AND l.LogLevel = @LogLevel";
            cmd.Parameters.AddWithValue("@LogLevel", logLevel);
        }
        query += " ORDER BY l.TimestampUtc DESC LIMIT @Limit;";
        cmd.Parameters.AddWithValue("@Limit", limit);

        cmd.CommandText = query;
        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<LogEntry>();
        while (await reader.ReadAsync())
        {
            list.Add(new LogEntry
            {
                Id = reader.GetString(0),
                ProjectId = reader.GetString(1),
                ProjectName = reader.IsDBNull(2) ? "미지정 프로젝트" : reader.GetString(2),
                IncidentId = reader.IsDBNull(3) ? null : reader.GetString(3),
                LogLevel = reader.GetString(4),
                Message = reader.GetString(5),
                ExceptionType = reader.IsDBNull(6) ? null : reader.GetString(6),
                StackTrace = reader.IsDBNull(7) ? null : reader.GetString(7),
                Source = reader.IsDBNull(8) ? null : reader.GetString(8),
                HostName = reader.IsDBNull(9) ? null : reader.GetString(9),
                Environment = reader.IsDBNull(10) ? null : reader.GetString(10),
                Fingerprint = reader.IsDBNull(11) ? null : reader.GetString(11),
                MetadataJson = reader.IsDBNull(12) ? null : reader.GetString(12),
                TimestampUtc = DateTime.Parse(reader.GetString(13))
            });
        }
        return list;
    }

    public async Task<IncidentCluster> UpsertIncidentClusterAsync(IncidentCluster incident)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        // Calculate Anomaly Spike (5-minute rolling window)
        var fiveMinAgo = DateTime.UtcNow.AddMinutes(-5).ToString("o");
        using var spikeCmd = conn.CreateCommand();
        spikeCmd.CommandText = "SELECT COUNT(*) FROM SentinelLogs WHERE ProjectId = @ProjectId AND Fingerprint = @Fingerprint AND TimestampUtc >= @FiveMinAgo;";
        spikeCmd.Parameters.AddWithValue("@ProjectId", incident.ProjectId);
        spikeCmd.Parameters.AddWithValue("@Fingerprint", incident.Fingerprint);
        spikeCmd.Parameters.AddWithValue("@FiveMinAgo", fiveMinAgo);
        var recentCount = Convert.ToInt32(await spikeCmd.ExecuteScalarAsync()) + 1;
        incident.RecentVelocityPerMin = Math.Round(recentCount / 5.0, 1);
        incident.IsAnomalySpike = recentCount >= 5;

        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT Id, OccurrenceCount, FirstSeenUtc, DiagnosisJson, TagsJson FROM IncidentClusters WHERE ProjectId = @ProjectId AND Fingerprint = @Fingerprint;";
        checkCmd.Parameters.AddWithValue("@ProjectId", incident.ProjectId);
        checkCmd.Parameters.AddWithValue("@Fingerprint", incident.Fingerprint);

        using var reader = await checkCmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var existingId = reader.GetString(0);
            var existingCount = reader.GetInt32(1);
            var firstSeen = DateTime.Parse(reader.GetString(2));
            var diagJson = reader.IsDBNull(3) ? null : reader.GetString(3);
            var existingTagsJson = reader.IsDBNull(4) ? null : reader.GetString(4);
            reader.Close();

            incident.Id = existingId;
            incident.OccurrenceCount = existingCount + incident.OccurrenceCount;
            incident.FirstSeenUtc = firstSeen;
            if (!string.IsNullOrWhiteSpace(diagJson))
            {
                try { incident.Diagnosis = JsonSerializer.Deserialize<AiDiagnosis>(diagJson); } catch {}
            }
            if (!string.IsNullOrWhiteSpace(existingTagsJson) && (incident.Tags == null || incident.Tags.Count == 0))
            {
                try { incident.Tags = JsonSerializer.Deserialize<List<string>>(existingTagsJson) ?? new(); } catch {}
            }

            using var updateCmd = conn.CreateCommand();
            updateCmd.CommandText = @"UPDATE IncidentClusters
                                      SET OccurrenceCount = @OccurrenceCount, LastSeenUtc = @LastSeenUtc, Status = 'Open',
                                          TagsJson = @TagsJson, IsAnomalySpike = @IsAnomalySpike
                                      WHERE Id = @Id;";
            updateCmd.Parameters.AddWithValue("@OccurrenceCount", incident.OccurrenceCount);
            updateCmd.Parameters.AddWithValue("@LastSeenUtc", incident.LastSeenUtc.ToString("o"));
            updateCmd.Parameters.AddWithValue("@TagsJson", JsonSerializer.Serialize(incident.Tags));
            updateCmd.Parameters.AddWithValue("@IsAnomalySpike", incident.IsAnomalySpike ? 1 : 0);
            updateCmd.Parameters.AddWithValue("@Id", existingId);
            await updateCmd.ExecuteNonQueryAsync();
            return incident;
        }
        reader.Close();

        using var insertCmd = conn.CreateCommand();
        insertCmd.CommandText = @"INSERT INTO IncidentClusters (Id, ProjectId, Fingerprint, Title, ExceptionType, Severity, Status, OccurrenceCount, FirstSeenUtc, LastSeenUtc, SampleStackTrace, SampleMessage, DiagnosisJson, TagsJson, IsAnomalySpike)
                                  VALUES (@Id, @ProjectId, @Fingerprint, @Title, @ExceptionType, @Severity, @Status, @OccurrenceCount, @FirstSeenUtc, @LastSeenUtc, @SampleStackTrace, @SampleMessage, @DiagnosisJson, @TagsJson, @IsAnomalySpike);";
        insertCmd.Parameters.AddWithValue("@Id", incident.Id);
        insertCmd.Parameters.AddWithValue("@ProjectId", incident.ProjectId);
        insertCmd.Parameters.AddWithValue("@Fingerprint", incident.Fingerprint);
        insertCmd.Parameters.AddWithValue("@Title", incident.Title);
        insertCmd.Parameters.AddWithValue("@ExceptionType", incident.ExceptionType);
        insertCmd.Parameters.AddWithValue("@Severity", incident.Severity);
        insertCmd.Parameters.AddWithValue("@Status", incident.Status);
        insertCmd.Parameters.AddWithValue("@OccurrenceCount", incident.OccurrenceCount);
        insertCmd.Parameters.AddWithValue("@FirstSeenUtc", incident.FirstSeenUtc.ToString("o"));
        insertCmd.Parameters.AddWithValue("@LastSeenUtc", incident.LastSeenUtc.ToString("o"));
        insertCmd.Parameters.AddWithValue("@SampleStackTrace", incident.SampleStackTrace);
        insertCmd.Parameters.AddWithValue("@SampleMessage", (object?)incident.SampleMessage ?? DBNull.Value);
        insertCmd.Parameters.AddWithValue("@DiagnosisJson", incident.Diagnosis != null ? JsonSerializer.Serialize(incident.Diagnosis) : DBNull.Value);
        insertCmd.Parameters.AddWithValue("@TagsJson", JsonSerializer.Serialize(incident.Tags));
        insertCmd.Parameters.AddWithValue("@IsAnomalySpike", incident.IsAnomalySpike ? 1 : 0);
        await insertCmd.ExecuteNonQueryAsync();
        return incident;
    }

    public async Task<List<IncidentCluster>> GetIncidentsAsync(string? projectId, string? severity, string? status, string? search, string? tag = null)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();

        var query = @"SELECT i.Id, i.ProjectId, p.Name as ProjectName, p.Environment, i.Fingerprint, i.Title, i.ExceptionType, i.Severity, i.Status, i.OccurrenceCount, i.FirstSeenUtc, i.LastSeenUtc, i.SampleStackTrace, i.SampleMessage, i.DiagnosisJson, i.TagsJson, i.IsAnomalySpike
                      FROM IncidentClusters i
                      LEFT JOIN SentinelProjects p ON i.ProjectId = p.Id
                      WHERE 1=1";
        if (!string.IsNullOrWhiteSpace(projectId))
        {
            query += " AND i.ProjectId = @ProjectId";
            cmd.Parameters.AddWithValue("@ProjectId", projectId);
        }
        if (!string.IsNullOrWhiteSpace(severity))
        {
            query += " AND i.Severity = @Severity";
            cmd.Parameters.AddWithValue("@Severity", severity);
        }
        if (!string.IsNullOrWhiteSpace(status))
        {
            query += " AND i.Status = @Status";
            cmd.Parameters.AddWithValue("@Status", status);
        }
        if (!string.IsNullOrWhiteSpace(tag))
        {
            query += " AND i.TagsJson LIKE @Tag";
            cmd.Parameters.AddWithValue("@Tag", $"%{tag}%");
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query += " AND (i.Title LIKE @Search OR i.ExceptionType LIKE @Search OR i.SampleMessage LIKE @Search OR p.Name LIKE @Search)";
            cmd.Parameters.AddWithValue("@Search", $"%{search}%");
        }
        query += " ORDER BY i.LastSeenUtc DESC;";

        cmd.CommandText = query;
        using var reader = await cmd.ExecuteReaderAsync();
        var list = new List<IncidentCluster>();
        while (await reader.ReadAsync())
        {
            var item = new IncidentCluster
            {
                Id = reader.GetString(0),
                ProjectId = reader.GetString(1),
                ProjectName = reader.IsDBNull(2) ? "미지정 프로젝트" : reader.GetString(2),
                Environment = reader.IsDBNull(3) ? "Production" : reader.GetString(3),
                Fingerprint = reader.GetString(4),
                Title = reader.GetString(5),
                ExceptionType = reader.GetString(6),
                Severity = reader.GetString(7),
                Status = reader.GetString(8),
                OccurrenceCount = reader.GetInt32(9),
                FirstSeenUtc = DateTime.Parse(reader.GetString(10)),
                LastSeenUtc = DateTime.Parse(reader.GetString(11)),
                SampleStackTrace = reader.IsDBNull(12) ? "" : reader.GetString(12),
                SampleMessage = reader.IsDBNull(13) ? null : reader.GetString(13)
            };
            if (!reader.IsDBNull(14))
            {
                try { item.Diagnosis = JsonSerializer.Deserialize<AiDiagnosis>(reader.GetString(14)); } catch {}
            }
            if (!reader.IsDBNull(15))
            {
                try { item.Tags = JsonSerializer.Deserialize<List<string>>(reader.GetString(15)) ?? new(); } catch {}
            }
            item.IsAnomalySpike = !reader.IsDBNull(16) && reader.GetInt32(16) == 1;
            list.Add(item);
        }
        return list;
    }

    public async Task<IncidentCluster?> GetIncidentByIdAsync(string incidentId)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT i.Id, i.ProjectId, p.Name as ProjectName, p.Environment, i.Fingerprint, i.Title, i.ExceptionType, i.Severity, i.Status, i.OccurrenceCount, i.FirstSeenUtc, i.LastSeenUtc, i.SampleStackTrace, i.SampleMessage, i.DiagnosisJson, i.TagsJson, i.IsAnomalySpike
                            FROM IncidentClusters i
                            LEFT JOIN SentinelProjects p ON i.ProjectId = p.Id
                            WHERE i.Id = @Id;";
        cmd.Parameters.AddWithValue("@Id", incidentId);
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var item = new IncidentCluster
            {
                Id = reader.GetString(0),
                ProjectId = reader.GetString(1),
                ProjectName = reader.IsDBNull(2) ? "미지정 프로젝트" : reader.GetString(2),
                Environment = reader.IsDBNull(3) ? "Production" : reader.GetString(3),
                Fingerprint = reader.GetString(4),
                Title = reader.GetString(5),
                ExceptionType = reader.GetString(6),
                Severity = reader.GetString(7),
                Status = reader.GetString(8),
                OccurrenceCount = reader.GetInt32(9),
                FirstSeenUtc = DateTime.Parse(reader.GetString(10)),
                LastSeenUtc = DateTime.Parse(reader.GetString(11)),
                SampleStackTrace = reader.IsDBNull(12) ? "" : reader.GetString(12),
                SampleMessage = reader.IsDBNull(13) ? null : reader.GetString(13)
            };
            if (!reader.IsDBNull(14))
            {
                try { item.Diagnosis = JsonSerializer.Deserialize<AiDiagnosis>(reader.GetString(14)); } catch {}
            }
            if (!reader.IsDBNull(15))
            {
                try { item.Tags = JsonSerializer.Deserialize<List<string>>(reader.GetString(15)) ?? new(); } catch {}
            }
            item.IsAnomalySpike = !reader.IsDBNull(16) && reader.GetInt32(16) == 1;
            return item;
        }
        return null;
    }

    public async Task UpdateIncidentStatusAsync(string incidentId, string status)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE IncidentClusters SET Status = @Status WHERE Id = @Id;";
        cmd.Parameters.AddWithValue("@Status", status);
        cmd.Parameters.AddWithValue("@Id", incidentId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task SaveIncidentDiagnosisAsync(string incidentId, AiDiagnosis diagnosis)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE IncidentClusters SET DiagnosisJson = @DiagnosisJson, Severity = @Severity WHERE Id = @Id;";
        cmd.Parameters.AddWithValue("@DiagnosisJson", JsonSerializer.Serialize(diagnosis));
        cmd.Parameters.AddWithValue("@Severity", diagnosis.SeverityAssessment);
        cmd.Parameters.AddWithValue("@Id", incidentId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<AnalyticsMetrics> GetAnalyticsMetricsAsync(string? projectId)
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var metrics = new AnalyticsMetrics();

        // 1. Total logs
        using (var cmd = conn.CreateCommand())
        {
            var q = "SELECT COUNT(*), SUM(CASE WHEN LogLevel IN ('ERROR', 'FATAL') THEN 1 ELSE 0 END) FROM SentinelLogs WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(projectId)) { q += " AND ProjectId = @ProjectId"; cmd.Parameters.AddWithValue("@ProjectId", projectId); }
            cmd.CommandText = q;
            using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                metrics.TotalLogsIngested = r.GetInt32(0);
                var errorCount = r.IsDBNull(1) ? 0 : r.GetInt32(1);
                metrics.ErrorRatePercentage = metrics.TotalLogsIngested > 0 ? Math.Round((double)errorCount / metrics.TotalLogsIngested * 100, 1) : 0;
            }
        }

        // 2. Incident severity breakdown
        using (var cmd = conn.CreateCommand())
        {
            var q = "SELECT Severity, Status, COUNT(*) FROM IncidentClusters WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(projectId)) { q += " AND ProjectId = @ProjectId"; cmd.Parameters.AddWithValue("@ProjectId", projectId); }
            q += " GROUP BY Severity, Status;";
            cmd.CommandText = q;
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var sev = r.GetString(0);
                var st = r.GetString(1);
                var cnt = r.GetInt32(2);

                metrics.TotalIncidentsCount += cnt;
                if (sev == "Critical") metrics.CriticalIncidentsCount += cnt;
                if (sev == "High") metrics.HighIncidentsCount += cnt;
                if (st == "Resolved") metrics.ResolvedIncidentsCount += cnt;
            }
        }

        // 3. Top exceptions
        using (var cmd = conn.CreateCommand())
        {
            var q = "SELECT ExceptionType, COUNT(*) as cnt FROM IncidentClusters WHERE ExceptionType != ''";
            if (!string.IsNullOrWhiteSpace(projectId)) { q += " AND ProjectId = @ProjectId"; cmd.Parameters.AddWithValue("@ProjectId", projectId); }
            q += " GROUP BY ExceptionType ORDER BY cnt DESC LIMIT 5;";
            cmd.CommandText = q;
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                metrics.TopExceptions.Add(new TopExceptionMetric
                {
                    ExceptionType = r.GetString(0),
                    Count = r.GetInt32(1)
                });
            }
        }

        // 4. Hourly volume timeline
        using (var cmd = conn.CreateCommand())
        {
            var q = @"SELECT strftime('%Y-%m-%d %H:00', TimestampUtc) as hr,
                             SUM(CASE WHEN LogLevel IN ('ERROR', 'FATAL') THEN 1 ELSE 0 END) as errs,
                             COUNT(*) as total
                      FROM SentinelLogs WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(projectId)) { q += " AND ProjectId = @ProjectId"; cmd.Parameters.AddWithValue("@ProjectId", projectId); }
            q += " GROUP BY hr ORDER BY hr DESC LIMIT 12;";
            cmd.CommandText = q;
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                metrics.HourlyVolumes.Add(new HourlyVolumeMetric
                {
                    HourLabel = r.GetString(0),
                    ErrorCount = r.IsDBNull(1) ? 0 : r.GetInt32(1),
                    TotalCount = r.GetInt32(2)
                });
            }
            metrics.HourlyVolumes.Reverse();
        }

        // 5. Cross-project Health Comparison Matrix
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"SELECT p.Id, p.Name, p.Environment,
                                       COUNT(l.Id) as totalLogs,
                                       SUM(CASE WHEN l.LogLevel IN ('ERROR', 'FATAL') THEN 1 ELSE 0 END) as errorCount,
                                       COUNT(DISTINCT i.Id) as incidentCount
                                FROM SentinelProjects p
                                LEFT JOIN SentinelLogs l ON p.Id = l.ProjectId
                                LEFT JOIN IncidentClusters i ON p.Id = i.ProjectId
                                GROUP BY p.Id, p.Name, p.Environment;";
            using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                var errCount = r.IsDBNull(4) ? 0 : r.GetInt32(4);
                var incCount = r.IsDBNull(5) ? 0 : r.GetInt32(5);
                var health = errCount == 0 ? "Healthy" : errCount > 5 ? "Critical" : "Warning";
                metrics.ProjectHealths.Add(new ProjectHealthMetric
                {
                    ProjectId = r.GetString(0),
                    ProjectName = r.GetString(1),
                    Environment = r.GetString(2),
                    TotalLogs = r.GetInt32(3),
                    ErrorCount = errCount,
                    IncidentCount = incCount,
                    HealthStatus = health
                });
            }
        }

        return metrics;
    }
}
