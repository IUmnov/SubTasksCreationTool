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

        private readonly IJiraClient _jiraClient;
        
        private JiraUser IlyaShalin = new JiraUser()
            {Id = "60d558a5dae56700681208f6", Name = "Ivan Umnov"};
        
        private Dictionary<RequestType, string> _requests = new Dictionary<RequestType, string>()
        {
            {
                RequestType.AddPU, "please add PU for this task"
            },
            {
                RequestType.UpdatePu, "sum of PU and estimates do not match, update these please"
            }
        };

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
                
                await ResetTheIssueOriginalEstimate(issue);

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

                string titleDevTask = "[Dev] Implementation of ";
                
                foreach (var typeOfSubtask in typeOfSubtasksToCreate)
                {
                    if (typeOfSubtask == QaSubTaskType)
                    {
                        Console.WriteLine($"Creating a QA-subtask for {issueKey}");
                        await CreateSubtask(issueKey, $"[QA] Testing of {mainTaskSummary}",
                            issue.Fields.QaPreliminaryUnits.Value, QaSubTaskType, projectKey);
                    }
                    else
                    {
                        if (mainTaskSummary.Contains("implement", StringComparison.OrdinalIgnoreCase) || mainTaskSummary.Contains("[Dev]", StringComparison.OrdinalIgnoreCase) || mainTaskSummary.Contains("investigat", StringComparison.OrdinalIgnoreCase))
                        {
                            titleDevTask = string.Empty;
                        }
                        Console.WriteLine($"Creating a Dev-subtask for {issueKey}");
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

        public async Task SendCommentsAboutAddingPU(IEnumerable<Issue> tasksWithoutNeededPu)
        {
            foreach (Issue issue in tasksWithoutNeededPu)
            {
                if (issue.Fields.DevPreliminaryUnits == null || issue.Fields.DevPreliminaryUnits == 0)
                {
                    if (issue.Fields.ResponsibleDev != null)
                    {
                        Console.WriteLine($"Sending mention to {issue.Fields.ResponsibleDev.Name} about adding Dev PU for {issue.Key}...");
                        await CreateComment(issue.Key, issue.Fields.ResponsibleDev, RequestType.AddPU);
                    }
                }

                if (issue.Fields.IssueType.Name == TaskType)
                {
                    if (issue.Fields.QaPreliminaryUnits == null)
                    {
                        Console.WriteLine($"Sending mention to {IlyaShalin.Name} about adding Dev PU for {issue.Key}...");
                        await CreateComment(issue.Key, IlyaShalin, RequestType.AddPU);
                    }
                }
            }
        }

        public async Task SendCommentsAboutUpdateEstimates(IEnumerable<Issue> issues)
        {
            foreach (var issue in issues)
            {
                var existingDevSubtasks = issue.Fields.Subtasks.Where(i => i.Fields.IssueType.Name == TaskType);
                if (existingDevSubtasks.Any())
                {
                    int sumEstimate = existingDevSubtasks.Select(s => s.Fields.TimeTracking.OriginalEstimateSeconds)
                        .Sum();
                    if (sumEstimate != (int) Math.Truncate(issue.Fields.DevPreliminaryUnits.Value * HoursInPu * 3600))
                    {
                        if (issue.Fields.ResponsibleDev != null)
                        {
                            Console.WriteLine($"Send mention to {issue.Fields.ResponsibleDev.Name} about updating Dev PU or estimates for {issue.Key}...");
                            await CreateComment(issue.Key, issue.Fields.ResponsibleDev, RequestType.UpdatePu);
                        }
                    }
                }
                
                var existingQaSubtasks = issue.Fields.Subtasks.Where(i => i.Fields.IssueType.Name == QaSubTaskType);
                if (existingQaSubtasks.Any())
                {
                    int sumEstimate = existingQaSubtasks.Select(s => s.Fields.TimeTracking.OriginalEstimateSeconds)
                        .Sum();
                    if (sumEstimate != (int) Math.Truncate(issue.Fields.QaPreliminaryUnits.Value * HoursInPu * 3600))
                    {
                        Console.WriteLine($"Send mention to {IlyaShalin.Name} about updating QA PU or estimates for {issue.Key}...");
                        await CreateComment(issue.Key, IlyaShalin, RequestType.UpdatePu);
                    }
                }
            }
        }
        
        private async Task  CreateComment(string issueKey, JiraUser user, RequestType requestType)
        {
            await _jiraClient.AddCommentAsync(issueKey, new Comment()
            {
                Body = new CommentBody()
                {
                    RootNodes = new RootNode[]
                    {
                        new RootNode
                        {
                            Type = "paragraph",
                            Content = new ChildNode[]
                            {
                                new MentionNode()
                                {
                                    Attributes = new MentionAttributes()
                                    {
                                        Id = user.Id,
                                        Text = user.Name
                                    }
                                },
                                new TextNode()
                                {
                                    Text = $", {_requests[requestType]}" 
                                }
                            }
                        }
                    }
                }
            });
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
            var builderTask = new NewIssueBuilder(_jiraClient, mainTaskSummary, subtaskType, projectKey);

            builderTask.WithParentIssue(mainTaskKey);
            string timeEstimate = GetOriginalEstimateToString(mainTaskPreliminaryUnits);
            builderTask.WithOriginalEstimate(timeEstimate);
            
            await builderTask.CreateAsync();
        }

        public void GetFullInfoAboutSubtasks(IEnumerable<Issue> issues)
        {
            foreach (var issue in issues)
            {
                var subtaskKeys = issue.Fields.Subtasks.Select(s => s.Key);
                issue.Fields.Subtasks = subtaskKeys.Select(s => _jiraClient.GetIssueAsync(s).Result).ToArray();
            }
        }
    }
}
