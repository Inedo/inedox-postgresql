using System;
using System.Data;
using Inedo.BuildMaster.Extensibility.Providers.Database;

namespace Inedo.BuildMasterExtensions.PostgreSql
{
    [Serializable]
    public sealed class PostgreSqlChangeScript : ChangeScript
    {
        public PostgreSqlChangeScript(DataRow dr)
            : base((long)dr["Numeric_Release_Number"], (int)dr["Script_Id"], (string)dr["Script_Name"], (DateTime)dr["Executed_Date"], dr["Success_Indicator"].ToString() == "Y")
        {
        }
    }
}
