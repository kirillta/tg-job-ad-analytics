namespace TgJobAdAnalytics.Models.Uploads.Enums;

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
    /// Append new messages while preserving existing ones.
    /// </summary>
    Append,

    /// <summary>
    /// Clean update with removal of all message data before updating.
    /// </summary>
    Clean
}    
