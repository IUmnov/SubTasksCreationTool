using OneInc.ProcessOne.Libs.JiraClient;
using IJiraClientSettingsProvider = OneInc.ProcessOne.Libs.JiraClient.IJiraClientSettingsProvider;

namespace OneInc.PortalOne.Utils.Common.SubtasksCreationTool
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
