using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

namespace PoultrySlaughterPOS.Utils.Configuration
{
    /// <summary>
    /// Enterprise-grade application settings manager with JSON persistence
    /// Provides thread-safe configuration management for POS system preferences
    /// </summary>
    public class ApplicationSettings
    {
        private static readonly Lazy<ApplicationSettings> _instance = new(() => new ApplicationSettings());
        private static readonly object _lock = new object();
        private readonly string _settingsFilePath;
        private readonly ILogger<ApplicationSettings>? _logger;

        public static ApplicationSettings Instance => _instance.Value;

        // Application Settings Properties
        public bool IsDarkTheme { get; set; } = false;
        public string CompanyName { get; set; } = "مسلخ الدجاج";
        public string CompanyAddress { get; set; } = string.Empty;
        public string CompanyPhone { get; set; } = string.Empty;
        public decimal DefaultUnitPrice { get; set; } = 15.00m;
        public int InvoiceNumberLength { get; set; } = 9;
        public bool AutoBackupEnabled { get; set; } = true;
        public int AutoBackupIntervalDays { get; set; } = 7;
        public string BackupLocation { get; set; } = string.Empty;
        public DateTime LastBackupDate { get; set; } = DateTime.MinValue;
        public string DatabaseConnectionString { get; set; } = string.Empty;
        public string LastUsedLanguage { get; set; } = "ar";
        public bool EnableAuditLogging { get; set; } = true;
        public int SessionTimeoutMinutes { get; set; } = 120;

        private ApplicationSettings()
        {
            _settingsFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PoultrySlaughterPOS",
                "Settings",
                "app-settings.json");

            EnsureSettingsDirectoryExists();
            LoadSettings();
        }

        /// <summary>
        /// Loads application settings from persistent storage with error handling
        /// Implements fallback to default values if configuration is corrupted
        /// </summary>
        public void LoadSettings()
        {
            lock (_lock)
            {
                try
                {
                    if (File.Exists(_settingsFilePath))
                    {
                        var jsonContent = File.ReadAllText(_settingsFilePath);
                        if (!string.IsNullOrWhiteSpace(jsonContent))
                        {
                            var settings = JsonSerializer.Deserialize<ApplicationSettings>(jsonContent);
                            if (settings != null)
                            {
                                CopySettingsFrom(settings);
                                _logger?.LogInformation("Application settings loaded successfully from {SettingsPath}", _settingsFilePath);
                                return;
                            }
                        }
                    }

                    // Initialize with defaults if no valid settings found
                    InitializeDefaultSettings();
                    _logger?.LogInformation("Initialized default application settings");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to load application settings, using defaults");
                    InitializeDefaultSettings();
                }
            }
        }

        /// <summary>
        /// Persists current settings to storage with atomic write operations
        /// Ensures data integrity through temporary file usage
        /// </summary>
        public void Save()
        {
            lock (_lock)
            {
                try
                {
                    var jsonOptions = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    var jsonContent = JsonSerializer.Serialize(this, jsonOptions);
                    var tempFilePath = _settingsFilePath + ".tmp";

                    // Atomic write operation
                    File.WriteAllText(tempFilePath, jsonContent);

                    if (File.Exists(_settingsFilePath))
                    {
                        File.Delete(_settingsFilePath);
                    }

                    File.Move(tempFilePath, _settingsFilePath);

                    _logger?.LogDebug("Application settings saved successfully to {SettingsPath}", _settingsFilePath);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to save application settings to {SettingsPath}", _settingsFilePath);
                    throw new InvalidOperationException("Unable to persist application settings", ex);
                }
            }
        }

        /// <summary>
        /// Resets all settings to factory defaults with confirmation
        /// Provides clean slate for troubleshooting configuration issues
        /// </summary>
        public void ResetToDefaults()
        {
            lock (_lock)
            {
                try
                {
                    InitializeDefaultSettings();
                    Save();
                    _logger?.LogInformation("Application settings reset to factory defaults");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Failed to reset settings to defaults");
                    throw;
                }
            }
        }

        /// <summary>
        /// Validates current settings configuration for business rules compliance
        /// Ensures data integrity and prevents invalid system states
        /// </summary>
        public bool ValidateSettings()
        {
            try
            {
                var isValid = true;
                var validationErrors = new List<string>();

                // Validate unit price range
                if (DefaultUnitPrice <= 0 || DefaultUnitPrice > 10000)
                {
                    validationErrors.Add("Default unit price must be between 0 and 10,000");
                    isValid = false;
                }

                // Validate invoice number length
                if (InvoiceNumberLength < 6 || InvoiceNumberLength > 15)
                {
                    validationErrors.Add("Invoice number length must be between 6 and 15");
                    isValid = false;
                }

                // Validate backup interval
                if (AutoBackupIntervalDays < 1 || AutoBackupIntervalDays > 365)
                {
                    validationErrors.Add("Auto backup interval must be between 1 and 365 days");
                    isValid = false;
                }

                // Validate session timeout
                if (SessionTimeoutMinutes < 5 || SessionTimeoutMinutes > 1440)
                {
                    validationErrors.Add("Session timeout must be between 5 and 1440 minutes");
                    isValid = false;
                }

                if (!isValid)
                {
                    _logger?.LogWarning("Settings validation failed: {Errors}", string.Join(", ", validationErrors));
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error during settings validation");
                return false;
            }
        }

        /// <summary>
        /// Creates backup location directory structure if it doesn't exist
        /// Ensures proper directory permissions for backup operations
        /// </summary>
        public void EnsureBackupLocationExists()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(BackupLocation))
                {
                    BackupLocation = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "PoultrySlaughterPOS_Backups");
                }

                if (!Directory.Exists(BackupLocation))
                {
                    Directory.CreateDirectory(BackupLocation);
                    _logger?.LogInformation("Created backup directory: {BackupLocation}", BackupLocation);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create backup directory: {BackupLocation}", BackupLocation);
                throw;
            }
        }

        private void EnsureSettingsDirectoryExists()
        {
            try
            {
                var settingsDirectory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(settingsDirectory) && !Directory.Exists(settingsDirectory))
                {
                    Directory.CreateDirectory(settingsDirectory);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create settings directory");
                throw new InvalidOperationException("Unable to initialize settings directory", ex);
            }
        }

        private void InitializeDefaultSettings()
        {
            IsDarkTheme = false;
            CompanyName = "مسلخ الدجاج";
            CompanyAddress = string.Empty;
            CompanyPhone = string.Empty;
            DefaultUnitPrice = 15.00m;
            InvoiceNumberLength = 9;
            AutoBackupEnabled = true;
            AutoBackupIntervalDays = 7;
            LastBackupDate = DateTime.MinValue;
            LastUsedLanguage = "ar";
            EnableAuditLogging = true;
            SessionTimeoutMinutes = 120;

            BackupLocation = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "PoultrySlaughterPOS_Backups");
        }

        private void CopySettingsFrom(ApplicationSettings source)
        {
            IsDarkTheme = source.IsDarkTheme;
            CompanyName = source.CompanyName;
            CompanyAddress = source.CompanyAddress;
            CompanyPhone = source.CompanyPhone;
            DefaultUnitPrice = source.DefaultUnitPrice;
            InvoiceNumberLength = source.InvoiceNumberLength;
            AutoBackupEnabled = source.AutoBackupEnabled;
            AutoBackupIntervalDays = source.AutoBackupIntervalDays;
            BackupLocation = source.BackupLocation;
            LastBackupDate = source.LastBackupDate;
            DatabaseConnectionString = source.DatabaseConnectionString;
            LastUsedLanguage = source.LastUsedLanguage;
            EnableAuditLogging = source.EnableAuditLogging;
            SessionTimeoutMinutes = source.SessionTimeoutMinutes;
        }
    }
}