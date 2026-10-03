using System.Windows;
using Microsoft.Win32;
using VPUploader.App.ViewModels;
using VPUploader.Core;
// new using: needed for the FluentWindow base class
using Wpf.Ui.Controls;

namespace VPUploader.App;

// changed base class from Window to FluentWindow, to match the XAML root element
public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // await _viewModel.LoadBoardsAsync();
    }

    private void ChooseImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files (*.jpg;*.jpeg;*.png;*.gif;*.bmp)|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All files (*.*)|*.*",
        };

        if (dialog.ShowDialog() == true)
        {
            _viewModel.SelectedFilePath = dialog.FileName;
        }
    }
}