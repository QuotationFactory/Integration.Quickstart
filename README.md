# EdgeConnector

This EdgeConnector extends the QF EdgeConnector as documented in [QF readme](https://github.com/QuotationFactory/Integration.Quickstart/blob/master/README.md)

For RODIN this is extended with the [Features.Graph.GraphConnector](./src/Integration.Host/Features/Graph/GraphConnector.cs) class to upload/download files from SharePointOnline

The **EdgeConnector** is used for **uploading** files from RODIN factory to RODIN SharePoint, specifically for both `QF` and `MMS` files generated on-site.

For **downloading** from SharePointOnline, RODIN uses **Azure Logic App** to trigger on a file created event in SharePointOnline and write file to Azure Blob storage, which is respectively synced via AzCopy to RODIN on-site.

## WatchDirectory

If specified in the configuration, the `WatchDirectory` is used for configuring the directory to watch, i.e. `\\MyServer\MyFolder`
Similarly the `WatchFilter`, i.e. `*.json`

Files are moved to `{RootDirectory}\Processed`
