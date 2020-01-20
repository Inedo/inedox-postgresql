using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Inedo.Data;
using Inedo.Diagnostics;
using Inedo.Extensibility.DatabaseConnections;
using Inedo.Extensions.PostgreSql.Properties;
using Inedo.Serialization;
using Npgsql;

namespace Inedo.Extensions.PostgreSql
{
    [DisplayName("PostgreSQL")]
    [Description("Supports PostgreSQL 8.0 and later.")]
    [PersistFrom("Inedo.BuildMasterExtensions.PostgreSql.PostgreSqlDatabaseProvider,PostgreSql")]
    public sealed class PostgreSqlDatabaseProvider : DatabaseConnection, IChangeScriptExecuter
    {
        public static IEnumerable<Assembly> EnumerateChangeScripterAssemblies() => new[] { typeof(NpgsqlCommand).Assembly };

        private LazyDisposableAsync<NpgsqlConnection> connection;

        public PostgreSqlDatabaseProvider()
        {
            this.connection = new LazyDisposableAsync<NpgsqlConnection>(this.CreateConnection, this.CreateConnectionAsync);
        }

        public int MaxChangeScriptVersion => 1;

        public override Task ExecuteQueryAsync(string query, CancellationToken cancellationToken) => this.ExecuteNonQueryAsync(query, cancellationToken);
        public async Task InitializeDatabaseAsync(CancellationToken cancellationToken)
        {
            var state = await this.GetStateAsync(cancellationToken).ConfigureAwait(false);
            if (state.IsInitialized)
                return;

            await this.ExecuteNonQueryAsync(Resources.Initialize, cancellationToken).ConfigureAwait(false);
        }
        public Task UpgradeSchemaAsync(IReadOnlyDictionary<int, Guid> canoncialGuids, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
        public async Task<ChangeScriptState> GetStateAsync(CancellationToken cancellationToken)
        {
            var tableName = await this.ExecuteScalarAsync(
                "select table_name from information_schema.tables where table_name='__buildmaster_dbschemachanges'",
                cancellationToken
            ).ConfigureAwait(false) as string;

            if (tableName == null)
                return new ChangeScriptState(false);

            var records = await this.ExecuteDataTableAsync(
                "select * from __buildmaster_dbschemachanges",
                GetRecord,
                cancellationToken
            ).ConfigureAwait(false);

            return new ChangeScriptState(1, records);
        }
        public async Task ExecuteChangeScriptAsync(ChangeScriptId scriptId, string scriptName, string scriptText, CancellationToken cancellationToken)
        {
            var scripts = await this.GetStateAsync(cancellationToken).ConfigureAwait(false);
            if (scripts.Scripts.Any(s => scriptId.ScriptId == s.Id.ScriptId))
            {
                this.LogInformation(scriptName + " already executed. Skipping...");
                return;
            }

            bool success;
            try
            {
                await this.ExecuteNonQueryAsync(scriptText, cancellationToken).ConfigureAwait(false);
                success = true;
                this.LogInformation(scriptName + " executed successfully.");
            }
            catch (Exception ex)
            {
                success = false;
                this.LogError(scriptName + " failed: " + ex.Message);
            }

            await this.RecordResultAsync(scriptId, scriptName, success).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            this.connection.Dispose();
            base.Dispose(disposing);
        }

        private NpgsqlConnection CreateConnection()
        {
            var conn = new NpgsqlConnection(this.ConnectionString);
            conn.Open();
            return conn;
        }
        private async Task<NpgsqlConnection> CreateConnectionAsync()
        {
            var conn = new NpgsqlConnection(this.ConnectionString);
            await conn.OpenAsync().ConfigureAwait(false);
            return conn;
        }
        private async Task ExecuteNonQueryAsync(string query, CancellationToken cancellationToken)
        {
            using (var cmd = new NpgsqlCommand(query, await this.connection.ValueAsync.ConfigureAwait(false)))
            {
                cmd.CommandTimeout = 0;
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        private async Task<List<TRow>> ExecuteDataTableAsync<TRow>(string query, Func<NpgsqlDataReader, TRow> adapter, CancellationToken cancellationToken)
        {
            using (var cmd = new NpgsqlCommand(query, await this.connection.ValueAsync.ConfigureAwait(false)))
            {
                cmd.CommandTimeout = 0;
                using (var reader = (NpgsqlDataReader)await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                {
                    var results = new List<TRow>();
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        results.Add(adapter(reader));
                    }

                    return results;
                }
            }
        }
        private async Task<object> ExecuteScalarAsync(string query, CancellationToken cancellationToken)
        {
            using (var cmd = new NpgsqlCommand(query, await this.connection.ValueAsync.ConfigureAwait(false)))
            {
                cmd.CommandTimeout = 0;
                return await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        private static ChangeScriptExecutionRecord GetRecord(NpgsqlDataReader reader)
        {
            return new ChangeScriptExecutionRecord(
                new ChangeScriptId((int)reader["script_id"], (long)reader["numeric_release_number"]),
                (string)reader["script_name"],
                (DateTime)reader["executed_date"],
                (YNIndicator)(string)reader["success_indicator"]
            );
        }
        private Task RecordResultAsync(ChangeScriptId scriptId, string scriptName, bool success)
        {
            var query = "insert into __buildmaster_dbschemachanges"
                + " (numeric_release_number, script_id, script_name, executed_date, success_indicator)"
                + " values"
                + $" ({scriptId.LegacyReleaseSequence}, {scriptId.ScriptId}, '{scriptName.Replace("'", "''")}', now(), '{(YNIndicator)success}')";

            return this.ExecuteNonQueryAsync(query, CancellationToken.None);
        }
    }
}
