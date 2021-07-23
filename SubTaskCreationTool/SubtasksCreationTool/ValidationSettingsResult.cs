using OneInc.ProcessOne.Libs.JiraClient;

namespace SubtasksCreationTool
{
    public class ValidationSettingsResult
    {
        public bool IsValid { get; set; }

        public string ErrorMessage { get; set; }

        public IJiraClient JiraClient { get; set; }
    }
}
