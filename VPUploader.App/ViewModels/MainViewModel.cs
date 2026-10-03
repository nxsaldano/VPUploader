using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VPUploader.App.Commands;
using VPUploader.Core;
using VPUploader.Core.Models;

namespace VPUploader.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly PinterestClient _client;

    public MainViewModel(PinterestClient client)
    {
        _client = client;
        UploadCommand = new RelayCommand(async () => await UploadAsync(), () => CanUpload);
    }

    public ObservableCollection<Board> Boards { get; } = new();
    
    private Board? _selectedBoard;
    public Board? SelectedBoard
    {
        get => _selectedBoard;
        set { _selectedBoard = value;
            OnPropertyChanged();
            RaiseCanUploadChanged();
        }
    }

    private string? _selectedFilePath;
    public string? SelectedFilePath
    {
        get => _selectedFilePath;
        set
        {
            _selectedFilePath = value;
            OnPropertyChanged();
            RaiseCanUploadChanged();
        }
    }
    
    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set { _title = value; OnPropertyChanged(); }
    }

    private string _description = string.Empty;
    public string Description
    {
        get => _description;
        set { _description = value; OnPropertyChanged(); }
    }
    
    private string _link = string.Empty;
    public string Link
    {
        get => _link;
        set { _link = value; OnPropertyChanged(); }
    }
    
    private string _altText = string.Empty;
    public string AltText
    {
        get => _altText;
        set { _altText = value; OnPropertyChanged(); }
    }
    
    private string _statusMessage = string.Empty;
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }
    
    private bool _isUploading;
    public bool IsUploading
    {
        get => _isUploading;
        set { _isUploading = value; OnPropertyChanged(); RaiseCanUploadChanged(); }
    }
    
    private bool CanUpload => !IsUploading && SelectedBoard != null && !string.IsNullOrEmpty(SelectedFilePath);
        
    public RelayCommand UploadCommand { get; }
    
    public async Task LoadBoardsAsync()
    {
        var boards = await _client.GetBoardsAsync();
        Boards.Clear();
        foreach (var board in boards)
            Boards.Add(board);
    }
    
    private async Task UploadAsync()
    {
        if (SelectedBoard == null || SelectedFilePath == null)
            return;

        IsUploading = true;
        StatusMessage = "Uploading...";

        try
        {
            await _client.CreatePinAsync(new PinRequest
            {
                BoardId = SelectedBoard.Id,
                ImagePath = SelectedFilePath,
                Title = Title,
                Description = Description,
                Link = Link,
                AltText = AltText,
            });
            StatusMessage = "Uploaded successfully.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Upload failed: {ex.Message}";
        }
        finally
        {
            IsUploading = false;
        }
    }
    
    private void RaiseCanUploadChanged() => UploadCommand.RaiseCanExecuteChanged();

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    
}