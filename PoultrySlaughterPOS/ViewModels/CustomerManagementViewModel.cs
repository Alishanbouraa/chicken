using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models.DTOs;
using PoultrySlaughterPOS.Models.Entities;
using PoultrySlaughterPOS.Services.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Advanced ViewModel for comprehensive customer management and debt tracking operations
    /// Implements sophisticated customer relationship management with payment processing capabilities
    /// </summary>
    public partial class CustomerManagementViewModel : BaseViewModel
    {
        private readonly ICustomerService _customerService;
        private readonly INavigationService _navigationService;

        #region Observable Properties

        [ObservableProperty]
        private ObservableCollection<Customer> _customers = new();

        [ObservableProperty]
        private ObservableCollection<CustomerAccountSummaryDto> _customerSummaries = new();

        [ObservableProperty]
        private Customer? _selectedCustomer;

        [ObservableProperty]
        private CustomerAccountSummaryDto? _selectedCustomerSummary;

        [ObservableProperty]
        [Required(ErrorMessage = "اسم الزبون مطلوب")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "اسم الزبون يجب أن يكون بين 2 و 100 حرف")]
        private string _customerName = string.Empty;

        [ObservableProperty]
        [StringLength(15, ErrorMessage = "رقم الهاتف لا يمكن أن يتجاوز 15 رقم")]
        private string _phoneNumber = string.Empty;

        [ObservableProperty]
        [StringLength(200, ErrorMessage = "العنوان لا يمكن أن يتجاوز 200 حرف")]
        private string _address = string.Empty;

        [ObservableProperty]
        [Range(0, 999999.99, ErrorMessage = "حد الائتمان يجب أن يكون بين 0 و 999,999.99")]
        private decimal _creditLimit;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private decimal _paymentAmount;

        [ObservableProperty]
        private string _paymentMethod = "نقدي";

        [ObservableProperty]
        private string _paymentNotes = string.Empty;

        [ObservableProperty]
        private DebtorsReportDto? _debtorsReport;

        #endregion

        public CustomerManagementViewModel(
            ICustomerService customerService,
            INavigationService navigationService,
            ILogger<CustomerManagementViewModel> logger) : base(logger)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            Title = "إدارة الزبائن والحسابات";
            InitializeAsync();
        }

        #region Relay Commands

        [RelayCommand(CanExecute = nameof(CanCreateCustomer))]
        private async Task CreateCustomerAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                ValidateAllProperties();
                if (HasErrors) return;

                var dto = new CreateCustomerDto
                {
                    CustomerName = CustomerName.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                    Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                    CreditLimit = CreditLimit
                };

                var result = await _customerService.CreateCustomerAsync(dto);
                if (result.IsSuccess && result.Data != null)
                {
                    await RefreshCustomersAsync();
                    ClearForm();
                    await _navigationService.ShowSuccessDialogAsync("نجح الحفظ", "تم إنشاء الزبون بنجاح");
                }
                else
                {
                    AddError(result.Message);
                    await _navigationService.ShowErrorDialogAsync("خطأ في الحفظ", result.Message);
                }
            }, true, "Create Customer");
        }

        [RelayCommand(CanExecute = nameof(CanUpdateCustomer))]
        private async Task UpdateCustomerAsync()
        {
            if (SelectedCustomer == null) return;

            await ExecuteAsyncOperation(async () =>
            {
                ValidateAllProperties();
                if (HasErrors) return;

                var dto = new UpdateCustomerDto
                {
                    CustomerId = SelectedCustomer.CustomerId,
                    CustomerName = CustomerName.Trim(),
                    PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                    Address = string.IsNullOrWhiteSpace(Address) ? null : Address.Trim(),
                    CreditLimit = CreditLimit,
                    IsActive = SelectedCustomer.IsActive
                };

                var result = await _customerService.UpdateCustomerAsync(SelectedCustomer.CustomerId, dto);
                if (result.IsSuccess)
                {
                    await RefreshCustomersAsync();
                    ExitEditMode();
                    await _navigationService.ShowSuccessDialogAsync("نجح التحديث", "تم تحديث بيانات الزبون بنجاح");
                }
                else
                {
                    AddError(result.Message);
                    await _navigationService.ShowErrorDialogAsync("خطأ في التحديث", result.Message);
                }
            }, true, "Update Customer");
        }

        [RelayCommand]
        private async Task ProcessPaymentAsync()
        {
            if (SelectedCustomer == null || PaymentAmount <= 0) return;

            var confirmed = await _navigationService.ShowConfirmationDialogAsync(
                "تأكيد الدفع",
                $"هل أنت متأكد من تسجيل دفعة بمبلغ {PaymentAmount:F2} ريال للزبون {SelectedCustomer.CustomerName}؟");

            if (!confirmed) return;

            await ExecuteAsyncOperation(async () =>
            {
                var dto = new ProcessPaymentDto
                {
                    Amount = PaymentAmount,
                    PaymentMethod = PaymentMethod,
                    PaymentDate = DateTime.Now,
                    Notes = string.IsNullOrWhiteSpace(PaymentNotes) ? null : PaymentNotes.Trim()
                };

                var result = await _customerService.ProcessPaymentAsync(SelectedCustomer.CustomerId, dto);
                if (result.IsSuccess)
                {
                    await RefreshCustomersAsync();
                    await RefreshCustomerSummaryAsync();
                    ClearPaymentForm();
                    await _navigationService.ShowSuccessDialogAsync("نجح التسجيل", "تم تسجيل الدفعة بنجاح");
                }
                else
                {
                    AddError(result.Message);
                    await _navigationService.ShowErrorDialogAsync("خطأ في التسجيل", result.Message);
                }
            }, true, "Process Payment");
        }

        [RelayCommand]
        private async Task SearchCustomersAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                await RefreshCustomersAsync();
                return;
            }

            await ExecuteAsyncOperation(async () =>
            {
                var result = await _customerService.SearchCustomersAsync(SearchText.Trim());
                if (result.IsSuccess && result.Data != null)
                {
                    Customers.Clear();
                    foreach (var customer in result.Data)
                    {
                        Customers.Add(customer);
                    }
                }
            }, true, "Search Customers");
        }

        [RelayCommand]
        private async Task GenerateDebtorsReportAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                var result = await _customerService.GenerateDebtorsReportAsync();
                if (result.IsSuccess && result.Data != null)
                {
                    DebtorsReport = result.Data;
                }
                else
                {
                    AddError(result.Message);
                }
            }, true, "Generate Debtors Report");
        }

        [RelayCommand]
        private void EditCustomer()
        {
            if (SelectedCustomer == null) return;

            CustomerName = SelectedCustomer.CustomerName;
            PhoneNumber = SelectedCustomer.PhoneNumber ?? string.Empty;
            Address = SelectedCustomer.Address ?? string.Empty;
            CreditLimit = SelectedCustomer.CreditLimit;
            IsEditMode = true;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            ExitEditMode();
            ClearForm();
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                await RefreshCustomersAsync();
                await RefreshCustomerSummaryAsync();
                await GenerateDebtorsReportAsync();
            }, true, "Refresh Data");
        }

        #endregion

        #region Command Can Execute Methods

        private bool CanCreateCustomer() => !IsBusy && !IsEditMode && !string.IsNullOrWhiteSpace(CustomerName);
        private bool CanUpdateCustomer() => !IsBusy && IsEditMode && SelectedCustomer != null && !string.IsNullOrWhiteSpace(CustomerName);

        #endregion

        #region Property Change Handlers

        partial void OnSelectedCustomerChanged(Customer? value)
        {
            if (value != null)
            {
                _ = RefreshCustomerSummaryAsync();
                PaymentAmount = 0;
                ClearPaymentForm();
            }
        }

        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _ = RefreshCustomersAsync();
            }
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                await RefreshCustomersAsync();
                await GenerateDebtorsReportAsync();
            }, true, "Initialize");
        }

        private async Task RefreshCustomersAsync()
        {
            var result = await _customerService.GetAllActiveCustomersAsync();
            if (result.IsSuccess && result.Data != null)
            {
                Customers.Clear();
                foreach (var customer in result.Data)
                {
                    Customers.Add(customer);
                }
            }
        }

        private async Task RefreshCustomerSummaryAsync()
        {
            if (SelectedCustomer == null) return;

            var result = await _customerService.GetCustomerAccountSummaryAsync(SelectedCustomer.CustomerId);
            if (result.IsSuccess && result.Data != null)
            {
                SelectedCustomerSummary = result.Data;
            }
        }

        private void ClearForm()
        {
            CustomerName = string.Empty;
            PhoneNumber = string.Empty;
            Address = string.Empty;
            CreditLimit = 0;
            ClearErrors();
        }

        private void ClearPaymentForm()
        {
            PaymentAmount = 0;
            PaymentMethod = "نقدي";
            PaymentNotes = string.Empty;
        }

        private void ExitEditMode()
        {
            IsEditMode = false;
            SelectedCustomer = null;
        }

        #endregion

        public override void Cleanup()
        {
            Customers.Clear();
            CustomerSummaries.Clear();
            base.Cleanup();
        }
    }
}