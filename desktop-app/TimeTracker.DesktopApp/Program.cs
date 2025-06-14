using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Security.Principal;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Main entry point for the TimeTracker Desktop Application.
/// Configures and runs the application as a desktop application with system tray integration,
/// dependency injection, logging, and configuration management.
/// </summary>
public class Program
{
    private static readonly string LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TimeTracker", "Logs");
    private static readonly string LogFilePath = Path.Combine(LogDirectory, "TimeTracker.log");

    [STAThread]
    public static async Task Main(string[] args)
    {
        // Temporary test mode - uncomment to test basic Windows Forms functionality
        // TestProgram.TestMain();
        // return;
        // Enable visual styles for Windows Forms
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        // Ensure log directory exists
        EnsureLogDirectoryExists();

        // Create initial file logger for startup diagnostics
        using var startupLoggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            AddFileLogging(builder);
        });
        var startupLogger = startupLoggerFactory.CreateLogger<Program>();

        try
        {
            startupLogger.LogInformation("=== TimeTracker Desktop Application Starting ===");
            startupLogger.LogInformation("Application Directory: {Directory}", AppContext.BaseDirectory);
            startupLogger.LogInformation("Command Line Args: {Args}", string.Join(" ", args));
            startupLogger.LogInformation(".NET Runtime Version: {Version}", RuntimeInformation.FrameworkDescription);
            startupLogger.LogInformation("OS Description: {OS}", RuntimeInformation.OSDescription);

            // Validate runtime environment
            await ValidateRuntimeEnvironmentAsync(startupLogger);

            // Create service collection and configure services
            var services = new ServiceCollection();

            // Configure logging
            ConfigureLogging(services, startupLogger);

            // Configure application settings
            var configuration = ConfigureAppSettings(startupLogger);
            services.AddSingleton<IConfiguration>(configuration);

            // Register application services
            RegisterServices(services, configuration);

            // Build service provider
            var serviceProvider = services.BuildServiceProvider();

            // Validate services
            await ValidateServicesAsync(serviceProvider, startupLogger);
            startupLogger.LogInformation("Service validation completed, proceeding to desktop application startup...");

            // Create and run the application context
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("TimeTracker Desktop Application configured successfully");
            logger.LogInformation("Starting desktop application...");

            try
            {
                logger.LogInformation("Creating TimeTrackerApplicationContext...");
                using var appContext = new TimeTrackerApplicationContext(serviceProvider, logger);
                logger.LogInformation("TimeTrackerApplicationContext created successfully");

                logger.LogInformation("Starting Application.Run() message loop...");
                Application.Run(appContext);
                logger.LogInformation("Application.Run() message loop ended");
            }
            catch (Exception contextEx)
            {
                logger.LogCritical(contextEx, "Critical error in application context or message loop");

                var errorMessage = $"TimeTracker failed during application context creation or execution:\n\n{contextEx.Message}\n\nStack Trace:\n{contextEx.StackTrace}";
                if (contextEx.InnerException != null)
                {
                    errorMessage += $"\n\nInner Exception:\n{contextEx.InnerException.Message}";
                }

                MessageBox.Show(errorMessage, "TimeTracker Context Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                throw;
            }

            logger.LogInformation("TimeTracker Desktop Application shutting down");
        }
        catch (Exception ex)
        {
            startupLogger.LogCritical(ex, "Critical failure during application startup");

            // Show detailed error to user
            var errorMessage = $"TimeTracker failed to start:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nInner Exception:\n{ex.InnerException.Message}";
            }

            MessageBox.Show(errorMessage, "TimeTracker Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            // Also write to console for debugging
            Console.WriteLine($"ERROR: {ex}");

            // Exit with error code
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Ensures the log directory exists
    /// </summary>
    private static void EnsureLogDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }
        catch
        {
            // If we can't create the log directory, continue without file logging
        }
    }

    /// <summary>
    /// Adds file logging to the logger builder
    /// </summary>
    private static void AddFileLogging(ILoggingBuilder builder)
    {
        try
        {
            builder.AddProvider(new FileLoggerProvider(LogFilePath));
        }
        catch
        {
            // If file logging fails, continue without it
        }
    }

    /// <summary>
    /// Validates the runtime environment with enhanced diagnostics
    /// </summary>
    private static async Task ValidateRuntimeEnvironmentAsync(ILogger logger)
    {
        try
        {
            logger.LogInformation("=== RUNTIME ENVIRONMENT VALIDATION ===");

            // Check .NET runtime version
            var runtimeVersion = Environment.Version;
            var frameworkDescription = RuntimeInformation.FrameworkDescription;
            logger.LogInformation("Runtime Version: {Version}", runtimeVersion);
            logger.LogInformation("Framework Description: {Framework}", frameworkDescription);

            // Validate .NET 8 runtime
            if (runtimeVersion.Major < 8)
            {
                throw new InvalidOperationException($"This application requires .NET 8.0 or later. Current version: {runtimeVersion}");
            }

            // Check if running on Windows
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("This application requires Windows OS");
            }

            // Check Windows version
            var osVersion = Environment.OSVersion;
            logger.LogInformation("OS Version: {OSVersion}", osVersion);

            // Validate Windows 10/11 or Windows Server 2016+
            if (osVersion.Version.Major < 10)
            {
                logger.LogWarning("This application is designed for Windows 10/11 or Windows Server 2016+. Current version: {Version}", osVersion.Version);
            }

            // Check available memory
            var workingSet = Environment.WorkingSet;
            var totalMemory = GC.GetTotalMemory(false);
            logger.LogInformation("Working Set Memory: {Memory:N0} bytes", workingSet);
            logger.LogInformation("GC Total Memory: {Memory:N0} bytes", totalMemory);

            // Check processor information
            var processorCount = Environment.ProcessorCount;
            logger.LogInformation("Processor Count: {Count}", processorCount);

            // Validate critical directories exist
            var appDir = AppContext.BaseDirectory;
            if (!Directory.Exists(appDir))
            {
                throw new DirectoryNotFoundException($"Application directory not found: {appDir}");
            }
            logger.LogInformation("Application Directory: {Directory}", appDir);

            // Check for required files
            var requiredFiles = new[] { "appsettings.json" };
            foreach (var file in requiredFiles)
            {
                var filePath = Path.Combine(appDir, file);
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Required file not found: {filePath}");
                }
                logger.LogInformation("Required file found: {File}", file);
            }

            // Check service account permissions
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            var isSystem = identity.IsSystem;
            var isService = identity.Name?.Contains("SERVICE", StringComparison.OrdinalIgnoreCase) ?? false;

            logger.LogInformation("Current Identity: {Name}", identity.Name);
            logger.LogInformation("Is Administrator: {IsAdmin}", isAdmin);
            logger.LogInformation("Is System: {IsSystem}", isSystem);
            logger.LogInformation("Is Service Account: {IsService}", isService);

            // Test file system permissions
            await TestFileSystemPermissionsAsync(logger);

            logger.LogInformation("Runtime environment validation completed successfully");
            logger.LogInformation("=== END RUNTIME ENVIRONMENT VALIDATION ===");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Runtime environment validation failed");
            throw;
        }
    }

    /// <summary>
    /// Tests file system permissions for critical directories
    /// </summary>
    private static async Task TestFileSystemPermissionsAsync(ILogger logger)
    {
        try
        {
            // Test application directory write access
            var appDir = AppContext.BaseDirectory;
            var testFile = Path.Combine(appDir, "permission_test.tmp");

            try
            {
                await File.WriteAllTextAsync(testFile, "test");
                File.Delete(testFile);
                logger.LogInformation("Application directory write access: OK");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Application directory write access: FAILED");
            }

            // Test logs directory access
            var logsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TimeTracker", "Logs");

            try
            {
                if (!Directory.Exists(logsDir))
                {
                    Directory.CreateDirectory(logsDir);
                }

                var logTestFile = Path.Combine(logsDir, "permission_test.tmp");
                await File.WriteAllTextAsync(logTestFile, "test");
                File.Delete(logTestFile);
                logger.LogInformation("Logs directory access: OK - {LogsDir}", logsDir);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Logs directory access: FAILED - {LogsDir}", logsDir);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "File system permissions test failed");
        }
    }

    /// <summary>
    /// Configures logging with multiple providers
    /// </summary>
    private static void ConfigureLogging(IServiceCollection services, ILogger startupLogger)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();

            // Add file logging
            AddFileLogging(logging);

            // Add Event Log with error handling for service scenarios
            try
            {
                logging.AddEventLog(options =>
                {
                    options.SourceName = "TimeTracker.DesktopApp";
                    options.LogName = "Application";
                });
                startupLogger.LogInformation("Event Log provider configured successfully");
            }
            catch (Exception ex)
            {
                startupLogger.LogWarning(ex, "Failed to configure Event Log provider - continuing without it");
            }

            logging.SetMinimumLevel(LogLevel.Information);
        });
    }

    /// <summary>
    /// Configures application settings with validation
    /// </summary>
    private static IConfiguration ConfigureAppSettings(ILogger logger)
    {
        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        logger.LogInformation("Looking for configuration file: {Path}", appSettingsPath);

        if (!File.Exists(appSettingsPath))
        {
            logger.LogError("Configuration file not found: {Path}", appSettingsPath);
            throw new FileNotFoundException($"Configuration file not found: {appSettingsPath}");
        }

        try
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            var configuration = builder.Build();
            logger.LogInformation("Configuration file loaded successfully");
            return configuration;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load configuration file");
            throw;
        }
    }

    /// <summary>
    /// Validates that all required services can be created
    /// </summary>
    private static async Task ValidateServicesAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            logger.LogInformation("Validating service dependencies...");

            // Test that we can resolve critical services
            var activityLogger = serviceProvider.GetRequiredService<ActivityLogger>();
            var dataAccess = serviceProvider.GetRequiredService<IDataAccess>();
            var pipedreamClient = serviceProvider.GetRequiredService<IPipedreamClient>();

            logger.LogInformation("All critical services resolved successfully");

            // Test database connectivity by getting count
            try
            {
                var activityCount = await dataAccess.GetActivityCountAsync();
                logger.LogInformation("Database connectivity validated. Current activity count: {Count}", activityCount);
            }
            catch (Exception dbEx)
            {
                logger.LogWarning(dbEx, "Database connectivity test failed - continuing without database validation");
                // Continue without database validation for now
            }

            logger.LogInformation("Service validation completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Service validation failed");
            throw;
        }
    }

    /// <summary>
    /// Registers application services with the dependency injection container
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The application configuration</param>
    private static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        // Register background task queue
        services.AddSingleton<BackgroundTaskQueue>();

        // Register SQL Server data access as the only database provider
        services.AddSingleton<SqlServerDataAccess>();
        services.AddSingleton<IDataAccess>(provider => provider.GetRequiredService<SqlServerDataAccess>());

        // Register Pipedream client
        services.AddSingleton<PipedreamClient>();

        // Register optimized monitoring components
        services.AddSingleton<IWindowMonitor, OptimizedWindowMonitor>();
        services.AddSingleton<IInputMonitor, OptimizedInputMonitor>();

        // Register Pipedream client interface
        services.AddSingleton<IPipedreamClient>(provider => provider.GetRequiredService<PipedreamClient>());

        // Register activity logger
        services.AddSingleton<ActivityLogger>();

        // Register batch processor (not as hosted service for desktop app)
        services.AddSingleton<BatchProcessor>();

        // Register desktop app specific services
        services.AddSingleton<TrayIconManager>();
        services.AddSingleton<SessionMonitor>();
        services.AddSingleton<ConfigurationManager>();

        // Register Phase 1 MVP services
        services.AddSingleton<MainForm>();
        services.AddSingleton<HeartbeatService>();
    }
}

/// <summary>
/// Application context for the TimeTracker desktop application.
/// Manages the application lifecycle, system tray integration, and coordinates all services.
/// </summary>
public class TimeTrackerApplicationContext : ApplicationContext
{
    private readonly ILogger<TimeTrackerApplicationContext> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ActivityLogger _activityLogger;
    private readonly TrayIconManager _trayIconManager;
    private readonly SessionMonitor _sessionMonitor;
    private readonly BatchProcessor _batchProcessor;
    private readonly MainForm _mainForm;
    private readonly HeartbeatService _heartbeatService;
    private bool _disposed = false;

    public TimeTrackerApplicationContext(IServiceProvider serviceProvider, ILogger logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = (ILogger<TimeTrackerApplicationContext>)logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("TimeTrackerApplicationContext constructor started");

        try
        {
            // Get required services
            _logger.LogInformation("Resolving ActivityLogger service...");
            _activityLogger = _serviceProvider.GetRequiredService<ActivityLogger>();
            _logger.LogInformation("ActivityLogger service resolved successfully");

            _logger.LogInformation("Resolving TrayIconManager service...");
            _trayIconManager = _serviceProvider.GetRequiredService<TrayIconManager>();
            _logger.LogInformation("TrayIconManager service resolved successfully");

            _logger.LogInformation("Resolving SessionMonitor service...");
            _sessionMonitor = _serviceProvider.GetRequiredService<SessionMonitor>();
            _logger.LogInformation("SessionMonitor service resolved successfully");

            _logger.LogInformation("Resolving BatchProcessor service...");
            _batchProcessor = _serviceProvider.GetRequiredService<BatchProcessor>();
            _logger.LogInformation("BatchProcessor service resolved successfully");

            _logger.LogInformation("Resolving MainForm service...");
            _mainForm = _serviceProvider.GetRequiredService<MainForm>();
            _logger.LogInformation("MainForm service resolved successfully");

            _logger.LogInformation("Resolving HeartbeatService service...");
            _heartbeatService = _serviceProvider.GetRequiredService<HeartbeatService>();
            _logger.LogInformation("HeartbeatService service resolved successfully");

            _logger.LogInformation("All services resolved, starting application initialization...");
            // Initialize the application
            InitializeApplication();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical error in TimeTrackerApplicationContext constructor");
            throw;
        }
    }

    /// <summary>
    /// Initializes the desktop application
    /// </summary>
    private async void InitializeApplication()
    {
        try
        {
            _logger.LogInformation("=== TimeTracker Desktop Application Initializing ===");
            _logger.LogInformation("Application Start Time: {StartTime}", DateTime.Now);
            _logger.LogInformation("Process ID: {ProcessId}", Environment.ProcessId);
            _logger.LogInformation("Working Directory: {WorkingDirectory}", Environment.CurrentDirectory);
            _logger.LogInformation("User Account: {Account}", Environment.UserName);

            // Start session monitoring first
            _logger.LogInformation("Starting session monitoring...");
            _sessionMonitor.Start();

            // Start tray icon manager
            _logger.LogInformation("Starting tray icon manager...");
            _trayIconManager.Start();

            // Show main form
            _logger.LogInformation("Showing main form...");
            _mainForm.Show();

            // Start heartbeat service
            _logger.LogInformation("Starting heartbeat service...");
            _heartbeatService.SetMainForm(_mainForm);
            await _heartbeatService.StartAsync(CancellationToken.None);

            // Start batch processor
            _logger.LogInformation("Starting batch processor...");
            await _batchProcessor.StartAsync(CancellationToken.None);

            // Start activity logging
            _logger.LogInformation("Starting activity logging...");
            await _activityLogger.StartAsync();

            _logger.LogInformation("TimeTracker Desktop Application initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Critical failure during application initialization");

            var errorMessage = $"Failed to initialize TimeTracker:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
            if (ex.InnerException != null)
            {
                errorMessage += $"\n\nInner Exception:\n{ex.InnerException.Message}";
            }

            MessageBox.Show(errorMessage, "TimeTracker Initialization Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);

            ExitThread();
        }
    }

    /// <summary>
    /// Disposes of the application context and all managed resources
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _disposed = true;

            try
            {
                _logger.LogInformation("TimeTracker Desktop Application shutting down...");

                // Stop all services in reverse order
                _activityLogger?.Stop();
                _batchProcessor?.StopAsync(CancellationToken.None).Wait(TimeSpan.FromSeconds(10));
                _heartbeatService?.StopAsync(CancellationToken.None).Wait(TimeSpan.FromSeconds(5));
                _trayIconManager?.Stop();
                _sessionMonitor?.Stop();

                // Dispose services
                _mainForm?.Dispose();
                _heartbeatService?.Dispose();
                _trayIconManager?.Dispose();
                _sessionMonitor?.Dispose();
                _activityLogger?.Dispose();
                _batchProcessor?.Dispose();

                _logger.LogInformation("TimeTracker Desktop Application shutdown completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application shutdown");
            }
        }

        base.Dispose(disposing);
    }

}
