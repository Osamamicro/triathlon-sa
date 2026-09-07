namespace Triathlon.Web.Services;

/// <summary>
/// Who is making the current change, for the audit stamps on every row and for the activity log.
/// <para>
/// The dashboard renders interactively, so a save started by an editor runs on a SignalR circuit
/// rather than inside an HTTP request, and <see cref="IHttpContextAccessor"/> is unsupported there:
/// asking it for the user would stamp <c>null</c> on every dashboard edit. This abstraction is what
/// lets one implementation answer for both worlds — the circuit's authentication state when there
/// is one, the request's principal otherwise.
/// </para>
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// The acting user's name, or <c>null</c> for a background job, a seed run or a test. Best
    /// effort and synchronous, because the save-changes interceptor has no asynchronous path;
    /// prefer <see cref="GetNameAsync"/> wherever the caller can await.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// The acting user's name, awaiting the circuit's authentication state when it is not yet
    /// resolved. Returns <c>null</c> when nobody is signed in.
    /// </summary>
    ValueTask<string?> GetNameAsync(CancellationToken ct = default);
}
