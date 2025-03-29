using System.Diagnostics;
using Microsoft.UI.Xaml;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace make_it_all_in_one
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();

            var rl = new Windows.ApplicationModel.Resources.ResourceLoader();
            Debug.WriteLine("Say Hallo:");
            Debug.WriteLine(rl.GetString("Button_ShowInFolder"));
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>

        public static MainWindow MainWindow = new();

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = MainWindow;
            m_window.Activate();
        }

        private Window? m_window;
    }
}
