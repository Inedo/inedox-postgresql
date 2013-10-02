using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.PostgreSql
{
    internal sealed class PostgreSqlDatabaseProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtConnectionString;

        public override void BindToForm(ProviderBase extension)
        {
            this.EnsureChildControls();

            var postgreSql = (PostgreSqlDatabaseProvider)extension;
            this.txtConnectionString.Text = postgreSql.ConnectionString;
        }

        public override ProviderBase CreateFromForm()
        {
            this.EnsureChildControls();

            return new PostgreSqlDatabaseProvider
            {
                ConnectionString = this.txtConnectionString.Text
            };
        }

        protected override void CreateChildControls()
        {
            this.txtConnectionString = new ValidatingTextBox
            {
                Width = 300,
                Required = true,
                TextMode = TextBoxMode.MultiLine,
                Rows = 5
            };

            this.Controls.Add(
                new FormFieldGroup(
                    "Connection String",
                    "The connection string to the PostgreSQL database. The standard format for this is:<br /><br />"
                    + "<em>User ID=root; Password=myPassword; Host=localhost; Database=myDataBase;</em>",
                    false,
                    new StandardFormField(string.Empty, this.txtConnectionString)
                )
            );

            base.CreateChildControls();
        }
    }
}
