using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Core.Lnk.Extensions;

namespace Core.Lnk
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;

        public int iIcon;

        public uint dwAttributes;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };

    public static class IconExtractor
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// Extracts the icon.
        /// </summary>
        /// <param name="targetPath">The target path.</param>
        /// <param name="iconPath">The file.</param>
        /// <param name="iconIndex">The number.</param>
        /// <returns></returns>
        public static byte[] ExtractFileIcon(string targetPath, string iconPath, int iconIndex)
        {
            // Attempt to extract the icon at the specified index.
            /*if (File.Exists(iconPath))
            {
                if (iconIndex >= 0)
                {
                    var iconHandle = default(IntPtr);
                    var smallIconHandle = default(IntPtr);

                    ExtractIconEx(iconPath, iconIndex, out iconHandle, out smallIconHandle, 1);

                    if (iconHandle != NullPointer)
                    {
                        using (MemoryStream iconMemoryStream = new MemoryStream())
                        {
                            using (var bitmap = Icon.FromHandle(iconHandle).ToBitmap())
                            {
                                bitmap.Save(iconMemoryStream, ImageFormat.Png);
                                return iconMemoryStream.ToArray();
                            }
                        }
                    }
                }
                else if (iconIndex < 0)
                {
                    // This is an icon group. This is not implemented yet, and 
                    // thus falls back to the ExtractAssociatedIcon code below.
                }
            }*/

            var result = default(byte[]);

            if (targetPath != null)
            {
                if (File.Exists(targetPath))
                {
                    result = Icon.ExtractAssociatedIcon(targetPath).ToByteArray();
                }
                else if (Directory.Exists(targetPath))
                {
                    result = ExtractDirectoryOrDeviceIcon(targetPath);
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts the directory or device icon.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static byte[] ExtractDirectoryOrDeviceIcon(string path)
        {
            uint SHGFI_ICON = 0x000000100; 
            uint SHGFI_USEFILEATTRIBUTES = 0x000000010; 
            uint SHGFI_OPENICON = 0x000000002; 
            //uint SHGFI_SMALLICON = 0x000000001; 
            uint SHGFI_LARGEICON = 0x000000000; 

            uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;

            uint flags = SHGFI_ICON | SHGFI_USEFILEATTRIBUTES | SHGFI_OPENICON | SHGFI_LARGEICON;

            // Get the folder icon    
            Icon icon = null;
            var shFileInfo = new SHFILEINFO();

            try
            {
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

                // Now clone the icon, so that it can be successfully stored in an ImageList
                icon = (Icon)Icon.FromHandle(shFileInfo.hIcon).Clone();
            }
            finally
            {
                // Clean up, since this is unmanaged code
                DestroyIcon(shFileInfo.hIcon);
            }

            // This may look odd, especially when one considers that 
            // this is a method call on a potentially null object.
            // However, this is an extension method which therefore 
            // to have the ability to handle this. 
            return icon.ToByteArray();
        }
    }
}
