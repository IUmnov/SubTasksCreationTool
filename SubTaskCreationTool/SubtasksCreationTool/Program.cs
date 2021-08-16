using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OneInc.ProcessOne.Libs.JiraClient.Models;

namespace SubtasksCreationTool
{
    internal class Program
    {
        private const string TaskUrl = "https://oneinc.atlassian.net/browse/";
        private const string TroubleIssuesFileName = "TroubleIssues.txt";

        public static async Task Main(string[] args)
        {
            try
            {
                var validationSettingsResult = await AppSettingsValidator.ValidateSettings();

                if (!validationSettingsResult.IsValid)
                {
                    throw new Exception(validationSettingsResult.ErrorMessage);
                }

                var jiraClient = validationSettingsResult.JiraClient;

                await jiraClient.AddCommentAsync("PROC-171256", new Comment { Body = "Ivan Umnov, test comment" });

                Console.Write("Input sprint id: ");
                int sprintId = Convert.ToInt32(Console.ReadLine());

                if (!await jiraClient.DoesSprintExistAsync(sprintId))
                {
                    throw new Exception("The requested sprint does not exist");
                }

                var allSprintTasks = await jiraClient.GetSprintIssuesAsync(sprintId);

                var tasksAndInternalTechTasks = JiraIssuesHepler.SelectTasksAndInternalTechTasks(allSprintTasks);
                
                var issuesHandler = new JiraIssuesHandler(jiraClient);

                var tasksWithoutNeededPu = JiraIssuesHepler.GetTasksWithoutNeededPU(tasksAndInternalTechTasks);

                await issuesHandler.SendCommentsAboutAddingPU(tasksWithoutNeededPu);

                var tasksWithNeededPu = tasksAndInternalTechTasks.Except(tasksWithoutNeededPu);
                
                await issuesHandler.SendCommentsAboutUpdateRemaining(tasksWithNeededPu);

                var needMoreInfoTasks = JiraIssuesHepler.GetTasksNeedMoreInfo(tasksWithNeededPu);

                var correctTasks = JiraIssuesHepler.ExcludeTasksWithSubtasks(tasksWithNeededPu);

                correctTasks = correctTasks.Except(needMoreInfoTasks);

                var correctTasksWithTypeOfSubtasksToCreate = issuesHandler.GetTypeOfSubtasksToCreate(correctTasks);

                await issuesHandler.CreateSubtasks(correctTasksWithTypeOfSubtasksToCreate);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Subtasks were successfully created");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.Message);
            }
        }
    }
}
