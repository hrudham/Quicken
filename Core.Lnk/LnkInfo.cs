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
using Core.Icons;
using System.Xml.Linq;
using System.Diagnostics;

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

        private int _flags;
        private int _iconIndex;
        private string _iconPath;
        public byte[] _icon { get; private set; }

        #endregion
        
        #region Properties

        public string Path { get; private set; }

        public string TargetPath { get; private set; }

        public string Name { get; private set; }

        public string RelativePath { get; private set; }

        public FileAttributes FileAttributes { get; private set; }

        public byte[] Icon
        {
            get
            {
                if (this._icon == null)
                {
                    this._icon = IconExtractor.GetIcon(
                        string.IsNullOrEmpty(this._iconPath) ? this.TargetPath : this._iconPath,
                        this._iconIndex);
                }

                return this._icon;
            }
        }

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
                        var name = binaryReader.ReadStringData(flagEncoding);

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

                    this._iconPath = string.IsNullOrEmpty(iconPath) ? null : Environment.ExpandEnvironmentVariables(iconPath);
                    this.TargetPath = string.IsNullOrEmpty(targetPath) ? null : Environment.ExpandEnvironmentVariables(targetPath);
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
