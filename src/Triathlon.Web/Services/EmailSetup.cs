namespace Triathlon.Web.Services;

/// <summary>
/// Picks the mail sender from configuration: the real SMTP one where a host is set, and the logging
/// stand-in everywhere else. Callers depend on <see cref="IEmailSender"/> and never learn which.
/// </summary>
public static class EmailSetup
{
    public static IServiceCollection AddAppEmail(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(EmailOptions.SectionName);
        services.Configure<EmailOptions>(section);

        if (string.IsNullOrWhiteSpace(section[nameof(EmailOptions.Host)]))
        {
            services.AddSingleton<IEmailSender, LoggingEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }

        return services;
    }
}
