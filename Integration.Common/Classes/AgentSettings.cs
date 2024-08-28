using System.ComponentModel.DataAnnotations;

namespace Integration.Common.Classes
{
    /// <summary>
    /// Setting about the Agent
    /// </summary>
    /// <remarks>
    /// Retrieve these settings with DI IOptions[AppSettings].
    /// </remarks>
    public class AgentSettings
    {
        /// <summary>
        /// This is the root directory of the Agent which contains the Input & Output folder and all specific integration folders (if enabled)
        /// </summary>
        /// <remarks>
        /// This folder influence the behaviour of the agent host, dont modify directories inside this folder unless you know what you're doing.
        /// </remarks>
        [Required]
        public string RootDirectory { get; set; }
    }
}
