using Core;
using System.Windows;
using System.Windows.Input;
using UI.OperatingSystem;
using System.Linq;
using Core.Entities;
using System.Windows.Media;
using System.IO;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Security.Principal;
using Core.Entities.Models;
using System.Collections.Generic;

namespace UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private IndexManager _indexManager = new IndexManager();
        private IList<Target> _currentTargets = new List<Target>();
        public bool _runAsAdministrator = false;

        #endregion

        #region Constructors 

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        } 

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Loaded event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Draggable.Register(this, MainGrid);
            UpdateRunAsAdministrator();
        }

        /// <summary>
        /// Handles the KeyDown event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            UpdateRunAsAdministrator();
        }

        /// <summary>
        /// Handles the KeyUp event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="KeyEventArgs"/> instance containing the event data.</param>
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            UpdateRunAsAdministrator();

            switch (e.Key)
            {
                case Key.Escape:
                    this.Hide();
                    break;
                case Key.Enter:
                    this.ExecuteCurrentTarget(this._runAsAdministrator);
                    break;
                case Key.Up:
                    this.AdjustCurrentTarget(true);
                    break;
                case Key.Down:
                    this.AdjustCurrentTarget(false);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handles the GotFocus event of the SearchTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            SearchTextBox.SelectAll();
        }

        /// <summary>
        /// Handles the TextChanged event of the SearchTextBox control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.TextChangedEventArgs"/> instance containing the event data.</param>
        private void SearchTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (this.IsLoaded)
            {
                this._currentTargets = this._indexManager.FindTargets(SearchTextBox.Text);

                if (this._currentTargets.Any())
                {
                    this.RenderCurrentTarget();
                }
                else
                {
                    this.ClearTarget();
                }
            }
        }

        /// <summary>
        /// Handles the Deactivated event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Window_Deactivated(object sender, System.EventArgs e)
        {
            this.Hide();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates the run as administrator.
        /// </summary>
        private void UpdateRunAsAdministrator()
        {
            this._runAsAdministrator = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && this._currentTargets.Any();

            if (this._runAsAdministrator)
            {
                RunAsAdministratorImage.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                RunAsAdministratorImage.Visibility = System.Windows.Visibility.Hidden;
            }
        }

        /// <summary>
        /// Runs the selected target.
        /// </summary>
        public void ExecuteCurrentTarget(bool runAsAdministrator)
        {
            if (this._currentTargets.Any())
            {
                this.Hide();

                var target = this._currentTargets.First();

                this._indexManager.UpdateTermTarget(target.TargetId, this.SearchTextBox.Text);

                var process = new Process();
                process.StartInfo.FileName = target.Path;

                if (runAsAdministrator)
                {
                    process.StartInfo.Verb = "runas";
                }

                process.Start();
            }
        }

        /// <summary>
        /// Displays this window in a state that is ready for a new search.
        /// </summary>
        public void Display()
        {
            if (!this.IsVisible)
            {
                this.SearchTextBox.Clear();
                this.ClearTarget();               
                this.Show();
            }

            this.Activate();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        private void ClearTarget()
        {
            this._currentTargets.Clear();
            this.TargetNameTextBlock.Text = string.Empty;
            this.TargetProductNameTextBlock.Text = string.Empty;
            this.TargetIconImage.Source = null;
        }

        /// <summary>
        /// Sets the target.
        /// </summary>
        /// <param name="target">The target.</param>
        private void RenderCurrentTarget()
        {
            var target = this._currentTargets.FirstOrDefault();

            if (target != null)
            {
                this.TargetNameTextBlock.Text = target.Name;
                this.TargetProductNameTextBlock.Text = target.ProductName;

                // Render the icon
                this.TargetIconImage.Source = null;
                if (target.Icon != null)
                {
                    var icon = new BitmapImage();

                    using (MemoryStream iconMemoryStream = new MemoryStream(target.Icon))
                    {
                        icon.BeginInit();

                        // Without the CacheOption set to BitmapCacheOption.OnLoad, the 
                        // BitmapImage will use lazy loading, and the stream will be closed
                        // by the time this image is actually requested to be rendered.
                        icon.CacheOption = BitmapCacheOption.OnLoad;

                        icon.StreamSource = iconMemoryStream;

                        icon.EndInit();
                    }

                    this.TargetIconImage.Source = icon;
                }
                
            }
        }

        /// <summary>
        /// Adjusts the current target.
        /// </summary>
        /// <param name="moveUp">if set to <c>true</c> [move up].</param>
        private void AdjustCurrentTarget(bool moveUp)
        {
            if (this._currentTargets.Count > 1)
            {
                var index = 0;

                if (moveUp)
                {
                    index = this._currentTargets.Count() - 1;
                }

                var newTarget = this._currentTargets[index];
                this._currentTargets.RemoveAt(index);
                
                if (moveUp)
                {
                    this._currentTargets.Insert(0, newTarget);
                }
                else
                {
                    this._currentTargets.Add(newTarget);
                }

                this.RenderCurrentTarget();
            }
        }
        
        #endregion
    }
}
