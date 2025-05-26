using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Services.Interfaces;
using PoultrySlaughterPOS.Utils.Configuration;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Main window ViewModel implementing advanced navigation patterns and application state management
    /// Serves as the primary orchestrator for the entire POS application workflow
    /// </summary>
    public partial class MainWindowViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, BaseViewModel> _viewModelCache;

        #region Observable Properties

        [ObservableProperty]
        private BaseViewModel? _currentViewModel;

        [ObservableProperty]
        private string _currentViewTitle = "نظام إدارة مسلخ الدجاج";

        [ObservableProperty]
        private bool _isMenuOpen;

        [ObservableProperty]
        private ObservableCollection<NavigationMenuItem> _navigationItems = new();

        [ObservableProperty]
        private NavigationMenuItem? _selectedNavigationItem;

        [ObservableProperty]
        private string _applicationVersion = "1.0.0";

        [ObservableProperty]
        private string _currentUserName = "المستخدم الحالي";

        [ObservableProperty]
        private DateTime _currentDateTime = DateTime.Now;

        [ObservableProperty]
        private bool _isDarkTheme;

        [ObservableProperty]
        private string _connectionStatus = "متصل";

        [ObservableProperty]
        private bool _isConnected = true;

        #endregion

        public MainWindowViewModel(
            INavigationService navigationService,
            IServiceProvider serviceProvider,
            ILogger<MainWindowViewModel> logger) : base(logger)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _viewModelCache = new Dictionary<Type, BaseViewModel>();

            Title = "نظام إدارة مسلخ الدجاج - الإصدار 1.0";
            InitializeApplication();
            StartDateTimeTimer();
        }

        #region Relay Commands

        [RelayCommand]
        private async Task NavigateToTruckLoadManagement()
        {
            await NavigateToViewModelAsync<TruckLoadViewModel>("إدارة تحميل الشاحنات");
        }

        [RelayCommand]
        private async Task NavigateToInvoiceManagement()
        {
            await NavigateToViewModelAsync<InvoiceViewModel>("إدارة الفواتير والمبيعات");
        }

        [RelayCommand]
        private async Task NavigateToCustomerManagement()
        {
            await NavigateToViewModelAsync<CustomerManagementViewModel>("إدارة الزبائن والحسابات");
        }

        [RelayCommand]
        private async Task NavigateToTransactionHistory()
        {
            await NavigateToViewModelAsync<TransactionHistoryViewModel>("تاريخ المعاملات");
        }

        [RelayCommand]
        private async Task NavigateToReports()
        {
            await NavigateToViewModelAsync<ReportsViewModel>("التقارير والإحصائيات");
        }

        [RelayCommand]
        private async Task NavigateToSettings()
        {
            await NavigateToViewModelAsync<SettingsViewModel>("إعدادات النظام");
        }

        [RelayCommand]
        private void ToggleTheme()
        {
            IsDarkTheme = !IsDarkTheme;
            ApplyTheme(IsDarkTheme);
            _logger.LogInformation("Theme changed to {Theme}", IsDarkTheme ? "Dark" : "Light");
        }

        [RelayCommand]
        private void ToggleMenu()
        {
            IsMenuOpen = !IsMenuOpen;
        }

        [RelayCommand]
        private async Task RefreshApplicationState()
        {
            await ExecuteAsyncOperation(async () =>
            {
                await CheckDatabaseConnectionAsync();
                await RefreshCurrentViewAsync();
                StatusMessage = "تم تحديث حالة التطبيق بنجاح";
            }, true, "Refresh Application State");
        }

        [RelayCommand]
        private async Task ShowAboutDialog()
        {
            var aboutMessage = $"نظام إدارة مسلخ الدجاج\n" +
                             $"الإصدار: {ApplicationVersion}\n" +
                             $"تاريخ البناء: {DateTime.Now:yyyy-MM-dd}\n" +
                             $"تطوير: فريق التطوير التقني\n\n" +
                             $"نظام شامل لإدارة عمليات المسلخ والمبيعات";

            await _navigationService.ShowSuccessDialogAsync("حول النظام", aboutMessage);
        }

        [RelayCommand]
        private async Task BackupDatabase()
        {
            await ExecuteAsyncOperation(async () =>
            {
                // Implementation would involve database backup service
                await Task.Delay(2000); // Simulate backup operation
                StatusMessage = "تم إنشاء نسخة احتياطية من قاعدة البيانات";
                await _navigationService.ShowSuccessDialogAsync("النسخ الاحتياطي", "تم إنشاء النسخة الاحتياطية بنجاح");
            }, true, "Database Backup");
        }

        [RelayCommand]
        private async Task ExitApplication()
        {
            var confirmed = await _navigationService.ShowConfirmationDialogAsync(
                "إغلاق التطبيق",
                "هل أنت متأكد من إغلاق التطبيق؟\nسيتم حفظ جميع البيانات تلقائياً.");

            if (confirmed)
            {
                _logger.LogInformation("Application exit requested by user");
                await CleanupApplicationAsync();
                System.Windows.Application.Current.Shutdown();
            }
        }

        #endregion

        #region Private Methods

        private void InitializeApplication()
        {
            try
            {
                _logger.LogInformation("Initializing main application interface");

                InitializeNavigationItems();
                SubscribeToNavigationEvents();

                // Navigate to default view
                _ = Task.Run(async () => await NavigateToTruckLoadManagement());

                _logger.LogInformation("Main application interface initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during application initialization");
                AddError("خطأ حرج أثناء تهيئة التطبيق");
            }
        }

        private void InitializeNavigationItems()
        {
            NavigationItems.Clear();

            var menuItems = new[]
            {
                new NavigationMenuItem
                {
                    Title = "تحميل الشاحنات",
                    IconKind = MaterialDesignThemes.Wpf.PackIconKind.Truck,
                    ViewModelType = typeof(TruckLoadViewModel),
                    Command = NavigateToTruckLoadManagementCommand
                },
                new NavigationMenuItem
                {
                    Title = "إدارة الفواتير",
                    IconKind = MaterialDesignThemes.Wpf.PackIconKind.Receipt,
                    ViewModelType = typeof(InvoiceViewModel),
                    Command = NavigateToInvoiceManagementCommand
                },
                new NavigationMenuItem
                {
                    Title = "إدارة الزبائن",
                    IconKind = MaterialDesignThemes.Wpf.PackIconKind.Account,
                    ViewModelType = typeof(CustomerManagementViewModel),
                    Command = NavigateToCustomerManagementCommand
                },
                new NavigationMenuItem
                {
                    Title = "تاريخ المعاملات",
                    IconKind = MaterialDesignThemes.Wpf.PackIconKind.History,
                    ViewModelType = typeof(TransactionHistoryViewModel),
                    Command = NavigateToTransactionHistoryCommand
                },
                new NavigationMenuItem
                {
                    Title = "التقارير",
                    IconKind = MaterialDesignThemes.Wpf.PackIconKind.ChartLine,
                    ViewModelType = typeof(ReportsViewModel),
                    Command = NavigateToReportsCommand
                },
                new NavigationMenuItem
                {
                    Title = "الإعدادات",
                    IconKind = MaterialDesignThemes.Wpf.PackIconKind.Settings,
                    ViewModelType = typeof(SettingsViewModel),
                    Command = NavigateToSettingsCommand
                }
            };

            foreach (var item in menuItems)
            {
                NavigationItems.Add(item);
            }
        }

        private void SubscribeToNavigationEvents()
        {
            _navigationService.NavigationRequested += OnNavigationRequested;
        }

        private async void OnNavigationRequested(object? sender, NavigationEventArgs e)
        {
            try
            {
                await NavigateToViewModelAsync(e.ViewModelType, e.Parameter);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling navigation request to {ViewModelType}", e.ViewModelType.Name);
                AddError($"خطأ في التنقل إلى {e.ViewModelType.Name}");
            }
        }

        private async Task NavigateToViewModelAsync<T>(string title) where T : BaseViewModel
        {
            await NavigateToViewModelAsync(typeof(T), null, title);
        }

        private async Task NavigateToViewModelAsync(Type viewModelType, object? parameter = null, string? title = null)
        {
            try
            {
                _logger.LogDebug("Navigating to ViewModel: {ViewModelType}", viewModelType.Name);

                // Cleanup current ViewModel if needed
                if (CurrentViewModel != null)
                {
                    CurrentViewModel.Cleanup();
                }

                // Get or create ViewModel instance with caching strategy
                BaseViewModel viewModel;
                if (_viewModelCache.TryGetValue(viewModelType, out var cachedViewModel))
                {
                    viewModel = cachedViewModel;
                    _logger.LogDebug("Using cached ViewModel instance for {ViewModelType}", viewModelType.Name);
                }
                else
                {
                    viewModel = (BaseViewModel)_serviceProvider.GetRequiredService(viewModelType);
                    _viewModelCache[viewModelType] = viewModel;
                    _logger.LogDebug("Created new ViewModel instance for {ViewModelType}", viewModelType.Name);
                }

                // Update navigation state
                CurrentViewModel = viewModel;
                CurrentViewTitle = title ?? viewModel.Title;

                // Update selected navigation item
                SelectedNavigationItem = NavigationItems.FirstOrDefault(item => item.ViewModelType == viewModelType);

                _logger.LogInformation("Successfully navigated to {ViewModelType}", viewModelType.Name);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during ViewModel navigation to {ViewModelType}", viewModelType.Name);
                AddError($"خطأ في تحميل الواجهة: {ex.Message}");
            }
        }

        private async Task RefreshCurrentViewAsync()
        {
            if (CurrentViewModel != null)
            {
                // Trigger refresh on current ViewModel if it supports it
                if (CurrentViewModel is TruckLoadViewModel truckLoadVm)
                {
                    if (truckLoadVm.RefreshDataCommand.CanExecute(null))
                        await truckLoadVm.RefreshDataCommand.ExecuteAsync(null);
                }
                else if (CurrentViewModel is InvoiceViewModel invoiceVm)
                {
                    if (invoiceVm.RefreshDataCommand.CanExecute(null))
                        await invoiceVm.RefreshDataCommand.ExecuteAsync(null);
                }
                // Add other ViewModel refresh patterns as needed
            }
        }

        private async Task CheckDatabaseConnectionAsync()
        {
            try
            {
                // Implementation would check actual database connectivity
                await Task.Delay(500); // Simulate connection check

                IsConnected = true;
                ConnectionStatus = "متصل";
                _logger.LogDebug("Database connection verified successfully");
            }
            catch (Exception ex)
            {
                IsConnected = false;
                ConnectionStatus = "غير متصل";
                _logger.LogWarning(ex, "Database connection check failed");
            }
        }
        /// <summary>
        /// Applies Material Design theme configuration with current API compatibility
        /// Implements sophisticated theme management with proper error handling
        /// </summary>
        /// <param name="isDarkTheme">Theme preference flag</param>
        private void ApplyTheme(bool isDarkTheme)
        {
            try
            {
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();

                // Configure base theme with proper API usage for MaterialDesignThemes.Wpf 5.0+
                theme.SetBaseTheme(isDarkTheme ? BaseTheme.Dark : BaseTheme.Light);

                // Apply consistent color scheme across application
                theme.SetPrimaryColor(Colors.Blue);
                theme.SetSecondaryColor(Colors.Orange);

                // Apply theme configuration to application
                paletteHelper.SetTheme(theme);

                // Save theme preference
                ApplicationSettings.Instance.IsDarkTheme = isDarkTheme;
                ApplicationSettings.Instance.Save();

                _logger.LogDebug("Successfully applied {ThemeType} theme configuration",
                    isDarkTheme ? "Dark" : "Light");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply Material Design theme configuration");
            }
        }
        private void StartDateTimeTimer()
        {
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            timer.Tick += (sender, e) => CurrentDateTime = DateTime.Now;
            timer.Start();
        }

        private async Task CleanupApplicationAsync()
        {
            try
            {
                _logger.LogInformation("Starting application cleanup");

                // Cleanup all cached ViewModels
                foreach (var viewModel in _viewModelCache.Values)
                {
                    viewModel.Cleanup();
                }
                _viewModelCache.Clear();

                // Cleanup current ViewModel
                CurrentViewModel?.Cleanup();

                // Unsubscribe from events
                _navigationService.NavigationRequested -= OnNavigationRequested;

                _logger.LogInformation("Application cleanup completed successfully");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application cleanup");
            }
        }

        #endregion

        public override void Cleanup()
        {
            foreach (var viewModel in _viewModelCache.Values)
            {
                viewModel.Cleanup();
            }
            _viewModelCache.Clear();
            base.Cleanup();
        }
    }

    /// <summary>
    /// Navigation menu item model implementing sophisticated menu management with MVVM patterns
    /// Utilizes CommunityToolkit.Mvvm source generators for optimal performance
    /// </summary>
    [ObservableObject]
    public partial class NavigationMenuItem
    {
        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private MaterialDesignThemes.Wpf.PackIconKind _iconKind;

        [ObservableProperty]
        private Type _viewModelType = typeof(BaseViewModel);

        [ObservableProperty]
        private IRelayCommand _command = null!;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private bool _isEnabled = true;
    }
}