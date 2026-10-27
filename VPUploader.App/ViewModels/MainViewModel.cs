using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VPUploader.App.Commands;
using VPUploader.Core;
using VPUploader.Core.Models;
using System.Diagnostics;

namespace VPUploader.App.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    // ---------- Dependencies ----------

    private readonly PinterestClient _client;
    private readonly ITokenProvider _auth;

    public MainViewModel(PinterestClient client, ITokenProvider auth)
    {
        _client = client;
        _auth = auth;
        UploadCommand = new RelayCommand(async () => await UploadAsync(), () => CanUpload);
        ConnectCommand = new RelayCommand(async () => await ConnectAsync(), () => true);
        OpenPinCommand = new RelayCommand(OpenPin, () => LastPinUrl != null);
    }

    // ---------- Commands ----------

    public RelayCommand UploadCommand { get; }
    public RelayCommand ConnectCommand { get; }
    public RelayCommand OpenPinCommand { get; }

    // ---------- Sign-in state ----------

    private bool _isSignedIn;
    public bool IsSignedIn
    {
        get => _isSignedIn;
        set
        {
            _isSignedIn = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotSignedIn));
        }
    }

    public bool IsNotSignedIn => !IsSignedIn;

    // ---------- Board and image selection ----------

    public ObservableCollection<Board> Boards { get; } = new();

    private Board? _selectedBoard;
    public Board? SelectedBoard
    {
        get => _selectedBoard;
        set
        {
            _selectedBoard = value;
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

    // ---------- Pin fields ----------

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

    // ---------- Upload status ----------

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
        set
        {
            _isUploading = value;
            OnPropertyChanged();
            RaiseCanUploadChanged();
        }
    }
    
    private string? _lastPinUrl;
    public string? LastPinUrl
    {
        get => _lastPinUrl;
        set
        {
            _lastPinUrl = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasPinLink));
        }
    }

    public bool HasPinLink => LastPinUrl != null;

    private bool CanUpload =>
        !IsUploading && SelectedBoard != null && !string.IsNullOrEmpty(SelectedFilePath);

    // ---------- Actions ----------

    // runs when the window loads: quietly loads boards if a login is saved
    public async Task InitializeAsync()
    {
        if (!_auth.HasSavedLogin)
        {
            StatusMessage = "Connect your Pinterest account to get started.";
            return;
        }

        try
        {
            await LoadBoardsAsync();
            IsSignedIn = true;
        }
        catch
        {
            // saved login no longer works (expired or revoked)
            StatusMessage = "Your session expired. Please connect again.";
        }
    }

    // runs when the Connect button is clicked: opens the browser login
    private async Task ConnectAsync()
    {
        try
        {
            StatusMessage = "Waiting for Pinterest in your browser...";
            await _auth.SignInAsync();
            await LoadBoardsAsync();
            IsSignedIn = true;
            StatusMessage = string.Empty;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Could not connect: {ex.Message}";
        }
    }

    public async Task LoadBoardsAsync()
    {
        var boards = await _client.GetBoardsAsync();

        // TEMPORARY: the Pinterest sandbox starts empty, so make a test board.
        // Remove this once we're on production.
        if (boards.Count == 0)
            boards.Add(await _client.CreateBoardAsync("VPUploader Sandbox"));

        Boards.Clear();
        foreach (var board in boards)
            Boards.Add(board);
    }

    private async Task UploadAsync()
    {
        if (SelectedBoard == null || SelectedFilePath == null)
            return;

        IsUploading = true;
        LastPinUrl = null;
        StatusMessage = "Uploading...";

        try
        {
            string pinId = await _client.CreatePinAsync(new PinRequest
            {
                BoardId = SelectedBoard.Id,
                ImagePath = SelectedFilePath,
                Title = Title,
                Description = Description,
                Link = Link,
                AltText = AltText,
            });
            LastPinUrl = $"https://www.pinterest.com/pin/{pinId}/";
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

    private void OpenPin()
    {
        if (LastPinUrl == null)
            return;

        // UseShellExecute makes Windows open the URL in the default browser
        Process.Start(new ProcessStartInfo(LastPinUrl) { UseShellExecute = true });
    }
    
    // ---------- Change notification (INotifyPropertyChanged) ----------

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}