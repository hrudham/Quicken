using Core;
using Core.Entities;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UI.OperatingSystem;
using UI.OperatingSystem.HotKey;

namespace UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Fields

        private TaskbarIcon _taskBarIcon;
        private MainWindow _mainWindow = new MainWindow();

        #endregion

        #region Events

        public static event EventHandler MainWindowShown;

        #endregion

        #region Event Handlers

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Create a new instance of the main window.
            Application.Current.MainWindow = _mainWindow;

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

            // Register the MainWindowShown event handler.
            MainWindowShown += App_OnMainWindowShown;
        }

        /// <summary>
        /// Handles the OnMainWindowShown event of the App control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void App_OnMainWindowShown(object sender, EventArgs e)
        {
            _mainWindow.Display();
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

        #region Methods

        /// <summary>
        /// Shows the main window.
        /// </summary>
        public static void ShowMainWindow()
        {
            MainWindowShown(App.Current, null);
        }

        #endregion
    }
}
