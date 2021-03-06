﻿using Core.Metro;
using Quicken.Core.Index;
using Quicken.Core.Index.Entities.Models;
using Quicken.Core.Index.Enumerations;
using Quicken.UI.OperatingSystem;
using Quicken.UI.OperatingSystem.Launcher;
using Quicken.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Quicken.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private IndexManager _indexManager = new IndexManager();
        private IList<Target> _currentTargets = new List<Target>();
        
        #endregion

        #region Properties

        private MainWindowViewModel ViewModel { get; set; }
        private BackgroundWorker RefreshBackgroundWorker { get; set; }

        #endregion

        #region Constructors 

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow" /> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            this.ViewModel = new MainWindowViewModel();
            this.DataContext = this.ViewModel;

            this.ViewModel.PropertyChanged += ViewModel_PropertyChanged;

            this.RefreshBackgroundWorker = new BackgroundWorker() 
            { 
                WorkerReportsProgress = false, 
                WorkerSupportsCancellation = false 
            };

            this.RefreshBackgroundWorker.DoWork += RefreshBackgroundWorker_DoWork;
            this.RefreshBackgroundWorker.RunWorkerCompleted += RefreshBackgroundWorker_RunWorkerCompleted;

            // Refresh the index once a week
            if (Properties.Settings.Default.LastRefreshDate.Add(new TimeSpan(7, 0, 0, 0)) < DateTime.Now)
            {
                this.Refresh();
                
                Properties.Settings.Default.LastRefreshDate = DateTime.Now;
                Properties.Settings.Default.Save();
            }

            this.HideWindow();
        }

        /// <summary>
        /// Handles the PropertyChanged event of the ViewModel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.PropertyChangedEventArgs"/> instance containing the event data.</param>
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Query")
            {
                if (!string.IsNullOrEmpty(this.ViewModel.Query))
                {
                    var newTargets = this._indexManager.FindTargets(this.ViewModel.Query);

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
                    this.ViewModel.CurrentTarget = null;
                }
            }
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
                    this.HideWindow();
                    break;
                case Key.Enter:
                    this.ExecuteCurrentTarget(this.ViewModel.IsRunAsAdministrator);
                    break;
                case Key.Up:
                    this.AdjustCurrentTarget(true);
                    break;
                case Key.Down:
                    this.AdjustCurrentTarget(false);
                    break;
                case Key.F5:
                    this.Refresh();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Handles the Activated event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void Window_Activated(object sender, EventArgs e)
        {
            Keyboard.Focus(this.SearchTextBox);
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
        /// Handles the Deactivated event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Window_Deactivated(object sender, System.EventArgs e)
        {
            this.HideWindow();
        }

        /// <summary>
        /// Handles the DoWork event of the RefreshBackgroundWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
        private void RefreshBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.ViewModel.IsUpdating = true;
            new IndexManager().UpdateIndex();
        }

        /// <summary>
        /// Handles the RunWorkerCompleted event of the RefreshBackgroundWorker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void RefreshBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.ViewModel.IsUpdating = false;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates this instance.
        /// </summary>
        internal void Refresh()
        {
            if (!this.RefreshBackgroundWorker.IsBusy)
            {
                this.RefreshBackgroundWorker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Updates the run as administrator.
        /// </summary>
        private void UpdateRunAsAdministrator()
        {
            this.ViewModel.IsRunAsAdministrator = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) 
                && this._currentTargets.Any() 
                && this._currentTargets.First().Platform != TargetType.Metro;
        }
        
        /// <summary>
        /// Runs the selected target.
        /// </summary>
        public void ExecuteCurrentTarget(bool runAsAdministrator)
        {
            this.HideWindow();

            if (this.ViewModel.CurrentTarget != null)
            {
                this._indexManager.UpdateTermTarget(this.ViewModel.CurrentTarget.TargetId, this.ViewModel.Query);

                if (this.ViewModel.CurrentTarget.Platform == TargetType.Metro)
                {
                    MetroManager.Start(this.ViewModel.CurrentTarget.Path);
                }
                else
                {
                    ShellLauncher.Start(this.ViewModel.CurrentTarget.Path, runAsAdministrator);
                }
            }
        }

        /// <summary>
        /// Hides the window.
        /// </summary>
        public void HideWindow()
        {
            this.SearchTextBox.SelectAll();
            this.Hide();
        }
        
        /// <summary>
        /// Sets the target.
        /// </summary>
        /// <param name="target">The target.</param>
        private void RenderCurrentTarget()
        {
            var target = this._currentTargets.FirstOrDefault();

            this.ViewModel.CurrentTarget = target;

            if (target != null)
            {                
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
