using Quicken.Core.Index;
using Quicken.Core.Index.Entities.Models;
using Quicken.UI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Quicken.UI.ViewModel
{
    public class MainWindowViewModel : NotifyPropertyChanged
    {
        #region Fields

        private Target _CurrentTarget = null;
        private bool _IsRunAsAdministrator = false;
        private bool _IsUpdating = false;
        private string _Query = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the current targets.
        /// </summary>
        /// <value>
        /// The current targets.
        /// </value>
        public Target CurrentTarget
        {
            get
            {
                return this._CurrentTarget;
            }
            set
            {
                this.SetField(ref this._CurrentTarget, value, () => this.CurrentTarget);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is run as administrator.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is run as administrator; otherwise, <c>false</c>.
        /// </value>
        public bool IsRunAsAdministrator
        {
            get
            {
                return this._IsRunAsAdministrator;
            }
            set
            {
                this.SetField(ref this._IsRunAsAdministrator, value, () => this.IsRunAsAdministrator);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is updating.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is updating; otherwise, <c>false</c>.
        /// </value>
        public bool IsUpdating
        {
            get
            {
                return this._IsUpdating;
            }
            set
            {
                this.SetField(ref this._IsUpdating, value, () => this.IsUpdating);
            }
        }

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        /// <value>
        /// The query.
        /// </value>
        public string Query
        {
            get
            {
                return this._Query;
            }
            set
            {
                this.SetField(ref this._Query, value, () => this.Query);
            }
        }

        #endregion
    }
}
