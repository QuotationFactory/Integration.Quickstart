# Quotation Factory Basic Integration Quickstart

## Introduction

In this quickstart we demonstrate how you could build an .net 8 host that process files from Quotation Factory.  
We use the Quotation Factory Agent/Connector that connects with Quotation Factory and downloads the file to the Output directory.

## Agent root directory

When you create an Quotation Factory Agent in the portal you need to provide a 'Root directory' this is the location of the directory the Quotation Factory Agent uses to operate with.  
Output files from Quotation Factory are stored in the Output directory inside the root directory of the Quotation Factory Agent.  

For example:
```
if the Quotation Factory Agent has the root directory configured like 'C:\Agent\Exchange', the Output files will be stored in 'C:\Agent\Exchange\Output'.
```

The **Root directory** needs to be configured in the appsettings.json or appsettings.development.json in this project.

## How it works

If we run the Quotation Factory.Host project inside this solution and we do an 'Export from Quotation Factory' the filewatcher will notice this new file the Quotation Factory Agent created and it will publish a MediatR notification 'AgentOutputFileCreated'.

The 'AgentOutputFileCreatedHandler' will process this notification and will read the file and deserialize it to the Project class.

## Export from Quotation Factory

When change the status of a project inside Quotation Factory to Quoted or Ordered and export will be triggered and if the Quotation Factory Agent is configured and running it will download the file to the Output directory of that agent.

## Help needed?

Please create an issue if you got a suggestion or if you need help.
