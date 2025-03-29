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

    public sealed partial class AudioEdit : Page
    {
        public MusicFileModifierViewModel ViewModel { get; set; }
        public AudioEdit()
        {
            this.InitializeComponent();
            ViewModel = new MusicFileModifierViewModel();
            this.DataContext = ViewModel;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is StorageFile openedFile)
            {
                Debug.WriteLine($"Received the file: {openedFile.Path}");
                _ = ViewModel.InitializeWithFileAsync(openedFile);
            }
            else
            {
                Debug.WriteLine("Error navigating to Pages.AudioEdit, returning to the home page...");
                App.MainWindow.ContentFrame.Navigate(typeof(Pages.HomePage));
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.CleanupTempFile();
        }
    }

}