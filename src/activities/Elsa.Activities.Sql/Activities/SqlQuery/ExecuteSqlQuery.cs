using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Sql.Factory;
using Elsa.Activities.Sql.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Elsa.Providers.WorkflowStorage;
using Elsa.Secrets.Extentions;
using Elsa.Secrets.Providers;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Sql.Activities
{
    /// <summary>
    /// Execute an SQL query on given database using connection string
    /// </summary>
    [Trigger(
        Category = "SQL",
        DisplayName = "Execute SQL Query",
        Description = "Execute given SQL query and returned execution result",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class ExecuteSqlQuery : Activity, IActivityPropertyOptionsProvider, IRuntimeSelectListProvider
    {
        /// <summary>
        /// Allowed databases to run SQL
        /// </summary>
        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Hint = "Allowed databases to run SQL.",
            Options = new[] { "", "MSSQLServer", "PostgreSql" },
            DefaultValue = "MSSQL Server",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Database { get; set; } = "MSSQL Server";

        /// <summary>
        /// SQl script to execute
        /// </summary>
        [ActivityInput(
            Hint = "SQL query to execute",
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Sql }
        )]
        public string Query { get; set; } = default!;

        [ActivityInput(
              UIHint = ActivityInputUIHints.Dropdown,
              Label = "Connection string",
              OptionsProvider = typeof(ExecuteSqlQuery),
              SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
           )]
        public string? CredentialString { get; set; }

        [ActivityOutput(DisableWorkflowProviderSelection = true, DefaultWorkflowStorageProvider = TransientWorkflowStorageProvider.ProviderName)]
        public DataSet? Output { get; set; }

        private readonly ISqlClientFactory _sqlClientFactory;
        private readonly ISecretsProvider _secretsProvider;

        public ExecuteSqlQuery(ISqlClientFactory sqlClientFactory, ISecretsProvider secretsProvider) 
        {
            _sqlClientFactory = sqlClientFactory;
            _secretsProvider = secretsProvider;
        }

        public object GetOptions(PropertyInfo property) => new RuntimeSelectListProviderSettings(GetType());

        public async ValueTask<SelectList> GetSelectListAsync(object? context = default, CancellationToken cancellationToken = default)
        {
            var secretsPostgre = await _secretsProvider.GetSecretsForSelectListAsync(SecretType.PostgreSql);
            var secretsMssql = await _secretsProvider.GetSecretsForSelectListAsync(SecretType.MsSql);

            var items = secretsMssql.Select(x => new SelectListItem(x.Item1, x.Item2)).ToList();
            items.AddRange(secretsPostgre.Select(x => new SelectListItem(x.Item1, x.Item2)).ToList());
            items.Insert(0, new SelectListItem("", "empty"));

            var list = new SelectList { Items = items };

            return list;
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => ExecuteQuery();

        private IActivityExecutionResult ExecuteQuery()
        {
            var sqlServerClient = _sqlClientFactory.CreateClient(new CreateSqlClientModel(Database, CredentialString));
            Output = sqlServerClient.ExecuteQuery(Query);

            return Done();
        }
    }
}