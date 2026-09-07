namespace Triathlon.Web.Services;

/// <summary>Registers the media store and the options that place it on disk.</summary>
public static class MediaSetup
{
    public static IServiceCollection AddAppMedia(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MediaOptions>(configuration.GetSection(MediaOptions.SectionName));

        // Stateless, so one instance serves every request. TimeProvider is registered by AddAppDatabase.
        services.AddSingleton<IFileStore, LocalFileStore>();

        return services;
    }
}
