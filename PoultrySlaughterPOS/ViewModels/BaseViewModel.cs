using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Enhanced base ViewModel implementation with comprehensive validation and error handling
    /// Implements INotifyPropertyChanged, INotifyDataErrorInfo for complete MVVM support
    /// </summary>
    public abstract partial class BaseViewModel : ObservableValidator, INotifyDataErrorInfo
    {
        protected readonly ILogger _logger;
        private readonly Dictionary<string, object> _propertyCache = new();
        private readonly Dictionary<string, List<string>> _propertyErrors = new();

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        // Remove ObservableProperty attribute - HasErrors is provided by INotifyDataErrorInfo
        private bool _hasErrors;
        public bool HasErrors
        {
            get => _hasErrors;
            private set => SetProperty(ref _hasErrors, value);
        }

        [ObservableProperty]
        private List<string> _errorMessages = new();

        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        protected BaseViewModel(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region INotifyDataErrorInfo Implementation

        bool INotifyDataErrorInfo.HasErrors => _propertyErrors.Any(x => x.Value.Any());

        public IEnumerable GetErrors(string? propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
                return _propertyErrors.SelectMany(x => x.Value);

            return _propertyErrors.TryGetValue(propertyName, out var errors) ? errors : Enumerable.Empty<string>();
        }

        protected void SetErrors(string propertyName, IEnumerable<string?> errors)
        {
            var errorList = errors.Where(e => !string.IsNullOrEmpty(e)).Cast<string>().ToList();

            if (errorList.Any())
            {
                _propertyErrors[propertyName] = errorList;
            }
            else
            {
                _propertyErrors.Remove(propertyName);
            }

            OnErrorsChanged(propertyName);
            HasErrors = _propertyErrors.Any(x => x.Value.Any());
        }

        protected void ClearErrors(string? propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                var propertyNames = _propertyErrors.Keys.ToList();
                _propertyErrors.Clear();

                foreach (var name in propertyNames)
                {
                    OnErrorsChanged(name);
                }
            }
            else
            {
                if (_propertyErrors.Remove(propertyName))
                {
                    OnErrorsChanged(propertyName);
                }
            }

            HasErrors = _propertyErrors.Any(x => x.Value.Any());
        }

        private void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }

        #endregion

        // Rest of implementation remains the same...
        protected async Task<bool> ExecuteAsyncOperation(Func<Task> operation, bool showLoading = true, string operationName = "Operation")
        {
            try
            {
                if (showLoading)
                {
                    IsLoading = true;
                    IsBusy = true;
                }

                ClearErrors();
                _logger.LogDebug("Starting {OperationName}", operationName);

                await operation();

                _logger.LogDebug("Completed {OperationName} successfully", operationName);
                StatusMessage = $"{operationName} completed successfully";
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {OperationName}", operationName);
                AddError($"{operationName} failed: {ex.Message}");
                StatusMessage = $"{operationName} failed";
                return false;
            }
            finally
            {
                IsLoading = false;
                IsBusy = false;
            }
        }

        protected async Task<T?> ExecuteAsyncOperation<T>(Func<Task<T>> operation, bool showLoading = true, string operationName = "Operation")
        {
            try
            {
                if (showLoading)
                {
                    IsLoading = true;
                    IsBusy = true;
                }

                ClearErrors();
                _logger.LogDebug("Starting {OperationName}", operationName);

                var result = await operation();

                _logger.LogDebug("Completed {OperationName} successfully", operationName);
                StatusMessage = $"{operationName} completed successfully";
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during {OperationName}", operationName);
                AddError($"{operationName} failed: {ex.Message}");
                StatusMessage = $"{operationName} failed";
                return default(T);
            }
            finally
            {
                IsLoading = false;
                IsBusy = false;
            }
        }

        protected void AddError(string errorMessage)
        {
            if (!ErrorMessages.Contains(errorMessage))
            {
                ErrorMessages.Add(errorMessage);
                OnPropertyChanged(nameof(ErrorMessages));
            }
        }

        public virtual void ValidateAllProperties()
        {
            try
            {
                ClearErrors();

                var validationResults = new List<ValidationResult>();
                var validationContext = new ValidationContext(this);

                if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
                {
                    var errorGroups = validationResults
                        .Where(r => r.MemberNames.Any())
                        .GroupBy(r => r.MemberNames.First());

                    foreach (var group in errorGroups)
                    {
                        var errors = group.Select(r => r.ErrorMessage).Where(e => !string.IsNullOrEmpty(e));
                        SetErrors(group.Key, errors!);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during property validation");
                AddError("Validation error occurred");
            }
        }

        public virtual void Cleanup()
        {
            _propertyCache.Clear();
            _propertyErrors.Clear();
            ErrorMessages.Clear();
            _logger.LogDebug("ViewModel cleanup completed for {ViewModelType}", GetType().Name);
        }
    }
}