using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OneInc.ProcessOne.Libs.CommonHelpers;
using OneInc.ProcessOne.Libs.JiraClient;

namespace SubtasksCreationTool
{
    public static class AppSettingsValidator
    {
        public static async Task<ValidationSettingsResult> ValidateSettings()
        {
            var builder = new ConfigurationBuilder().SetBasePath($"{Directory.GetCurrentDirectory()}/Properties")
                .AddJsonFile("appsettings.json", optional: false);

            IConfiguration config = builder.Build();

            string url = config.GetSection("Url").Value;
            string login = config.GetSection("Login").Value;
            string apiToken = config.GetSection("ApiToken").Value;

            Guard.IsNotNullOrEmpty(url, nameof(url));
            Guard.IsNotNullOrEmpty(login, nameof(login));
            Guard.IsNotNullOrEmpty(apiToken, nameof(apiToken));

            var compositionContext = UtilsCompositionProvider.GlobalCompositionContext.Value;

            var jiraClientFactory = compositionContext.GetExport<IJiraClientFactory>();

            var jiraClient = jiraClientFactory.Create(new JiraClientSettingsProvider(url, login, apiToken));

            if (!await jiraClient.IsConnectionEstablishedAsync())
            {
                return new ValidationSettingsResult
                {
                    IsValid = false,
                    ErrorMessage = "Failed to authorize, check your login and api-token",
                };
            }

            return new ValidationSettingsResult
            {
                IsValid = true,
                JiraClient = jiraClient,
            };
        }
    }
}
