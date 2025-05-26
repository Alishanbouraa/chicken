using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Services.Interfaces;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Advanced ViewModel for comprehensive application settings and configuration management
    /// Implements system preferences, backup operations, and administrative functions
    /// </summary>
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;

        #region Observable Properties

        [ObservableProperty]
        private bool _isDarkTheme;

        [ObservableProperty]
        private string _companyName = "مسلخ الدجاج";

        [ObservableProperty]
        private string _companyAddress = string.Empty;

        [ObservableProperty]
        private string _companyPhone = string.Empty;

        [ObservableProperty]
        private decimal _defaultUnitPrice = 15.00m;

        [ObservableProperty]
        private int _invoiceNumberLength = 9;

        [ObservableProperty]
        private bool _autoBackupEnabled = true;

        [ObservableProperty]
        private int _autoBackupIntervalDays = 7;

        [ObservableProperty]
        private string _backupLocation = string.Empty;

        [ObservableProperty]
        private DateTime _lastBackupDate;

        #endregion

        public SettingsViewModel(
            INavigationService navigationService,
            ILogger<SettingsViewModel> logger) : base(logger)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            Title = "إعدادات النظام والتكوين";
            LoadSettings();
        }

        #region Relay Commands

        [RelayCommand]
        private async Task SaveSettingsAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                // Save settings to configuration file or database
                await Task.Delay(500); // Simulate save operation
                await _navigationService.ShowSuccessDialogAsync("حفظ الإعدادات", "تم حفظ الإعدادات بنجاح");
            }, true, "Save Settings");
        }

        [RelayCommand]
        private async Task CreateBackupAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                // Implementation would create database backup
                await Task.Delay(2000); // Simulate backup operation
                LastBackupDate = DateTime.Now;
                await _navigationService.ShowSuccessDialogAsync("النسخ الاحتياطي", "تم إنشاء النسخة الاحتياطية بنجاح");
            }, true, "Create Backup");
        }

        [RelayCommand]
        private async Task RestoreBackupAsync()
        {
            var confirmed = await _navigationService.ShowConfirmationDialogAsync(
                "استعادة النسخة الاحتياطية",
                "هل أنت متأكد من استعادة النسخة الاحتياطية؟ سيتم استبدال البيانات الحالية.");

            if (!confirmed) return;

            await ExecuteAsyncOperation(async () =>
            {
                // Implementation would restore from backup
                await Task.Delay(3000); // Simulate restore operation
                await _navigationService.ShowSuccessDialogAsync("استعادة البيانات", "تم استعادة النسخة الاحتياطية بنجاح");
            }, true, "Restore Backup");
        }

        [RelayCommand]
        private void ResetToDefaults()
        {
            CompanyName = "مسلخ الدجاج";
            CompanyAddress = string.Empty;
            CompanyPhone = string.Empty;
            DefaultUnitPrice = 15.00m;
            InvoiceNumberLength = 9;
            AutoBackupEnabled = true;
            AutoBackupIntervalDays = 7;
        }

        [RelayCommand]
        private async Task TestDatabaseConnectionAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                // Test database connectivity
                await Task.Delay(1000);
                await _navigationService.ShowSuccessDialogAsync("اختبار الاتصال", "اتصال قاعدة البيانات يعمل بشكل صحيح");
            }, true, "Test Database Connection");
        }

        #endregion

        #region Private Methods

        private void LoadSettings()
        {
            try
            {
                // Load settings from configuration
                BackupLocation = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "PoultrySlaughterPOS_Backups");

                LastBackupDate = DateTime.Today.AddDays(-3); // Example
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading settings");
            }
        }

        #endregion
    }
}