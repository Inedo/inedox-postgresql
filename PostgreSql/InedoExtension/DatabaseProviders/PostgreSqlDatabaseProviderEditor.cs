using Inedo.BuildMaster.Extensibility.DatabaseConnections;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.Extensions.PostgreSql
{
    internal sealed class PostgreSqlDatabaseProviderEditor : DatabaseConnectionEditorBase
    {
        private ValidatingTextBox txtConnectionString;

        public override void BindToForm(DatabaseConnection extension)
        {
            var postgreSql = (PostgreSqlDatabaseProvider)extension;
            this.txtConnectionString.Text = postgreSql.ConnectionString;
        }
        public override DatabaseConnection CreateFromForm()
        {
            return new PostgreSqlDatabaseProvider
            {
                ConnectionString = this.txtConnectionString.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtConnectionString = new ValidatingTextBox
            {
                Required = true,
                DefaultText = "ex: User ID=posgres; Password=myPassword; Host=localhost; Database=myDataBase;"
            };

            this.Controls.Add(new SlimFormField("Connection string:", this.txtConnectionString));

            base.CreateChildControls();
        }
    }
}
