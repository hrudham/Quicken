using Core.Metro;
using Quicken.Core.Index;
using Quicken.Core.Index.Entities.Models;
using Quicken.Core.Index.Enumerations;
using Quicken.UI.OperatingSystem;
using Quicken.UI.OperatingSystem.Launcher;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Quicken.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private Thickness _nameLabelWithDescriptionMargin;
        private Thickness _nameLabelWithoutDescriptionMargin;
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
            this.ClearTarget();
            this._nameLabelWithDescriptionMargin = this.TargetNameLabel.Margin;
            this._nameLabelWithoutDescriptionMargin = new Thickness(
                this._nameLabelWithDescriptionMargin.Left,
                0, 
                this._nameLabelWithDescriptionMargin.Right, 
                0);
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
                if (!string.IsNullOrEmpty(SearchTextBox.Text))
                {
                    var newTargets = this._indexManager.FindTargets(SearchTextBox.Text);

                    // Only update the results if we actually receive new ones; if nothing
                    // comes back, we still want to display the old results. This allows
                    // queries like like "notepaddd" to still find "notepad"
                    if (newTargets.Any())
                    {
                        this._currentTargets = newTargets;
                        this.RenderCurrentTarget();
                    }
                }
                else
                {
                    // No query was specified, so display no results.
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
            this._runAsAdministrator = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) 
                && this._currentTargets.Any() 
                && this._currentTargets.First().Platform != TargetType.Metro;

            if (this._runAsAdministrator)
            {
                this.RunAsAdministratorImage.Visibility = Visibility.Visible;
            }
            else
            {
                this.RunAsAdministratorImage.Visibility = Visibility.Hidden;
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

                if (target.Platform == TargetType.Metro)
                {
                    MetroManager.Start(target.Path);
                }
                else
                {
                    ShellLauncher.Start(target.Path, runAsAdministrator);
                }
            }
        }

        /// <summary>
        /// Displays this window in a state that is ready for a new search.
        /// </summary>
        public void Display()
        {
            // Re-center the application every time it's shown, and make sure it's always on the primary screen.
            // If we just use the Window's start up position, it sometimes ends up on secondary screens.
            this.Left = (SystemParameters.PrimaryScreenWidth / 2) - (this.Width / 2);
            this.Top = (SystemParameters.PrimaryScreenHeight / 2) - (this.Height / 2);

            if (!this.IsVisible)
            {
                this.SearchTextBox.Clear();
                this.ClearTarget();
                this.RunAsAdministratorImage.Visibility = System.Windows.Visibility.Hidden;
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
            this.TargetDescriptionTextBlock.Text = string.Empty;
            this.TargetDescriptionTextBlock.ToolTip = string.Empty;
            this.TargetIconImage.Source = null;
            this.RunAsAdministratorImage.Visibility = Visibility.Hidden;
            this.TargetTypeBorder.Visibility = Visibility.Hidden;
            this.TargetTypeImage.Visibility = Visibility.Hidden;
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
                this.TargetDescriptionTextBlock.Text = target.Description;
                this.TargetDescriptionTextBlock.ToolTip = target.Description;

                if (!string.IsNullOrEmpty(target.Description))
                {
                    this.TargetNameLabel.Margin = this._nameLabelWithDescriptionMargin;
                }
                else
                {
                    this.TargetNameLabel.Margin = this._nameLabelWithoutDescriptionMargin;
                }

                // Render the target type
                if (target.Platform == TargetType.Metro)
                {
                    this.TargetTypeImage.Source = new BitmapImage(new Uri("Images/target-metro-application.png", UriKind.Relative));
                }
                else if (target.Platform == TargetType.Desktop)
                {
                    this.TargetTypeImage.Source = new BitmapImage(new Uri("Images/target-desktop-application.png", UriKind.Relative));
                }
                else
                {
                    this.TargetTypeImage.Source = new BitmapImage(new Uri("Images/target-file.png", UriKind.Relative));
                }

                this.TargetTypeBorder.Visibility = Visibility.Visible;
                this.TargetTypeImage.Visibility = Visibility.Visible;

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
