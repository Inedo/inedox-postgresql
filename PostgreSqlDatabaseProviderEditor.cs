using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.PostgreSql
{
    internal sealed class PostgreSqlDatabaseProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtConnectionString;

        public override void BindToForm(ProviderBase extension)
        {
            var postgreSql = (PostgreSqlDatabaseProvider)extension;
            this.txtConnectionString.Text = postgreSql.ConnectionString;
        }

        public override ProviderBase CreateFromForm()
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
