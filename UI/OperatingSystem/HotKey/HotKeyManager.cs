using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Interop;

namespace UI.OperatingSystem.HotKey
{
    /// <summary>
    /// Code largely based upon this Stackoverflow answer, although fairly modified: 
    /// http://stackoverflow.com/questions/48935/how-can-i-register-a-global-hot-key-to-say-ctrlshiftletter-using-wpf-and-ne#answer-9330358
    /// </summary>
    internal static class HotKeyManager
    {
        #region Fields

        private static IDictionary<int, HotKeyBinding> _bindings;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, UInt32 fsModifiers, UInt32 vlc);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int WmHotKey = 0x0312;

        #endregion

        #region Methods

        /// <summary>
        /// Registers this a hot-key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="keyModifiers">The key modifiers.</param>
        /// <param name="action">The action.</param>
        /// <returns></returns>
        public static bool Register(Key key, KeyModifier keyModifiers, Action action)
        {
            if (_bindings == null)
            {
                _bindings = new Dictionary<int, HotKeyBinding>();
                ComponentDispatcher.ThreadFilterMessage += new ThreadMessageEventHandler(OnKeyboardMessage);
            }

            int virtualKeyCode = KeyInterop.VirtualKeyFromKey(key);

            var id = virtualKeyCode + ((int)keyModifiers * 0x10000);

            var binding = new HotKeyBinding(
                id,
                key,
                keyModifiers,
                action);

            bool result = RegisterHotKey(IntPtr.Zero, id, (UInt32)keyModifiers, (UInt32)virtualKeyCode);

            _bindings.Add(binding.Id, binding);

            return result;
        }

        /// <summary>
        /// Unregisters all hot-key bindings with the specified key and modifiers.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="keyModifiers">The key modifiers.</param>
        public static void Unregister(Key key, KeyModifier keyModifiers)
        {
            var bindings = _bindings
                .Where(
                    i => 
                        i.Value.Key == key &&
                        i.Value.KeyModifiers == keyModifiers);

            foreach (var binding in bindings)
            {
                Unregister(binding.Value.Id);
            }
        }

        /// <summary>
        /// Unregisters a hot-key by ID.
        /// </summary>
        public static void Unregister(int id)
        {
            UnregisterHotKey(IntPtr.Zero, id);
        }

        /// <summary>
        /// Called when a keyboard message is received. Checks to see if 
        /// they hot-key has been fired, and executes it associated action 
        /// if so.
        /// </summary>
        /// <param name="msg">The MSG.</param>
        /// <param name="handled">if set to <c>true</c> [handled].</param>
        private static void OnKeyboardMessage(ref MSG msg, ref bool handled)
        {
            if (!handled)
            {
                if (msg.message == WmHotKey)
                {
                    var id = (int)msg.wParam;
                    HotKeyBinding binding;

                    if (_bindings.TryGetValue(id, out binding))
                    {
                        if (binding.Action != null)
                        {
                            binding.Action.Invoke();
                        }
                    }

                    handled = true;
                }
            }
        }

        #endregion
    }
}
