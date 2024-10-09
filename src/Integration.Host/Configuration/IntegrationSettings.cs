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

    public string WatchDirectory { get; set; }
    public string WatchFilter { get; set; }

    [Required]
    public string TenantId { get; set; }

    [Required]
    public string ClientId { get; set; }

    [Required]
    public string ClientSecret { get; set; }

    public string DriveId { get; set; }

    public string TargetDirectory { get; set; }    
}
