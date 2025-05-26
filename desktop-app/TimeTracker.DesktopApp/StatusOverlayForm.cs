using Microsoft.Extensions.Logging;
using System.Drawing;

namespace TimeTracker.DesktopApp;

/// <summary>
/// A small, non-modal overlay window that displays real-time activity status information.
/// Shows current window, time since last input, Pipedream status, and pending submissions.
/// </summary>
public partial class StatusOverlayForm : Form
{
    private readonly ActivityLogger _activityLogger;
    private readonly ILogger _logger;
    private readonly System.Windows.Forms.Timer _refreshTimer;
    private readonly System.Windows.Forms.Timer _autoHideTimer;

    // UI Controls
    private Label _titleLabel;
    private Label _activeWindowLabel;
    private Label _lastInputLabel;
    private Label _pipedreamStatusLabel;
    private Label _pendingSubmissionsLabel;
    private Label _trackingStatusLabel;

    public StatusOverlayForm(ActivityLogger activityLogger, ILogger logger)
    {
        _activityLogger = activityLogger ?? throw new ArgumentNullException(nameof(activityLogger));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize timers
        _refreshTimer = new System.Windows.Forms.Timer();
        _refreshTimer.Interval = 2000; // Refresh every 2 seconds
        _refreshTimer.Tick += OnRefreshTimer;

        _autoHideTimer = new System.Windows.Forms.Timer();
        _autoHideTimer.Interval = 10000; // Auto-hide after 10 seconds
        _autoHideTimer.Tick += OnAutoHideTimer;

        InitializeComponent();
        SetupFormProperties();
    }

    /// <summary>
    /// Initializes the form components
    /// </summary>
    private void InitializeComponent()
    {
        SuspendLayout();

        // Form properties
        this.Text = "TimeTracker Status";
        this.Size = new Size(350, 200);
        this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
        this.ShowInTaskbar = false;
        this.TopMost = true;
        this.StartPosition = FormStartPosition.Manual;

        // Title label
        _titleLabel = new Label
        {
            Text = "TimeTracker Status",
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Location = new Point(10, 10),
            Size = new Size(320, 20),
            ForeColor = Color.DarkBlue
        };

        // Tracking status label
        _trackingStatusLabel = new Label
        {
            Text = "Status: Loading...",
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            Location = new Point(10, 35),
            Size = new Size(320, 18),
            ForeColor = Color.DarkGreen
        };

        // Active window label
        _activeWindowLabel = new Label
        {
            Text = "Active Window: Loading...",
            Font = new Font("Segoe UI", 8.25F),
            Location = new Point(10, 58),
            Size = new Size(320, 16),
            AutoEllipsis = true
        };

        // Last input label
        _lastInputLabel = new Label
        {
            Text = "Time Since Last Input: Loading...",
            Font = new Font("Segoe UI", 8.25F),
            Location = new Point(10, 78),
            Size = new Size(320, 16)
        };

        // Pipedream status label
        _pipedreamStatusLabel = new Label
        {
            Text = "Pipedream: Loading...",
            Font = new Font("Segoe UI", 8.25F),
            Location = new Point(10, 98),
            Size = new Size(320, 16)
        };

        // Pending submissions label
        _pendingSubmissionsLabel = new Label
        {
            Text = "Pending Submissions: Loading...",
            Font = new Font("Segoe UI", 8.25F),
            Location = new Point(10, 118),
            Size = new Size(320, 16)
        };

        // Add controls to form
        this.Controls.AddRange(new Control[]
        {
            _titleLabel,
            _trackingStatusLabel,
            _activeWindowLabel,
            _lastInputLabel,
            _pipedreamStatusLabel,
            _pendingSubmissionsLabel
        });

        ResumeLayout(false);
    }

    /// <summary>
    /// Sets up additional form properties and event handlers
    /// </summary>
    private void SetupFormProperties()
    {
        // Position the form near the system tray (bottom-right corner)
        var workingArea = Screen.PrimaryScreen.WorkingArea;
        this.Location = new Point(
            workingArea.Right - this.Width - 10,
            workingArea.Bottom - this.Height - 10
        );

        // Handle form events
        this.Deactivate += OnFormDeactivate;
        this.KeyDown += OnFormKeyDown;
        this.Click += OnFormClick;

        // Handle mouse leave to start auto-hide timer
        this.MouseLeave += OnFormMouseLeave;
        this.MouseEnter += OnFormMouseEnter;
    }

    /// <summary>
    /// Shows the status overlay and starts refresh/auto-hide timers
    /// </summary>
    public void ShowStatus()
    {
        try
        {
            // Update status immediately
            UpdateStatusDisplay();

            // Show the form
            this.Show();
            this.BringToFront();

            // Start timers
            _refreshTimer.Start();
            _autoHideTimer.Start();

            _logger.LogDebug("Status overlay shown");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing status overlay");
        }
    }

    /// <summary>
    /// Hides the status overlay and stops timers
    /// </summary>
    public void HideStatus()
    {
        try
        {
            _refreshTimer.Stop();
            _autoHideTimer.Stop();
            this.Hide();

            _logger.LogDebug("Status overlay hidden");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hiding status overlay");
        }
    }

    /// <summary>
    /// Updates the status display with current information
    /// </summary>
    private void UpdateStatusDisplay()
    {
        try
        {
            // Get tracking status
            if (_activityLogger.IsTrackingActive())
            {
                _trackingStatusLabel.Text = "Status: Active";
                _trackingStatusLabel.ForeColor = Color.DarkGreen;
            }
            else if (_activityLogger.IsTrackingPaused())
            {
                _trackingStatusLabel.Text = "Status: Paused";
                _trackingStatusLabel.ForeColor = Color.Orange;
            }
            else
            {
                _trackingStatusLabel.Text = "Status: Inactive";
                _trackingStatusLabel.ForeColor = Color.Gray;
            }

            // Get current activity
            var currentActivity = _activityLogger.GetCurrentActivity();
            if (currentActivity != null)
            {
                _activeWindowLabel.Text = $"Active Window: {currentActivity.ActiveWindowTitle}";
            }
            else
            {
                _activeWindowLabel.Text = "Active Window: No data available";
            }

            // Get status info and parse it
            var statusInfo = _activityLogger.GetStatusInfo();
            ParseAndDisplayStatusInfo(statusInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating status display");
            
            // Show error state
            _trackingStatusLabel.Text = "Status: Error";
            _trackingStatusLabel.ForeColor = Color.Red;
            _activeWindowLabel.Text = "Active Window: Error retrieving data";
            _lastInputLabel.Text = "Time Since Last Input: Error";
            _pipedreamStatusLabel.Text = "Pipedream: Error";
            _pendingSubmissionsLabel.Text = "Pending Submissions: Error";
        }
    }

    /// <summary>
    /// Parses the status info string and updates individual labels
    /// </summary>
    private void ParseAndDisplayStatusInfo(string statusInfo)
    {
        try
        {
            // Parse the status info string
            // Format: "Current Status: {status}, Time since last input: {time}, Pipedream: {status}, Pending submissions: {count}"
            
            var parts = statusInfo.Split(',');
            
            foreach (var part in parts)
            {
                var trimmedPart = part.Trim();
                
                if (trimmedPart.StartsWith("Time since last input:"))
                {
                    _lastInputLabel.Text = trimmedPart;
                }
                else if (trimmedPart.StartsWith("Pipedream:"))
                {
                    _pipedreamStatusLabel.Text = trimmedPart;
                }
                else if (trimmedPart.StartsWith("Pending submissions:"))
                {
                    _pendingSubmissionsLabel.Text = trimmedPart;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing status info: {StatusInfo}", statusInfo);
            
            // Fallback to showing raw status info
            _lastInputLabel.Text = "Time Since Last Input: Parse error";
            _pipedreamStatusLabel.Text = "Pipedream: Parse error";
            _pendingSubmissionsLabel.Text = "Pending Submissions: Parse error";
        }
    }

    /// <summary>
    /// Event handler for refresh timer
    /// </summary>
    private void OnRefreshTimer(object? sender, EventArgs e)
    {
        UpdateStatusDisplay();
    }

    /// <summary>
    /// Event handler for auto-hide timer
    /// </summary>
    private void OnAutoHideTimer(object? sender, EventArgs e)
    {
        HideStatus();
    }

    /// <summary>
    /// Event handler for form deactivate (lost focus)
    /// </summary>
    private void OnFormDeactivate(object? sender, EventArgs e)
    {
        // Start auto-hide timer when form loses focus
        if (!_autoHideTimer.Enabled)
        {
            _autoHideTimer.Start();
        }
    }

    /// <summary>
    /// Event handler for key down events
    /// </summary>
    private void OnFormKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            HideStatus();
        }
    }

    /// <summary>
    /// Event handler for form click
    /// </summary>
    private void OnFormClick(object? sender, EventArgs e)
    {
        // Reset auto-hide timer on click
        _autoHideTimer.Stop();
        _autoHideTimer.Start();
    }

    /// <summary>
    /// Event handler for mouse leave
    /// </summary>
    private void OnFormMouseLeave(object? sender, EventArgs e)
    {
        // Start auto-hide timer when mouse leaves
        if (!_autoHideTimer.Enabled)
        {
            _autoHideTimer.Start();
        }
    }

    /// <summary>
    /// Event handler for mouse enter
    /// </summary>
    private void OnFormMouseEnter(object? sender, EventArgs e)
    {
        // Stop auto-hide timer when mouse enters
        _autoHideTimer.Stop();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            
            _autoHideTimer?.Stop();
            _autoHideTimer?.Dispose();
        }
        
        base.Dispose(disposing);
    }
}
