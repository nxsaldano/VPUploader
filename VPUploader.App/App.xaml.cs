using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
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

        try
        {
            EnsureContextMenuRegistered();

            ApplicationThemeManager.Apply(ApplicationTheme.Dark);

            // anchored to the exe's own folder, not the ambiguous working directory
            // (launching via the right-click menu doesn't always set the working
            // directory to where the exe actually lives)
            string configPath = Path.Combine(AppContext.BaseDirectory, "appsettings.local.json");
            var configText = File.ReadAllText(configPath);
            using var configDoc = JsonDocument.Parse(configText);
            var pinterestConfig = configDoc.RootElement.GetProperty("Pinterest");

            var authService = new AuthService(
                clientId: pinterestConfig.GetProperty("ClientId").GetString()!,
                clientSecret: pinterestConfig.GetProperty("ClientSecret").GetString()!);

            var pinterestClient = new PinterestClient(authService);
            var viewModel = new MainViewModel(pinterestClient);
            var mainWindow = new MainWindow(viewModel);

            // if launched via right-click with a file path, pre-fill it.
            if (e.Args.Length == 1)
            {
                viewModel.SelectedFilePath = e.Args[0];
            }

            mainWindow.Show();
        }
        catch (Exception ex)
        {
            // without this, any startup failure closes the app silently with no
            // explanation - this turns that into an actual visible error message
            MessageBox.Show(ex.ToString(), "Startup failed");
        }
    }

    private static void EnsureContextMenuRegistered()
    {
        string exePath = Environment.ProcessPath!;
        string expectedCommand = $"\"{exePath}\" \"%1\"";
        const string expectedLabel = "Upload with VPUploader";

        using var key = Registry.CurrentUser.CreateSubKey(
            @"Software\Classes\SystemFileAssociations\image\shell\UploadToPinterest");

        // the parent key's default value is the TEXT shown in the context menu
        if (key.GetValue(null) as string != expectedLabel)
        {
            key.SetValue(null, expectedLabel);
        }

        // the \command subkey's default value is the actual command that runs
        using var commandKey = key.CreateSubKey("command");
        if (commandKey.GetValue(null) as string != expectedCommand)
        {
            commandKey.SetValue(null, expectedCommand);
        }
    }
}