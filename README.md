# Integration Quickstart

## Introduction

In this quickstart we demonstrate how you could build an .net 8 host that process files from Quotation Factory.
We use the Quotation Factory Edge Connector that connects with Quotation Factory and downloads the file to the Output directory.

## Edge Connector - root directory

When you create a QF Edge Connector in the portal you need to provide a 'Root directory' this is the location of the directory the QF Edge Connector uses to operate with.
Output files from Quotation Factory are stored in the Output directory inside the root directory of the QF Edge Connector.

For example:
if the QF Edge Connector has the root directory configured like 'C:\Qf\EdgeConnector\Exchange', the Output files will be stored in 'C:\Qf\EdgeConnector\Exchange\Output'.

The **Root directory** needs to be configured in the appsettings.json in this project.

## How it works

If we run the Integration.Host project inside this solution and we do an 'Export from Quotation Factory' the filewatcher will notice this new file the Edge Connector created and it will publish a MediatR notification 'OutputFileCreated'.

The 'OutputFileCreatedHandler' will process this notification and will read the file and deserialize it to the Project class or process the message request

## Export from Quotation Factory

When change the status of a project inside Quotation Factory to Quoted or Ordered and export will be triggered and if the Edge Connector is configured and running it will download the file to the Output directory of that connector.

## Help needed?

Please create an issue if you got a suggestion or if you need help.

# Developer Guide for Custom Quotation Factory Integrations

## Overview

The QF Edge Connector (previously named QF Agent) enables robust, custom integrations with the Quotation Factory cloud platform. Developers can leverage the example project provided on GitHub as a starting point for building their own integrations. Access the project here: [Integration.Quickstart Repository](https://github.com/QuotationFactory/Integration.Quickstart/tree/master).

## Message Handling in Edge Connector

The `OutputFileCreatedHandler.cs` class within the example project includes various message handlers that facilitate interactions between your local systems and the QF cloud platform. Below is a detailed table summarizing the messages, their purposes, and the corresponding response objects.

| Message | Response Object | Description |
|---------|-----------------|-------------|
| `ReadProjectZipFileAsync` | `ExportToErpResponse` | Handles requests to read project data compressed as a ZIP file and prepares it for export to an ERP system. |
| `RequestAddressBookSyncMessage` | `RequestAddressBookSyncMessageResponse` | Initiates synchronization of address book data between the local system and the QF platform. |
| `RequestArticlesSyncMessage` | `RequestArticlesSyncMessageResponse` | Triggers the synchronization of article data, including updates or new entries, between local systems and the cloud platform. |
| `RequestManufacturabilityCheckOfPartTypeMessage` | `RequestManufacturabilityCheckOfPartTypeMessageResponse` | Requests a manufacturability check for a specific part type, with the system returning issues or confirmations. |
| `RequestProductionTimeEstimationOfPartTypeMessage` | `RequestProductionTimeEstimationOfPartTypeMessageResponse` | Asks for an estimation of production time for a part type, essential for planning and scheduling in manufacturing processes. |
| `RequestAdditionalCostsOfPartTypeMessage` | `RequestAdditionalCostsOfPartTypeMessageResponse` | Inquires about additional costs associated with manufacturing a particular part type, aiding in financial planning and quotation accuracy. |
| `ProjectStatusChangedMessage` | None (Echo Message) | Communicates changes in the project status back to the QF platform, updating the current state of the project. |
| `ChangeProjectOrderNumberMessage` | None (Echo Message) | Allows updating the project order number in the QF platform, reflecting any changes made locally. |
| `RequestSellingBuyingPartyArticleMessage` | `RequestSellingBuyingPartyArticleMessageResponse` | Syncs selling and buying party article references. |
| `RequestAutoInviteMessage` | `RequestAutoInviteMessageResponse` | Triggers automatic invite logic for collaborators. |

## Building Your Integration

To develop your own integration using the Edge Connector, follow these steps:

1. **Clone the Example Project**: Start by cloning the example project from the GitHub repository.
2. **Review the Example Handlers**: Understand how the handlers in the `OutputFileCreatedHandler.cs` class interact with the Quotation Factory platform.
3. **Customize Message Handlers**: Modify existing handlers or create new ones to meet the specific needs of your integration.
4. **Test Your Integration**: Thoroughly test the integration in a controlled environment to ensure it functions correctly and interacts with the platform as expected.
5. **Deploy**: Once testing is complete, deploy your custom integration to production.

### Best Practices
- **Use Clear Naming Conventions**: Like the examples, ending handler names with `Message` helps in identifying the purpose and nature of the handler.
- **Handle Failures Gracefully**: Implement error handling within your handlers to manage and log failures or unexpected issues.
- **Test Thoroughly**: Before deploying your handlers, thoroughly test them to ensure they handle all expected scenarios correctly.

## Conclusion
Building integrations with the Edge Connector provides a powerful way to enhance the functionality of the Quotation Factory platform, enabling customized workflows and improved data management. By following this guide and utilizing the provided example project, you can effectively develop robust integrations tailored to your specific needs.

