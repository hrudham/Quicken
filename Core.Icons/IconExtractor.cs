using Core.Icons.Extensions;
using Core.Icons.Models;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Core.Icons
{
    public static class IconExtractor
    {
        #region Unmanaged Methods

        // http://msdn.microsoft.com/en-us/library/windows/desktop/ms648069(v=vs.85).aspx
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
        /// Gets the icon.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static byte[] GetIcon(string path)
        {
            return GetIcon(path, null);
        }

        /// <summary>
        /// Gets the icon.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public static byte[] GetIcon(string path, int? index)
        {
            var result = default(byte[]);

            if (!index.HasValue)
            {
                // If this value is zero, then the unmanaged ExtractIconEx 
                // function will pull out the first icon.
                index = 0;
            }

            if (path != null)
            {
                if (File.Exists(path))
                {
                    switch (System.IO.Path.GetExtension(path))
                    {
                        case ".library-ms":
                            result = GetLibraryIcon(path);
                            break;
                        default:
                            result = GetFileIcon(path, index.Value);
                            break;
                    }

                    if (result == null)
                    {
                        // This is the last resort if we really can't find an icon.
                        result = Icon.ExtractAssociatedIcon(path).ToByteArray();
                    }
                }
                else if (Directory.Exists(path))
                {
                    result = GetGenericIcon(path, true);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the file icon.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        private static byte[] GetFileIcon(string path, int index)
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

            if (iconData == null && File.Exists(path))
            {
                //TODO: path sometimes returns "%1". We need to handle this somehow.

                // This icon is probably just file that does not have an icon 
                // explicity associated with it, so attempt to get it's generic
                // icon as defined by the OS.
                iconData = GetExtensionIcon(Path.GetExtension(path));
            }

            return iconData;
        }

        /// <summary>
        /// Gets the default icon as defined by the Windows registry for the specified extension.
        /// </summary>
        /// <param name="extension">The extension.</param>
        /// <returns></returns>
        private static byte[] GetExtensionIcon(string extension)
        {
            if (!string.IsNullOrEmpty(extension))
            {
                var extensionKey = Registry.ClassesRoot.OpenSubKey(extension);
                if (extensionKey != null)
                {
                    var extensionValue = extensionKey.GetValue(string.Empty);

                    if (extensionValue != null)
                    {
                        var defaultIconKey = Registry.ClassesRoot.OpenSubKey(
                            string.Format("{0}\\DefaultIcon", extensionValue));

                        if (defaultIconKey != null)
                        {
                            return GetIconFromReference(
                                defaultIconKey.GetValue(string.Empty) as string);
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the library icon.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private static byte[] GetLibraryIcon(string path)
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

                return GetIconFromReference(reference);
            }

            return null;
        }
        
        /// <summary>
        /// Gets the directory or device icon.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private static byte[] GetGenericIcon(string path, bool isDirectoryOrDevice)
        {
            const uint SHGFI_ICON = 0x000000100;
            const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
            const uint SHGFI_OPENICON = 0x000000002;
            const uint SHGFI_LARGEICON = 0x000000000;

            const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

            // Get the folder icon
            byte[] icon = null;
            var shFileInfo = new ShellFileInfo();

            unsafe
            {
                try
                {
                    var flags = SHGFI_ICON | SHGFI_LARGEICON;

                    if (isDirectoryOrDevice)
                    {
                        flags = flags | SHGFI_USEFILEATTRIBUTES | SHGFI_OPENICON;
                    }

                    var result = SHGetFileInfo(
                        path,
                        FILE_ATTRIBUTE_DIRECTORY,
                        out shFileInfo,
                        (uint)Marshal.SizeOf(shFileInfo),
                        flags);

                    if (result == IntPtr.Zero)
                    {
                        throw Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }

                    // Load the icon from an HICON handle  
                    Icon.FromHandle(shFileInfo.hIcon);

                    icon = Icon.FromHandle(shFileInfo.hIcon).ToByteArray();
                }
                finally
                {
                    // Clean up, since this is unmanaged code
                    DestroyIcon(shFileInfo.hIcon);
                }
            }

            return icon;
        }

        /// <summary>
        /// Gets the icon from reference, like one in the form of "shell32.dll,4".
        /// </summary>
        /// <param name="reference">The reference.</param>
        /// <returns></returns>
        private static byte[] GetIconFromReference(string reference)
        {
            if (!string.IsNullOrEmpty(reference))
            {
                var ReferenceArray = reference.Split(',');

                var iconPath = Environment.ExpandEnvironmentVariables(ReferenceArray[0]);
                int iconIndex = 0;

                if (ReferenceArray.Length > 1)
                {
                    int.TryParse(ReferenceArray[1], out iconIndex);
                }

                return GetFileIcon(iconPath, iconIndex);
            }

            return null;
        }

        #endregion
    }
}
