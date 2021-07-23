using System.Collections.Generic;
using System.Threading.Tasks;
using OneInc.ProcessOne.Libs.JiraClient.Models;

namespace SubtasksCreationTool
{
    public interface IJiraIssuesHandler
    {
        Task CreateSubtasks(IEnumerable<Issue> parentIssues);
    }
}
