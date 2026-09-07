namespace Triathlon.Web.Services;

/// <summary>
/// The Federation's outbound SMTP account. Everything but the port and the StartTLS switch is
/// deployment-specific and arrives as an environment variable — never from a checked-in file.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>SMTP host. Empty disables real sending; see <see cref="LoggingEmailSender"/>.</summary>
    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 587;

    /// <summary>Upgrade the connection with STARTTLS, which is what port 587 expects.</summary>
    public bool UseStartTls { get; set; } = true;

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FromName { get; set; } = "Saudi Triathlon Federation";

    public string FromAddress { get; set; } = "no-reply@triathlon.sa";
}
