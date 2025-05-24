using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging;
using TimeTracker.DesktopApp.Interfaces;

namespace TimeTracker.DesktopApp;

/// <summary>
/// Main entry point for the TimeTracker Desktop Application.
/// Configures and runs the application as a Windows Service with dependency injection,
/// logging, and configuration management.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            // Create and configure the host builder
            var builder = Host.CreateApplicationBuilder(args);

            // Configure the application to run as a Windows Service
            builder.Services.AddWindowsService(options =>
            {
                options.ServiceName = "TimeTracker.DesktopApp";
            });

            // Configure logging
            builder.Services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddEventLog(); // For Windows Service logging
                logging.SetMinimumLevel(LogLevel.Information);
            });

            // Configure application settings
            builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            // Register application services
            RegisterServices(builder.Services, builder.Configuration);

            // Register the main worker service
            builder.Services.AddHostedService<TimeTrackerWorkerService>();

            // Build and run the host
            var host = builder.Build();

            // Log startup information
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("TimeTracker Desktop Application starting...");
            logger.LogInformation("Running as Windows Service: {IsWindowsService}",
                WindowsServiceHelpers.IsWindowsService());

            await host.RunAsync();
        }
        catch (Exception ex)
        {
            // Log startup errors to Windows Event Log
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddEventLog());
            var logger = loggerFactory.CreateLogger<Program>();
            logger.LogCritical(ex, "Application failed to start");

            // Exit with error code
            Environment.Exit(1);
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

        // Register optimized SQLite data access
        services.AddSingleton<OptimizedSQLiteDataAccess>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<OptimizedSQLiteDataAccess>>();
            var config = provider.GetRequiredService<IConfiguration>();
            var databasePath = configuration["TimeTracker:DatabasePath"] ?? "TimeTracker.db";

            // Ensure database path is absolute
            if (!Path.IsPathRooted(databasePath))
            {
                var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                databasePath = Path.Combine(appDirectory, databasePath);
            }

            return new OptimizedSQLiteDataAccess(databasePath, config, logger);
        });

        // Register Pipedream client
        services.AddSingleton<PipedreamClient>();

        // Register optimized monitoring components
        services.AddSingleton<IWindowMonitor, OptimizedWindowMonitor>();
        services.AddSingleton<IInputMonitor, OptimizedInputMonitor>();

        // Register data access interface (using optimized implementation)
        services.AddSingleton<IDataAccess>(provider => provider.GetRequiredService<OptimizedSQLiteDataAccess>());

        // Register Pipedream client interface
        services.AddSingleton<IPipedreamClient>(provider => provider.GetRequiredService<PipedreamClient>());

        // Register activity logger
        services.AddSingleton<ActivityLogger>();
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
        _logger.LogInformation("TimeTracker Worker Service is starting");

        try
        {
            await _activityLogger.StartAsync();
            _logger.LogInformation("Activity logging started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to start activity logging");
            _applicationLifetime.StopApplication();
            return;
        }

        await base.StartAsync(cancellationToken);
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
}
