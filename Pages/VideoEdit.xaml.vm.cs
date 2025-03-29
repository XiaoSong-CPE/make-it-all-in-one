using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using make_it_all_in_one.Utils;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace make_it_all_in_one.Pages
{

    // The ViewModel should be outside the Page class for easy instantiation and binding
    public partial class VideEditViewModel : ObservableObject
    {
        public AsyncRelayCommand SelectImageCommand { get; }
        public AsyncRelayCommand RemoveImageCommand { get; }
        public AsyncRelayCommand SaveAsCommand { get; }
        public VideEditViewModel()
        {
            SelectImageCommand = new AsyncRelayCommand(SelectImageFileAsync);
            RemoveImageCommand = new AsyncRelayCommand(RemoveImageFromFileAsync);
            SaveAsCommand = new AsyncRelayCommand(SaveAsAsync);
            rl = new ResourceLoader(); // Add this line to initialize the ResourceLoader
            fh = new FFmpegHelper();
        }
        private FFmpegHelper fh;
        private readonly ResourceLoader rl; // Add this line to declare the ResourceLoader

        private bool _isLoading = true;
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }
        private StorageFile? _selectedFile;
        public StorageFile? SelectedFile
        {
            get => _selectedFile;
            set => SetProperty(ref _selectedFile, value);
        }

        private string? _tempImageFilePath;
        public string? TempImageFilePath
        {
            get => _tempImageFilePath;
            set => SetProperty(ref _tempImageFilePath, value);
        }

        private string? _posterImageTitle;
        public string? PosterImageTitle
        {
            get => _posterImageTitle;
            set => SetProperty(ref _posterImageTitle, value);
        }
        private string? _posterImageDescription;
        public string? PosterImageDescription
        {
            get => _posterImageDescription;
            set => SetProperty(ref _posterImageDescription, value);
        }
        private List<FFmpegHelper.StreamDetail>? _streamDetails;
        public List<FFmpegHelper.StreamDetail>? StreamDetails
        {
            get => _streamDetails;
            set => SetProperty(ref _streamDetails, value);
        }
        public async Task CleanupTempFile()
        {
            if (!string.IsNullOrEmpty(_tempImageFilePath) && System.IO.File.Exists(_tempImageFilePath))
            {
                try
                {
                    Debug.WriteLine($"Cleaning up: {_tempImageFilePath}");
                    var file = await StorageFile.GetFileFromPathAsync(_tempImageFilePath);
                    await file.DeleteAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to delete temp file: {_tempImageFilePath}. Exception: {ex.Message}");
                }
            }
        }

        public async Task InitializeWithFileAsync(StorageFile file)
        {
            try
            {
                SelectedFile = file;
                StreamDetails = await fh.GetStreamDetails(file.Path);
                List<int> posterIndexes = fh.GetPosterIndexFromVideo(StreamDetails);
                if (posterIndexes.Count == 0)
                {
                    Debug.WriteLine("No poster stream found.");
                    PosterImageTitle = "No Poster Image";
                }
                else if (posterIndexes.Count == 1)
                {
                    TempImageFilePath = await fh.GetPosterFromVideo(file.Path, StreamDetails);
                    PosterImageTitle = "Poster Image Loaded";
                    var i = posterIndexes[0];
                    PosterImageDescription = $"Dimensions: {StreamDetails[i].width} x {StreamDetails[i].height}\n";
                    PosterImageDescription += $"Size: {(new System.IO.FileInfo(TempImageFilePath).Length).Bytes().Humanize()}\n";
                    PosterImageDescription += $"Format: {StreamDetails[i].codec_long_name}";
                }
                else
                {
                    throw new Exception("Multiple poster streams found.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing with file: {ex.Message}");
                await ShowErrorDialogAsync("Initialization Failed", ex.Message);
                // Since the init failed, we should navigate back to the home page
                App.MainWindow.ContentFrame.Navigate(typeof(HomePage));
                // TODO: navigation menu doen't follow the actuall navPath
            }
            finally
            {
                IsLoading = false;
            }
        }
        private async Task ShowErrorDialogAsync(string title,string message)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = rl.GetString("Button_OK"), // "OK"
                XamlRoot = App.MainWindow.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
        private async Task SelectImageFileAsync()
        {
            var file = await PickAFile(new List<string> { ".jpg", ".png", ".jpeg" });
            if (file != null)
            {
                try
                {
                    await CleanupTempFile();
                    string fileExt = Path.GetExtension(file.Path);
                    string newTempFile = Path.GetTempFileName() + fileExt;
                    System.IO.File.Copy(file.Path, newTempFile);
                    TempImageFilePath = newTempFile;
                    Debug.WriteLine($"TempImageFilePath: {TempImageFilePath}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading image: {ex.Message}");
                    await ShowErrorDialogAsync("Load Image Failed", ex.Message);
                }
            }
        }
        private async Task<StorageFile?> PickAFile(List<string> allowList)
        {
            var openPicker = new FileOpenPicker();
            var window = App.MainWindow;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
            foreach (var item in allowList)
            {
                openPicker.FileTypeFilter.Add(item);
            }
            return await openPicker.PickSingleFileAsync();
        }
        private async Task RemoveImageFromFileAsync()
        {
            await CleanupTempFile();
            TempImageFilePath = null;
        }
        private async Task SaveAsAsync()
        {
            var outputFile = await GetOutputFilePathAsync();
            if (outputFile != null)
            {
                try
                {

                    await fh.SaveVideoFile(SelectedFile!.Path, TempImageFilePath, outputFile, StreamDetails!);

                    var dialog = new ContentDialog
                    {
                        Title = rl.GetString("Success_Title"), // "Success"
                        Content = string.Format(rl.GetString("Success_Content"), outputFile.Path), // "Saved to {0}"
                        CloseButtonText = rl.GetString("Button_OK"), // "OK"
                        PrimaryButtonText = rl.GetString("Button_ShowInFolder"), // "Show in Folder"
                        XamlRoot = App.MainWindow.Content.XamlRoot
                    };

                    dialog.PrimaryButtonClick += async (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(outputFile.Path))
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = "explorer.exe",
                                    Arguments = $"/select, \"{outputFile.Path}\""
                                });
                            }
                            catch (Exception ex)
                            {
                                await ShowErrorDialogAsync("Show In Folder Failed", ex.Message);
                            }
                        }
                    };

                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving file: {ex.Message}");
                    await ShowErrorDialogAsync("Save File Failed", ex.Message);
                }
            }
            else
            {
                Debug.WriteLine("Save operation cancelled.");
            }
        }
        private async Task<StorageFile?> GetOutputFilePathAsync()
        {
            FileSavePicker savePicker = new FileSavePicker();
            var window = App.MainWindow;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
            var fileTypeName = "MP4 File";
            savePicker.FileTypeChoices.Add(fileTypeName, new List<string>() { ".mp4" });
            savePicker.SuggestedFileName = System.IO.Path.GetFileName(SelectedFile!.Path);
            var outputFile = await savePicker.PickSaveFileAsync();
            return outputFile;
        }
    }


}
