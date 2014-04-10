using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using Windows.Management.Deployment;

namespace Core.Metro
{
    public static class MetroManager
    {
        #region Methods

        /// <summary>
        /// Gets the metro applications.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<MetroApplication> GetApplications()
        {
            var packages = new PackageManager()
                .FindPackagesForUser(WindowsIdentity.GetCurrent().User.Value);

            var metroPackages = new List<MetroPackage>();

            foreach (var package in packages)
            {
                var metroPackage = new MetroPackage(package);

                if (metroPackage.Applications.Any())
                {
                    metroPackages.Add(new MetroPackage(package));
                }
            }

            var metroApplications = metroPackages
                .SelectMany(package => package.Applications)
                .Where(
                    application => 
                        !string.IsNullOrEmpty(application.AppUserModelId) && 
                        !string.IsNullOrEmpty(application.Name));

            return metroApplications;
        }

        /// <summary>
        /// Launches the package.
        /// </summary>
        /// <param name="appUserModelId">The application user model identifier.</param>
        public static void Start(string appUserModelId)
        {
            uint processId;
            ApplicationActivationManager activationManager = new ApplicationActivationManager();
            activationManager.ActivateApplication(
                appUserModelId,
                null,
                Enumerations.ActivateOptions.None, 
                out processId);
        }

        #endregion
    }
}
