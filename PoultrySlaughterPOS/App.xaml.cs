using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Data.Context;
using PoultrySlaughterPOS.Utils.Extensions;
using PoultrySlaughterPOS.ViewModels;
using PoultrySlaughterPOS.Views;
using Serilog;
using System.IO;
using System.Windows;

namespace PoultrySlaughterPOS
{
    public partial class App : Application
    {
        private IHost? _host;
        private ILogger<App>? _logger;

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                ConfigureAdvancedLogging();
                _host = CreateAdvancedHostBuilder().Build();
                InitializeDatabaseInfrastructure();

                var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                var mainWindowViewModel = _host.Services.GetRequiredService<MainWindowViewModel>();

                mainWindow.DataContext = mainWindowViewModel;
                mainWindow.Show();

                _logger?.LogInformation("Poultry Slaughter POS application started successfully");
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                _logger?.LogCritical(ex, "Critical error during application startup");

                MessageBox.Show(
                    $"حدث خطأ حرج أثناء بدء تشغيل التطبيق:\n{ex.Message}",
                    "خطأ في بدء التشغيل",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Environment.Exit(1);
            }
        }

        private static void ConfigureAdvancedLogging()
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PoultrySlaughterPOS",
                "Logs");

            Directory.CreateDirectory(logDirectory);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
                .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.WithEnvironmentUserName()
                .WriteTo.File(
                    path: Path.Combine(logDirectory, "pos-log-.txt"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    fileSizeLimitBytes: 100_000_000,
                    rollOnFileSizeLimit: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }

        private static IHostBuilder CreateAdvancedHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    ConfigureEntityFrameworkAdvanced(services);
                    ConfigureApplicationServices(services);
                });
        }

        private static void ConfigureEntityFrameworkAdvanced(IServiceCollection services)
        {
            var connectionString = BuildOptimizedConnectionString();

            services.AddDbContext<PoultryDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null);

                    sqlOptions.CommandTimeout(30);
                    sqlOptions.MigrationsAssembly("PoultrySlaughterPOS");
                });

                options.EnableSensitiveDataLogging(false);
                options.EnableDetailedErrors(true);
                options.EnableServiceProviderCaching(true);

#if DEBUG
                options.LogTo(message => System.Diagnostics.Debug.WriteLine(message));
#endif
            });
        }

        private static void ConfigureApplicationServices(IServiceCollection services)
        {
            services.AddAdvancedLogging();
            services.AddBusinessServices();
            services.AddViewModels();
            services.AddViews();
            services.AddUtilityServices();
        }

        private void InitializeDatabaseInfrastructure()
        {
            try
            {
                using var scope = _host!.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<PoultryDbContext>();
                _logger = scope.ServiceProvider.GetRequiredService<ILogger<App>>();

                _logger.LogInformation("Initializing database infrastructure");

                dbContext.Database.EnsureCreated();

                if (dbContext.Database.CanConnect())
                {
                    _logger.LogInformation("Database connection established successfully");
                }
                else
                {
                    throw new InvalidOperationException("Cannot establish database connection");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Database initialization failed");
                throw;
            }
        }

        private static string BuildOptimizedConnectionString()
        {
            var databaseDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PoultrySlaughterPOS",
                "Database");

            Directory.CreateDirectory(databaseDirectory);

            return $@"Server=(localdb)\MSSQLLocalDB;Database=PoultrySlaughterPOS;AttachDbFilename={Path.Combine(databaseDirectory, "PoultrySlaughterPOS.mdf")};Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;Connection Timeout=30;Command Timeout=60;";
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _logger?.LogInformation("Initiating graceful application shutdown");
                _host?.Dispose();
                Log.CloseAndFlush();
            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry(
                    "PoultrySlaughterPOS",
                    $"Error during application shutdown: {ex.Message}",
                    System.Diagnostics.EventLogEntryType.Error);
            }
            finally
            {
                base.OnExit(e);
            }
        }
    }
}