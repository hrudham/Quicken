using Hardcodet.Wpf.TaskbarNotification;
using Quicken.UI.OperatingSystem;
using Quicken.UI.OperatingSystem.HotKey;
using System;
using System.Windows;
using System.Linq;
using System.Windows.Input;

namespace Quicken.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Fields

        private TaskbarIcon _taskBarIcon;

        #endregion
        
        #region Event Handlers

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Create a new instance of the main window.
            var mainWindow = new MainWindow();
            Application.Current.MainWindow = mainWindow;

            if (e.Args.Any(arg => arg.ToUpper().Equals("/refresh")))
            {
                mainWindow.Refresh();
            }

            //create the notifyicon (it's a resource declared in SystemTray/SystemTrayResources.xaml).
            _taskBarIcon = (TaskbarIcon)FindResource("SystemTrayIcon");

            // Register the hot-key that will show the application
            HotKeyManager.Register(
                Key.Space,
                OperatingSystem.KeyModifier.Alt,
                () => 
                    {
                        var command = GlobalCommands.ShowWindowCommand();

                        if (command.CanExecute(null))
                        {
                            command.Execute(null);
                        }
                    });
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Application.Exit" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.Windows.ExitEventArgs" /> that contains the event data.</param>
        protected override void OnExit(ExitEventArgs e)
        {
            _taskBarIcon.Dispose(); //the icon would clean up automatically, but this is cleaner.
            base.OnExit(e);
        }

        #endregion
    }
}
