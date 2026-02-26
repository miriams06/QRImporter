namespace Importer.Client.Models
{
    /// <summary>
    /// Estado de sincronização de um documento local.
    /// </summary>
    public enum SyncStatus
    {
        Draft,
        PendingSync,
        Synced,
        Error
    }
}
