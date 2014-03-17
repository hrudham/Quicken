using Core.Lnk.Enumerations;
using Core.Lnk.ExtraData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Core.Lnk.Extensions
{
    public static class BinaryReaderExtensions
    {
        /// <summary>
        /// Reads a string null terminated.
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns></returns>
        public static string ReadStringNullTerminated(this BinaryReader binaryReader, Encoding encoding)
        {
            var bytes = new List<byte>();
            var byteCount = encoding.GetByteCount(" ");

            while (true)
            {
                var currentByte = binaryReader.ReadBytes(byteCount);

                if (currentByte.First() == 0)
                {
                    break;
                }

                bytes.AddRange(currentByte);
            }

            return encoding.GetString(bytes.ToArray());
        }

        /// <summary>
        /// Reads the string data.
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns></returns>
        public static string ReadStringData(this BinaryReader binaryReader, Encoding encoding)
        {
            int size = binaryReader.ReadUInt16();

            return encoding.GetString(
                binaryReader.ReadBytes(
                    size * encoding.GetByteCount(" ")));
        }

        /// <summary>
        /// Skips the string data.
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        /// <param name="encoding">The encoding.</param>
        public static void SkipStringData(this BinaryReader binaryReader, Encoding encoding)
        {
            int size = binaryReader.ReadUInt16();
            binaryReader.BaseStream.Seek(size * encoding.GetByteCount(" "), SeekOrigin.Current);
        }

        private static ExtraDataBlock ReadStringExtraDataBlock(this BinaryReader binaryReader, ExtraDataBlockSignature signature)
        {
            var defaultValue = binaryReader.ReadExtraDataBlockStringEncoded(Encoding.Default);
            var unicodeValue = binaryReader.ReadExtraDataBlockStringEncoded(Encoding.Unicode);

            var value = unicodeValue;
            if (string.IsNullOrEmpty(unicodeValue))
            {
                value = defaultValue;
            }

            return new ExtraDataBlock()
            {
                Signature = signature,
                Value = value
            };
        }

        /// <summary>
        /// Reads the extra data block.
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns></returns>
        internal static ExtraDataBlock ReadExtraDataBlock(this BinaryReader binaryReader)
        {
            int blockSize = binaryReader.ReadInt32();
            if (blockSize < 0x4)
            {
                return null;
            }

            var signature = (ExtraDataBlockSignature)(binaryReader.ReadInt32());

            var extraDataBlock = default(ExtraDataBlock);

            switch (signature)
            {
                // Raw
                case ExtraDataBlockSignature.UnknownDataBlock:
                case ExtraDataBlockSignature.ConsoleDataBlock:
                case ExtraDataBlockSignature.ConsoleFEDataBlock:
                case ExtraDataBlockSignature.PropertyStoreDataBlock:
                case ExtraDataBlockSignature.ShimDataBlock:
                case ExtraDataBlockSignature.TrackerDataBlock:
                case ExtraDataBlockSignature.VistaAndAboveIDListDataBlock:
                // Base Folder ID
                case ExtraDataBlockSignature.KnownFolderDataBlock:
                case ExtraDataBlockSignature.SpecialFolderDataBlock:
                    {
                        // Skip reading these extra data blocks.
                        binaryReader.BaseStream.Seek(blockSize - Marshal.SizeOf(blockSize) - Marshal.SizeOf(typeof(int)), SeekOrigin.Current);
                        extraDataBlock = new ExtraDataBlock()
                            {
                                Signature = signature
                            };
                        break;
                    }
                // String
                case ExtraDataBlockSignature.DarwinDataBlock:
                case ExtraDataBlockSignature.EnvironmentVariableDataBlock:
                case ExtraDataBlockSignature.IconEnvironmentDataBlock:
                    {
                        extraDataBlock = binaryReader.ReadStringExtraDataBlock(signature);
                        break;
                    }
            }

            return extraDataBlock;
        }

        /// <summary>
        /// Reads the encoded extra data block.
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        /// <param name="encoding">The encoding.</param>
        /// <param name="padding">The padding.</param>
        /// <returns></returns>
        private static string ReadExtraDataBlockStringEncoded(this BinaryReader binaryReader, Encoding encoding)
        {
            var valueSize = 260;

            if (encoding == Encoding.Unicode)
            {
                valueSize = 520;
            }

            var currentOffset = binaryReader.BaseStream.Position;

            var extraDataBlockString = binaryReader.ReadStringNullTerminated(encoding);

            binaryReader.BaseStream.Seek(currentOffset + valueSize, SeekOrigin.Begin);

            return extraDataBlockString;
        }
    }
}
