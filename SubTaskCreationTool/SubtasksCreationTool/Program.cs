using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
                var validationSettingsResult = AppSettingsValidator.ValidateSettings();

                if (!validationSettingsResult.IsValid)
                {
                    throw new Exception(validationSettingsResult.ErrorMessage);
                }

                var jiraClient = validationSettingsResult.JiraClient;

                Console.Write("Input sprint id: ");
                int sprintId = Convert.ToInt32(Console.ReadLine());

                if (!jiraClient.IsTheSprintExist(sprintId))
                {
                    throw new Exception("The requested sprint does not exist");
                }

                var allSprintTasks = await jiraClient.GetSprintTasksAsync(sprintId);

                var tasksAndInternalTechTasks = JiraIssuesHepler.SelectTasksAndInternalTechTasks(allSprintTasks);

                var tasksWithoutNeededPu = JiraIssuesHepler.GetTasksWithoutNeededPU(tasksAndInternalTechTasks);

                var tasksWithNeededPu = tasksAndInternalTechTasks.Except(tasksWithoutNeededPu);

                var needMoreInfoTasks = JiraIssuesHepler.GetTasksNeedMoreInfo(tasksWithNeededPu);

                var correctTasks = JiraIssuesHepler.ExcludeTasksWithSubtasks(tasksWithNeededPu);

                correctTasks = correctTasks.Except(needMoreInfoTasks);

                var issuesHandler = new JiraIssuesHandler(jiraClient);

                await issuesHandler.CreateSubtasks(correctTasks);

                using (StreamWriter sw = new StreamWriter($"{Directory.GetCurrentDirectory()}/{TroubleIssuesFileName}"))
                {
                    await sw.WriteLineAsync("Please set the PU for these tasks:");

                    foreach (var issue in tasksWithoutNeededPu)
                    {
                        await sw.WriteLineAsync($"{TaskUrl}{issue.Key}");
                    }

                    await sw.WriteLineAsync();

                    await sw.WriteLineAsync("These tasks are in the status Need more info:");

                    foreach (var issue in needMoreInfoTasks)
                    {
                        await sw.WriteLineAsync($"{TaskUrl}{issue.Key}");
                    }
                }

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
