using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Main entry point for the TimeTracker Desktop Application.
/// Configures and runs the application as a Windows Service with dependency injection,
/// logging, and configuration management.
/// </summary>
public class Program
{
    private static readonly string LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TimeTracker", "Logs");
    private static readonly string LogFilePath = Path.Combine(LogDirectory, "TimeTracker.log");

    public static async Task Main(string[] args)
    {
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
            startupLogger.LogInformation("Running as Windows Service: {IsWindowsService}", WindowsServiceHelpers.IsWindowsService());
            startupLogger.LogInformation(".NET Runtime Version: {Version}", RuntimeInformation.FrameworkDescription);
            startupLogger.LogInformation("OS Description: {OS}", RuntimeInformation.OSDescription);

            // Validate runtime environment
            await ValidateRuntimeEnvironmentAsync(startupLogger);

            // Create and configure the host builder
            var builder = Host.CreateApplicationBuilder(args);

            // Configure the application to run as a Windows Service
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "TimeTracker.DesktopApp";
            });

            // Configure enhanced logging
            ConfigureLogging(builder.Services, startupLogger);

            // Configure application settings with validation
            ConfigureAppSettings(builder, startupLogger);

            // Register application services
            RegisterServices(builder.Services, builder.Configuration);

            // Register the main worker service
            builder.Services.AddHostedService<TimeTrackerWorkerService>();

            // Build and run the host
            var host = builder.Build();

            // Perform startup validation
            await ValidateServicesAsync(host, startupLogger);

            // Log startup information
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("TimeTracker Desktop Application configured successfully");
            logger.LogInformation("Starting host...");

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            startupLogger.LogCritical(ex, "Critical failure during application startup");

            // Try to log to Event Log as fallback
            try
            {
                using var eventLoggerFactory = LoggerFactory.Create(builder =>
                {
                    try
                    {
                        builder.AddEventLog(options =>
                        {
                            options.SourceName = "TimeTracker.DesktopApp";
                            options.LogName = "Application";
                        });
                    }
                    catch
                    {
                        // Event log may not be available
                    }
                });
                var eventLogger = eventLoggerFactory.CreateLogger<Program>();
                eventLogger.LogCritical(ex, "TimeTracker service failed to start");
            }
            catch
            {
                // Ignore event log errors during shutdown
            }

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
    private static void ConfigureAppSettings(HostApplicationBuilder builder, ILogger logger)
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
            builder.Configuration.AddJsonFile(appSettingsPath, optional: false, reloadOnChange: true);
            logger.LogInformation("Configuration file loaded successfully");
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
    private static async Task ValidateServicesAsync(IHost host, ILogger logger)
    {
        try
        {
            logger.LogInformation("Validating service dependencies...");

            // Test that we can resolve critical services
            var activityLogger = host.Services.GetRequiredService<ActivityLogger>();
            var dataAccess = host.Services.GetRequiredService<IDataAccess>();
            var pipedreamClient = host.Services.GetRequiredService<IPipedreamClient>();

            logger.LogInformation("All critical services resolved successfully");

            // Test database connectivity by getting count
            var activityCount = await dataAccess.GetActivityCountAsync();
            logger.LogInformation("Database connectivity validated. Current activity count: {Count}", activityCount);

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

        // Register batch processor as hosted service
        services.AddHostedService<BatchProcessor>();
    }
}

/// <summary>
/// Background worker service that orchestrates the activity monitoring process.
/// Implements IHostedService to integrate with the .NET hosting model.
/// </summary>
public class TimeTrackerWorkerService : BackgroundService
{
    private readonly ILogger<TimeTrackerWorkerService> _logger;
    private readonly ActivityLogger _activityLogger;
    private readonly IHostApplicationLifetime _applicationLifetime;

    public TimeTrackerWorkerService(
        ActivityLogger activityLogger,
        ILogger<TimeTrackerWorkerService> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _activityLogger = activityLogger;
        _logger = logger;
        _applicationLifetime = applicationLifetime;
    }

    /// <summary>
    /// Called when the service is starting
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== TimeTracker Worker Service Starting ===");
        _logger.LogInformation("Service Start Time: {StartTime}", DateTime.Now);
        _logger.LogInformation("Process ID: {ProcessId}", Environment.ProcessId);
        _logger.LogInformation("Working Directory: {WorkingDirectory}", Environment.CurrentDirectory);
        _logger.LogInformation("Service Account: {Account}", Environment.UserName);

        // Call base.StartAsync first to signal to SCM that service is starting
        await base.StartAsync(cancellationToken);

        // Start initialization in background to avoid SCM timeout
        _ = Task.Run(async () =>
        {
            try
            {
                // Log to Event Log for service startup tracking
                await LogToEventLogAsync("TimeTracker service startup initiated", EventLogEntryType.Information);

                // Minimal delay for system stabilization
                _logger.LogInformation("Waiting for system stabilization...");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                // Validate environment before starting
                await ValidateServiceEnvironmentAsync();

                // Start activity logging with enhanced timeout and retry logic
                _logger.LogInformation("Starting activity logging...");
                await StartActivityLoggingWithRetryAsync(cancellationToken);

                // Verify service is working
                await VerifyServiceOperationAsync();

                // Log successful startup
                await LogToEventLogAsync("TimeTracker service started successfully", EventLogEntryType.Information);
                _logger.LogInformation("TimeTracker Worker Service startup completed successfully");
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Service startup was cancelled");
                await LogToEventLogAsync("TimeTracker service startup was cancelled", EventLogEntryType.Warning);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical failure during service startup");
                await LogToEventLogAsync($"TimeTracker service startup failed: {ex.Message}", EventLogEntryType.Error);

                // Try to provide more diagnostic information
                try
                {
                    await LogDiagnosticInformationAsync();
                }
                catch (Exception diagEx)
                {
                    _logger.LogError(diagEx, "Failed to log diagnostic information");
                }

                // Create failure report
                await CreateFailureReportAsync(ex);

                // Stop the application gracefully
                _applicationLifetime.StopApplication();
            }
        }, cancellationToken);

        _logger.LogInformation("TimeTracker Worker Service StartAsync completed - initialization continuing in background");
    }

    /// <summary>
    /// Starts activity logging with retry logic
    /// </summary>
    private async Task StartActivityLoggingWithRetryAsync(CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        const int baseDelaySeconds = 5;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Starting activity logging (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(30)); // 30-second timeout per attempt for faster service startup

                await _activityLogger.StartAsync();
                _logger.LogInformation("Activity logging started successfully on attempt {Attempt}", attempt);
                return; // Success, exit retry loop
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(baseDelaySeconds * attempt);
                _logger.LogWarning(ex, "Activity logging start failed on attempt {Attempt}/{MaxRetries}. Retrying in {Delay} seconds",
                    attempt, maxRetries, delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
            }
        }

        // If we get here, all retries failed
        throw new InvalidOperationException($"Failed to start activity logging after {maxRetries} attempts");
    }

    /// <summary>
    /// Logs messages to Windows Event Log
    /// </summary>
    private async Task LogToEventLogAsync(string message, EventLogEntryType entryType)
    {
        try
        {
            await Task.Run(() =>
            {
                try
                {
                    using var eventLog = new EventLog("Application");
                    eventLog.Source = "TimeTracker.DesktopApp";
                    eventLog.WriteEntry(message, entryType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to write to Event Log internally: {Message}", message);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write to Event Log: {Message}", message);
        }
    }

    /// <summary>
    /// Creates a failure report for troubleshooting
    /// </summary>
    private async Task CreateFailureReportAsync(Exception exception)
    {
        try
        {
            var reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "TimeTracker", "Logs", $"failure_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

            var report = new StringBuilder();
            report.AppendLine("=== TIMETRACKER SERVICE FAILURE REPORT ===");
            report.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"Machine: {Environment.MachineName}");
            report.AppendLine($"User: {Environment.UserName}");
            report.AppendLine($"OS: {Environment.OSVersion}");
            report.AppendLine($"Runtime: {RuntimeInformation.FrameworkDescription}");
            report.AppendLine($"Process ID: {Environment.ProcessId}");
            report.AppendLine($"Working Directory: {Environment.CurrentDirectory}");
            report.AppendLine($"Application Directory: {AppContext.BaseDirectory}");
            report.AppendLine();
            report.AppendLine("=== EXCEPTION DETAILS ===");
            report.AppendLine(exception.ToString());
            report.AppendLine();
            report.AppendLine("=== ENVIRONMENT VARIABLES ===");

            foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
            {
                report.AppendLine($"{env.Key}={env.Value}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            await File.WriteAllTextAsync(reportPath, report.ToString());

            _logger.LogInformation("Failure report created: {ReportPath}", reportPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create failure report");
        }
    }

    /// <summary>
    /// Main execution loop for the background service
    /// </summary>
    /// <param name="stoppingToken">Cancellation token for stopping the service</param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TimeTracker Worker Service is running");

        try
        {
            // Log status information periodically
            while (!stoppingToken.IsCancellationRequested)
            {
                // Log status every 5 minutes
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

                if (!stoppingToken.IsCancellationRequested)
                {
                    var statusInfo = _activityLogger.GetStatusInfo();
                    var activityCount = await _activityLogger.GetActivityCountAsync();

                    _logger.LogInformation("Status: {StatusInfo}, Total activities logged: {ActivityCount}",
                        statusInfo, activityCount);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
            _logger.LogInformation("TimeTracker Worker Service execution was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TimeTracker Worker Service execution");
        }
    }

    /// <summary>
    /// Called when the service is stopping
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("TimeTracker Worker Service is stopping");

        try
        {
            _activityLogger.Stop();
            _logger.LogInformation("Activity logging stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping activity logging");
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("TimeTracker Worker Service stopped");
    }

    public override void Dispose()
    {
        try
        {
            _activityLogger?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing ActivityLogger");
        }

        base.Dispose();
    }

    /// <summary>
    /// Validates the service environment before startup
    /// </summary>
    private async Task ValidateServiceEnvironmentAsync()
    {
        _logger.LogInformation("Validating service environment...");

        // Check if running as a service
        var isWindowsService = WindowsServiceHelpers.IsWindowsService();
        _logger.LogInformation("Running as Windows Service: {IsService}", isWindowsService);

        // Check application directory permissions
        var appDir = AppContext.BaseDirectory;
        if (!Directory.Exists(appDir))
        {
            throw new DirectoryNotFoundException($"Application directory not found: {appDir}");
        }

        // Check if we can write to logs directory
        var logsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TimeTracker", "Logs");
        try
        {
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }

            var testFile = Path.Combine(logsDir, "test.tmp");
            await File.WriteAllTextAsync(testFile, "test");
            File.Delete(testFile);
            _logger.LogInformation("Logs directory access validated: {LogsDir}", logsDir);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cannot write to logs directory: {LogsDir}", logsDir);
        }

        _logger.LogInformation("Service environment validation completed");
    }

    /// <summary>
    /// Verifies that the service is operating correctly after startup
    /// </summary>
    private async Task VerifyServiceOperationAsync()
    {
        _logger.LogInformation("Verifying service operation...");

        try
        {
            // Check if activity logger is responsive
            var statusInfo = _activityLogger.GetStatusInfo();
            _logger.LogInformation("Activity Logger Status: {Status}", statusInfo);

            // Test database connectivity
            var activityCount = await _activityLogger.GetActivityCountAsync();
            _logger.LogInformation("Database connectivity verified. Activity count: {Count}", activityCount);

            _logger.LogInformation("Service operation verification completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Service operation verification failed");
            throw;
        }
    }

    /// <summary>
    /// Logs diagnostic information for troubleshooting
    /// </summary>
    private async Task LogDiagnosticInformationAsync()
    {
        try
        {
            _logger.LogInformation("=== DIAGNOSTIC INFORMATION ===");
            _logger.LogInformation("Machine Name: {MachineName}", Environment.MachineName);
            _logger.LogInformation("User Name: {UserName}", Environment.UserName);
            _logger.LogInformation("OS Version: {OSVersion}", Environment.OSVersion);
            _logger.LogInformation("Processor Count: {ProcessorCount}", Environment.ProcessorCount);
            _logger.LogInformation("Working Set: {WorkingSet} bytes", Environment.WorkingSet);
            _logger.LogInformation("System Directory: {SystemDirectory}", Environment.SystemDirectory);
            _logger.LogInformation("Current Directory: {CurrentDirectory}", Environment.CurrentDirectory);
            _logger.LogInformation("Application Base Directory: {BaseDirectory}", AppContext.BaseDirectory);

            // Check file system permissions
            var appDir = AppContext.BaseDirectory;
            var appSettingsPath = Path.Combine(appDir, "appsettings.json");
            _logger.LogInformation("App Settings File Exists: {Exists}", File.Exists(appSettingsPath));

            if (File.Exists(appSettingsPath))
            {
                var fileInfo = new FileInfo(appSettingsPath);
                _logger.LogInformation("App Settings File Size: {Size} bytes", fileInfo.Length);
                _logger.LogInformation("App Settings Last Modified: {LastModified}", fileInfo.LastWriteTime);
            }

            _logger.LogInformation("=== END DIAGNOSTIC INFORMATION ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect diagnostic information");
        }
    }
}
