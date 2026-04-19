using ZenUpdate.Core.Enums;

namespace ZenUpdate.Core.Models;

/// <summary>
/// Abstract base class for all update items, regardless of source.
/// Subclass this for winget apps, Windows Updates, and drivers.
/// </summary>
public abstract class UpdateItem
{
    /// <summary>
    /// Unique identifier for this update item.
    /// For winget: the package ID (e.g. "Microsoft.VisualStudioCode").
    /// For Windows Update: the KB article number.
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>Human-readable display name shown in the UI.</summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>The version currently installed on this machine.</summary>
    public string CurrentVersion { get; init; } = string.Empty;

    /// <summary>The newer version available to install.</summary>
    public string AvailableVersion { get; init; } = string.Empty;

    /// <summary>Which system reported this update (Winget, WindowsUpdate, Driver).</summary>
    public UpdateSource Source { get; init; }

    /// <summary>
    /// Current lifecycle state of this update.
    /// Changes as the user interacts with it (Pending → Installing → Succeeded).
    /// </summary>
    public UpdateStatus Status { get; set; } = UpdateStatus.Pending;

    /// <summary>Whether the user has selected this item for batch updating.</summary>
    public bool IsSelected { get; set; }
}
