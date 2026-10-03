using System.IO;
using System.Text.Json;
using System.Windows;
using VPUploader.App.ViewModels;
using VPUploader.Core;
using Wpf.Ui.Appearance;

namespace VPUploader.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        
        // the base keyword refers to the parent class's version of the method
        // skipping this line can cause issues since it would omit WPF's startup logic
        base.OnStartup(e);
        
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
        
        // TODO: replace with your actual Pinterest app credentials
        // (see SETUP.md)
        var configText = File.ReadAllText("appsettings.local.json");
        using var configDoc = JsonDocument.Parse(configText);
        var pinterestConfig = configDoc.RootElement.GetProperty("Pinterest");

        var authService = new AuthService(
            clientId: pinterestConfig.GetProperty("ClientId").GetString()!,
            clientSecret: pinterestConfig.GetProperty("ClientSecret").GetString()!);
        
        var pinterestClient  = new PinterestClient(authService);
        var viewModel = new MainViewModel(pinterestClient);
        var mainWindow = new MainWindow(viewModel);
        
        mainWindow.Show();

    }
    
}