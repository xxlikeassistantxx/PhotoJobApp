using System;
using System.IO;
using System.Text;

namespace PhotoJobApp
{
    public partial class LogViewerPage : ContentPage
    {
        public LogViewerPage()
        {
            InitializeComponent();
            LoadLogs();
        }

        private void LoadLogs()
        {
            try
            {
#if IOS
				var latestPath = PersistentLogger.GetLatestLogPath();

				if (string.IsNullOrEmpty(latestPath) || !File.Exists(latestPath))
				{
					LogContentLabel.Text = "No log file found. Launch the app on device to start collecting logs.";
					LogContentLabel.TextColor = Colors.Gray;
					return;
				}

				var logContent = File.ReadAllText(latestPath);

				if (string.IsNullOrWhiteSpace(logContent))
				{
					LogContentLabel.Text = "No activity recorded in the last 10 minutes.";
					LogContentLabel.TextColor = Colors.Gray;
				}
				else
				{
					LogContentLabel.Text = logContent;
					LogContentLabel.TextColor = Colors.Black;
				}
#else
				LogContentLabel.Text = "Persistent device logging is only available on iOS builds.\n\nUse the platform debugger or console to view logs.";
				LogContentLabel.TextColor = Colors.Gray;
#endif
            }
            catch (Exception ex)
            {
                LogContentLabel.Text = $"Error loading logs:\n{ex.Message}\n\n{ex.StackTrace}";
                LogContentLabel.TextColor = Colors.Red;
            }
        }

        private void OnRefreshClicked(object sender, EventArgs e)
        {
            LoadLogs();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Refresh logs when page appears
            LoadLogs();
        }
    }
}

