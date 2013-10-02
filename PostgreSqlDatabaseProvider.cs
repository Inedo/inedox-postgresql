using System;
using System.Data;
using System.Text;
using Devart.Data.PostgreSql;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.Database;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.PostgreSql
{
    [ProviderProperties("PostgreSQL", "Supports PostgreSQL 8.0 and later.", RequiresTransparentProxy = true)]
    [CustomEditor(typeof(PostgreSqlDatabaseProviderEditor))]
    public sealed class PostgreSqlDatabaseProvider : DatabaseProviderBase, IChangeScriptProvider
    {
        public PostgreSqlDatabaseProvider()
        {
        }

        public override bool IsAvailable()
        {
            return true;
        }
        public override void ValidateConnection()
        {
            this.ExecuteQuery("select 1");
        }
        public override void ExecuteQueries(string[] queries)
        {
            using (var cmd = this.CreateCommand(string.Empty))
            {
                try
                {
                    cmd.Connection.Open();
                    foreach (var sqlCommand in queries)
                    {
                        cmd.CommandText = sqlCommand;
                        cmd.ExecuteNonQuery();
                    }
                }
                finally
                {
                    cmd.Connection.Close();
                }
            }
        }
        public override void ExecuteQuery(string query)
        {
            this.ExecuteQueries(new[] { query });
        }
        public override string ToString()
        {
            try
            {
                var csb = new PgSqlConnectionStringBuilder(ConnectionString);
                var toString = new StringBuilder();
                if (!string.IsNullOrEmpty(csb.Database))
                    toString.Append("PostgreSQL database \"" + csb.Database + "\"");
                else
                    toString.Append("PostgreSQL");

                if (!string.IsNullOrEmpty(csb.Host))
                    toString.Append(" on host \"" + csb.Host + "\"");

                return toString.ToString();
            }
            catch
            {
                return "PostgreSQL";
            }
        }

        public void InitializeDatabase()
        {
            if (this.IsDatabaseInitialized())
                throw new InvalidOperationException("The database has already been initialized.");

            this.ExecuteQuery(Properties.Resources.Initialize);
        }
        public bool IsDatabaseInitialized()
        {
            this.ValidateConnection();

            var tables = this.ExecuteDataTable("select 1 from information_schema.tables where table_name='__buildmaster_dbschemachanges'");
            return tables.Rows.Count != 0;
        }
        public ChangeScript[] GetChangeHistory()
        {
            this.ValidateInitialization();

            var tables = this.ExecuteDataTable("select * from __buildmaster_dbschemachanges");
            var scripts = new PostgreSqlChangeScript[tables.Rows.Count];
            for (int i = 0; i < tables.Rows.Count; i++)
                scripts[i] = new PostgreSqlChangeScript(tables.Rows[i]);

            return scripts;
        }
        public long GetSchemaVersion()
        {
            this.ValidateInitialization();

            return (long)this.ExecuteDataTable(
                "select coalesce(max(numeric_release_number),0) from __buildmaster_dbschemachanges"
                ).Rows[0][0];
        }
        public ExecutionResult ExecuteChangeScript(long numericReleaseNumber, int scriptId, string scriptName, string scriptText)
        {
            this.ValidateInitialization();

            var tables = this.ExecuteDataTable("select * from __buildmaster_dbschemachanges");
            if (tables.Select("Script_Id=" + scriptId.ToString()).Length > 0)
                return new ExecutionResult(ExecutionResult.Results.Skipped, scriptName + " already executed.");

            Exception ex = null;
            try { this.ExecuteQuery(scriptText); }
            catch (Exception _ex) { ex = _ex; }

            this.ExecuteQuery(string.Format(
                "insert into __buildmaster_dbschemachanges "
                + " (numeric_release_number, script_id, script_name, executed_date, success_indicator) "
                + "values "
                + "({0}, {1}, '{2}', now(), '{3}')",
                numericReleaseNumber,
                scriptId,
                scriptName.Replace("'", "''"),
                ex == null ? "Y" : "N"));

            if (ex == null)
                return new ExecutionResult(ExecutionResult.Results.Success, scriptName + " executed successfully.");
            else
                return new ExecutionResult(ExecutionResult.Results.Failed, scriptName + " execution failed:" + ex.Message);
        }

        private PgSqlConnection CreateConnection()
        {
            var conStr = new PgSqlConnectionStringBuilder(this.ConnectionString)
            {
                Pooling = false,
                Protocol = ProtocolVersion.Ver20
            };

            return new PgSqlConnection(conStr.ToString());
        }
        private PgSqlCommand CreateCommand(string cmdText)
        {
            return new PgSqlCommand
            {
                CommandTimeout = 0,
                CommandText = cmdText,
                Connection = this.CreateConnection()
            };
        }
        private DataTable ExecuteDataTable(string sqlCommand)
        {
            var dt = new DataTable();
            using (var cmd = this.CreateCommand(string.Empty))
            {
                try
                {
                    cmd.Connection.Open();
                    cmd.CommandText = sqlCommand;
                    dt.Load(cmd.ExecuteReader());
                    return dt;
                }
                finally
                {
                    cmd.Connection.Close();
                }
            }
        }
        private void ValidateInitialization()
        {
            if (!this.IsDatabaseInitialized())
                throw new InvalidOperationException("The database has not been initialized.");
        }
    }
}
