using PoultrySlaughterPOS.ViewModels;

namespace PoultrySlaughterPOS.Services.Interfaces
{
    /// <summary>
    /// Advanced navigation service interface for comprehensive view management
    /// Provides type-safe navigation with parameter passing and view lifecycle management
    /// </summary>
    public interface INavigationService
    {
        event EventHandler<NavigationEventArgs>? NavigationRequested;

        Task NavigateToAsync<TViewModel>() where TViewModel : BaseViewModel;
        Task NavigateToAsync<TViewModel>(object? parameter) where TViewModel : BaseViewModel;
        Task NavigateToAsync(Type viewModelType, object? parameter = null);

        bool CanGoBack { get; }
        bool CanGoForward { get; }

        Task GoBackAsync();
        Task GoForwardAsync();

        void ClearNavigationHistory();
        Task ShowDialogAsync<TViewModel>() where TViewModel : BaseViewModel;
        Task<bool> ShowConfirmationDialogAsync(string title, string message);
        Task ShowErrorDialogAsync(string title, string message);
        Task ShowSuccessDialogAsync(string title, string message);
    }

    /// <summary>
    /// Navigation event arguments for advanced navigation scenarios
    /// Provides comprehensive context for navigation operations
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        public Type ViewModelType { get; set; } = null!;
        public object? Parameter { get; set; }
        public bool IsDialog { get; set; }
        public string? Title { get; set; }
        public NavigationType NavigationType { get; set; }
    }

    /// <summary>
    /// Navigation operation types for sophisticated routing
    /// </summary>
    public enum NavigationType
    {
        Navigate,
        Dialog,
        Back,
        Forward,
        Replace
    }
}