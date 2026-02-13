using MudBlazor;

namespace MannaHp.Client.Theme;

public static class MannaTheme
{
    public static MudTheme Theme => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#5D4037",          // Coffee brown
            PrimaryDarken = "#3E2723",    // Dark espresso
            PrimaryLighten = "#8D6E63",   // Light mocha
            Secondary = "#FF8F00",        // Warm amber
            SecondaryDarken = "#E65100",   // Deep amber
            SecondaryLighten = "#FFB300",  // Light amber
            Tertiary = "#2E7D32",         // Fresh green (for bowls)
            Background = "#FFF8F0",       // Warm cream
            Surface = "#FFFFFF",
            AppbarBackground = "#4E342E", // Dark coffee appbar
            AppbarText = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#3E2723",
            TextPrimary = "#3E2723",      // Dark espresso text
            TextSecondary = "#6D4C41",    // Medium brown text
            ActionDefault = "#5D4037",
            ActionDisabled = "#BCAAA4",
            ActionDisabledBackground = "#D7CCC8",
            Success = "#2E7D32",
            Warning = "#F57F17",
            Error = "#C62828",
            Info = "#1565C0",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Roboto", "Helvetica", "Arial", "sans-serif"]
            },
            H5 = new H5Typography
            {
                FontWeight = "600"
            },
            H6 = new H6Typography
            {
                FontWeight = "600"
            }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px"
        }
    };
}
