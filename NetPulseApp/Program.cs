using NetPulseApp.Forms;
using NetPulseApp.Helper;
using NetPulseApp.Service.Dpi;

namespace NetPulseApp
{
    internal static class Program
    {
        private static readonly string MutexName = $@"Local\{AppConfig.AppDataFolderName}.Instance";
        private static Mutex _singleInstanceMutex;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            _singleInstanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirstInstance);
            if (!isFirstInstance)
            {
                Managers.AlertNotificationService.ShowInfo($"{AppConfig.AppName} is already running.", AppConfig.AppName);
                _singleInstanceMutex.Dispose();
                return;
            }

            // Initialize DPI awareness FIRST
            DpiAwareService.Initialize();

            // Enable visual styles for modern appearance
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Set high DPI support for modern displays
            Application.SetHighDpiMode(HighDpiMode.SystemAware);

            Application.Run(new MainForm());
        }
    }
}
