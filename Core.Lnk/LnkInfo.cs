using Core.Lnk.Enumerations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Core.Lnk.Extensions;

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

        #endregion

        #region Properties

        public string Path { get; private set; }
        public string TargetPath { get; private set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public string IconLocation { get; set; }
        public string RelativePath { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LnkInfo"/> class.
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
                    // Read the lnk flags.
                    fileStream.Seek(0x14, SeekOrigin.Begin);
                    this._flags = binaryReader.ReadInt32();

                    var flagEncoding = this.CheckFlag(LnkFlags.IsUnicode) ? Encoding.Unicode : Encoding.Default;

                    // Read the icon index.
                    fileStream.Seek(0x38, SeekOrigin.Begin);
                    this._iconIndex = binaryReader.ReadInt32();

                    fileStream.Seek(0x4C, SeekOrigin.Begin);

                    // Read link target ID list section
                    if (this.CheckFlag(LnkFlags.HasLinkTargetIDList))
                    {
                        short idListSize = binaryReader.ReadInt16();

                        // Skip the target ID list
                        if (idListSize > 0)
                        {
                            fileStream.Seek(idListSize, SeekOrigin.Current);
                        }
                    }

                    // Read link info section
                    if (this.CheckFlag(LnkFlags.HasLinkInfo))
                    {
                        var infoSectionOffset = binaryReader.BaseStream.Position;

                        var linkInfoSize = binaryReader.ReadInt32();
                        var linkInfoHeaderSize = binaryReader.ReadInt32();
                        var linkInfoFlags = (LnkInfoFlags)(binaryReader.ReadInt32());

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
                            
                        if ((linkInfoFlags & LnkInfoFlags.VolumeIDAndLocalBasePath) > 0)
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

                            this.TargetPath = System.IO.Path.Combine(localBasePath, commonPathSuffix);
                        }

                        fileStream.Seek(infoSectionOffset + linkInfoSize, SeekOrigin.Begin);
                    }

                    // Read string data
                    if (this.CheckFlag(LnkFlags.HasName))
                    {
                        this.Comment = binaryReader.ReadStringData(flagEncoding);
                    }

                    if (this.CheckFlag(LnkFlags.HasRelativePath))
                    {
                        this.RelativePath = binaryReader.ReadStringData(flagEncoding);

                        this.TargetPath = System.IO.Path.Combine(
                            System.IO.Path.GetDirectoryName(path), 
                            this.RelativePath);
                    }

                    if (this.CheckFlag(LnkFlags.HasWorkingDir))
                    {
                        binaryReader.SkipStringData(flagEncoding);
                    }

                    if (this.CheckFlag(LnkFlags.HasArguments))
                    {
                        binaryReader.SkipStringData(flagEncoding);
                    }

                    if (this.CheckFlag(LnkFlags.HasIconLocation))
                    {
                        this.IconLocation = binaryReader.ReadStringData(flagEncoding);
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
                            case LnkExtraDataBlockSignature.EnvironmentVariableDataBlock:
                                {
                                    if (string.IsNullOrEmpty(TargetPath))
                                    {
                                        if (!string.IsNullOrEmpty(extraDataBlock.ValueUnicode))
                                        {
                                            this.TargetPath = Environment.ExpandEnvironmentVariables(extraDataBlock.ValueUnicode);
                                        }
                                        else if (!string.IsNullOrEmpty(extraDataBlock.Value))
                                        {
                                            this.TargetPath = Environment.ExpandEnvironmentVariables(extraDataBlock.Value);
                                        }
                                    }
                                    break;
                                }
                        }
                    }
                }
            }            
        }

        /// <summary>
        /// Determines whether the specified flag is flag.
        /// </summary>
        /// <param name="flag">The flag.</param>
        /// <returns></returns>
        private bool CheckFlag(LnkFlags flag)
        {
            return (this._flags & (int)flag) > 0;
        }
        
        #endregion
    }
}
