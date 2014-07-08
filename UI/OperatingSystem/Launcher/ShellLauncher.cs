using Quicken.UI.Extensions;
using Quicken.UI.OperatingSystem.Launcher.Enumerations;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Quicken.UI.OperatingSystem.Launcher
{
    public static class ShellLauncher
    {
        #region Unmanaged Methods

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Wow64RevertWow64FsRedirection(IntPtr ptr);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);

        #endregion

        #region Methods

        /// <summary>
        /// Starts the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="runAsAdministrator">if set to <c>true</c> [run as administrator].</param>
        public static void Start(string path, bool runAsAdministrator)
        {
            IntPtr ptr = new IntPtr();

            try
            {
                // Disable file system redirection on this thread so that we can launch 
                // x64 applications from here even when compiled as x86.
                Wow64DisableWow64FsRedirection(ref ptr);

                /*
                    Note that this code is not complete; we are currently attempting to execute LNK 
                    files (that we can correctly resolve the path for), which are then attempting to 
                    execute their applications, which, if they are x64, cannot be found if this is
                    compiled for x86.
                 
                    This code still works fine for everything else, and if this application is 
                    compiled as x64 it all works, so I'm leaving this as is for now.
                */
                
                var startInfo = new ProcessStartInfo(path);
                startInfo.UseShellExecute = true;
                startInfo.Verb = runAsAdministrator ? Verbs.RunAs.GetDisplayName() : Verbs.Default.GetDisplayName();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                Process.Start(startInfo);
            }
            finally
            {
                // Always restore file system redirection.
                Wow64RevertWow64FsRedirection(ptr);
            }
        } 

        #endregion
    }
}
