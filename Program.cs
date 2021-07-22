using System;
using System.Composition.Hosting;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Microsoft.Extensions.Configuration;
using OneInc.ProcessOne.Libs.CommonHelpers;
using OneInc.ProcessOne.Libs.JiraClient;
using OneInc.ProcessOne.Libs.JiraClient.Models;
using ConfigurationBuilder = Microsoft.Extensions.Configuration.ConfigurationBuilder;

namespace OneInc.PortalOne.Utils.Common.SubtasksCreationTool
{
    internal class Program
    {
        private const string TaskUrl = "https://oneinc.atlassian.net/browse/";

        public static void Main(string[] args)
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

                var allSprintTasks = jiraClient.GetSprintTasksAsync(sprintId).Result;

                var tasksAndInternalTechTasks = JiraIssuesHepler.SelectTasksAndInternalTechTasks(allSprintTasks);

                var tasksWithoutNeededPu = JiraIssuesHepler.GetTasksWithoutNeededPU(tasksAndInternalTechTasks);

                var tasksWithNeededPu = tasksAndInternalTechTasks.Except(tasksWithoutNeededPu);

                var correctTasks = JiraIssuesHepler.ExcludeTasksWithSubtasks(tasksWithNeededPu);

                var subtasksCreator = new JiraSubtasksCreator(jiraClient);

                subtasksCreator.CreateSubtasks(correctTasks);
                using (StreamWriter sw = new StreamWriter($"{Directory.GetCurrentDirectory()}/IssuesWithoutPU.txt"))
                {
                    sw.WriteLine("Please set the PU for these tasks:");

                    foreach (var issue in tasksWithoutNeededPu)
                    {
                        sw.WriteLine($"{TaskUrl}{issue.Key}");
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
