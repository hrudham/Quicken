using Core.Lnk.Enumerations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Core.Lnk.Extensions;
using System.Drawing;
using System.Drawing.Imaging;

namespace Core.Lnk
{
    /// <summary>
    /// This class parses Windows LNK files. It was highly influenced by the following sources:
    /// - Shellify: https://github.com/sailro/Shellify
    /// - WindowsShortcuts: https://github.com/codebling/WindowsShortcuts/blob/master/org/stackoverflowusers/file/WindowsShortcut.java
    /// - The Shell Link (.LNK) Technical Document: http://msdn.microsoft.com/en-us/library/dd871305.aspx
    /// 
    /// Note that this is not a full implementation; it simply pulls what I need, which isn't much.
    /// Look at Shellify you want a more complete solution.
    /// </summary>
    public class LnkInfo
    {
        #region Fields

        private static IntPtr NullPointer = default(IntPtr);
        
        private int _flags;

        #endregion

        #region Unmanaged Methods

        [DllImport("Shell32.dll", EntryPoint = "ExtractIconExW", CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern int ExtractIconEx(string sFile, int iIndex, out IntPtr piLargeVersion, out IntPtr piSmallVersion, int amountIcons);

        #endregion

        #region Properties

        public string Path { get; private set; }
        public string TargetPath { get; private set; }
        public string Name { get; private set; }
        public string Comment { get; private set; }
        public string RelativePath { get; private set; }
        public byte[] IconData { get; private set; }
        public FileAttributes FileAttributes { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LnkInfo" /> class.
        /// </summary>
        /// <param name="path">The path.</param>
        public LnkInfo(string path)
        {
            this.Path = path;
            this.Name = System.IO.Path.GetFileNameWithoutExtension(path);

            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (var binaryReader = new BinaryReader(fileStream))
                {
                    var targetPath = default(string);
                    var iconPath = default(string);

                    // Read the lnk flags.
                    fileStream.Seek(0x14, SeekOrigin.Begin);
                    this._flags = binaryReader.ReadInt32();
                    var flagEncoding = this.CheckFlag(LinkFlags.IsUnicode) ? Encoding.Unicode : Encoding.Default;

                    // Read the file attributes
                    fileStream.Seek(0x18, SeekOrigin.Begin);
                    this.FileAttributes = (FileAttributes)binaryReader.ReadInt32();
                    
                    // Read the icon index.
                    fileStream.Seek(0x38, SeekOrigin.Begin);
                    var iconIndex = binaryReader.ReadInt32();

                    fileStream.Seek(0x4C, SeekOrigin.Begin);

                    // Read link target ID list section
                    if (this.CheckFlag(LinkFlags.HasLinkTargetIDList))
                    {
                        short idListSize = binaryReader.ReadInt16();

                        // Skip the target ID list
                        if (idListSize > 0)
                        {
                            fileStream.Seek(idListSize, SeekOrigin.Current);
                        }
                    }

                    // Read link info section
                    if (this.CheckFlag(LinkFlags.HasLinkInfo))
                    {
                        var infoSectionOffset = binaryReader.BaseStream.Position;

                        var linkInfoSize = binaryReader.ReadInt32();
                        var linkInfoHeaderSize = binaryReader.ReadInt32();
                        var linkInfoFlags = (InfoFlags)(binaryReader.ReadInt32());

                        fileStream.Seek(0x04, SeekOrigin.Current);

                        var localBasePathOffset = binaryReader.ReadInt32();

                        fileStream.Seek(0x04, SeekOrigin.Current);

                        var commonPathSuffixOffset = binaryReader.ReadInt32();

                        int? localBasePathOffsetUnicode = null;
                        int? commonPathSuffixOffsetUnicode = null;

                        if (linkInfoHeaderSize > 0x1C)
                        {
                            localBasePathOffsetUnicode = binaryReader.ReadInt32();
                            commonPathSuffixOffsetUnicode = binaryReader.ReadInt32();
                        }
                            
                        if ((linkInfoFlags & InfoFlags.VolumeIDAndLocalBasePath) > 0)
                        {
                            fileStream.Seek(
                                infoSectionOffset + (localBasePathOffsetUnicode.HasValue ? localBasePathOffsetUnicode.Value : localBasePathOffset),
                                SeekOrigin.Begin);

                            var localBasePath = binaryReader.ReadStringNullTerminated(
                                localBasePathOffsetUnicode.HasValue ? Encoding.Unicode : Encoding.Default);

                            fileStream.Seek(
                                infoSectionOffset + (commonPathSuffixOffsetUnicode.HasValue ? commonPathSuffixOffsetUnicode.Value : commonPathSuffixOffset),
                                SeekOrigin.Begin);

                            var commonPathSuffix = binaryReader.ReadStringNullTerminated(
                                commonPathSuffixOffsetUnicode.HasValue ? Encoding.Unicode : Encoding.Default);

                            targetPath = System.IO.Path.Combine(localBasePath, commonPathSuffix);
                        }

                        fileStream.Seek(infoSectionOffset + linkInfoSize, SeekOrigin.Begin);
                    }

                    // Read string data
                    if (this.CheckFlag(LinkFlags.HasName))
                    {
                        this.Comment = binaryReader.ReadStringData(flagEncoding);
                    }

                    if (this.CheckFlag(LinkFlags.HasRelativePath))
                    {
                        this.RelativePath = binaryReader.ReadStringData(flagEncoding);

                        targetPath = System.IO.Path.Combine(
                            System.IO.Path.GetDirectoryName(path), 
                            this.RelativePath);
                    }

                    if (this.CheckFlag(LinkFlags.HasWorkingDir))
                    {
                        binaryReader.SkipStringData(flagEncoding);
                    }

                    if (this.CheckFlag(LinkFlags.HasArguments))
                    {
                        binaryReader.SkipStringData(flagEncoding);
                    }

                    if (this.CheckFlag(LinkFlags.HasIconLocation))
                    {
                        iconPath = binaryReader.ReadStringData(flagEncoding);
                    }

                    // Must attempt to pull the environment variable data block.
                    while (fileStream.Position < fileStream.Length)
                    {
                        var extraDataBlock = binaryReader.ReadExtraDataBlock();

                        if (extraDataBlock == null)
                        {
                            break;
                        }

                        switch (extraDataBlock.Signature)
                        {
                            case ExtraDataBlockSignature.EnvironmentVariableDataBlock:
                                {
                                    if (string.IsNullOrEmpty(TargetPath))
                                    {
                                        targetPath = Environment.ExpandEnvironmentVariables(extraDataBlock.Value);
                                    }
                                    break;
                                }
                            case ExtraDataBlockSignature.IconEnvironmentDataBlock:
                                {
                                    if (string.IsNullOrEmpty(iconPath))
                                    {
                                        iconPath = extraDataBlock.Value;
                                    }
                                    break;
                                }
                        }
                    }

                    targetPath = targetPath != null ? Environment.ExpandEnvironmentVariables(targetPath) : null;
                    iconPath = iconPath != null ? Environment.ExpandEnvironmentVariables(iconPath) : null;

                    this.TargetPath = targetPath;

                    this.IconData = IconExtractor.ExtractFileIcon(targetPath, iconPath, iconIndex);
                }
            }            
        }

        /// <summary>
        /// Determines whether the specified flag is flag.
        /// </summary>
        /// <param name="flag">The flag.</param>
        /// <returns></returns>
        private bool CheckFlag(LinkFlags flag)
        {
            return (this._flags & (int)flag) > 0;
        }
        
        #endregion
    }
}
