using System.Collections.Generic;
using OneInc.ProcessOne.Libs.JiraClient.Models;

namespace OneInc.PortalOne.Utils.Common.SubtasksCreationTool
{
    public interface IJiraSubtasksCreator
    {
        void CreateSubtasks(IEnumerable<Issue> issues);
    }
}
