using System;
using System.Collections.Generic;
using System.Linq;
using OneInc.ProcessOne.Libs.JiraClient;
using OneInc.ProcessOne.Libs.JiraClient.Models;

namespace OneInc.PortalOne.Utils.Common.SubtasksCreationTool
{
    public class JiraSubtasksCreator : IJiraSubtasksCreator
    {
        private const string TaskType = "Task";
        private const string InternalTechnicalTaskType = "Internal Technical Task";
        private const string QaSubTaskType = "QA Sub-task";
        private const string SubTaskType = "Sub-task";
        private const int HoursInPu = 6;

        private IJiraClient _jiraClient;

        public JiraSubtasksCreator(IJiraClient jiraClient)
        {
            _jiraClient = jiraClient;
        }

        public void CreateSubtasks(IEnumerable<Issue> issues)
        {
            foreach (var issue in issues)
            {
                var typeOfSubtasksToCreate = new string[] { QaSubTaskType, SubTaskType }.ToList();
                var existingTypeOfSubtasks = issue.Fields.Subtasks.Select(i => i.Fields.IssueType.Name).ToList();
                typeOfSubtasksToCreate = typeOfSubtasksToCreate.Except(existingTypeOfSubtasks).ToList();

                if (issue.Fields.IssueType.Name == InternalTechnicalTaskType)
                {
                    if (issue.Fields.QaPreliminaryUnits == null)
                    {
                        typeOfSubtasksToCreate.Remove(QaSubTaskType);
                    }
                }

                string mainTaskSummary = issue.Fields.Summary;
                string projectKey = issue.Fields.Project.Key;
                string issueKey = issue.Key;

                foreach (var typeOfSubtask in typeOfSubtasksToCreate)
                {
                    if (typeOfSubtask == QaSubTaskType)
                    {
                        NewIssueBuilder builderQaTask = new NewIssueBuilder(_jiraClient, $"[QA] Testing of {mainTaskSummary}", QaSubTaskType, projectKey);

                        builderQaTask.WithParentIssue(issueKey);
                        string timeEstimate = GetOriginalEstimateToString(issue.Fields.QaPreliminaryUnits.Value);
                        builderQaTask.WithOriginalEstimate(timeEstimate);
                        builderQaTask.CreateAsync();
                    }
                    else
                    {
                        NewIssueBuilder builderDevTask = new NewIssueBuilder(_jiraClient, $"[Dev] Implementation of {mainTaskSummary}", SubTaskType, projectKey);

                        builderDevTask.WithParentIssue(issueKey);
                        string timeEstimate = GetOriginalEstimateToString(issue.Fields.DevPreliminaryUnits.Value);
                        builderDevTask.WithOriginalEstimate(timeEstimate);
                        builderDevTask.CreateAsync();
                    }
                }
            }
        }

        private static string GetOriginalEstimateToString(double originalEstimate)
        {
            double originalEstimateInDouble = HoursInPu * originalEstimate;
            int originalEstimateHours = (int)Math.Truncate(originalEstimateInDouble);
            double originalEstimateMinutes = (int)Math.Truncate((originalEstimateInDouble - originalEstimateHours) * 60);
            return $"{originalEstimateHours}h {originalEstimateMinutes}m";
        }
    }
}
