using System;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NUnit.Framework;
using OneInc.ProcessOne.Libs.JiraClient;
using OneInc.ProcessOne.Libs.JiraClient.Models;
using SubtasksCreationTool;

namespace SubTaskCreationToolTests
{
    [TestFixture]
    public class Tests
    {
        private IJiraClient _jiraClient;
        private IJiraIssuesHandler _subtasksCreator;
        
        private static Issue _taskIssueWithoutSubTasksAndWithPu;
        private static Issue _taskIssueWithoutDevSubTaskAndWithPuAndQaSubTask;
        private static Issue _internalTaskWithDevPuAndWithoutDevQaSubtasksAndQaPu;
        private static Issue _taskWithDevAndQaPuAndOriginalEstimate;
        private static Issue _internalTaskWithDevAndQaPuAndWithoutDevQaSubtasks;

        [SetUp]
        public void SetUp()
        {
            _jiraClient = Substitute.For<IJiraClient>();
            _subtasksCreator = new JiraIssuesHandler(_jiraClient);
            InitializeIssues();
        }
        
        [Test]
        public async Task CreateSubtasks_WhenTheTaskWithTaskTypeWithDevAndQAPuAndWithoutDevAndQASubtask_ShouldCreateQAAndDevSubtasks()
        {
            // Arrange 
            int numberOfTasksCreated = 2;
            
            // Act
            await _subtasksCreator.CreateSubtasks(new Issue[] {_taskIssueWithoutSubTasksAndWithPu}.AsEnumerable());

            // Assert
            await _jiraClient.Received(numberOfTasksCreated).CreateIssueAsync(Arg.Any<Issue>());
        }
        
        [Test]
        public async Task CreateSubtasks_WhenTheTaskWithTaskTypeWithDevAndQAPuQaSubTaskAndWithoutDevSubtask_ShouldCreateDevSubtask()
        {
            // Arrange 
            int numberOfTasksCreated = 1;
            
            // Act
            await _subtasksCreator.CreateSubtasks(new Issue[] {_taskIssueWithoutDevSubTaskAndWithPuAndQaSubTask}.AsEnumerable());

            // Assert
            await _jiraClient.Received(numberOfTasksCreated).CreateIssueAsync(Arg.Any<Issue>());
        }
        
        [Test]
        public async Task CreateSubtasks_WhenTheInternalTaskWithDevPuAndWithoutDevAndQaSubtasks_ShouldCreateDevSubtask()
        {
            // Arrange 
            int numberOfTasksCreated = 1;
            
            // Act
            await _subtasksCreator.CreateSubtasks(new Issue[] {_internalTaskWithDevPuAndWithoutDevQaSubtasksAndQaPu}.AsEnumerable());

            // Assert
            await _jiraClient.Received(numberOfTasksCreated).CreateIssueAsync(Arg.Any<Issue>());
        }
        
        [Test]
        public async Task CreateSubtasks_WhenTheInternalTaskWithDevAndQAPuAndWithoutDevAndQASubtask_ShouldCreateQAAndDevSubtasks()
        {
            // Arrange 
            int numberOfTasksCreated = 2;
            
            // Act
            await _subtasksCreator.CreateSubtasks(new Issue[] {_internalTaskWithDevAndQaPuAndWithoutDevQaSubtasks}.AsEnumerable());

            // Assert
            await _jiraClient.Received(numberOfTasksCreated).CreateIssueAsync(Arg.Any<Issue>());
        }
        
        [Test]
        public async Task CreateSubtasks_WhenTheTaskWithDevQAPuAndOrigEstimateWithoutDevAndQASubtask_ShouldCreateQAAndDevSubtasksAndClearedEstimate()
        {
            // Arrange 
            int numberOfTasksCreated = 2;
            
            // Act
            await _subtasksCreator.CreateSubtasks(new Issue[] {_taskWithDevAndQaPuAndOriginalEstimate}.AsEnumerable());

            // Assert
            await _jiraClient.Received(numberOfTasksCreated).CreateIssueAsync(Arg.Any<Issue>());
            await _jiraClient.Received().UpdateIssueAsync(Arg.Any<string>(), Arg.Any<Issue>());
        }
        
        private void InitializeIssues()
        {
            double Pu = 1;
            const string QaSubTaskType = "QA Sub-task";
            ProjectField project = new ProjectField {Id = 1, Key = "Key"};
            IssueTypeField taskIssueType = new IssueTypeField {Name = "Task"};
            IssueTypeField internalTaskIssueType = new IssueTypeField {Name = "Internal Technical Task"};
            
            _taskIssueWithoutSubTasksAndWithPu = new Issue
            {
                Key = "Key",
                Id = 1,
                Fields = new IssueFields
                {
                    DevPreliminaryUnits = Pu,
                    QaPreliminaryUnits = Pu,
                    Project = project,
                    IssueType = taskIssueType,
                    Subtasks = Array.Empty<Issue>(),
                    TimeTracking = new TimeTracking {OriginalEstimate = "0h", TimeRemaining = "0h"},
                    Status = new StatusField { Name = "Technical analysis needed"},
                }
            };
            
            _taskIssueWithoutDevSubTaskAndWithPuAndQaSubTask = new Issue
            {
                Key = "Key",
                Id = 1,
                Fields = new IssueFields
                {
                    DevPreliminaryUnits = Pu,
                    QaPreliminaryUnits = Pu,
                    Project = project,
                    IssueType = taskIssueType,
                    Subtasks = new Issue[] {new Issue { Fields = new IssueFields {IssueType = new IssueTypeField {Name = QaSubTaskType}}}},
                    TimeTracking = new TimeTracking {OriginalEstimate = "0h", TimeRemaining = "0h"},
                    Status = new StatusField { Name = "Technical analysis needed"},
                }
            };
            
            _internalTaskWithDevPuAndWithoutDevQaSubtasksAndQaPu = new Issue
            {
                Key = "Key",
                Id = 1,
                Fields = new IssueFields
                {
                    DevPreliminaryUnits = Pu,
                    Project = project,
                    IssueType = internalTaskIssueType,
                    Subtasks = Array.Empty<Issue>(),
                    TimeTracking = new TimeTracking {OriginalEstimate = "0h", TimeRemaining = "0h"},
                    Status = new StatusField { Name = "Technical analysis needed"},
                }
            };
            
            _internalTaskWithDevAndQaPuAndWithoutDevQaSubtasks = new Issue
            {
                Key = "Key",
                Id = 1,
                Fields = new IssueFields
                {
                    DevPreliminaryUnits = Pu,
                    QaPreliminaryUnits = Pu,
                    Project = project,
                    IssueType = internalTaskIssueType,
                    Subtasks = Array.Empty<Issue>(),
                    TimeTracking = new TimeTracking {OriginalEstimate = "0h", TimeRemaining = "0h"},
                    Status = new StatusField { Name = "Technical analysis needed"},
                }
            };

            _taskWithDevAndQaPuAndOriginalEstimate = new Issue
            {
                Key = "Key",
                Id = 1,
                Fields = new IssueFields
                {
                    DevPreliminaryUnits = Pu,
                    QaPreliminaryUnits = Pu,
                    Project = project,
                    IssueType = taskIssueType,
                    Subtasks = Array.Empty<Issue>(),
                    TimeTracking = new TimeTracking {OriginalEstimate = "1h", TimeRemaining = "1h"},
                    Status = new StatusField {Name = "Technical analysis needed"},
                }
            };
        }
    }
}