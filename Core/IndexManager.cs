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
                foreach (var file in new DirectoryInfo(directory).GetFiles("*.lnk", SearchOption.AllDirectories))
                {
                    var target = BuildTarget(file);
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
        /// Builds the target.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns></returns>
        private static Target BuildTarget(FileInfo shortcutFile)
        {
            var lnkInfo = new LnkInfo(shortcutFile.FullName);

            if (File.Exists(lnkInfo.TargetPath))
            {
                // Extract the product name
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(lnkInfo.TargetPath);
                var productName = versionInfo.ProductName ?? string.Empty;

                return
                    new Target()
                    {
                        Name = lnkInfo.Name,
                        ProductName = productName,
                        Path = shortcutFile.FullName,
                        Icon = lnkInfo.IconData
                    };
            }

            return null;
        }

        #endregion
    }
}
