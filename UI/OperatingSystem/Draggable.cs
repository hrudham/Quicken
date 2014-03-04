using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace UI.OperatingSystem
{
    /// <summary>
    /// Allows controls other than the title-bar to be clicked on in order to drag the window to a new location.
    /// Based on the code found here: http://www.codeproject.com/Articles/11114/Move-window-form-without-Titlebar-in-C
    /// </summary>
    internal static class Draggable
    {
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        [DllImportAttribute("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImportAttribute("user32.dll")]
        private static extern bool ReleaseCapture();

        /// <summary>
        /// Registers the window such that it can be dragged via it's body rather than the title bar.
        /// </summary>
        /// <param name="window">The window.</param>
        public static void Register(Window window)
        {
            Register(window, window);
        }

        /// <summary>
        /// Registers the an element within a window, allowing the user to use it to drag the window.
        /// Make sure that this method is called after the window has loaded; placing it in the 
        /// window's constructor will not work.
        /// </summary>
        /// <param name="window">The window.</param>
        /// <param name="element">The element.</param>
        public static void Register(Window window, UIElement element)
        {
            var windowHandle = new WindowInteropHelper(window).Handle;

            if (windowHandle.ToInt64() > 0)
            {
                element.MouseMove += delegate(object sender, MouseEventArgs e)
                {
                    if (e.Source == element)
                    {
                        Action moveWindow = () =>
                            {
                                if (e.LeftButton == MouseButtonState.Pressed)
                                {
                                    ReleaseCapture();
                                    SendMessage(
                                        windowHandle,
                                        WM_NCLBUTTONDOWN,
                                        HT_CAPTION, 0);
                                }
                            };

                        // This may seem a bit odd; I was encountering an odd exception with the 
                        // moveWindow code when I did not use the dispatcher and tried to drag off 
                        // of a control that was not the window itself. 
                        // More details here: http://stackoverflow.com/questions/7442943/dispatcher-throws-invalidoperationexception-on-messagebox-show-in-textchanged-ev#answer-7443519
                        Application.Current.Dispatcher.BeginInvoke(
                            DispatcherPriority.Normal,
                            moveWindow);
                    }
                };
            }
        }
    }
}
