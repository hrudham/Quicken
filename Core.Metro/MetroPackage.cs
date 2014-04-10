using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Windows.ApplicationModel;

namespace Core.Metro
{
    public class MetroPackage
    {
        #region Unmanaged Methods

        [DllImport("shlwapi.dll", BestFitMapping = false, CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = false, ThrowOnUnmappableChar = true)]
        private static extern int SHLoadIndirectString(string pszSource, StringBuilder pszOutBuf, int cchOutBuf, IntPtr ppvReserved);

        #endregion

        #region Fields

        /// <summary>
        /// The applications in this package.
        /// </summary>
        private IList<MetroApplication> _applications = new List<MetroApplication>();

        #endregion

        #region Properties
        
        /// <summary>
        /// Gets the applications.
        /// </summary>
        /// <value>
        /// The applications.
        /// </value>
        public IList<MetroApplication> Applications
        {
            get
            {
                return this._applications;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MetroPackage"/> class.
        /// </summary>
        /// <param name="package">The package.</param>
        public MetroPackage(Package package)
        {
            this.ParsePackage(package);
        }

        #endregion

        #region Methods

        /// <summary>
        /// Parses the package.
        /// </summary>
        /// <param name="manifestPath">The manifest path.</param>
        private void ParsePackage(Package package)
        {
            var manifestPath = Path.Combine(
                    package.InstalledLocation.Path,
                    "AppxManifest.xml");

            var xManifest = XDocument.Load(
                manifestPath,
                LoadOptions.None);

            var xDefaultNamespace = xManifest.Root.GetDefaultNamespace();

            // Find all of the namespaces that we can ignore.
            var ignorableNamespaces = new List<string>();
            var xIgnoredNamespaces = xManifest.Root.Attribute("IgnorableNamespaces");
            if (xIgnoredNamespaces != null)
            {
                ignorableNamespaces = xIgnoredNamespaces.Value.Split(' ').ToList();
            }

            // Find useable namespaces for this manifest
            var manifestNamespaces = xManifest.Root
                .Attributes()
                .Where(
                    attr =>
                        attr.IsNamespaceDeclaration &&
                        !ignorableNamespaces.Contains(attr.Value))
                .Select(ns => XNamespace.Get(ns.Value))
                .OrderBy(ns => ns == xDefaultNamespace ? 0 : 1);

            var xApplications = xManifest.Root.Element(xDefaultNamespace + "Applications");

            if (xApplications != null)
            {
                foreach (var xApplication in xApplications.Elements(xDefaultNamespace + "Application"))
                {
                    var applicationId = xApplication.Attribute("Id").Value;

                    foreach (var xNamespace in manifestNamespaces)
                    {
                        var xVisualElements = xApplication.Element(xNamespace + "VisualElements");

                        if (xVisualElements != null)
                        {
                            // Name
                            var displayNameAttribute = xVisualElements.Attribute("DisplayName");
                            var displayName = displayNameAttribute != null ? displayNameAttribute.Value : null;

                            // Description
                            var descriptionAttribute = xVisualElements.Attribute("Description");
                            var description = descriptionAttribute != null ? descriptionAttribute.Value : null;

                            // Icon
                            var smallIconPathAttribute = xVisualElements.Attribute("Square30x30Logo");
                            if (smallIconPathAttribute == null)
                            {
                                smallIconPathAttribute = xVisualElements.Attribute("SmallLogo");
                            }
                            var smallIconPath = smallIconPathAttribute != null ? smallIconPathAttribute.Value : null;
                            
                            var application = new MetroApplication()
                            {
                                ApplicationId = applicationId,
                                AppUserModelId = ResolveAppUserModelId(package.Id.FullName, applicationId),
                                Name = ResolveManifestString(displayName, package.Id.Name, package.InstalledLocation.Path),
                                Description = ResolveManifestString(description, package.Id.Name, package.InstalledLocation.Path),
                                Icon = ResolveIcon(smallIconPath, package.InstalledLocation.Path)
                            };

                            this.Applications.Add(application);

                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resolves the file.
        /// </summary>
        /// <param name="relativePath">The relative path.</param>
        /// <param name="installationPath">The installation path.</param>
        /// <returns></returns>
        private byte[] ResolveIcon(string relativePath, string installationPath)
        {
            var path = Path.Combine(installationPath, relativePath);

            //TODO: Fix this up; it's so far largely a guessing game as to
            //      how to find the icon. There has got to be a better way.

            var targetSizeFilePath = Path.ChangeExtension(path, "targetsize-48.png");
            if (File.Exists(targetSizeFilePath))
            {
                return File.ReadAllBytes(targetSizeFilePath);
            }

            var targetSizeFolderPath = Path.Combine(
                Path.GetDirectoryName(path), 
                "targetsize-48", 
                Path.GetFileName(path));
            if (File.Exists(targetSizeFolderPath))
            {
                return File.ReadAllBytes(targetSizeFolderPath);
            }

            if (File.Exists(path))
            {
                return File.ReadAllBytes(path);
            }

            return null;
        }

        /// <summary>
        /// Resolves the AppUserModelId of an application.
        /// </summary>
        /// <param name="packageFullName">Full name of the package.</param>
        /// <param name="applicationId">The application identifier.</param>
        /// <returns></returns>
        private static string ResolveAppUserModelId(string packageFullName, string applicationId)
        {
            var serverRegistryName = string.Format(
                "Software\\Classes\\ActivatableClasses\\Package\\{0}\\Server", 
                packageFullName);

            using (var serverRegistryKey = Registry.CurrentUser.OpenSubKey(serverRegistryName))
            {
                if (serverRegistryKey != null && serverRegistryKey.SubKeyCount > 0)
                {
                    // Attempt to find the most relevant application 
                    // entry, and pull the AppUserModelId from it.
                    var applicationNames = serverRegistryKey.GetSubKeyNames()
                        .OrderBy(name => name.StartsWith(applicationId) ? 0 : 1)
                        .ThenBy(name => name);

                    foreach (var applicationName in applicationNames)
                    {
                        using (var firstRegistryKey = serverRegistryKey.OpenSubKey(applicationName))
                        {
                            var appUserModelId = firstRegistryKey.GetValue("AppUserModelId") as string;
                            if (!string.IsNullOrEmpty(appUserModelId))
                            {
                                return appUserModelId;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves the display name of an application.
        /// </summary>
        /// <param name="visualElementDisplayName">The display name.</param>
        /// <param name="packageName">Name of the package.</param>
        /// <param name="installationPath">The installation path.</param>
        /// <returns></returns>
        private static string ResolveManifestString(string visualElementDisplayName, string packageName, string installationPath)
        {
            Uri displayNameUri = null;
            if (!Uri.TryCreate(visualElementDisplayName, UriKind.Absolute, out displayNameUri))
            {
                // This is a normal display name; just return it.
                return visualElementDisplayName;
            }

            var resourcePath = Path.Combine(installationPath, "resources.pri");

            var resourceKey = string.Format(
                "ms-resource://{0}/resources/{1}",
                packageName,
                displayNameUri.Segments.Last());

            var resolvedDisplayName = ExtractResourceString(resourcePath, resourceKey);

            if (string.IsNullOrEmpty(resolvedDisplayName))
            {
                resourceKey = string.Format(
                    "ms-resource://{0}/{1}",
                    packageName,
                    string.Concat(displayNameUri.Segments.Skip(1)));

                resolvedDisplayName = ExtractResourceString(resourcePath, resourceKey);
            }

            return resolvedDisplayName;
        }

        /// <summary>
        /// Extracts the resource string.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        private static string ExtractResourceString(string path, string key)
        {
            string manifestString = string.Format("@{{{0}? {1}}}", path, key);
            var output = new StringBuilder(1024);
            SHLoadIndirectString(manifestString, output, output.Capacity, IntPtr.Zero);
            return output.ToString();
        }

        #endregion
    }
}
