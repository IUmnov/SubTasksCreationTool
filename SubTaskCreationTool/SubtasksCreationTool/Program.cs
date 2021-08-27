using System;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Channels;
using System.Threading.Tasks;
using OneInc.ProcessOne.Libs.JiraClient.Models;

namespace SubtasksCreationTool
{
    internal class Program
    {
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

                Console.Write("Input sprint id: ");
                int sprintId = Convert.ToInt32(Console.ReadLine());

                if (!await jiraClient.DoesSprintExistAsync(sprintId))
                {
                    throw new Exception("The requested sprint does not exist");
                }

                Console.WriteLine("Get info about sprint tasks...");

                var allSprintTasks = await jiraClient.GetSprintIssuesAsync(sprintId);

                Console.WriteLine("Filtering internal technical and simple tasks...");

                var tasksAndInternalTechTasks = JiraIssuesHepler.SelectTasksStoriesAndInternalTechTasks(allSprintTasks);

                var issuesHandler = new JiraIssuesHandler(jiraClient);

                Console.WriteLine("Filtering tasks without needed PU...");

                var tasksWithoutNeededPu = JiraIssuesHepler.GetTasksWithoutNeededPU(tasksAndInternalTechTasks);

                Console.WriteLine("Sending messages about adding PU...");

                await issuesHandler.SendCommentsAboutAddingPU(tasksWithoutNeededPu);

                var tasksWithNeededPu = tasksAndInternalTechTasks.Except(tasksWithoutNeededPu);

                Console.WriteLine("Loading full information about subtasks...");

                issuesHandler.GetFullInfoAboutSubtasks(tasksWithNeededPu);

                Console.WriteLine("Sending messages about updating PU and estimates...");

                await issuesHandler.SendCommentsAboutUpdateEstimates(tasksWithNeededPu);

                var needMoreInfoTasks = JiraIssuesHepler.GetTasksNeedMoreInfo(tasksWithNeededPu);

                var correctTasks = JiraIssuesHepler.ExcludeTasksWithSubtasks(tasksWithNeededPu);

                correctTasks = correctTasks.Except(needMoreInfoTasks);

                var correctTasksWithTypeOfSubtasksToCreate = issuesHandler.GetTypeOfSubtasksToCreate(correctTasks);

                Console.WriteLine("Creating subtasks...");

                await issuesHandler.CreateSubtasks(correctTasksWithTypeOfSubtasksToCreate);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Subtasks were successfully created!");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine(ex.Message);
            }
        }
    }
}
