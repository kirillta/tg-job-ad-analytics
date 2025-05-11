using System;

namespace TgJobAdAnalytics.Services.Messages
{
    /// <summary>
    /// Defines the mode of operation for the upload process.
    /// </summary>
    public enum UploadMode
    {
        /// <summary>
        /// Skip the update process entirely.
        /// </summary>
        Skip,        
        
        /// <summary>
        /// OnlyNewMessages update that adds new messages while preserving existing ones.
        /// </summary>
        OnlyNewMessages,

        /// <summary>
        /// Clean update with removal of all message data before updating.
        /// </summary>
        Clean
    }    /// <summary>
    /// Configuration options for the <see cref="UploadService"/>.
    /// </summary>
    public class UploadOptions
    {
        /// <summary>
        /// Gets or sets the mode of operation for the upload process.
        /// </summary>
        public UploadMode Mode { get; set; } = UploadMode.OnlyNewMessages;

        /// <summary>
        /// Gets or sets the batch size for message uploads. Larger values can improve performance but require more memory.
        /// </summary>
        public int BatchSize { get; set; } = 10000;
    }
}
