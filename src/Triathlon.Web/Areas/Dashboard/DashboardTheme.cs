using MudBlazor;

namespace Triathlon.Web.Areas.Dashboard;

/// <summary>The dashboard's MudBlazor theme: the Federation's green, not the prototype's navy/teal.</summary>
public static class DashboardTheme
{
    public static readonly MudTheme Theme = Build();

    private static MudTheme Build()
    {
        var theme = new MudTheme();
        theme.PaletteLight.Primary = "#008C3D";
        theme.PaletteLight.Secondary = "#00662F";
        theme.PaletteLight.AppbarBackground = "#0A2015";
        theme.Typography.Default.FontFamily = ["IBM Plex Sans", "IBM Plex Sans Arabic", "system-ui", "sans-serif"];
        return theme;
    }
}
