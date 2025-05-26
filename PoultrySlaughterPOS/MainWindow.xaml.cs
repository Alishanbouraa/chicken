using Microsoft.Extensions.DependencyInjection;
using PoultrySlaughterPOS.ViewModels;
using System.Windows;
using MaterialDesignThemes.Wpf;
using System.Windows.Media;
using PoultrySlaughterPOS.Utils.Configuration;
namespace PoultrySlaughterPOS.Views
{
    /// <summary>
    /// Main application window implementing enterprise-grade Material Design architecture
    /// Provides primary user interface container with sophisticated theme management
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            ConfigureWindowProperties();
            InitializeThemeConfiguration();
        }

        public MainWindow(MainWindowViewModel viewModel) : this()
        {
            DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        /// <summary>
        /// Configures window properties with professional defaults and icon management
        /// Implements proper window positioning and behavior patterns
        /// </summary>
        private void ConfigureWindowProperties()
        {
            try
            {
                // Configure application icon with fallback handling
                var iconUri = new Uri("pack://application:,,,/Resources/Images/app-icon.ico", UriKind.Absolute);
                Icon = new System.Windows.Media.Imaging.BitmapImage(iconUri);
            }
            catch (Exception)
            {
                // Icon loading failed - application continues without custom icon
                // Production systems should log this appropriately
            }

            // Configure window behavior for optimal user experience
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;

            // Set minimum viable window dimensions for POS operations
            MinWidth = 1200;
            MinHeight = 700;
        }

        /// <summary>
        /// Initializes Material Design theme configuration with user preferences
        /// Applies saved theme settings and establishes color palette
        /// </summary>
        private void InitializeThemeConfiguration()
        {
            try
            {
                // Load user theme preference from application settings
                var isDarkTheme = ApplicationSettings.Instance.IsDarkTheme;
                ApplyMaterialDesignTheme(isDarkTheme);
            }
            catch (Exception)
            {
                // Fallback to light theme if settings unavailable
                ApplyMaterialDesignTheme(false);
            }
        }

        /// <summary>
        /// Applies Material Design theme with proper API usage for current library version
        /// Implements robust theme switching with error handling
        /// </summary>
        /// <param name="isDarkTheme">Theme preference flag</param>
        private static void ApplyMaterialDesignTheme(bool isDarkTheme)
        {
            try
            {
                var paletteHelper = new PaletteHelper();
                var theme = paletteHelper.GetTheme();

                // Configure base theme according to user preference - CORRECTED API
                theme.SetBaseTheme(isDarkTheme ? BaseTheme.Dark : BaseTheme.Light);

                // Apply enterprise-appropriate color scheme
                theme.SetPrimaryColor(Colors.Blue);
                theme.SetSecondaryColor(Colors.Orange);

                // Apply theme to application resources
                paletteHelper.SetTheme(theme);
            }
            catch (Exception ex)
            {
                // Theme application failed - log error and continue with default theme
                System.Diagnostics.Debug.WriteLine($"Material Design theme configuration failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles application shutdown with proper resource cleanup and settings persistence
        /// Ensures graceful termination of all application resources
        /// </summary>
        /// <param name="e">Event arguments containing shutdown context</param>
        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Persist application settings for next session
                ApplicationSettings.Instance.Save();

                // Execute ViewModel cleanup if available
                if (DataContext is MainWindowViewModel mainViewModel)
                {
                    mainViewModel.Cleanup();
                }
            }
            catch (Exception ex)
            {
                // Settings persistence failed - log but don't prevent shutdown
                System.Diagnostics.Debug.WriteLine($"Settings save failed during shutdown: {ex.Message}");
            }
            finally
            {
                base.OnClosed(e);
            }
        }
    }
}