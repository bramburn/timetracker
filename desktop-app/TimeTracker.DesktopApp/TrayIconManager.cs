using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Drawing;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Manages the system tray icon and its associated context menu for the TimeTracker application.
/// Provides user interface for controlling tracking state and accessing application features.
/// </summary>
public class TrayIconManager : IDisposable
{
    private readonly ILogger<TrayIconManager> _logger;
    private readonly ActivityLogger _activityLogger;
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly System.Windows.Forms.Timer _statusUpdateTimer;

    private bool _disposed = false;
    private SettingsForm? _settingsForm;
    private StatusOverlayForm? _statusOverlayForm;
    private MainForm? _mainForm;

    // Menu items for state management
    private ToolStripMenuItem _showMainWindowMenuItem;
    private ToolStripMenuItem _startTrackingMenuItem;
    private ToolStripMenuItem _pauseTrackingMenuItem;
    private ToolStripMenuItem _stopTrackingMenuItem;
    private ToolStripMenuItem _settingsMenuItem;
    private ToolStripMenuItem _viewStatusMenuItem;
    private ToolStripMenuItem _exitMenuItem;

    public TrayIconManager(ActivityLogger activityLogger, ILogger<TrayIconManager> logger)
    {
        _activityLogger = activityLogger ?? throw new ArgumentNullException(nameof(activityLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize NotifyIcon
        _notifyIcon = new NotifyIcon();

        // Initialize context menu
        _contextMenu = new ContextMenuStrip();

        // Initialize status update timer
        _statusUpdateTimer = new System.Windows.Forms.Timer();
        _statusUpdateTimer.Interval = 5000; // Update every 5 seconds
        _statusUpdateTimer.Tick += OnStatusUpdateTimer;

        InitializeNotifyIcon();
        InitializeContextMenu();
        UpdateTrayIconState();

        _logger.LogInformation("TrayIconManager initialized successfully");
    }

    /// <summary>
    /// Initializes the NotifyIcon with default settings
    /// </summary>
    private void InitializeNotifyIcon()
    {
        try
        {
            _logger.LogInformation("Initializing NotifyIcon...");

            // Load the application icon
            Icon appIcon = LoadApplicationIcon();
            _notifyIcon.Icon = appIcon;
            _logger.LogInformation("NotifyIcon icon loaded from app.ico");

            _notifyIcon.Text = "TimeTracker: Inactive";
            _logger.LogInformation("NotifyIcon text set to: {Text}", _notifyIcon.Text);

            _notifyIcon.Visible = true;
            _logger.LogInformation("NotifyIcon visibility set to: {Visible}", _notifyIcon.Visible);

            // Wire up events
            _notifyIcon.MouseClick += OnNotifyIconClick;
            _logger.LogInformation("NotifyIcon MouseClick event handler attached");

            _notifyIcon.ContextMenuStrip = _contextMenu;
            _logger.LogInformation("NotifyIcon ContextMenuStrip attached");

            _logger.LogInformation("NotifyIcon initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize NotifyIcon");
            throw;
        }
    }

    /// <summary>
    /// Loads the application icon from file or embedded resource
    /// </summary>
    private Icon LoadApplicationIcon()
    {
        try
        {
            // First try to load from file in the application directory
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
            {
                _logger.LogInformation("Loading icon from file: {IconPath}", iconPath);
                return new Icon(iconPath);
            }

            // Try to load from embedded resource
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var resourceName = "TimeTracker.DesktopApp.app.ico";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                _logger.LogInformation("Loading icon from embedded resource: {ResourceName}", resourceName);
                return new Icon(stream);
            }

            // Fallback to system icon
            _logger.LogWarning("Could not find app.ico file or embedded resource, using SystemIcons.Application");
            return SystemIcons.Application;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading application icon, falling back to SystemIcons.Application");
            return SystemIcons.Application;
        }
    }

    /// <summary>
    /// Initializes the context menu with all menu items
    /// </summary>
    private void InitializeContextMenu()
    {
        try
        {
            // Create menu items
            _showMainWindowMenuItem = new ToolStripMenuItem("Show Main Window", null, OnShowMainWindow);
            _startTrackingMenuItem = new ToolStripMenuItem("Start Tracking", null, OnStartTracking);
            _pauseTrackingMenuItem = new ToolStripMenuItem("Pause Tracking", null, OnPauseTracking);
            _stopTrackingMenuItem = new ToolStripMenuItem("Stop Tracking", null, OnStopTracking);
            _settingsMenuItem = new ToolStripMenuItem("Settings", null, OnOpenSettings);
            _viewStatusMenuItem = new ToolStripMenuItem("View Status", null, OnViewStatus);
            _exitMenuItem = new ToolStripMenuItem("Exit", null, OnExit);

            // Add items to context menu
            _contextMenu.Items.AddRange(new ToolStripItem[]
            {
                _showMainWindowMenuItem,
                new ToolStripSeparator(),
                _startTrackingMenuItem,
                _pauseTrackingMenuItem,
                _stopTrackingMenuItem,
                new ToolStripSeparator(),
                _settingsMenuItem,
                _viewStatusMenuItem,
                new ToolStripSeparator(),
                _exitMenuItem
            });

            _logger.LogDebug("Context menu initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize context menu");
            throw;
        }
    }

    /// <summary>
    /// Starts the tray icon manager and begins status updates
    /// </summary>
    public void Start()
    {
        if (_disposed)
        {
            _logger.LogWarning("Cannot start disposed TrayIconManager");
            return;
        }

        try
        {
            _logger.LogInformation("Starting TrayIconManager...");

            // Ensure the NotifyIcon is visible
            _notifyIcon.Visible = true;
            _logger.LogInformation("NotifyIcon visibility set to true");

            // Force a refresh of the tray icon
            _notifyIcon.Icon = LoadApplicationIcon();
            _logger.LogInformation("NotifyIcon icon refreshed with app.ico");

            _statusUpdateTimer.Start();
            _logger.LogInformation("Status update timer started");

            // Show balloon tip to make the tray icon more noticeable
            _notifyIcon.ShowBalloonTip(5000, "TimeTracker", "Application started successfully - Look for the icon in your system tray!", ToolTipIcon.Info);
            _logger.LogInformation("Balloon tip displayed");

            _logger.LogInformation("TrayIconManager started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start TrayIconManager");
            throw;
        }
    }

    /// <summary>
    /// Stops the tray icon manager
    /// </summary>
    public void Stop()
    {
        if (_disposed) return;

        try
        {
            _statusUpdateTimer.Stop();
            _logger.LogInformation("TrayIconManager stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping TrayIconManager");
        }
    }

    /// <summary>
    /// Updates the tray icon state based on current tracking status
    /// </summary>
    private void UpdateTrayIconState()
    {
        try
        {
            if (_activityLogger.IsTrackingActive())
            {
                _notifyIcon.Text = "TimeTracker: Active";
                _notifyIcon.Icon = LoadApplicationIcon(); // Use app icon for active state

                _startTrackingMenuItem.Enabled = false;
                _pauseTrackingMenuItem.Enabled = true;
                _stopTrackingMenuItem.Enabled = true;
            }
            else if (_activityLogger.IsTrackingPaused())
            {
                _notifyIcon.Text = "TimeTracker: Paused";
                _notifyIcon.Icon = SystemIcons.Warning; // Yellow-ish icon for paused

                _startTrackingMenuItem.Enabled = true;
                _pauseTrackingMenuItem.Enabled = false;
                _stopTrackingMenuItem.Enabled = true;
            }
            else
            {
                _notifyIcon.Text = "TimeTracker: Inactive";
                _notifyIcon.Icon = LoadApplicationIcon(); // Use app icon for inactive state

                _startTrackingMenuItem.Enabled = true;
                _pauseTrackingMenuItem.Enabled = false;
                _stopTrackingMenuItem.Enabled = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tray icon state");
        }
    }

    /// <summary>
    /// Event handler for NotifyIcon click events
    /// </summary>
    private void OnNotifyIconClick(object? sender, MouseEventArgs e)
    {
        try
        {
            if (e.Button == MouseButtons.Left)
            {
                // Left click - show main window if available, otherwise show status overlay
                if (_mainForm != null)
                {
                    _mainForm.ShowForm();
                }
                else
                {
                    ShowStatusOverlay();
                }
            }
            // Right click is handled automatically by ContextMenuStrip
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling NotifyIcon click");
        }
    }

    /// <summary>
    /// Shows the status overlay window
    /// </summary>
    private void ShowStatusOverlay()
    {
        try
        {
            if (_statusOverlayForm == null || _statusOverlayForm.IsDisposed)
            {
                _statusOverlayForm = new StatusOverlayForm(_activityLogger, _logger);
            }

            _statusOverlayForm.ShowStatus();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing status overlay");
        }
    }

    /// <summary>
    /// Event handler for status update timer
    /// </summary>
    private void OnStatusUpdateTimer(object? sender, EventArgs e)
    {
        UpdateTrayIconState();
    }

    /// <summary>
    /// Event handler for Start Tracking menu item
    /// </summary>
    private async void OnStartTracking(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("User requested to start tracking");

            if (_activityLogger.IsTrackingPaused())
            {
                _activityLogger.ResumeTracking();
            }
            else
            {
                await _activityLogger.StartAsync();
            }

            UpdateTrayIconState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting tracking");
            MessageBox.Show($"Failed to start tracking: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Event handler for Pause Tracking menu item
    /// </summary>
    private void OnPauseTracking(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("User requested to pause tracking");
            _activityLogger.PauseTracking();
            UpdateTrayIconState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing tracking");
            MessageBox.Show($"Failed to pause tracking: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Event handler for Stop Tracking menu item
    /// </summary>
    private void OnStopTracking(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("User requested to stop tracking");
            _activityLogger.Stop();
            UpdateTrayIconState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping tracking");
            MessageBox.Show($"Failed to stop tracking: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Event handler for Settings menu item
    /// </summary>
    private void OnOpenSettings(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("User requested to open settings");

            if (_settingsForm == null || _settingsForm.IsDisposed)
            {
                _settingsForm = new SettingsForm(_logger);
            }

            _settingsForm.Show();
            _settingsForm.BringToFront();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening settings");
            MessageBox.Show($"Failed to open settings: {ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Event handler for View Status menu item
    /// </summary>
    private void OnViewStatus(object? sender, EventArgs e)
    {
        try
        {
            ShowStatusOverlay();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing status");
        }
    }

    /// <summary>
    /// Event handler for Show Main Window menu item
    /// </summary>
    private void OnShowMainWindow(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("User requested to show main window");

            if (_mainForm != null)
            {
                _mainForm.ShowForm();
            }
            else
            {
                _logger.LogWarning("Main form is not set");
                MessageBox.Show("Main window is not available.", "TimeTracker",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing main window");
        }
    }

    /// <summary>
    /// Event handler for Exit menu item
    /// </summary>
    private void OnExit(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("User requested application exit");

            // Confirm exit
            var result = MessageBox.Show("Are you sure you want to exit TimeTracker?",
                "Confirm Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // If main form is available, use its exit method for clean shutdown
                if (_mainForm != null)
                {
                    _mainForm.ExitApplication();
                }
                else
                {
                    Application.Exit();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during application exit");
        }
    }

    /// <summary>
    /// Sets the main form reference for tray icon integration
    /// </summary>
    public void SetMainForm(MainForm mainForm)
    {
        _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
        _logger.LogInformation("Main form reference set in TrayIconManager");
    }

    /// <summary>
    /// Shows a balloon tip notification
    /// </summary>
    public void ShowBalloonTip(string title, string text, ToolTipIcon icon)
    {
        try
        {
            _notifyIcon.ShowBalloonTip(5000, title, text, icon);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing balloon tip");
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;

            try
            {
                Stop();

                _statusUpdateTimer?.Stop();
                _statusUpdateTimer?.Dispose();

                _notifyIcon?.Dispose();
                _contextMenu?.Dispose();

                _settingsForm?.Dispose();
                _statusOverlayForm?.Dispose();

                _logger.LogInformation("TrayIconManager disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing TrayIconManager");
            }
        }
    }
}
