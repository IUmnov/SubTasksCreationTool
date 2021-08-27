using System.Collections.Generic;
using System.Linq;
using OneInc.ProcessOne.Libs.JiraClient.Models;

namespace SubtasksCreationTool
{
    public static class JiraIssuesHepler
    {
        private const string TaskType = "Task";
        private const string NeedMoreInfoStatus = "Need more info";
        private const string InternalTechnicalTaskType = "Internal Technical Task";
        private const string StoryType = "Story";
        private const string QaSubTaskType = "QA Sub-task";
        private const string SubTaskType = "Sub-task";

        public static IEnumerable<Issue> SelectTasksStoriesAndInternalTechTasks(IEnumerable<Issue> issues)
        {
            return issues.Where(i => i.Fields.IssueType.Name == TaskType || i.Fields.IssueType.Name == InternalTechnicalTaskType || i.Fields.IssueType.Name == StoryType);
        }

        public static IEnumerable<Issue> GetTasksWithoutNeededPU(IEnumerable<Issue> issues)
        {
            return issues.Where(
                i => (i.Fields.IssueType.Name == TaskType && (i.Fields.DevPreliminaryUnits == null || i.Fields.QaPreliminaryUnits == null))
                    || (i.Fields.IssueType.Name == InternalTechnicalTaskType
                        && i.Fields.DevPreliminaryUnits == null));
        }
        
        public static IEnumerable<Issue> GetTasksNeedMoreInfo(IEnumerable<Issue> issues)
        {
            return issues.Where(i => i.Fields.Status.Name == NeedMoreInfoStatus );
        }
        public static IEnumerable<Issue> ExcludeTasksWithSubtasks(IEnumerable<Issue> issues)
        {
            return issues.Where(
                i => (i.Fields.IssueType.Name == TaskType
                        && (!i.Fields.Subtasks.Select(s => s.Fields.IssueType.Name).Contains(QaSubTaskType)
                            || !i.Fields.Subtasks.Select(s => s.Fields.IssueType.Name).Contains(SubTaskType)))
                    || (i.Fields.IssueType.Name == InternalTechnicalTaskType
                        && !i.Fields.Subtasks.Select(s => s.Fields.IssueType.Name).Contains(SubTaskType)));
        }
        
        public static IEnumerable<Issue> GetTasksWithQaSubtask(IEnumerable<Issue> issues)
        {
            return issues.Where(
                i => (i.Fields.Subtasks.Select(s => s.Fields.IssueType.Name).Contains(QaSubTaskType)));
        }
    }
}
