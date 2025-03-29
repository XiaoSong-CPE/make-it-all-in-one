using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using TagLib;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace make_it_all_in_one.Pages
{

    public partial class MusicFileModifierViewModel : ObservableObject
    {
        private TagLib.File _tFile;
        public TagLib.File TFile
        {
            get => _tFile;
            set => SetProperty(ref _tFile, value);
        }

        private string? _selectedFilePath;
        public string? SelectedFilePath
        {
            get => _selectedFilePath;
            set => SetProperty(ref _selectedFilePath, value);
        }

        private string? _tempFilePath;

        private ImageSource? _posterImage;
        public ImageSource? PosterImage
        {
            get => _posterImage;
            set
            {
                if (SetProperty(ref _posterImage, value))
                {
                    if (value != null)
                    {
                        PosterImageTitle = "Poster Image Loaded";
                    }
                    else
                    {
                        PosterImageTitle = "No Poster Image";
                        PosterImageDescription = "";
                    }
                }
            }
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

        private string? _originalFileExtension;

        // #region Music Metadata Properties
        private string _musicTitle;
        public string MusicTitle
        {
            get => _musicTitle;
            set
            {
                if (SetProperty(ref _musicTitle, value) && TFile != null)
                {
                    TFile.Tag.Title = value;
                }
            }
        }
        private string[] _musicArtist;
        public string[] MusicArtist
        {
            get => _musicArtist;
            set
            {
                if (SetProperty(ref _musicArtist, value) && TFile != null)
                {
                    TFile.Tag.Performers = value;
                }
            }
        }

        private string _musicAlbum;
        public string MusicAlbum
        {
            get => _musicAlbum;
            set
            {
                if (SetProperty(ref _musicAlbum, value) && TFile != null)
                {
                    TFile.Tag.Album = value;
                }
            }
        }

        private uint _musicYear;
        public uint MusicYear
        {
            get => _musicYear;
            set
            {
                if (SetProperty(ref _musicYear, value) && TFile != null)
                {
                    TFile.Tag.Year = value;
                }
            }
        }

        private uint _musicTrack;
        public uint MusicTrack
        {
            get => _musicTrack;
            set
            {
                if (SetProperty(ref _musicTrack, value) && TFile != null)
                {
                    TFile.Tag.Track = value;
                }
            }
        }

        private string[] _musicGenre;
        public string[] MusicGenre
        {
            get => _musicGenre;
            set
            {
                if (SetProperty(ref _musicGenre, value) && TFile != null)
                {
                    TFile.Tag.Genres = value;
                }
            }
        }

        private string[] _musicAlbumArtist;
        public string[] MusicAlbumArtist
        {
            get => _musicAlbumArtist;
            set
            {
                if (SetProperty(ref _musicAlbumArtist, value) && TFile != null)
                {
                    TFile.Tag.AlbumArtists = value;
                }
            }
        }

        private string[] _musicComposer;
        public string[] MusicComposer
        {
            get => _musicComposer;
            set
            {
                if (SetProperty(ref _musicComposer, value) && TFile != null)
                {
                    TFile.Tag.Composers = value;
                }
            }
        }

        private uint _musicDiscnumber;
        public uint MusicDiscnumber
        {
            get => _musicDiscnumber;
            set
            {
                if (SetProperty(ref _musicDiscnumber, value) && TFile != null)
                {
                    TFile.Tag.Disc = value;
                }
            }
        }

        private string _musicLyrics;
        public string MusicLyrics
        {
            get => _musicLyrics;
            set
            {
                if (SetProperty(ref _musicLyrics, value) && TFile != null)
                {
                    TFile.Tag.Lyrics = value;
                }
            }
        }

        // #endregion

        public AsyncRelayCommand SelectImageCommand { get; }
        public AsyncRelayCommand RemoveImageCommand { get; }
        public AsyncRelayCommand SaveAsCommand { get; }

        public MusicFileModifierViewModel()
        {
            SelectImageCommand = new AsyncRelayCommand(SelectImageFileAsync);
            RemoveImageCommand = new AsyncRelayCommand(RemoveImageFromFileAsync);
            SaveAsCommand = new AsyncRelayCommand(SaveAsAsync);
            rl = new ResourceLoader();
        }
        private readonly ResourceLoader rl; // Add this line to declare the ResourceLoader

        public async Task InitializeWithFileAsync(StorageFile file)
        {
            SelectedFilePath = file.Path;

            if (!string.IsNullOrEmpty(SelectedFilePath))
            {
                try
                {
                    _originalFileExtension = Path.GetExtension(SelectedFilePath).ToLowerInvariant();
                    _tempFilePath = Path.GetTempFileName() + _originalFileExtension;
                    System.IO.File.Copy(SelectedFilePath, _tempFilePath, true);
                    TFile = TagLib.File.Create(_tempFilePath);

                    // Debug all available tags
                    DebugAllTags(TFile);

                    // Initialize music metadata properties
                    MusicTitle = TFile.Tag.Title ?? string.Empty;
                    MusicArtist = TFile.Tag.Performers ?? Array.Empty<string>();
                    MusicAlbum = TFile.Tag.Album ?? string.Empty;
                    MusicYear = TFile.Tag.Year;
                    MusicTrack = TFile.Tag.Track;
                    MusicGenre = TFile.Tag.Genres ?? Array.Empty<string>();
                    MusicAlbumArtist = TFile.Tag.AlbumArtists ?? Array.Empty<string>();
                    MusicComposer = TFile.Tag.Composers ?? Array.Empty<string>();
                    MusicDiscnumber = TFile.Tag.Disc;
                    MusicLyrics = TFile.Tag.Lyrics;

                    // Extract poster image if available
                    if (TFile.Tag.Pictures != null && TFile.Tag.Pictures.Length > 0)
                    {
                        var picture = TFile.Tag.Pictures[0];
                        var imageData = picture.Data.Data;
                        await LoadAlbumArtAsync(imageData);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Unexpected error: {ex.Message}");
                    await ShowErrorDialogAsync($"An unexpected error occurred. ({ex.Message})");
                }
            }
        }

        private void DebugAllTags(TagLib.File file)
        {
            Debug.WriteLine("Available Tags:");
            foreach (var tag in file.Tag.GetType().GetProperties())
            {
                try
                {
                    var value = tag.GetValue(file.Tag);
                    if (value is IEnumerable<string> stringEnumerable)
                    {
                        Debug.WriteLine($"{tag.Name}: {string.Join(", ", stringEnumerable)}");
                    }
                    else
                    {
                        Debug.WriteLine($"{tag.Name}: {value}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error reading tag {tag.Name}: {ex.Message}");
                }
            }
        }

        private async Task LoadAlbumArtAsync(byte[] imageData)
        {
            using (var stream = new InMemoryRandomAccessStream())
            {
                await stream.WriteAsync(imageData.AsBuffer());
                stream.Seek(0);
                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                PosterImage = bitmap;
                PosterImageDescription = $"Dimensions: {bitmap.PixelWidth} x {bitmap.PixelHeight}\n";
                PosterImageDescription += $"Size: {imageData.Length.Bytes().Humanize()}\n";
                PosterImageDescription += $"Format: {TFile.Tag.Pictures[0].MimeType}";
            }
        }

        public void CleanupTempFile()
        {
            if (!string.IsNullOrEmpty(_tempFilePath) && System.IO.File.Exists(_tempFilePath))
            {
                try
                {
                    System.IO.File.Delete(_tempFilePath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to delete temp file: {_tempFilePath}. Exception: {ex.Message}");
                }
            }
        }

        private async Task SaveAsAsync()
        {
            if (string.IsNullOrEmpty(SelectedFilePath))
            {
                await ShowErrorDialogAsync(rl.GetString("Error_SelectMusicFile")); // "Please select a music file."
                return;
            }

            var outputFilePath = await GetOutputFilePathAsync();
            if (outputFilePath != null)
            {
                try
                {
                    TFile.Save();
                    System.IO.File.Copy(_tempFilePath, outputFilePath, true);
                    var dialog = new ContentDialog
                    {
                        Title = rl.GetString("Success_Title"), // "Success"
                        Content = string.Format(rl.GetString("Success_Content"), outputFilePath), // "Saved to {0}"
                        CloseButtonText = rl.GetString("Button_OK"), // "OK"
                        PrimaryButtonText = rl.GetString("Button_ShowInFolder"), // "Show in Folder"
                        XamlRoot = App.MainWindow.Content.XamlRoot
                    };

                    dialog.PrimaryButtonClick += async (sender, args) =>
                    {
                        if (!string.IsNullOrEmpty(outputFilePath))
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = "explorer.exe",
                                    Arguments = $"/select, \"{outputFilePath}\""
                                });
                            }
                            catch (Exception ex)
                            {
                                await ShowErrorDialogAsync(string.Format(rl.GetString("Error_OpenFolder"), ex.Message)); // "Failed to open folder: {0}"
                            }
                        }
                    };

                    await dialog.ShowAsync();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error saving file: {ex.Message}");
                    await ShowErrorDialogAsync(rl.GetString("Error_SaveFile")); // "Failed to save the file."
                }
            }
            else
            {
                Debug.WriteLine("Save operation cancelled.");
            }
        }

        private async Task SelectImageFileAsync()
        {
            var file = await PickAFile(new List<string> { ".jpg", ".png", ".jpeg" });
            if (file != null)
            {
                try
                {
                    var buffer = await FileIO.ReadBufferAsync(file);
                    var bytes = buffer.ToArray();
                    var picture = new TagLib.Picture(new TagLib.ByteVector(bytes))
                    {
                        Type = PictureType.FrontCover
                    };
                    TFile.Tag.Pictures = new TagLib.IPicture[] { picture };
                    await LoadAlbumArtAsync(bytes);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading image: {ex.Message}");
                    await ShowErrorDialogAsync(rl.GetString("Error_LoadImage")); // "Error loading image."
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

        private async Task<string?> GetOutputFilePathAsync()
        {
            if (string.IsNullOrEmpty(_originalFileExtension))
                return null;

            FileSavePicker savePicker = new FileSavePicker();
            var window = App.MainWindow;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);
            var fileTypeName = _originalFileExtension switch
            {
                ".mp3" => "MP3 Audio",
                ".flac" => "FLAC Audio",
                ".m4a" => "MPEG-4 Audio",
                _ => "Music File"
            };
            savePicker.FileTypeChoices.Add(fileTypeName, new List<string>() { _originalFileExtension });
            savePicker.SuggestedFileName = System.IO.Path.GetFileNameWithoutExtension(SelectedFilePath) + _originalFileExtension;

            var outputFile = await savePicker.PickSaveFileAsync();
            return outputFile?.Path;
        }

        private async Task RemoveImageFromFileAsync()
        {
            if (string.IsNullOrEmpty(SelectedFilePath))
            {
                await ShowErrorDialogAsync(rl.GetString("Error_SelectMusicFile")); // "Please select a music file."
                return;
            }

            TFile.Tag.Pictures = new IPicture[0];
            PosterImage = null;
        }

        private async Task ShowErrorDialogAsync(string message)
        {
            var dialog = new ContentDialog
            {
                Title = rl.GetString("Error_Title"), // "Error"
                Content = message,
                CloseButtonText = rl.GetString("Button_OK"), // "OK"
                XamlRoot = App.MainWindow.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}