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

namespace Quicken.Core.Index
{
    public class IndexManager
    {
        #region Fields

        private HashSet<string> _directories;
        private static TargetRepository _targetRepository = new TargetRepository();
        private static IWshRuntimeLibrary.WshShell _shell = new IWshRuntimeLibrary.WshShell(); 

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="IndexManager"/> class.
        /// </summary>
        public IndexManager()
        {
            // Make sure that the database is always created if it changes, or simply does not exist.
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<DataContext>());

            this._directories = new HashSet<string>();

            this._directories.Add(Environment.ExpandEnvironmentVariables("%ProgramData%\\Microsoft\\Windows\\Start Menu"));
            this._directories.Add(Environment.ExpandEnvironmentVariables("%AppData%\\Microsoft\\Windows\\Start Menu"));
            this._directories.Add(Environment.ExpandEnvironmentVariables("%appdata%\\Microsoft\\Internet Explorer\\Quick Launch"));

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
            var targets = new Dictionary<string, Target>();

            foreach (var directory in this._directories)
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
                                        Icon = IconExtractor.GetIcon(file.FullName)
                                    };
                            }
                            break;
                    }
                    
                    if (target != null)
                    {
                        // Windows is case-insensitive when it comes to paths, and we
                        // want unique paths, hence why we make the key uppercase here.
                        targets[target.Path.ToUpperInvariant()] = target;
                    }
                }
            }

            // Note that this method often gets run as a separate 
            // thread, and since SQL CE is not inherently 
            // thread-safe, it has it's own TargetRepository.
            new TargetRepository().UpdateTargets(
                targets
                    .Select(i => i.Value)
                    .ToList());
        }

        /// <summary>
        /// Gets the LNK target.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns></returns>
        private static Target GetLnkTarget(FileInfo file)
        {
            var lnkInfo = new LnkInfo(file.FullName);
            return new Target()
                {
                    Name = lnkInfo.Name,
                    Description = GetFileDescription(lnkInfo.TargetPath),
                    Path = file.FullName,
                    Icon = lnkInfo.Icon
                };
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
                                                
                return new Target()
                {
                    Name = file.Name.Replace(file.Extension, string.Empty),
                    Description = url,
                    Path = url,
                    Icon = icon
                };
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
        
        #endregion
    }
}
