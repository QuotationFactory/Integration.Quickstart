# Rhodium24 Basic Integration Quickstart

## Introduction

In this quickstart we demonstrate how you could build an .net 5 host that process files from Rhodium24.  
We use the Rhodium24 Agent that connects with Rhodium24 and downloads the file to the Output directory.

## Agent root directory

When you create an Rhodium24 Agent in the portal you need to provide a 'Root directory' this is the location of the directory the Rhodium24 Agent uses to operate with.  
Output files from Rhodium24 are stored in the Output directory inside the root directory of the Rhodium24 Agent.  

For example:
```
if the Rhodium24 Agent has the root directory configured like 'C:\Agent\Exchange', the Output files will be stored in 'C:\Agent\Exchange\Output'.
```

The **Root directory** needs to be configured in the appsettings.json or appsettings.development.json in this project.

## How it works

If we run the Rhodium24.Host project inside this solution and we do an 'Export from Rhodium24' the filewatcher will notice this new file the Rhodium24 Agent created and it will publish a MediatR notification 'AgentOutputFileCreated'.

The 'AgentOutputFileCreatedHandler' will process this notification and will read the file and deserialize it to the Project class.

## Export from Rhodium24

When change the status of a project inside Rhodium24 to Quoted or Ordered and export will be triggered and if the Rhodium24 Agent is configured and running it will download the file to the Output directory of that agent.

## Help needed?

Please create an issue if you got a suggestion or if you need help.
