using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage.Pickers;

namespace make_it_all_in_one.Pages
{
    public class AudioHomeViewModel
    {
        public AsyncRelayCommand OpenFileCommand { get; }
        public MainWindowViewModel NavViewModel { get; set; }

        public AudioHomeViewModel()
        {
            OpenFileCommand = new AsyncRelayCommand(OpenFile);
            NavViewModel = new MainWindowViewModel();
        }

        private async Task OpenFile()
        {
            var openPicker = new FileOpenPicker();
            var window = App.MainWindow;
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);
            var allowList = new List<string> { ".mp3", ".flac" };
            foreach (var item in allowList)
            {
                openPicker.FileTypeFilter.Add(item);
            }
            var openedFile = await openPicker.PickSingleFileAsync();

            if (openedFile != null)
            {
                // Navigate to the new page with the openedFile as a parameter
                NavViewModel.NavPath.Add(openedFile.Name);
                App.MainWindow.ContentFrame.Navigate(typeof(Pages.AudioEdit), openedFile);
            }
        }
    }

}
