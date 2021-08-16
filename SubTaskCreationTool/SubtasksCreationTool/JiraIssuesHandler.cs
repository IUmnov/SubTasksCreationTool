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
        private const string TaskUrl = "https://oneinc.atlassian.net/browse/";
        private const string InternalTechnicalTaskType = "Internal Technical Task";
        private const string OpenStatus = "Open";
        private const string TechAnalysisReviewStatus = "Technical Analysis Review";
        private const string QaSubTaskType = "QA Sub-task";
        private const string SubTaskType = "Sub-task";
        private const string IlyaShalin = "Ilya Shalin";
        private const int TechnicalAnalysisNeededIdFromOpen = 11;
        private const int TechnicalAnalysisId = 621;
        private const int TechnicalAnalysisReviewId = 21;
        private readonly Customer[] NaCustomer = new Customer[] {new Customer {Id = "15500", Value = "N/A"}};
        
        private const int HoursInPu = 6;

        private readonly IJiraClient _jiraClient;

        public JiraIssuesHandler(IJiraClient jiraClient)
        {
            _jiraClient = jiraClient;
        }

        public async Task CreateSubtasks(IEnumerable<IssueDto> parentIssues)
        {
            foreach (var issueDto in parentIssues)
            {
                var issue = issueDto.Issue;
                var typeOfSubtasksToCreate = issueDto.TypeOfSubtasksToCreate;

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
                        new IssueFields { Customer = NaCustomer });
                }
                else if (issue.Fields.Status.Name == TechAnalysisReviewStatus)
                {
                    changedTechReviewStatus = true;
                    
                    await _jiraClient.TransitIssueWithFieldsAsync(issueId, TechnicalAnalysisId,
                        new IssueFields { Customer = NaCustomer });
                }

                await ResetTheIssueOriginalEstimate(issue);

                string titleDevTask = "[Dev] Implementation of ";
                
                foreach (var typeOfSubtask in typeOfSubtasksToCreate)
                {
                    if (typeOfSubtask == QaSubTaskType)
                    {
                        await CreateSubtask(issueKey, $"[QA] Testing of {mainTaskSummary}",
                            issue.Fields.QaPreliminaryUnits.Value, QaSubTaskType, projectKey);
                    }
                    else
                    {
                        if (mainTaskSummary.Contains("imlementation", StringComparison.OrdinalIgnoreCase)|| mainTaskSummary.Contains("[Dev]", StringComparison.OrdinalIgnoreCase))
                        {
                            titleDevTask = string.Empty;
                        }
                        await CreateSubtask(issueKey, $"{titleDevTask}{mainTaskSummary}",
                            issue.Fields.DevPreliminaryUnits.Value, SubTaskType, projectKey);
                    }
                }

                if (changedTechReviewStatus)
                {
                    await _jiraClient.TransitIssueAsync(issue.Id, TechnicalAnalysisReviewId);
                }
            }
        }

        public IEnumerable<IssueDto> GetTypeOfSubtasksToCreate(IEnumerable<Issue> issues)
        {
            List<IssueDto> result = new List<IssueDto>();
            
            foreach (var issue in issues)
            {
                var typeOfSubtasksToCreate =  new string[] { QaSubTaskType, SubTaskType }.ToList();
                var existingTypeOfSubtasks = issue.Fields.Subtasks.Select(i => i.Fields.IssueType.Name).ToList();
                typeOfSubtasksToCreate = typeOfSubtasksToCreate.Except(existingTypeOfSubtasks).ToList();

                if (issue.Fields.IssueType.Name == InternalTechnicalTaskType)
                {
                    if (issue.Fields.QaPreliminaryUnits == null || issue.Fields.QaPreliminaryUnits == 0)
                    {
                        typeOfSubtasksToCreate.Remove(QaSubTaskType);
                    }
                }
                
                if (issue.Fields.QaPreliminaryUnits.HasValue && issue.Fields.QaPreliminaryUnits == 0)
                {
                    typeOfSubtasksToCreate.Remove(QaSubTaskType);
                }
                
                result.Add(new IssueDto { Issue = issue, TypeOfSubtasksToCreate = typeOfSubtasksToCreate });
            }
            
            return result;
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

        public async Task SendCommentsAboutAddingPU(IEnumerable<Issue> tasksWithoutNeededPu)
        {
            foreach (Issue issue in tasksWithoutNeededPu)
            {
                if (issue.Fields.DevPreliminaryUnits == null || issue.Fields.DevPreliminaryUnits == 0)
                {
                    await _jiraClient.AddCommentAsync(issue.Key, new Comment{ Body = $"{issue.Fields.ResponsibleDev}, please add PU for this task: {TaskUrl}{issue.Key}"}); 
                }

                if (issue.Fields.IssueType.Name == InternalTechnicalTaskType)
                {
                    if (issue.Fields.QaPreliminaryUnits == null)
                    {
                        await _jiraClient.AddCommentAsync(issue.Key,
                            new Comment {Body = $"{IlyaShalin}, please add PU for this task: {TaskUrl}{issue.Key}"});
                    }
                }
                else
                {
                    if (issue.Fields.QaPreliminaryUnits == null || issue.Fields.QaPreliminaryUnits == 0)
                    {
                        await _jiraClient.AddCommentAsync(issue.Key,
                            new Comment {Body = $"{IlyaShalin}, please add PU for this task: {TaskUrl}{issue.Key}"});
                    }
                }
            }
        }

        public async Task SendCommentsAboutUpdateRemaining(IEnumerable<Issue> issues)
        {
            foreach (var issue in issues)
            {
                var existingDevSubtasks = issue.Fields.Subtasks.Where(i => i.Fields.IssueType.Name == TaskType);
                if (existingDevSubtasks.Count() == 0)
                {
                    int sumEstimate = existingDevSubtasks.Select(s => s.Fields.TimeTracking.RemainingEstimateSeconds)
                        .Sum();
                    if (sumEstimate != (int) Math.Truncate(issue.Fields.DevPreliminaryUnits.Value * HoursInPu * 3600))
                    {
                        await _jiraClient.AddCommentAsync(issue.Key,
                            new Comment
                            {
                                Body =
                                    $"{issue.Fields.ResponsibleDev}, sum of PU and estimates do not match, update these please: {TaskUrl}{issue.Key}"
                            });
                    }
                }

                var existingQaSubtasks = issue.Fields.Subtasks.Where(i => i.Fields.IssueType.Name == QaSubTaskType);
                if (existingQaSubtasks.Count() == 0)
                {
                    int sumEstimate = existingQaSubtasks.Select(s => s.Fields.TimeTracking.RemainingEstimateSeconds)
                        .Sum();
                    if (sumEstimate != (int) Math.Truncate(issue.Fields.QaPreliminaryUnits.Value * HoursInPu * 3600))
                    {
                        await _jiraClient.AddCommentAsync(issue.Key,
                            new Comment
                            {
                                Body =
                                    $"{IlyaShalin}, sum of PU and estimates do not match, update these please: {TaskUrl}{issue.Key}"
                            });
                    }
                }
                
            }
        }
    }
}
