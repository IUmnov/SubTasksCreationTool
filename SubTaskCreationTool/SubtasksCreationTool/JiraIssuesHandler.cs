using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OneInc.ProcessOne.Libs.JiraClient;
using OneInc.ProcessOne.Libs.JiraClient.Models;

namespace SubtasksCreationTool
{
    public class JiraIssuesHandler : IJiraIssuesHandler
    {
        private const string TaskType = "Task";
        private const string InternalTechnicalTaskType = "Internal Technical Task";
        private const string OpenStatus = "Open";
        private const string TechAnalysisReviewStatus = "Technical Analysis Review";
        private const string QaSubTaskType = "QA Sub-task";
        private const string SubTaskType = "Sub-task";
        private const int TechnicalAnalysisNeededIdFromOpen = 11;
        private const int TechnicalAnalysisId = 621;
        private const int TechnicalAnalysisReviewId = 21;
        private readonly Customer[] NaCustomer = new Customer[] {new Customer {Id = "15500", Value = "N/A"}};
        
        private const int HoursInPu = 6;

        private IJiraClient _jiraClient;

        public JiraIssuesHandler(IJiraClient jiraClient)
        {
            _jiraClient = jiraClient;
        }

        public async Task CreateSubtasks(IEnumerable<Issue> parentIssues)
        {
            foreach (var issue in parentIssues)
            {
                var typeOfSubtasksToCreate =  new string[] { QaSubTaskType, SubTaskType }.ToList();
                var existingTypeOfSubtasks = issue.Fields.Subtasks.Select(i => i.Fields.IssueType.Name).ToList();
                typeOfSubtasksToCreate = typeOfSubtasksToCreate.Except(existingTypeOfSubtasks).ToList();

                if (issue.Fields.IssueType.Name == InternalTechnicalTaskType)
                {
                    if (issue.Fields.QaPreliminaryUnits == null)
                    {
                        typeOfSubtasksToCreate.Remove(QaSubTaskType);
                    }
                }

                if (typeOfSubtasksToCreate.Count == 0)
                {
                    continue;
                }
                
                string mainTaskSummary = issue.Fields.Summary;
                string projectKey = issue.Fields.Project.Key;
                string issueKey = issue.Key;
                int issueId = issue.Id;
                
                bool changedTechReviewStatus = false;
                
                if (issue.Fields.Status.Name == OpenStatus)
                {
                    await _jiraClient.TransitIssueWithFieldsAsync(issueId, TechnicalAnalysisNeededIdFromOpen,
                        new IssueFields {Customer = NaCustomer});
                }
                else if (issue.Fields.Status.Name == TechAnalysisReviewStatus)
                {
                    changedTechReviewStatus = true;
                    
                    await _jiraClient.TransitIssueWithFieldsAsync(issueId, TechnicalAnalysisId,
                        new IssueFields {Customer = NaCustomer});
                }

                await ResetTheIssueOriginalEstimate(issue);

                foreach (var typeOfSubtask in typeOfSubtasksToCreate)
                {
                    if (typeOfSubtask == QaSubTaskType)
                    {
                        await CreateSubtask(issueKey, $"[QA] Testing of {mainTaskSummary}",
                            issue.Fields.QaPreliminaryUnits.Value, QaSubTaskType, projectKey);
                    }
                    else
                    {
                        await CreateSubtask(issueKey, $"[Dev] Implementation of {mainTaskSummary}",
                            issue.Fields.DevPreliminaryUnits.Value, SubTaskType, projectKey);
                    }
                }

                if (changedTechReviewStatus)
                {
                    await _jiraClient.TransitIssueAsync(issue.Id, TechnicalAnalysisReviewId);
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

        private async Task ResetTheIssueOriginalEstimate(Issue issue)
        {
            if (issue.Fields.TimeTracking.OriginalEstimate != "0h")
            {
                await _jiraClient.UpdateIssueAsync(issue.Key, new Issue{Fields = new IssueFields{TimeTracking = new TimeTracking {OriginalEstimate = "0h", TimeRemaining = "0h"}}});
            }
        }
        
        private async Task CreateSubtask(string mainTaskKey, string mainTaskSummary, double mainTaskPreliminaryUnits,
            string subtaskType, string projectKey)
        {
            NewIssueBuilder builderQaTask = new NewIssueBuilder(_jiraClient, mainTaskSummary, subtaskType, projectKey);

            builderQaTask.WithParentIssue(mainTaskKey);
            string timeEstimate = GetOriginalEstimateToString(mainTaskPreliminaryUnits);
            builderQaTask.WithOriginalEstimate(timeEstimate);
            
            await builderQaTask.CreateAsync();
        }
    }
}
