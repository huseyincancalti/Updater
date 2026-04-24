using System;
using System.Windows;
using MaterialDesignThemes.Wpf;
using ZenUpdate.Core.Enums;

namespace ZenUpdate.App.Services;

/// <summary>
/// Default <see cref="IThemeService"/> implementation. Works by:
///   1. Replacing the currently merged <c>ZenColors.*.xaml</c> dictionary on
///      <see cref="Application.Current"/>, which carries the Zen* brushes used
///      throughout the app via <c>DynamicResource</c>.
///   2. Asking <see cref="PaletteHelper"/> to flip Material Design's base theme
///      so bundled brushes (MaterialDesignPaper, MaterialDesignBody, etc.) follow.
/// </summary>
public sealed class ThemeService : IThemeService
{
    /// <summary>A known key that only lives in the Zen theme dictionaries; used to locate the active one.</summary>
    private const string ZenThemeMarkerKey = "ZenBackgroundBrush";

    private static readonly Uri DarkThemeUri =
        new("pack://application:,,,/ZenUpdate;component/Themes/ZenColors.Dark.xaml", UriKind.Absolute);

    private static readonly Uri LightThemeUri =
        new("pack://application:,,,/ZenUpdate;component/Themes/ZenColors.Light.xaml", UriKind.Absolute);

    /// <inheritdoc />
    public void ApplyTheme(AppTheme theme)
    {
        var app = Application.Current;
        if (app is null)
        {
            return;
        }

        ReplaceZenDictionary(app, theme);
        ApplyMaterialDesignBaseTheme(theme);
    }

    /// <summary>
    /// Finds the previously merged Zen dictionary (identified by <see cref="ZenThemeMarkerKey"/>)
    /// and replaces it in place. Inserts a new one if none is merged yet.
    /// </summary>
    private static void ReplaceZenDictionary(Application app, AppTheme theme)
    {
        var uri = theme == AppTheme.Light ? LightThemeUri : DarkThemeUri;
        var newDict = new ResourceDictionary { Source = uri };

        var dictionaries = app.Resources.MergedDictionaries;
        for (var i = 0; i < dictionaries.Count; i++)
        {
            if (dictionaries[i].Contains(ZenThemeMarkerKey))
            {
                dictionaries[i] = newDict;
                return;
            }
        }

        dictionaries.Add(newDict);
    }

    /// <summary>Flips Material Design's bundled Light/Dark base theme while preserving the primary color.</summary>
    private static void ApplyMaterialDesignBaseTheme(AppTheme theme)
    {
        var paletteHelper = new PaletteHelper();
        var mdTheme = paletteHelper.GetTheme();
        mdTheme.SetBaseTheme(theme == AppTheme.Light ? BaseTheme.Light : BaseTheme.Dark);
        paletteHelper.SetTheme(mdTheme);
    }
}
