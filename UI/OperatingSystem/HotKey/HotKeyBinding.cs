using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Quicken.UI.OperatingSystem.HotKey
{
    internal class HotKeyBinding : IDisposable
    {
        #region Fields

        private bool _disposed = false; 

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public Key Key { get; private set; }

        /// <summary>
        /// Gets or sets the key modifiers.
        /// </summary>
        /// <value>
        /// The key modifiers.
        /// </value>
        public KeyModifier KeyModifiers { get; private set; }

        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int Id { get; private set; }

        /// <summary>
        /// Gets or sets the action.
        /// </summary>
        /// <value>
        /// The action.
        /// </value>
        public Action Action { get; private set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HotKeyBinding"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="key">The key.</param>
        /// <param name="keyModifiers">The key modifiers.</param>
        /// <param name="action">The action.</param>
        public HotKeyBinding(int id, Key key, KeyModifier keyModifiers, Action action)
        {
            this.Key = key;
            this.KeyModifiers = keyModifiers;
            this.Action = action;
            this.Id = id;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Releases unmanaged and, optionally, managed resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be _disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be _disposed.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this._disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    HotKeyManager.Unregister(this.Id);
                }

                // Note disposing has been done.
                _disposed = true;
            }
        }

        #endregion
    }
}
