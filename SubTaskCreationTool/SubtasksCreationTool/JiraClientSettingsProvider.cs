using IJiraClientSettingsProvider = OneInc.ProcessOne.Libs.JiraClient.IJiraClientSettingsProvider;

namespace SubtasksCreationTool
{
    public class JiraClientSettingsProvider : IJiraClientSettingsProvider
    {
        public string Url { get;  }

        public string Login { get;  }

        public string ApiToken { get;  }

        public JiraClientSettingsProvider(string url, string login, string apiToken)
        {
            Url = url;
            Login = login;
            ApiToken = apiToken;
        }
    }
}
