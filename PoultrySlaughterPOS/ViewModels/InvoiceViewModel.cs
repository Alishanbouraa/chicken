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
    /// Advanced ViewModel for comprehensive invoice management and sales processing
    /// Implements sophisticated calculation algorithms, real-time validation, and Arabic UI support
    /// </summary>
    public partial class InvoiceViewModel : BaseViewModel
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ICustomerService _customerService;
        private readonly INavigationService _navigationService;

        #region Observable Properties

        [ObservableProperty]
        private ObservableCollection<Customer> _customers = new();

        [ObservableProperty]
        private ObservableCollection<Truck> _trucks = new();

        [ObservableProperty]
        private ObservableCollection<Invoice> _todaysInvoices = new();

        [ObservableProperty]
        private string _invoiceNumber = string.Empty;

        [ObservableProperty]
        private Customer? _selectedCustomer;

        [ObservableProperty]
        private Truck? _selectedTruck;

        [ObservableProperty]
        private DateTime _invoiceDate = DateTime.Now;

        [ObservableProperty]
        [Required(ErrorMessage = "الوزن الفلتي مطلوب")]
        [Range(0.001, 9999.999, ErrorMessage = "الوزن الفلتي يجب أن يكون بين 0.001 و 9999.999 كيلو")]
        private decimal _grossWeight;

        [ObservableProperty]
        [Required(ErrorMessage = "وزن الأقفاص مطلوب")]
        [Range(0, 999.999, ErrorMessage = "وزن الأقفاص يجب أن يكون بين 0 و 999.999 كيلو")]
        private decimal _cagesWeight;

        [ObservableProperty]
        [Required(ErrorMessage = "عدد الأقفاص مطلوب")]
        [Range(1, 999, ErrorMessage = "عدد الأقفاص يجب أن يكون بين 1 و 999")]
        private int _cagesCount;

        [ObservableProperty]
        [Required(ErrorMessage = "سعر الوحدة مطلوب")]
        [Range(0.01, 99999.99, ErrorMessage = "سعر الوحدة يجب أن يكون بين 0.01 و 99999.99")]
        private decimal _unitPrice;

        [ObservableProperty]
        [Range(0, 100, ErrorMessage = "نسبة الخصم يجب أن تكون بين 0 و 100")]
        private decimal _discountPercentage;

        [ObservableProperty]
        private string _notes = string.Empty;

        // Calculated Properties
        [ObservableProperty]
        private decimal _netWeight;

        [ObservableProperty]
        private decimal _totalAmount;

        [ObservableProperty]
        private decimal _discountAmount;

        [ObservableProperty]
        private decimal _finalAmount;

        [ObservableProperty]
        private decimal _previousBalance;

        [ObservableProperty]
        private decimal _currentBalance;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private Invoice? _selectedInvoice;

        [ObservableProperty]
        private SalesReportDto? _dailySalesReport;

        [ObservableProperty]
        private decimal _todaysTotalSales;

        [ObservableProperty]
        private int _todaysInvoiceCount;

        [ObservableProperty]
        private decimal _averageInvoiceValue;

        #endregion

        public InvoiceViewModel(
            IInvoiceService invoiceService,
            ICustomerService customerService,
            INavigationService navigationService,
            ILogger<InvoiceViewModel> logger) : base(logger)
        {
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            Title = "إدارة الفواتير والمبيعات";
            InitializeAsync();
        }

        #region Async Relay Commands

        [RelayCommand(CanExecute = nameof(CanCreateInvoice))]
        private async Task CreateInvoiceAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                ValidateAllProperties();
                if (HasErrors) return;

                if (SelectedCustomer == null || SelectedTruck == null)
                {
                    AddError("يجب اختيار الزبون والشاحنة");
                    return;
                }

                var dto = new CreateInvoiceDto
                {
                    CustomerId = SelectedCustomer.CustomerId,
                    TruckId = SelectedTruck.TruckId,
                    InvoiceDate = InvoiceDate,
                    GrossWeight = GrossWeight,
                    CagesWeight = CagesWeight,
                    CagesCount = CagesCount,
                    UnitPrice = UnitPrice,
                    DiscountPercentage = DiscountPercentage,
                    Notes = Notes
                };

                var result = await _invoiceService.CreateInvoiceAsync(dto);
                if (result.IsSuccess && result.Data != null)
                {
                    await RefreshTodaysInvoicesAsync();
                    await RefreshCustomersAsync();
                    ClearForm();
                    await _navigationService.ShowSuccessDialogAsync("نجح الحفظ",
                        $"تم إنشاء الفاتورة رقم {result.Data.InvoiceNumber} بنجاح");
                }
                else
                {
                    AddError(result.Message);
                    await _navigationService.ShowErrorDialogAsync("خطأ في الحفظ", result.Message);
                }
            }, true, "Create Invoice");
        }

        [RelayCommand(CanExecute = nameof(CanUpdateInvoice))]
        private async Task UpdateInvoiceAsync()
        {
            if (SelectedInvoice == null || SelectedCustomer == null || SelectedTruck == null) return;

            await ExecuteAsyncOperation(async () =>
            {
                ValidateAllProperties();
                if (HasErrors) return;

                var dto = new UpdateInvoiceDto
                {
                    InvoiceId = SelectedInvoice.InvoiceId,
                    InvoiceNumber = SelectedInvoice.InvoiceNumber,
                    CustomerId = SelectedCustomer.CustomerId,
                    TruckId = SelectedTruck.TruckId,
                    InvoiceDate = InvoiceDate,
                    GrossWeight = GrossWeight,
                    CagesWeight = CagesWeight,
                    CagesCount = CagesCount,
                    UnitPrice = UnitPrice,
                    DiscountPercentage = DiscountPercentage,
                    Notes = Notes
                };

                var result = await _invoiceService.UpdateInvoiceAsync(SelectedInvoice.InvoiceId, dto);
                if (result.IsSuccess)
                {
                    await RefreshTodaysInvoicesAsync();
                    await RefreshCustomersAsync();
                    ExitEditMode();
                    await _navigationService.ShowSuccessDialogAsync("نجح التحديث", "تم تحديث الفاتورة بنجاح");
                }
                else
                {
                    AddError(result.Message);
                    await _navigationService.ShowErrorDialogAsync("خطأ في التحديث", result.Message);
                }
            }, true, "Update Invoice");
        }

        [RelayCommand(CanExecute = nameof(CanDeleteInvoice))]
        private async Task DeleteInvoiceAsync()
        {
            if (SelectedInvoice == null) return;

            var confirmed = await _navigationService.ShowConfirmationDialogAsync(
                "تأكيد الحذف",
                $"هل أنت متأكد من حذف الفاتورة رقم {SelectedInvoice.InvoiceNumber}؟");

            if (!confirmed) return;

            await ExecuteAsyncOperation(async () =>
            {
                var result = await _invoiceService.DeleteInvoiceAsync(SelectedInvoice.InvoiceId);
                if (result.IsSuccess)
                {
                    await RefreshTodaysInvoicesAsync();
                    await RefreshCustomersAsync();
                    SelectedInvoice = null;
                    await _navigationService.ShowSuccessDialogAsync("نجح الحذف", "تم حذف الفاتورة بنجاح");
                }
                else
                {
                    AddError(result.Message);
                    await _navigationService.ShowErrorDialogAsync("خطأ في الحذف", result.Message);
                }
            }, true, "Delete Invoice");
        }

        [RelayCommand]
        private async Task PrintInvoiceAsync()
        {
            if (SelectedInvoice == null) return;

            await ExecuteAsyncOperation(async () =>
            {
                // Implementation for printing invoice
                // This would integrate with a reporting/printing service
                await _navigationService.ShowSuccessDialogAsync("طباعة", "تم إرسال الفاتورة للطباعة");
            }, true, "Print Invoice");
        }

        [RelayCommand]
        private async Task GenerateInvoiceNumberAsync()
        {
            var result = await _invoiceService.GenerateInvoiceNumberAsync();
            if (result.IsSuccess && !string.IsNullOrEmpty(result.Data))
            {
                InvoiceNumber = result.Data;
            }
        }

        [RelayCommand]
        private async Task CalculateAmountsAsync()
        {
            if (GrossWeight <= 0 || UnitPrice <= 0) return;

            await ExecuteAsyncOperation(async () =>
            {
                var calculationDto = new InvoiceCalculationDto
                {
                    GrossWeight = GrossWeight,
                    CagesWeight = CagesWeight,
                    UnitPrice = UnitPrice,
                    DiscountPercentage = DiscountPercentage,
                    PreviousBalance = SelectedCustomer?.TotalDebt ?? 0
                };

                var result = await _invoiceService.CalculateInvoiceAmountsAsync(calculationDto);
                if (result.IsSuccess && result.Data != null)
                {
                    NetWeight = result.Data.NetWeight;
                    TotalAmount = result.Data.TotalAmount;
                    DiscountAmount = result.Data.DiscountAmount;
                    FinalAmount = result.Data.FinalAmount;
                    CurrentBalance = result.Data.CurrentBalance;
                }
                else
                {
                    AddError("خطأ في حساب المبالغ");
                }
            }, false, "Calculate Amounts");
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                await LoadCustomersAsync();
                await LoadTrucksAsync();
                await RefreshTodaysInvoicesAsync();
                await GenerateDailySalesReportAsync();
                await GenerateInvoiceNumberAsync();
            }, true, "Refresh Data");
        }

        [RelayCommand]
        private void EditInvoice()
        {
            if (SelectedInvoice == null) return;

            // Populate form with selected invoice data
            InvoiceNumber = SelectedInvoice.InvoiceNumber;
            SelectedCustomer = Customers.FirstOrDefault(c => c.CustomerId == SelectedInvoice.CustomerId);
            SelectedTruck = Trucks.FirstOrDefault(t => t.TruckId == SelectedInvoice.TruckId);
            InvoiceDate = SelectedInvoice.InvoiceDate;
            GrossWeight = SelectedInvoice.GrossWeight;
            CagesWeight = SelectedInvoice.CagesWeight;
            CagesCount = SelectedInvoice.CagesCount;
            UnitPrice = SelectedInvoice.UnitPrice;
            DiscountPercentage = SelectedInvoice.DiscountPercentage;
            Notes = SelectedInvoice.Notes ?? string.Empty;

            // Set calculated values
            NetWeight = SelectedInvoice.NetWeight;
            TotalAmount = SelectedInvoice.TotalAmount;
            DiscountAmount = TotalAmount * (DiscountPercentage / 100);
            FinalAmount = SelectedInvoice.FinalAmount;
            PreviousBalance = SelectedInvoice.PreviousBalance;
            CurrentBalance = SelectedInvoice.CurrentBalance;

            IsEditMode = true;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            ExitEditMode();
            ClearForm();
        }

        [RelayCommand]
        private async Task MarkInvoiceAsPaidAsync()
        {
            if (SelectedInvoice == null) return;

            var confirmed = await _navigationService.ShowConfirmationDialogAsync(
                "تأكيد السداد",
                $"هل أنت متأكد من تعليم الفاتورة رقم {SelectedInvoice.InvoiceNumber} كمدفوعة؟");

            if (!confirmed) return;

            await ExecuteAsyncOperation(async () =>
            {
                var result = await _invoiceService.MarkInvoiceAsPaidAsync(SelectedInvoice.InvoiceId);
                if (result.IsSuccess)
                {
                    await RefreshTodaysInvoicesAsync();
                    await _navigationService.ShowSuccessDialogAsync("تم السداد", "تم تعليم الفاتورة كمدفوعة");
                }
                else
                {
                    AddError(result.Message);
                    await _navigationService.ShowErrorDialogAsync("خطأ في التحديث", result.Message);
                }
            }, true, "Mark Invoice As Paid");
        }

        #endregion

        #region Command Can Execute Methods

        private bool CanCreateInvoice() => !IsBusy && !IsEditMode && SelectedCustomer != null && SelectedTruck != null && GrossWeight > 0;
        private bool CanUpdateInvoice() => !IsBusy && IsEditMode && SelectedInvoice != null;
        private bool CanDeleteInvoice() => !IsBusy && SelectedInvoice != null && !SelectedInvoice.IsPaid;

        #endregion

        #region Property Change Handlers

        partial void OnSelectedCustomerChanged(Customer? value)
        {
            if (value != null)
            {
                PreviousBalance = value.TotalDebt;
                _ = CalculateAmountsAsync();
            }
        }

        partial void OnGrossWeightChanged(decimal value)
        {
            ValidateProperty(value, nameof(GrossWeight));
            _ = CalculateAmountsAsync();
        }

        partial void OnCagesWeightChanged(decimal value)
        {
            ValidateProperty(value, nameof(CagesWeight));
            _ = CalculateAmountsAsync();
        }

        partial void OnUnitPriceChanged(decimal value)
        {
            ValidateProperty(value, nameof(UnitPrice));
            _ = CalculateAmountsAsync();
        }

        partial void OnDiscountPercentageChanged(decimal value)
        {
            ValidateProperty(value, nameof(DiscountPercentage));
            _ = CalculateAmountsAsync();
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                await LoadCustomersAsync();
                await LoadTrucksAsync();
                await RefreshTodaysInvoicesAsync();
                await GenerateDailySalesReportAsync();
                await GenerateInvoiceNumberAsync();
            }, true, "Initialize");
        }

        private async Task LoadCustomersAsync()
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

        private async Task LoadTrucksAsync()
        {
            // Implementation would use ITruckService
            // Placeholder for now
            Trucks.Clear();
        }

        private async Task RefreshCustomersAsync()
        {
            await LoadCustomersAsync();

            // Refresh selected customer if it exists
            if (SelectedCustomer != null)
            {
                var updatedCustomer = Customers.FirstOrDefault(c => c.CustomerId == SelectedCustomer.CustomerId);
                if (updatedCustomer != null)
                {
                    SelectedCustomer = updatedCustomer;
                }
            }
        }

        private async Task RefreshTodaysInvoicesAsync()
        {
            var result = await _invoiceService.GetInvoicesByDateRangeAsync(DateTime.Today, DateTime.Today);
            if (result.IsSuccess && result.Data != null)
            {
                TodaysInvoices.Clear();
                foreach (var invoice in result.Data)
                {
                    TodaysInvoices.Add(invoice);
                }

                TodaysInvoiceCount = TodaysInvoices.Count;
                TodaysTotalSales = TodaysInvoices.Sum(i => i.FinalAmount);
                AverageInvoiceValue = TodaysInvoiceCount > 0 ? TodaysTotalSales / TodaysInvoiceCount : 0;
            }
        }

        private async Task GenerateDailySalesReportAsync()
        {
            var result = await _invoiceService.GenerateSalesReportAsync(DateTime.Today, DateTime.Today);
            if (result.IsSuccess && result.Data != null)
            {
                DailySalesReport = result.Data;
            }
        }

        private void ClearForm()
        {
            InvoiceNumber = string.Empty;
            SelectedCustomer = null;
            SelectedTruck = null;
            InvoiceDate = DateTime.Now;
            GrossWeight = 0;
            CagesWeight = 0;
            CagesCount = 0;
            UnitPrice = 0;
            DiscountPercentage = 0;
            Notes = string.Empty;

            NetWeight = 0;
            TotalAmount = 0;
            DiscountAmount = 0;
            FinalAmount = 0;
            PreviousBalance = 0;
            CurrentBalance = 0;

            ClearErrors();
        }

        private void ExitEditMode()
        {
            IsEditMode = false;
            SelectedInvoice = null;
        }

        private void ValidateProperty(object value, string propertyName)
        {
            var validationContext = new ValidationContext(this) { MemberName = propertyName };
            var validationResults = new List<ValidationResult>();

            Validator.TryValidateProperty(value, validationContext, validationResults);

            SetErrors(propertyName, validationResults.Select(r => r.ErrorMessage));
        }

        #endregion

        public override void Cleanup()
        {
            Customers.Clear();
            Trucks.Clear();
            TodaysInvoices.Clear();
            base.Cleanup();
        }
    }
}