using System.Windows;

namespace ZenUpdate.App;

/// <summary>
/// Code-behind for the main application window.
/// Kept minimal — all logic lives in <see cref="ViewModels.ShellViewModel"/>.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>Initializes the MainWindow and its XAML components.</summary>
    public MainWindow()
    {
        InitializeComponent();
    }
}
