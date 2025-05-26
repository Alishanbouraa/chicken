using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Services.Interfaces;
using PoultrySlaughterPOS.Services.Implementations;
using PoultrySlaughterPOS.ViewModels;
using PoultrySlaughterPOS.Views;
using PoultrySlaughterPOS.Utils.Converters;
using PoultrySlaughterPOS.Utils.Validation;
using Serilog;

namespace PoultrySlaughterPOS.Utils.Extensions
{
    /// <summary>
    /// Advanced service collection extensions implementing enterprise-grade dependency injection patterns
    /// Provides comprehensive registration strategies for all application layers with proper lifetime management
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers business services with optimal lifetime configuration for data consistency
        /// Implements Repository and Unit of Work patterns with proper transaction scoping
        /// </summary>
        /// <param name="services">Service collection for dependency injection registration</param>
        /// <returns>Updated service collection for method chaining</returns>
        public static IServiceCollection AddBusinessServices(this IServiceCollection services)
        {
            // Repository pattern registration with scoped lifetime for transaction consistency
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ITruckRepository, TruckRepository>();
            services.AddScoped<ICustomerRepository, CustomerRepository>();
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();

            // Business services registration with comprehensive error handling and logging
            services.AddScoped<ITruckLoadService, TruckLoadService>();
            services.AddScoped<IInvoiceService, InvoiceService>();
            services.AddScoped<ICustomerService, CustomerService>();

            // Infrastructure services with singleton lifetime for performance optimization
            services.AddSingleton<INavigationService, NavigationService>();

            return services;
        }

        /// <summary>
        /// Registers ViewModels with transient lifetime for optimal memory management
        /// Ensures fresh ViewModel instances for each navigation operation
        /// </summary>
        /// <param name="services">Service collection for dependency injection registration</param>
        /// <returns>Updated service collection for method chaining</returns>
        public static IServiceCollection AddViewModels(this IServiceCollection services)
        {
            // Main application ViewModels with proper lifetime management
            services.AddTransient<MainWindowViewModel>();
            services.AddTransient<TruckLoadViewModel>();
            services.AddTransient<InvoiceViewModel>();
            services.AddTransient<CustomerManagementViewModel>();
            services.AddTransient<TransactionHistoryViewModel>();
            services.AddTransient<ReportsViewModel>();
            services.AddTransient<SettingsViewModel>();

            return services;
        }

        /// <summary>
        /// Registers Views with transient lifetime for UI component management
        /// Implements view-first navigation pattern with DataTemplate resolution
        /// </summary>
        /// <param name="services">Service collection for dependency injection registration</param>
        /// <returns>Updated service collection for method chaining</returns>
        public static IServiceCollection AddViews(this IServiceCollection services)
        {
            // Window registration for dependency injection container management
            services.AddTransient<MainWindow>();

            // UserControl views for content presentation with proper lifecycle
            services.AddTransient<TruckLoadView>();
            services.AddTransient<InvoiceView>();
            services.AddTransient<CustomerManagementView>();
            services.AddTransient<TransactionHistoryView>();
            services.AddTransient<ReportsView>();
            services.AddTransient<SettingsView>();

            return services;
        }

        /// <summary>
        /// Configures advanced logging infrastructure with structured logging patterns
        /// Implements multiple log targets with appropriate filtering and formatting
        /// </summary>
        /// <param name="services">Service collection for dependency injection registration</param>
        /// <returns>Updated service collection for method chaining</returns>
        public static IServiceCollection AddAdvancedLogging(this IServiceCollection services)
        {
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
                builder.SetMinimumLevel(LogLevel.Information);

#if DEBUG
                builder.AddConsole();
                builder.AddDebug();
#endif
            });

            return services;
        }

        /// <summary>
        /// Registers utility services and converters for UI operations
        /// Provides comprehensive support for data binding, validation, and document generation
        /// </summary>
        /// <param name="services">Service collection for dependency injection registration</param>
        /// <returns>Updated service collection for method chaining</returns>
        public static IServiceCollection AddUtilityServices(this IServiceCollection services)
        {
            // Value converters for advanced data binding scenarios
            services.AddSingleton<DecimalToStringConverter>();
            services.AddSingleton<BooleanToVisibilityConverter>();
            services.AddSingleton<InverseBooleanConverter>();

            // Validation services for comprehensive input validation
            services.AddSingleton<ValidationService>();

            // Document generation and printing services with proper interface resolution
            services.AddScoped<IPrintingService, Services.Implementations.PrintingService>();
            services.AddScoped<IReportingService, Services.Implementations.ReportingService>();

            return services;
        }
    }
}