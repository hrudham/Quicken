using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Icons.Extensions
{
    static class IconExtensions
    {
        /// <summary>
        /// Converts an icon to a byte array in the PNG image format.
        /// </summary>
        /// <param name="icon">The icon.</param>
        /// <returns></returns>
        public static byte[] ToByteArray(this Icon icon)
        {
            return icon.ToByteArray(ImageFormat.Png);
        }

        /// <summary>
        /// Converts an icon to a byte array in the specified image format
        /// </summary>
        /// <param name="icon">The icon.</param>
        /// <param name="imageFormat">The image format.</param>
        /// <returns></returns>
        public static byte[] ToByteArray(this Icon icon, ImageFormat imageFormat)
        {
            if (icon != null)
            {
                using (MemoryStream iconMemoryStream = new MemoryStream())
                {
                    using (var bitmap = icon.ToBitmap())
                    {
                        bitmap.Save(iconMemoryStream, imageFormat);
                        return iconMemoryStream.ToArray();
                    }
                }
            }

            return null;
        }
    }
}
