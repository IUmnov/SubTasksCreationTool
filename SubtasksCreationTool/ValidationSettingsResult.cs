using OneInc.ProcessOne.Libs.JiraClient;

namespace OneInc.PortalOne.Utils.Common.SubtasksCreationTool
{
    public class ValidationSettingsResult
    {
        public bool IsValid { get; set; }

        public string ErrorMessage { get; set; }

        public IJiraClient JiraClient { get; set; }
    }
}
