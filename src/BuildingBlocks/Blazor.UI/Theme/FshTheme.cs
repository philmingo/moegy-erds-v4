using MudBlazor;

namespace FSH.Framework.Blazor.UI.Theme;

public static class FshTheme
{
    public static MudTheme Build()
    {
        return new MudTheme
        {
            PaletteLight = LightPalette(),
            PaletteDark = DarkPalette(),
            LayoutProperties = LayoutProperties(),
            ZIndex = ZIndexLevels()
        };
    }

    private static PaletteLight LightPalette()
    {
        return new PaletteLight
        {
            // Primary Brand Colors
            Primary = "#6366F1",
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = "#4F46E5",
            PrimaryLighten = "#818CF8",

            // Secondary/Accent
            Secondary = "#EC4899",
            SecondaryContrastText = "#FFFFFF",
            SecondaryDarken = "#DB2777",
            SecondaryLighten = "#F472B6",

            // Tertiary
            Tertiary = "#10B981",
            TertiaryContrastText = "#FFFFFF",

            // Background & Surfaces
            Background = "#FFFFFF",
            Surface = "#FFFFFF",

            // AppBar
            AppbarBackground = "#FFFFFF",
            AppbarText = "#171717",

            // Drawer
            DrawerBackground = "#FFFFFF",
            DrawerText = "#171717",
            DrawerIcon = "#525252",

            // Text Colors
            TextPrimary = "#171717",
            TextSecondary = "#525252",
            TextDisabled = "#A3A3A3",

            // Action Colors
            ActionDefault = "#525252",
            ActionDisabled = "#D4D4D4",
            ActionDisabledBackground = "#F5F5F5",

            // Dividers & Lines
            Divider = "#E5E5E5",
            DividerLight = "#F5F5F5",
            LinesDefault = "#E5E5E5",
            LinesInputs = "#D4D4D4",
            TableLines = "#E5E5E5",

            // Semantic Colors
            Success = "#10B981",
            SuccessContrastText = "#FFFFFF",
            Info = "#3B82F6",
            InfoContrastText = "#FFFFFF",
            Warning = "#F59E0B",
            WarningContrastText = "#FFFFFF",
            Error = "#EF4444",
            ErrorContrastText = "#FFFFFF",

            // Hover & Ripple
            HoverOpacity = 0.04,
            RippleOpacity = 0.1,
        };
    }

    private static PaletteDark DarkPalette()
    {
        return new PaletteDark
        {
            // Primary Brand Colors - Lighter for dark backgrounds
            Primary = "#818CF8",
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = "#6366F1",
            PrimaryLighten = "#A5B4FC",

            // Secondary/Accent
            Secondary = "#F472B6",
            SecondaryContrastText = "#FFFFFF",
            SecondaryDarken = "#EC4899",
            SecondaryLighten = "#F9A8D4",

            // Tertiary
            Tertiary = "#34D399",
            TertiaryContrastText = "#FFFFFF",

            // Background & Surfaces
            Black = "#0F0F12",
            Background = "#0F0F12",
            Surface = "#18181B",

            // AppBar
            AppbarBackground = "#18181B",
            AppbarText = "#E4E4E7",

            // Drawer
            DrawerBackground = "#18181B",
            DrawerText = "#E4E4E7",
            DrawerIcon = "#A1A1AA",

            // Text Colors
            TextPrimary = "#E4E4E7",
            TextSecondary = "#A1A1AA",
            TextDisabled = "#52525B",

            // Action Colors
            ActionDefault = "#A1A1AA",
            ActionDisabled = "#3F3F46",
            ActionDisabledBackground = "#27272A",

            // Dividers & Lines
            Divider = "#3F3F46",
            DividerLight = "#27272A",
            LinesDefault = "#3F3F46",
            LinesInputs = "#52525B",
            TableLines = "#3F3F46",

            // Semantic Colors
            Success = "#10B981",
            SuccessContrastText = "#FFFFFF",
            Info = "#3B82F6",
            InfoContrastText = "#FFFFFF",
            Warning = "#F59E0B",
            WarningContrastText = "#000000",
            Error = "#EF4444",
            ErrorContrastText = "#FFFFFF",

            // Hover & Ripple
            HoverOpacity = 0.08,
            RippleOpacity = 0.12,
        };
    }

    private static LayoutProperties LayoutProperties()
    {
        return new LayoutProperties
        {
            DefaultBorderRadius = "0.5rem",
            DrawerWidthLeft = "280px",
            DrawerWidthRight = "280px",
            AppbarHeight = "64px"
        };
    }

    private static ZIndex ZIndexLevels()
    {
        return new ZIndex
        {
            Drawer = 1040,
            Dialog = 1060,
            Popover = 1070,
            Tooltip = 1080,
            Snackbar = 1090,
            AppBar = 1100
        };
    }
}
