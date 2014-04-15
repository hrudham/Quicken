using Core.Icons;
using Core.Lnk;
using Quicken.Core.Index.Entities;
using Quicken.Core.Index.Entities.Models;
using Quicken.Core.Index.Repositories;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using System.Reflection;
using Core.Metro;
using Quicken.Core.Index.Enumerations;
using Quicken.Core.Index.Extensions;

namespace Quicken.Core.Index
{
    public class IndexManager
    {
        #region Fields

        private bool _includeMetro = true;
        private HashSet<string> _directories;
        private static TargetRepository _targetRepository = new TargetRepository();
        private static IWshRuntimeLibrary.WshShell _shell = new IWshRuntimeLibrary.WshShell(); 

        #endregion

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this instance has metro support.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has metro support; otherwise, <c>false</c>.
        /// </value>
        public bool HasMetroSupport
        {
            get
            {
                return true;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexManager"/> class.
        /// </summary>
        public IndexManager()
        {
            // Make sure that the database is always created if it changes, or simply does not exist.
            Database.SetInitializer(new DataInitializer());

            this._directories = new HashSet<string>();

            this._directories.Add(Environment.ExpandEnvironmentVariables("%ProgramData%\\Microsoft\\Windows\\Start Menu"));
            this._directories.Add(Environment.ExpandEnvironmentVariables("%AppData%\\Microsoft\\Windows\\Start Menu"));
            this._directories.Add(Environment.ExpandEnvironmentVariables("%appdata%\\Microsoft\\Internet Explorer\\Quick Launch"));
            this._directories.Add(Environment.ExpandEnvironmentVariables("%homedrive%%homepath%\\Links"));

            Task.Factory.StartNew(UpdateIndex);
        } 

        #endregion

        #region Methods

        /// <summary>
        /// Finds the targets.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public IList<Target> FindTargets(string text)
        {
            return _targetRepository.FindTargets(text);
        }

        /// <summary>
        /// Updates the term target.
        /// </summary>
        /// <param name="targetId">The target identifier.</param>
        /// <param name="text">The text.</param>
        public void UpdateTermTarget(int targetId, string text)
        {
            _targetRepository.UpdateTermTarget(targetId, text);
        }

        /// <summary>
        /// Updates the index. 
        /// repository.
        /// </summary>
        private void UpdateIndex()
        {
            var targets = this.GetDesktopTargets();
            targets = targets.Union(this.GetMetroTargets());

            // Note that this method often gets run as a separate 
            // thread, and since SQL CE is not inherently 
            // thread-safe, it has it's own TargetRepository.
            new TargetRepository().UpdateTargets(
                targets
                    .ToLookup(pair => pair.Path.ToUpperInvariant(), pair => pair)
                    .ToDictionary(group => group.Key, group => group.First())
                    .Select(i => i.Value)
                    .ToList());
        }

        /// <summary>
        /// Gets the desktop targets.
        /// </summary>
        /// <param name="directories">The directories.</param>
        /// <returns></returns>
        private IEnumerable<Target> GetDesktopTargets()
        {
            var targets = new List<Target>();

            foreach (var directory in this._directories)
            {
                if (Directory.Exists(directory))
                {
                    // Find all links that point to an executable, and add them to the list of targets.
                    foreach (var file in new DirectoryInfo(directory).GetFiles("*.*", SearchOption.AllDirectories))
                    {
                        var target = default(Target);

                        switch (file.Extension.ToUpperInvariant())
                        {
                            case ".LNK":
                                target = GetLnkTarget(file);
                                break;
                            case ".URL":
                                target = GetUrlTarget(file);
                                break;
                            default:
                                {
                                    target = new Target()
                                    {
                                        Name = file.Name.Replace(file.Extension, string.Empty),
                                        Description = GetFileDescription(file.FullName),
                                        Path = file.FullName,
                                        Icon = IconExtractor.GetIcon(file.FullName),
                                        Platform = GetFilePlatformType(file.FullName)
                                    };
                                }
                                break;
                        }

                        if (target != null)
                        {
                            //Add generic alias information
                            target.AddAlias(target.Name);

                            target.AddAlias(GetPathAliasText(target.Path));

                            // Add this new target to our collection
                            targets.Add(target);
                        }
                    }
                }
            }

            return targets;
        }

        /// <summary>
        /// Gets the metro targets.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Target> GetMetroTargets()
        {
            if (this._includeMetro && this.HasMetroSupport)
            {
                return MetroManager.GetApplications()
                    .Select(
                        app =>
                            {
                                var target = new Target()
                                {
                                    Name = app.Name,
                                    Description = app.Description,
                                    Path = app.AppUserModelId,
                                    Icon = app.Icon,
                                    Platform = TargetType.Metro,
                                };

                                target.AddAlias(target.Name);

                                return target;
                            });
            }

            return new List<Target>();
        }

        /// <summary>
        /// Gets the LNK target.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns></returns>
        private static Target GetLnkTarget(FileInfo file)
        {
            var lnkInfo = new LnkInfo(file.FullName);

            var target = new Target()
                {
                    Name = lnkInfo.Name,
                    Description = GetFileDescription(lnkInfo.TargetPath),
                    Path = file.FullName,
                    Icon = lnkInfo.Icon,
                    Platform = GetFilePlatformType(lnkInfo.TargetPath)
                };

            target.AddAlias(GetPathAliasText(lnkInfo.TargetPath));

            return target;
        }
             
        /// <summary>
        /// Gets the URL target.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns></returns>
        private static Target GetUrlTarget(FileInfo file)
        {
            using (var streamReader = new StreamReader(file.FullName))
            {
                var url = string.Empty;
                var iconPath = file.FullName;
                int? iconIndex = null;

                bool foundHeader = false;

                string line = null;
                while ((line = streamReader.ReadLine()) != null)
                {
                    if (!foundHeader)
                    {
                        foundHeader = (line == "[InternetShortcut]");
                    }
                    else
                    {
                        if (line.StartsWith("["))
                        {
                            break;
                        }

                        var firstEquals = line.IndexOf('=');
                        if (firstEquals >= 0)
                        {
                            var key = line.Substring(0, firstEquals);
                            var value = line.Substring(firstEquals + 1);

                            switch (key)
                            {
                                case "URL":
                                    url = value;
                                    break;
                                case "IconIndex":
                                    int parsedIconIndex = 0;
                                    if (int.TryParse(value, out parsedIconIndex))
                                    {
                                        iconIndex = parsedIconIndex;
                                    }
                                    break;
                                case "IconFile":
                                    iconPath = value;
                                    break;
                            }
                        }
                    }
                }

                var icon = IconExtractor.GetIcon(iconPath, iconIndex);
                                                
                var target = new Target()
                {
                    Name = file.Name.Replace(file.Extension, string.Empty),
                    Description = url,
                    Path = url,
                    Icon = icon,
                    Platform = TargetType.File
                };

                return target;
            }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private static string GetFileDescription(string path)
        {
            var description = path;

            if (File.Exists(path))
            {
                switch (System.IO.Path.GetExtension(path).ToUpperInvariant())
                {
                    case ".LIBRARY-MS":
                        description = "Library";
                        break;
                    case ".APPREF-MS":
                        description = string.Empty;
                        break;
                    case ".EXE":
                        // Extract the product name as the description
                        FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(path);
                        description = versionInfo.ProductName ?? string.Empty;
                        break;
                }
            }
            else if (Directory.Exists(path))
            {
                description = path;
            }

            return description;
        }

        /// <summary>
        /// Gets the type of the file platform.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private static TargetType GetFilePlatformType(string path)
        {
            return File.Exists(path) && Path.GetExtension(path).ToUpperInvariant() == ".EXE" ? TargetType.Desktop : TargetType.File;
        }

        /// <summary>
        /// Gets the path alias text.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        private static string GetPathAliasText(string path)
        {
            var targetUri = default(Uri);
            if (Uri.TryCreate(path, UriKind.RelativeOrAbsolute, out targetUri))
            {
                if (targetUri.IsFile)
                {
                    return Path.GetFileNameWithoutExtension(path);
                }
            }

            return null;
        }
                
        #endregion
    }
}
