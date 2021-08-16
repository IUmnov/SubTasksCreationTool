using System.Collections.Generic;
using OneInc.ProcessOne.Libs.JiraClient.Models;

namespace SubtasksCreationTool
{
    public class IssueDto
    {
        public Issue Issue { get; set; }
        public List<string> TypeOfSubtasksToCreate { get; set; }
    }
}