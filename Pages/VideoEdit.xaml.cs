using System.Diagnostics;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage;

namespace make_it_all_in_one.Pages
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VideoEdit : Page
    {

        // Create an instance of the ViewModel
        public VideEditViewModel ViewModel { get; set; }

        public VideoEdit()
        {
            this.InitializeComponent();
            ViewModel = new VideEditViewModel();
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
                Debug.WriteLine("Error navigating to `Pages.AudioEdit`, returning to the home page...");
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
