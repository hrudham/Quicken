using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Core.Icons.Extensions;
using Core.Icons.Models;
using System.Xml.Linq;
using System.IO;
using System.Xml.XPath;

namespace Core.Icons
{
    public static class IconExtractor
    {
        #region Unmanaged Methods

        [DllImport("Shell32", CharSet = CharSet.Auto)]
        private static unsafe extern int ExtractIconEx(
            string lpszFile,
            int nIconIndex,
            IntPtr[] phIconLarge,
            IntPtr[] phIconSmall,
            int nIcons);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(
            string pszPath,
            uint dwFileAttributes,
            out ShellFileInfo psfi,
            uint cbFileInfo,
            uint uFlags);

        [DllImport("user32.dll", EntryPoint = "DestroyIcon", SetLastError = true)]
        private static unsafe extern int DestroyIcon(IntPtr hIcon);

        #endregion

        #region Methods

        /// <summary>
        /// Gets the file icon.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public static byte[] GetFileIcon(string path, int index)
        {
            var iconData = default(byte[]);
            var smallIconHandles = new IntPtr[1] { IntPtr.Zero };
            var largeIconHandles = new IntPtr[1] { IntPtr.Zero };

            unsafe
            {
                try
                {
                    var iconCount = ExtractIconEx(path, index, largeIconHandles, smallIconHandles, 1);

                    if (iconCount > 0 && largeIconHandles[0] != IntPtr.Zero)
                    {
                        iconData = Icon.FromHandle(largeIconHandles[0]).ToByteArray();
                    }
                }
                finally
                {
                    // Release all resources
                    foreach (IntPtr handle in largeIconHandles)
                    {
                        if (handle != IntPtr.Zero)
                        {
                            DestroyIcon(handle);
                        }
                    }

                    foreach (IntPtr handle in smallIconHandles)
                    {
                        if (handle != IntPtr.Zero)
                        {
                            DestroyIcon(handle);
                        }
                    }
                }
            }

            return iconData;
        }

        /// <summary>
        /// Gets the library icon.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static byte[] GetLibraryIcon(string path)
        {
            if (!Path.GetExtension(path).Equals(".library-ms", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Library files are expected to have the '.library-ms' file extension.");
            }

            // MS Library files are basically just XML files. 
            XDocument xDocument = XDocument.Load(path);

            var referenceElement = xDocument.Root.Elements().FirstOrDefault(element => element.Name.LocalName == "iconReference");

            if (referenceElement != null)
            {
                var reference = referenceElement.Value;

                if (!string.IsNullOrEmpty(reference))
                {
                    var ReferenceArray = reference.Split(',');

                    var iconPath = ReferenceArray[0];
                    int iconIndex = 0;

                    if (ReferenceArray.Length > 1)
                    {
                        int.TryParse(ReferenceArray[1], out iconIndex);
                    }

                    return IconExtractor.GetFileIcon(iconPath, iconIndex);
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the directory or device icon.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static byte[] GetDirectoryOrDeviceIcon(string path)
        {
            const uint SHGFI_ICON = 0x000000100;
            const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
            const uint SHGFI_OPENICON = 0x000000002;
            const uint SHGFI_LARGEICON = 0x000000000;

            const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

            // Get the folder icon
            Icon icon = null;
            var shFileInfo = new ShellFileInfo();

            unsafe
            {
                try
                {
                    var result = SHGetFileInfo(
                        path,
                        FILE_ATTRIBUTE_DIRECTORY,
                        out shFileInfo,
                        (uint)Marshal.SizeOf(shFileInfo),
                        SHGFI_ICON | SHGFI_USEFILEATTRIBUTES | SHGFI_OPENICON | SHGFI_LARGEICON);

                    if (result == IntPtr.Zero)
                    {
                        throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }

                    // Load the icon from an HICON handle  
                    Icon.FromHandle(shFileInfo.hIcon);

                    // Now clone the icon, so that it can be successfully stored in an ImageList
                    icon = (Icon)Icon.FromHandle(shFileInfo.hIcon).Clone();
                }
                finally
                {
                    // Clean up, since this is unmanaged code
                    DestroyIcon(shFileInfo.hIcon);
                }
            }

            // This may look odd, especially when one considers that 
            // this is a method call on a potentially null object.
            // However, this is an extension method which therefore 
            // to have the ability to handle this. 
            return icon.ToByteArray();
        }

        #endregion
    }
}
