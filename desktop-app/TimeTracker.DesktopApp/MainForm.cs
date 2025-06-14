using Microsoft.Extensions.Logging;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Main application window for the TimeTracker desktop application.
/// Provides the primary user interface and handles window lifecycle events.
/// </summary>
public partial class MainForm : Form
{
    private readonly ILogger<MainForm> _logger;
    private readonly TrayIconManager _trayIconManager;
    private MenuStrip _menuStrip;
    private ToolStripMenuItem _fileMenuItem;
    private ToolStripMenuItem _quitMenuItem;
    private Label _statusLabel;
    private Label _heartbeatLabel;
    private Button _testConnectionButton;
    private bool _isClosing = false;

    public MainForm(ILogger<MainForm> logger, TrayIconManager trayIconManager)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _trayIconManager = trayIconManager ?? throw new ArgumentNullException(nameof(trayIconManager));

        _logger.LogInformation("MainForm constructor started");
        InitializeComponent();
        _logger.LogInformation("MainForm initialized successfully");
    }

    /// <summary>
    /// Initializes the form components
    /// </summary>
    private void InitializeComponent()
    {
        try
        {
            _logger.LogInformation("Initializing MainForm components...");

            // Form properties
            Text = "TimeTracker MVP";
            Size = new Size(500, 350);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(400, 300);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;

            // Set the form icon
            try
            {
                using var iconStream = GetType().Assembly.GetManifestResourceStream("TimeTracker.DesktopApp.app.ico");
                if (iconStream != null)
                {
                    Icon = new Icon(iconStream);
                    _logger.LogInformation("Form icon loaded successfully");
                }
                else
                {
                    _logger.LogWarning("Could not load form icon from embedded resource");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load form icon");
            }

            // Create menu strip
            CreateMenuStrip();

            // Create main content
            CreateMainContent();

            // Wire up events
            FormClosing += OnFormClosing;
            Load += OnFormLoad;

            _logger.LogInformation("MainForm components initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing MainForm components");
            throw;
        }
    }

    /// <summary>
    /// Creates the main menu strip
    /// </summary>
    private void CreateMenuStrip()
    {
        _menuStrip = new MenuStrip();

        // File menu
        _fileMenuItem = new ToolStripMenuItem("&File");
        _quitMenuItem = new ToolStripMenuItem("&Quit", null, OnQuitMenuClick);
        _quitMenuItem.ShortcutKeys = Keys.Control | Keys.Q;

        _fileMenuItem.DropDownItems.Add(_quitMenuItem);
        _menuStrip.Items.Add(_fileMenuItem);

        // Add menu strip to form
        MainMenuStrip = _menuStrip;
        Controls.Add(_menuStrip);

        _logger.LogInformation("Menu strip created with File > Quit option");
    }

    /// <summary>
    /// Creates the main content area
    /// </summary>
    private void CreateMainContent()
    {
        var mainPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(20)
        };

        // Configure row styles
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        // Title label
        var titleLabel = new Label
        {
            Text = "TimeTracker Desktop Application",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            AutoSize = true
        };

        // Status label
        _statusLabel = new Label
        {
            Text = "Status: Application running in background",
            Font = new Font("Segoe UI", 10),
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 20, 0, 10)
        };

        // Heartbeat status label
        _heartbeatLabel = new Label
        {
            Text = "Heartbeat: Waiting for first transmission...",
            Font = new Font("Segoe UI", 10),
            TextAlign = ContentAlignment.MiddleLeft,
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };

        // Test connection button
        _testConnectionButton = new Button
        {
            Text = "Test Pipedream Connection",
            Size = new Size(200, 35),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
            Margin = new Padding(0, 10, 0, 0)
        };
        _testConnectionButton.Click += OnTestConnectionClick;

        // Add controls to panel
        mainPanel.Controls.Add(titleLabel, 0, 0);
        mainPanel.Controls.Add(_statusLabel, 0, 1);
        mainPanel.Controls.Add(_heartbeatLabel, 0, 2);
        mainPanel.Controls.Add(_testConnectionButton, 0, 3);

        Controls.Add(mainPanel);

        _logger.LogInformation("Main content area created");
    }

    /// <summary>
    /// Handles form load event
    /// </summary>
    private void OnFormLoad(object? sender, EventArgs e)
    {
        try
        {
            _logger.LogInformation("MainForm loaded and displayed");

            // Update status
            _statusLabel.Text = "Status: Application running - minimize to system tray by clicking X";

            // Set up tray icon manager to handle this form
            _trayIconManager?.SetMainForm(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MainForm OnFormLoad");
        }
    }

    /// <summary>
    /// Handles form closing event - minimizes to tray instead of closing
    /// </summary>
    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_isClosing && e.CloseReason == CloseReason.UserClosing)
        {
            _logger.LogInformation("User clicked X button - minimizing to system tray");

            // Cancel the close and hide the form instead
            e.Cancel = true;
            Hide();

            // Show balloon tip to inform user
            _trayIconManager.ShowBalloonTip("TimeTracker",
                "Application minimized to system tray. Right-click the tray icon to exit.",
                ToolTipIcon.Info);
        }
        else
        {
            _logger.LogInformation("Form closing - CloseReason: {CloseReason}, IsClosing: {IsClosing}",
                e.CloseReason, _isClosing);
        }
    }

    /// <summary>
    /// Handles File > Quit menu click
    /// </summary>
    private void OnQuitMenuClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("File > Quit menu clicked - initiating application exit");
        ExitApplication();
    }

    /// <summary>
    /// Handles test connection button click
    /// </summary>
    private async void OnTestConnectionClick(object? sender, EventArgs e)
    {
        _logger.LogInformation("Test connection button clicked");

        _testConnectionButton.Enabled = false;
        _testConnectionButton.Text = "Testing...";

        try
        {
            // This would need to be injected or accessed through the service provider
            // For now, just show a message
            MessageBox.Show("Connection test feature will be implemented in the next iteration.",
                "Test Connection", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally
        {
            _testConnectionButton.Enabled = true;
            _testConnectionButton.Text = "Test Pipedream Connection";
        }
    }

    /// <summary>
    /// Shows the main form (restores from tray)
    /// </summary>
    public void ShowForm()
    {
        _logger.LogInformation("Showing MainForm (restore from tray)");

        Show();
        WindowState = FormWindowState.Normal;
        BringToFront();
        Activate();
    }

    /// <summary>
    /// Updates the heartbeat status display
    /// </summary>
    public void UpdateHeartbeatStatus(string status)
    {
        if (InvokeRequired)
        {
            Invoke(new Action<string>(UpdateHeartbeatStatus), status);
            return;
        }

        _heartbeatLabel.Text = $"Heartbeat: {status}";
        _logger.LogDebug("Heartbeat status updated: {Status}", status);
    }

    /// <summary>
    /// Exits the application cleanly
    /// </summary>
    public void ExitApplication()
    {
        _logger.LogInformation("Initiating clean application exit");

        _isClosing = true;
        Application.Exit();
    }

    /// <summary>
    /// Clean up resources
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _menuStrip?.Dispose();
            _logger.LogInformation("MainForm disposed");
        }
        base.Dispose(disposing);
    }
}
