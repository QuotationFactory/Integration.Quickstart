using System.ComponentModel.DataAnnotations;

namespace Integration.Host.Configuration;

/// <summary>
/// Setting about the Integration Settings
/// </summary>
/// <remarks>
/// Retrieve these settings with DI IOptions[AppSettings].
/// </remarks>
public class IntegrationSettings
{
    /// <summary>
    /// This is the root directory of the edge connect which contains the Input & Output folder and all specific integration folders (if enabled)
    /// </summary>
    /// <remarks>
    /// This folder influence the behaviour of the edge connector host, dont modify directories inside this folder unless you know what you're doing.
    /// </remarks>
    [Required]
    public required string RootDirectory { get; set; }
    [Required]
    public required int NumberOfConcurrentTasks { get; set; }
    [Required]
    public required bool EnableSftpUpload { get; set; }
    [Required]
    public required bool EnableProjectFiles { get; set; }
    [Required]
    public required bool EnableProjectFilesWithTimeRegistration { get; set; }
    [Required]
    public required bool EnableAddressBookSyncMessage { get; set; }
    [Required]
    public required bool EnableArticleSyncMessages { get; set; }
    [Required]
    public required bool EnableManufacturabilityCheckOfPartTypeMessages { get; set; }
    [Required]
    public required bool EnableProjectStatusChangedMessages { get; set; }
    [Required]
    public required bool EnableSellingBuyingPartyArticleMessages { get; set; }
    [Required]
    public required bool EnableProductionTimeEstimationOfPartTypeMessages { get; set; }
    [Required]
    public required bool EnableAdditionalCostsOfPartTypeMessages { get; set; }
}
