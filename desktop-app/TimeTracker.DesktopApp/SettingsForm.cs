using Microsoft.Extensions.Logging;
using System.Drawing;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Settings form for configuring TimeTracker application parameters.
/// Allows users to modify Pipedream endpoint, intervals, and other settings.
/// </summary>
public partial class SettingsForm : Form
{
    private readonly ILogger _logger;
    private ConfigurationManager? _configManager;

    // UI Controls
    private TabControl _tabControl;
    private TabPage _generalTab;
    private TabPage _pipedreamTab;
    private TabPage _advancedTab;

    // General settings controls
    private CheckBox _autoStartCheckBox;
    private NumericUpDown _activityTimeoutNumeric;
    private Label _activityTimeoutLabel;

    // Pipedream settings controls
    private TextBox _pipedreamUrlTextBox;
    private Label _pipedreamUrlLabel;
    private Button _testConnectionButton;
    private Label _connectionStatusLabel;
    private NumericUpDown _batchIntervalNumeric;
    private Label _batchIntervalLabel;

    // Advanced settings controls
    private NumericUpDown _maxConcurrentSubmissionsNumeric;
    private Label _maxConcurrentSubmissionsLabel;
    private NumericUpDown _retryAttemptsNumeric;
    private Label _retryAttemptsLabel;

    // Action buttons
    private Button _saveButton;
    private Button _cancelButton;
    private Button _resetButton;

    public SettingsForm(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        InitializeComponent();
        SetupFormProperties();
        LoadCurrentSettings();
    }

    /// <summary>
    /// Initializes the form components
    /// </summary>
    private void InitializeComponent()
    {
        SuspendLayout();

        // Form properties
        this.Text = "TimeTracker Settings";
        this.Size = new Size(500, 400);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ShowInTaskbar = true;
        this.StartPosition = FormStartPosition.CenterScreen;

        // Tab control
        _tabControl = new TabControl
        {
            Location = new Point(10, 10),
            Size = new Size(465, 310),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        // General tab
        _generalTab = new TabPage("General");
        InitializeGeneralTab();
        _tabControl.TabPages.Add(_generalTab);

        // Pipedream tab
        _pipedreamTab = new TabPage("Pipedream");
        InitializePipedreamTab();
        _tabControl.TabPages.Add(_pipedreamTab);

        // Advanced tab
        _advancedTab = new TabPage("Advanced");
        InitializeAdvancedTab();
        _tabControl.TabPages.Add(_advancedTab);

        // Action buttons
        _saveButton = new Button
        {
            Text = "Save",
            Location = new Point(235, 330),
            Size = new Size(75, 25),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _saveButton.Click += OnSaveClick;

        _cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(320, 330),
            Size = new Size(75, 25),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _cancelButton.Click += OnCancelClick;

        _resetButton = new Button
        {
            Text = "Reset",
            Location = new Point(400, 330),
            Size = new Size(75, 25),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
        _resetButton.Click += OnResetClick;

        // Add controls to form
        this.Controls.AddRange(new Control[]
        {
            _tabControl,
            _saveButton,
            _cancelButton,
            _resetButton
        });

        ResumeLayout(false);
    }

    /// <summary>
    /// Initializes the General tab controls
    /// </summary>
    private void InitializeGeneralTab()
    {
        // Auto-start checkbox
        _autoStartCheckBox = new CheckBox
        {
            Text = "Start with Windows",
            Location = new Point(15, 20),
            Size = new Size(200, 20),
            Checked = false
        };

        // Activity timeout
        _activityTimeoutLabel = new Label
        {
            Text = "Activity Timeout (seconds):",
            Location = new Point(15, 55),
            Size = new Size(150, 20)
        };

        _activityTimeoutNumeric = new NumericUpDown
        {
            Location = new Point(170, 53),
            Size = new Size(80, 20),
            Minimum = 10,
            Maximum = 3600,
            Value = 60
        };

        _generalTab.Controls.AddRange(new Control[]
        {
            _autoStartCheckBox,
            _activityTimeoutLabel,
            _activityTimeoutNumeric
        });
    }

    /// <summary>
    /// Initializes the Pipedream tab controls
    /// </summary>
    private void InitializePipedreamTab()
    {
        // Pipedream URL
        _pipedreamUrlLabel = new Label
        {
            Text = "Pipedream Endpoint URL:",
            Location = new Point(15, 20),
            Size = new Size(150, 20)
        };

        _pipedreamUrlTextBox = new TextBox
        {
            Location = new Point(15, 43),
            Size = new Size(400, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Test connection button
        _testConnectionButton = new Button
        {
            Text = "Test Connection",
            Location = new Point(15, 75),
            Size = new Size(120, 25)
        };
        _testConnectionButton.Click += OnTestConnectionClick;

        // Connection status label
        _connectionStatusLabel = new Label
        {
            Text = "Connection status: Not tested",
            Location = new Point(145, 80),
            Size = new Size(250, 20),
            ForeColor = Color.Gray
        };

        // Batch interval
        _batchIntervalLabel = new Label
        {
            Text = "Batch Interval (seconds):",
            Location = new Point(15, 115),
            Size = new Size(150, 20)
        };

        _batchIntervalNumeric = new NumericUpDown
        {
            Location = new Point(170, 113),
            Size = new Size(80, 20),
            Minimum = 5,
            Maximum = 3600,
            Value = 30
        };

        _pipedreamTab.Controls.AddRange(new Control[]
        {
            _pipedreamUrlLabel,
            _pipedreamUrlTextBox,
            _testConnectionButton,
            _connectionStatusLabel,
            _batchIntervalLabel,
            _batchIntervalNumeric
        });
    }

    /// <summary>
    /// Initializes the Advanced tab controls
    /// </summary>
    private void InitializeAdvancedTab()
    {
        // Max concurrent submissions
        _maxConcurrentSubmissionsLabel = new Label
        {
            Text = "Max Concurrent Submissions:",
            Location = new Point(15, 20),
            Size = new Size(180, 20)
        };

        _maxConcurrentSubmissionsNumeric = new NumericUpDown
        {
            Location = new Point(200, 18),
            Size = new Size(60, 20),
            Minimum = 1,
            Maximum = 10,
            Value = 3
        };

        // Retry attempts
        _retryAttemptsLabel = new Label
        {
            Text = "Retry Attempts:",
            Location = new Point(15, 55),
            Size = new Size(100, 20)
        };

        _retryAttemptsNumeric = new NumericUpDown
        {
            Location = new Point(120, 53),
            Size = new Size(60, 20),
            Minimum = 1,
            Maximum = 10,
            Value = 3
        };

        _advancedTab.Controls.AddRange(new Control[]
        {
            _maxConcurrentSubmissionsLabel,
            _maxConcurrentSubmissionsNumeric,
            _retryAttemptsLabel,
            _retryAttemptsNumeric
        });
    }

    /// <summary>
    /// Sets up additional form properties
    /// </summary>
    private void SetupFormProperties()
    {
        // Initialize configuration manager
        try
        {
            _configManager = new ConfigurationManager(_logger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize configuration manager");
            MessageBox.Show("Failed to initialize settings. Some features may not work correctly.", 
                "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Loads current settings into the form controls
    /// </summary>
    private void LoadCurrentSettings()
    {
        try
        {
            if (_configManager == null) return;

            var config = _configManager.LoadConfiguration();

            // Load general settings
            _autoStartCheckBox.Checked = config.AutoStartWithWindows;
            _activityTimeoutNumeric.Value = config.ActivityTimeoutMs / 1000; // Convert to seconds

            // Load Pipedream settings
            _pipedreamUrlTextBox.Text = config.PipedreamEndpointUrl ?? "";
            _batchIntervalNumeric.Value = config.BatchIntervalMs / 1000; // Convert to seconds

            // Load advanced settings
            _maxConcurrentSubmissionsNumeric.Value = config.MaxConcurrentSubmissions;
            _retryAttemptsNumeric.Value = config.RetryAttempts;

            _logger.LogDebug("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load current settings");
            MessageBox.Show("Failed to load current settings. Default values will be used.", 
                "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    /// <summary>
    /// Event handler for Save button click
    /// </summary>
    private async void OnSaveClick(object? sender, EventArgs e)
    {
        try
        {
            if (_configManager == null)
            {
                MessageBox.Show("Configuration manager not available.", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Validate inputs
            if (!ValidateInputs())
            {
                return;
            }

            // Create configuration object
            var config = new UserConfiguration
            {
                AutoStartWithWindows = _autoStartCheckBox.Checked,
                ActivityTimeoutMs = (int)_activityTimeoutNumeric.Value * 1000,
                PipedreamEndpointUrl = _pipedreamUrlTextBox.Text.Trim(),
                BatchIntervalMs = (int)_batchIntervalNumeric.Value * 1000,
                MaxConcurrentSubmissions = (int)_maxConcurrentSubmissionsNumeric.Value,
                RetryAttempts = (int)_retryAttemptsNumeric.Value
            };

            // Save configuration
            await _configManager.SaveConfigurationAsync(config);

            _logger.LogInformation("Settings saved successfully");
            MessageBox.Show("Settings saved successfully!", "Success", 
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /// <summary>
    /// Event handler for Cancel button click
    /// </summary>
    private void OnCancelClick(object? sender, EventArgs e)
    {
        this.Close();
    }

    /// <summary>
    /// Event handler for Reset button click
    /// </summary>
    private void OnResetClick(object? sender, EventArgs e)
    {
        var result = MessageBox.Show("Are you sure you want to reset all settings to default values?", 
            "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            ResetToDefaults();
        }
    }

    /// <summary>
    /// Event handler for Test Connection button click
    /// </summary>
    private async void OnTestConnectionClick(object? sender, EventArgs e)
    {
        try
        {
            _testConnectionButton.Enabled = false;
            _connectionStatusLabel.Text = "Testing connection...";
            _connectionStatusLabel.ForeColor = Color.Blue;

            var url = _pipedreamUrlTextBox.Text.Trim();
            if (string.IsNullOrEmpty(url))
            {
                _connectionStatusLabel.Text = "Connection status: Please enter a URL";
                _connectionStatusLabel.ForeColor = Color.Red;
                return;
            }

            // Test connection (simplified - you may want to inject IPipedreamClient)
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            
            var response = await httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                _connectionStatusLabel.Text = "Connection status: Success";
                _connectionStatusLabel.ForeColor = Color.Green;
            }
            else
            {
                _connectionStatusLabel.Text = $"Connection status: Failed ({response.StatusCode})";
                _connectionStatusLabel.ForeColor = Color.Red;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed");
            _connectionStatusLabel.Text = $"Connection status: Error - {ex.Message}";
            _connectionStatusLabel.ForeColor = Color.Red;
        }
        finally
        {
            _testConnectionButton.Enabled = true;
        }
    }

    /// <summary>
    /// Validates form inputs
    /// </summary>
    private bool ValidateInputs()
    {
        // Validate Pipedream URL
        var url = _pipedreamUrlTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(url) && !Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            MessageBox.Show("Please enter a valid Pipedream URL.", "Validation Error", 
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            _tabControl.SelectedTab = _pipedreamTab;
            _pipedreamUrlTextBox.Focus();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Resets all controls to default values
    /// </summary>
    private void ResetToDefaults()
    {
        _autoStartCheckBox.Checked = false;
        _activityTimeoutNumeric.Value = 60;
        _pipedreamUrlTextBox.Text = "";
        _batchIntervalNumeric.Value = 30;
        _maxConcurrentSubmissionsNumeric.Value = 3;
        _retryAttemptsNumeric.Value = 3;
        _connectionStatusLabel.Text = "Connection status: Not tested";
        _connectionStatusLabel.ForeColor = Color.Gray;
    }
}
