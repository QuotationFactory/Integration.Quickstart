using Integration.Common.FileWatcher;
using Integration.Host.Configuration;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Versioned.ExternalDataContracts.Enums;

namespace Integration.Host.Features.FileOrchestrator;

/// <summary>
/// Service that watches on the output directory of the edge connector for *.json files
/// Publishes an OutputFileCreated notification if a file is created
/// </summary>
public class OutputFileWatcherService : FileWatcherService
{
    private readonly IMediator _mediator;
    private readonly ILogger<OutputFileWatcherService> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    // this dictionary maps the IntegrationTypeV1 enum values to their corresponding subdirectory names
    // this is used to add file watchers for each integration type output directory if the directory exists
    // this definition should be kept in sync with the IntegrationTypeV1 enum and is defined in the backend of Quotation Factory.
    // do not change strings
    private static readonly Dictionary<IntegrationTypeV1, string> s_integrationSubDirectoryNames = new()
    {
        { IntegrationTypeV1.ERP_ECI_RidderIQ, "Ridder IQ" },
        { IntegrationTypeV1.CAM_LVD_CadmanB, "LVD CadMan-B" },
        { IntegrationTypeV1.CAM_LVD_CadmanSdi, "LVD CadMan-SDI" },
        { IntegrationTypeV1.CAM_WiCAM_PN4000, "WiCAM PN4000" },
        { IntegrationTypeV1.CAM_BySoft_CAM, "BySoft Cam" },
        { IntegrationTypeV1.ERP_ISAH_ISAH, "ISAH_ISAH_BS" },
        { IntegrationTypeV1.CAM_Trumpf_TruTops_Boost, "Trumpf_TruTops_Boost" },
        { IntegrationTypeV1.MES_Trumpf_TruTops_Fab, "Trumpf_TruTops_Fab" },
        { IntegrationTypeV1.ERP_ECI_Bemet, "ECI_Bemet" },
        { IntegrationTypeV1.Custom , "Custom" },
        { IntegrationTypeV1.ERP_Lantek_Integra , "Lantek Integra" },
        { IntegrationTypeV1.FCC_AutoPOL , "FCC AutoPOL" },
        { IntegrationTypeV1.ERP_MKG_V3 , "MKG3" },
        { IntegrationTypeV1.ERP_MKG_V4 , "MKG4" },
        { IntegrationTypeV1.ERP_MKG_V5 , "MKG5" },
        { IntegrationTypeV1.Feat_Selling_Buying_ArticleNumber_Sync, "Selling Buying ArticleNumber Sync" },
    };

    public OutputFileWatcherService(IMediator mediator, IOptions<IntegrationSettings> options, ILogger<OutputFileWatcherService> logger)
    {
        _mediator = mediator;
        _logger = logger;

        if (options.Value.NumberOfConcurrentTasks > 1)
        {
            _semaphore = new(options.Value.NumberOfConcurrentTasks, options.Value.NumberOfConcurrentTasks);
            _logger.LogInformation("Semaphore initialized with {NumberOfConcurrentTasks} concurrent tasks", options.Value.NumberOfConcurrentTasks);
        }

        // add file watcher to the output directory
        AddFileWatcher(options.Value.GetOrCreateOutputDirectory(createIfNotExists: true), "*.json");

        // add file watchers for each integration type output directory if the directory exists
        // this is to support multiple integration types with their own subdirectory
        // e.g. Ridder IQ, LVD CadMan-B, BySoft CAM, etc.
        // only add the watcher if the directory exists
        // this is to avoid adding watchers for integration types that are not used
        // this is a temporary solution until we have a better way to configure the integration types
        var integrationTypes = Enum.GetValues<IntegrationTypeV1>()
            .Select(i=> s_integrationSubDirectoryNames.GetValueOrDefault(i)).Where(i => i != null).ToList();

        foreach (var integrationType in integrationTypes)
        {
            if (!Directory.Exists(Path.Combine(options.Value.RootDirectory, integrationType!, "Output")))
            {
                continue;
            }

            AddFileWatcher(Path.Combine(options.Value.RootDirectory, integrationType!, "Output"), "*.json");
        }

    }

#pragma warning disable VSTHRD100
    protected override async void OnAllChanges(object sender, FileSystemEventArgs e)
#pragma warning restore VSTHRD100
    {
        bool? isDone = null;
        try
        {
            await _semaphore.WaitAsync();
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                    await _mediator.Publish(new OutputFileOrchestrator.OutputFileCreated(e.FullPath));
                    isDone = true;
                    _logger.LogInformation("Successfully processed {Event} for file {FilePath}", e.ChangeType, e.FullPath);
                    break;
                case WatcherChangeTypes.Deleted:
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.Renamed:
                case WatcherChangeTypes.All:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception ex)
        {
            isDone = false;
            _logger.LogError(ex, "Error while processing {Event} for file {FilePath}", e.ChangeType, e.FullPath);
        }
        finally
        {
            MoveHandledFile(e.FullPath, isDone);
            _semaphore.Release();
        }
    }

#pragma warning disable VSTHRD100
    protected override async void OnExistingFile(object sender, FileSystemEventArgs e)
#pragma warning restore VSTHRD100
    {
        bool? isDone = null;
        try
        {
            await _semaphore.WaitAsync();
            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Created:
                case WatcherChangeTypes.Deleted:
                case WatcherChangeTypes.Changed:
                case WatcherChangeTypes.Renamed:
                    break;
                case WatcherChangeTypes.All:
                    await _mediator.Publish(new OutputFileOrchestrator.OutputFileCreated(e.FullPath));
                    isDone = true;
                    _logger.LogInformation("Successfully processed {Event} for file {FilePath}", e.ChangeType, e.FullPath);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception ex)
        {
            isDone = false;
            _logger.LogError(ex, "Error while processing {Event} for file {FilePath}", e.ChangeType, e.FullPath);
        }
        finally
        {
            MoveHandledFile(e.FullPath, isDone);
            _semaphore.Release();
        }
    }

    private void MoveHandledFile(string filePath, bool? isDone)
    {
        switch (isDone)
        {
            case true:
                {
                    // move file to done directory
                    var destinationFilePath = Path.Combine(Path.GetDirectoryName(filePath)?? string.Empty, "done");
                    filePath.MoveFileToDirectory(destinationFilePath);
                    // also move the file with the same name but .zip in the input directory to done directory
                    var zipFilePath = Path.ChangeExtension(filePath, ".zip");
                    if (File.Exists(zipFilePath))
                    {
                        var zipDestinationFilePath = Path.Combine(Path.GetDirectoryName(zipFilePath)?? string.Empty, "done");
                        zipFilePath.MoveFileToDirectory(zipDestinationFilePath);
                    }

                    break;
                }
            case false:
                {
                    // move file to error directory
                    var destinationFilePath = Path.Combine(Path.GetDirectoryName(filePath)?? string.Empty, "error");
                    filePath.MoveFileToDirectory(destinationFilePath);
                    // also move the file with the same name but .zip in the input directory to error directory
                    var zipFilePath = Path.ChangeExtension(filePath, ".zip");
                    if (File.Exists(zipFilePath))
                    {
                        var zipDestinationFilePath = Path.Combine(Path.GetDirectoryName(zipFilePath)?? string.Empty, "error");
                        zipFilePath.MoveFileToDirectory(zipDestinationFilePath);
                    }

                    break;
                }
            case null:
                {
                    // do nothing, file is not handled
                    break;
                }
        }
    }
}
