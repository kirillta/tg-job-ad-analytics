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
    /// OnlyNewMessages update that adds new messages while preserving existing ones.
    /// </summary>
    OnlyNewMessages,

    /// <summary>
    /// Clean update with removal of all message data before updating.
    /// </summary>
    Clean
}    
