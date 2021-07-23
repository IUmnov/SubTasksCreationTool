This tool is provided to create subtasks for sprint tasks. 

In the file Properties/appsettings.json, specify your jira-url, login and api-token. APi-token can be created by going to atlassian settings account, secutiry tab.

To get a sprint id, go to jira and select the desired sprint in the burndownchart and copy the value from the address line of the sprint variable.

Example: https://oneinc.atlassian.net/secure/RapidBoard.jspa?rapidView=540&projectKey=PROC&view=reporting&chart=burndownChart&sprint=1254
Sprint id: 1254

Sub-tasks are created only for tasks with task type and internal technical task and with DEV and QA PU values. If an internal technical taskhas no QA PU, the QA-subtask will not be created for it
At the end of the program the QA sub-task / subtask with summary [QA] Testing of {mainTaskSummary} / [Dev] Implementation of {mainTaskSummary} and original estimate 6*QA_Pu / 6*Dev_Pu will be created for the sprint tasks listed above. 
For main tasks with the specified original estimate, the value will be zeroed. 
A text file will also be created with references to tasks without PUs and tasks in need more info status

