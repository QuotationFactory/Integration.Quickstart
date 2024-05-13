# Quotation Factory Basic Integration Quickstart

## Introduction

In this quickstart we demonstrate how you could build an .net 8 host that process files from Quotation Factory.  
We use the Quotation Factory Agent/Connector that connects with Quotation Factory and downloads the file to the Output directory.

## Agent root directory

When you create an Quotation Factory Agent in the portal you need to provide a 'Root directory' this is the location of the directory the Quotation Factory Agent uses to operate with.  
Output files from Quotation Factory are stored in the Output directory inside the root directory of the Quotation Factory Agent.  

For example:
if the Quotation Factory Agent has the root directory configured like 'C:\Agent\Exchange', the Output files will be stored in 'C:\Agent\Exchange\Output'.

The **Root directory** needs to be configured in the appsettings.json or appsettings.development.json in this project.

## How it works

If we run the Quotation Factory.Host project inside this solution and we do an 'Export from Quotation Factory' the filewatcher will notice this new file the Quotation Factory Agent created and it will publish a MediatR notification 'AgentOutputFileCreated'.

The 'AgentOutputFileCreatedHandler' will process this notification and will read the file and deserialize it to the Project class.

## Export from Quotation Factory

When change the status of a project inside Quotation Factory to Quoted or Ordered and export will be triggered and if the Quotation Factory Agent is configured and running it will download the file to the Output directory of that agent.

## Help needed?

Please create an issue if you got a suggestion or if you need help.

# Developer Guide for Building Integrations Using QF Agent

## Introduction
This guide is designed to help developers leverage the Quotation Factory Agent (QF Agent) to create custom integrations with the Quotation Factory cloud platform. The QF Agent enables robust interaction between local systems and the cloud, facilitating a variety of automated tasks and data synchronization processes.

## Getting Started
To jump-start the development of your custom integration, you can utilize an example project provided in our GitHub repository. This project contains foundational code and examples that demonstrate how to implement specific interactions using the QF Agent.

### Quickstart Project Repository
- **Repository**: [QF.Basic.Integration.Quickstart](https://github.com/QuotationFactory/QF.Basic.Integration.Quickstart/tree/master)

This repository contains sample handlers and templates that you can use to understand the mechanisms of QF Agent integration and to build upon for creating customized functionalities.

## Key Components in the Example Project

### Class File: `AgentOutputFileCreatedHandler.cs`
This class file includes various handlers that act upon commands and queries facilitated by the QF Agent. Here's a brief overview of each handler available in the example project:

#### Command Handlers
Commands are used to perform operations that modify data or state on the server. In CQRS (Command Query Responsibility Segregation) architecture, these commands represent operations that directly influence system behaviors:

- **RequestAddressBookSyncMessage**: Triggers synchronization of the address book data from the local system to the cloud.
- **RequestArticlesSyncMessage**: Initiates synchronization of article data between the local system and the cloud platform.
- **RequestManufacturabilityCheckOfPartTypeMessage**: Sends a request to evaluate the manufacturability of a specified part type.
- **RequestProductionTimeEstimationOfPartTypeMessage**: Requests an estimation of production time for a specific part type.
- **RequestAdditionalCostsOfPartTypeMessage**: Queries additional cost estimates associated with manufacturing a part type.
- **ChangeProjectOrderNumberMessage**: Modifies the order number associated with a project.

#### Query Handler
Queries are used to request data from the server without modifying any state. They are part of the CQRS pattern:

- **ProjectStatusChangedMessage**: Retrieves updates about changes in project status.

### Developing Custom Handlers
To develop your own custom handlers, follow these steps:

1. **Clone the Repository**: Start by cloning the `QF.Basic.Integration.Quickstart` repository to your local development environment.
2. **Explore Existing Handlers**: Familiarize yourself with the existing handlers in `AgentOutputFileCreatedHandler.cs` to understand how they interact with the QF Agent and the cloud platform.
3. **Create New Handlers**:
   - Identify the operations you need to handle.
   - Implement new handlers by extending the `AgentOutputFileCreatedHandler.cs` or creating new class files as needed.
   - Ensure that command handlers are idempotent and that query handlers are side-effect free.

### Best Practices
- **Use Clear Naming Conventions**: Like the examples, ending handler names with `Message` helps in identifying the purpose and nature of the handler.
- **Handle Failures Gracefully**: Implement error handling within your handlers to manage and log failures or unexpected issues.
- **Test Thoroughly**: Before deploying your handlers, thoroughly test them to ensure they handle all expected scenarios correctly.

## Conclusion
Building integrations with the QF Agent provides a powerful way to enhance the functionality of the Quotation Factory platform, enabling customized workflows and improved data management. By following this guide and utilizing the provided example project, you can effectively develop robust integrations tailored to your specific needs.

