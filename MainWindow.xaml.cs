using System;
using System.Diagnostics;
using make_it_all_in_one.Pages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace make_it_all_in_one
{
    public sealed partial class MainWindow : Window
    {
        public MainWindowViewModel NavViewModel;
        public MainWindow()
        {
            this.InitializeComponent();
            this.SetTitleBar(AppTitleBar);
            this.ExtendsContentIntoTitleBar = true;

            // Initialise NavViewModel
            NavViewModel = new MainWindowViewModel();

            // Initialize NavigationView
            InitializeNavigation();
        }

        // Expose the contentFrame as a public property
        public Frame ContentFrame => contentFrame;

        private void InitializeNavigation()
        {
            // Set default page
            NavigateToPage("HomePage");

            // Dynamically select the menu item with the "HomePage" tag
            foreach (var item in nvSample.MenuItems)
            {
                if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "HomePage")
                {
                    nvSample.SelectedItem = navItem;
                    break;
                }
            }
        }

        private void NavigateToPage(string pageTag)
        {
            Type? pageType = pageTag switch
            {
                "HomePage" => typeof(HomePage),
                "VideoPage" => typeof(VideoHome),
                "AudioPage" => typeof(AudioHome),
                "ModelPage" => typeof(ModelHome),
                "SettingsPage" => typeof(SettingsPage),
                _ => null
            };

            if (pageType != null)
            {
                contentFrame.Navigate(pageType);
            }
            else
            {
                Debug.WriteLine($"Unknown page tag: {pageTag}");
            }
        }

        private void MenuSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                NavigateToPage("SettingsPage");
                return;
            }

            if (args.SelectedItem is NavigationViewItem selectedItem)
            {
                string selectedTag = selectedItem.Tag?.ToString();
                NavigateToPage(selectedTag);
            }
            else
            {
                Debug.WriteLine("SelectedItem is not a NavigationViewItem.");
            }
        }
    }
}