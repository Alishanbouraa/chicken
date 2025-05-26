using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Services.Interfaces;
using PoultrySlaughterPOS.ViewModels;

namespace PoultrySlaughterPOS.Services.Implementations
{
    /// <summary>
    /// Advanced navigation service implementation with comprehensive view management
    /// Provides sophisticated navigation patterns with history management and dialog support
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly ILogger<NavigationService> _logger;
        private readonly Stack<Type> _navigationHistory;
        private readonly Stack<Type> _forwardHistory;
        private const int MaxHistorySize = 50;

        public event EventHandler<NavigationEventArgs>? NavigationRequested;

        public NavigationService(ILogger<NavigationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _navigationHistory = new Stack<Type>();
            _forwardHistory = new Stack<Type>();
        }

        public bool CanGoBack => _navigationHistory.Count > 1;
        public bool CanGoForward => _forwardHistory.Count > 0;

        public async Task NavigateToAsync<TViewModel>() where TViewModel : BaseViewModel
        {
            await NavigateToAsync<TViewModel>(null);
        }

        public async Task NavigateToAsync<TViewModel>(object? parameter) where TViewModel : BaseViewModel
        {
            await NavigateToAsync(typeof(TViewModel), parameter);
        }

        public async Task NavigateToAsync(Type viewModelType, object? parameter = null)
        {
            try
            {
                _logger.LogInformation("Navigating to {ViewModelType}", viewModelType.Name);

                if (!typeof(BaseViewModel).IsAssignableFrom(viewModelType))
                {
                    throw new ArgumentException($"Type {viewModelType.Name} must inherit from BaseViewModel", nameof(viewModelType));
                }

                // Manage navigation history
                if (_navigationHistory.Count >= MaxHistorySize)
                {
                    var tempStack = new Stack<Type>();
                    for (int i = 0; i < MaxHistorySize - 1; i++)
                    {
                        if (_navigationHistory.Count > 0)
                            tempStack.Push(_navigationHistory.Pop());
                    }
                    _navigationHistory.Clear();
                    while (tempStack.Count > 0)
                        _navigationHistory.Push(tempStack.Pop());
                }

                _navigationHistory.Push(viewModelType);
                _forwardHistory.Clear(); // Clear forward history when navigating to new view

                var navigationArgs = new NavigationEventArgs
                {
                    ViewModelType = viewModelType,
                    Parameter = parameter,
                    IsDialog = false,
                    NavigationType = NavigationType.Navigate
                };

                NavigationRequested?.Invoke(this, navigationArgs);

                _logger.LogDebug("Navigation completed to {ViewModelType}", viewModelType.Name);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during navigation to {ViewModelType}", viewModelType.Name);
                throw;
            }
        }

        public async Task GoBackAsync()
        {
            try
            {
                if (!CanGoBack)
                {
                    _logger.LogWarning("Cannot go back - no history available");
                    return;
                }

                var currentView = _navigationHistory.Pop();
                _forwardHistory.Push(currentView);

                var previousView = _navigationHistory.Peek();

                _logger.LogInformation("Navigating back to {ViewModelType}", previousView.Name);

                var navigationArgs = new NavigationEventArgs
                {
                    ViewModelType = previousView,
                    Parameter = null,
                    IsDialog = false,
                    NavigationType = NavigationType.Back
                };

                NavigationRequested?.Invoke(this, navigationArgs);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during back navigation");
                throw;
            }
        }

        public async Task GoForwardAsync()
        {
            try
            {
                if (!CanGoForward)
                {
                    _logger.LogWarning("Cannot go forward - no forward history available");
                    return;
                }

                var forwardView = _forwardHistory.Pop();
                _navigationHistory.Push(forwardView);

                _logger.LogInformation("Navigating forward to {ViewModelType}", forwardView.Name);

                var navigationArgs = new NavigationEventArgs
                {
                    ViewModelType = forwardView,
                    Parameter = null,
                    IsDialog = false,
                    NavigationType = NavigationType.Forward
                };

                NavigationRequested?.Invoke(this, navigationArgs);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forward navigation");
                throw;
            }
        }

        public void ClearNavigationHistory()
        {
            _navigationHistory.Clear();
            _forwardHistory.Clear();
            _logger.LogDebug("Navigation history cleared");
        }

        public async Task ShowDialogAsync<TViewModel>() where TViewModel : BaseViewModel
        {
            try
            {
                _logger.LogInformation("Showing dialog {ViewModelType}", typeof(TViewModel).Name);

                var navigationArgs = new NavigationEventArgs
                {
                    ViewModelType = typeof(TViewModel),
                    Parameter = null,
                    IsDialog = true,
                    NavigationType = NavigationType.Dialog
                };

                NavigationRequested?.Invoke(this, navigationArgs);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing dialog {ViewModelType}", typeof(TViewModel).Name);
                throw;
            }
        }

        public async Task<bool> ShowConfirmationDialogAsync(string title, string message)
        {
            try
            {
                _logger.LogDebug("Showing confirmation dialog: {Title}", title);

                var result = System.Windows.MessageBox.Show(message, title,
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);

                var confirmed = result == System.Windows.MessageBoxResult.Yes;
                _logger.LogDebug("Confirmation dialog result: {Result}", confirmed);

                return await Task.FromResult(confirmed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing confirmation dialog");
                return await Task.FromResult(false);
            }
        }

        public async Task ShowErrorDialogAsync(string title, string message)
        {
            try
            {
                _logger.LogDebug("Showing error dialog: {Title}", title);

                System.Windows.MessageBox.Show(message, title,
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing error dialog");
            }
        }

        public async Task ShowSuccessDialogAsync(string title, string message)
        {
            try
            {
                _logger.LogDebug("Showing success dialog: {Title}", title);

                System.Windows.MessageBox.Show(message, title,
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing success dialog");
            }
        }
    }
}