using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Quicken.UI.OperatingSystem
{
    internal static class GlobalCommands
    {
        /// <summary>
        /// Shows a window, if none is already open.
        /// </summary>
        public static ICommand ShowWindowCommand()
        {
            return new DelegateCommand
            {
                CanExecuteFunc = () =>
                    Application.Current != null &&
                    Application.Current.MainWindow != null,
                CommandAction = () =>
                {
                    Application.Current.MainWindow.Show();
                }
            };
        }

        /// <summary>
        /// Hides the main window. This command is only enabled if a window is open.
        /// </summary>
        public static ICommand HideWindowCommand()
        {
            return new DelegateCommand
            {
                CanExecuteFunc = () =>
                    Application.Current != null &&
                    Application.Current.MainWindow != null && 
                    Application.Current.MainWindow.IsVisible,
                CommandAction = () => 
                    Application.Current.MainWindow.Hide()
            };
        }

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        public static ICommand ExitApplicationCommand()
        {
            return new DelegateCommand
            {
                CommandAction = Application.Current.Shutdown
            };
        } 
    }
}
