using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace make_it_all_in_one
{
    public class MainWindowViewModel : ObservableObject
    {

        // Navigation
        private string _selectedPageTag = "HomePage";
        public string SelectedPageTag
        {
            get => _selectedPageTag;
            set => SetProperty(ref _selectedPageTag, value);
        }

        private List<string> _navPath = new List<string>();
        public List<string> NavPath
        {
            get => _navPath;
            set => SetProperty(ref _navPath, value);
        }

        public Frame ContentFrame => App.MainWindow.ContentFrame;

    }

}