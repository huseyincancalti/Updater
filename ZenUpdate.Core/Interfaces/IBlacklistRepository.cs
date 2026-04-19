namespace ZenUpdate.Core.Interfaces;

/// <summary>
/// Provides read and write access to the blacklist file.
/// The blacklist stores package IDs that should be excluded from winget update results.
/// The file is stored at <c>%APPDATA%\ZenUpdate\blacklist.json</c>.
/// </summary>
public interface IBlacklistRepository
{
    /// <summary>
    /// Returns all currently blacklisted winget package IDs.
    /// </summary>
    Task<IReadOnlyList<string>> GetBlacklistedIdsAsync();

    /// <summary>
    /// Adds a package ID to the blacklist and saves the file.
    /// </summary>
    /// <param name="packageId">The winget package ID to blacklist. Example: "Microsoft.Teams"</param>
    Task AddAsync(string packageId);

    /// <summary>
    /// Removes a package ID from the blacklist and saves the file.
    /// </summary>
    /// <param name="packageId">The winget package ID to remove from the blacklist.</param>
    Task RemoveAsync(string packageId);
}
