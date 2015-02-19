using System;
using System.Data;
using Inedo.BuildMaster.Extensibility.Providers.Database;

namespace Inedo.BuildMasterExtensions.PostgreSql
{
    [Serializable]
    public sealed class PostgreSqlChangeScript : ChangeScript
    {
        public PostgreSqlChangeScript(DataRow dr)
            : base((long)dr["numeric_release_number"], (int)dr["script_id"], (string)dr["script_name"], (DateTime)dr["executed_date"], dr["success_indicator"].ToString() == "Y")
        {
        }
    }
}
