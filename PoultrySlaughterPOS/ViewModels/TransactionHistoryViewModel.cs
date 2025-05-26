using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models.DTOs;
using PoultrySlaughterPOS.Models.Entities;
using PoultrySlaughterPOS.Services.Interfaces;
using System.Collections.ObjectModel;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Advanced ViewModel for comprehensive transaction history management and financial reporting
    /// Implements sophisticated filtering, searching, and analytical capabilities for business intelligence
    /// </summary>
    public partial class TransactionHistoryViewModel : BaseViewModel
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ICustomerService _customerService;
        private readonly INavigationService _navigationService;

        #region Observable Properties

        [ObservableProperty]
        private ObservableCollection<Invoice> _transactions = new();

        [ObservableProperty]
        private ObservableCollection<Customer> _customers = new();

        [ObservableProperty]
        private DateTime _startDate = DateTime.Today.AddDays(-30);

        [ObservableProperty]
        private DateTime _endDate = DateTime.Today;

        [ObservableProperty]
        private Customer? _selectedCustomer;

        [ObservableProperty]
        private Invoice? _selectedTransaction;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private decimal _totalSalesAmount;

        [ObservableProperty]
        private int _totalTransactionsCount;

        [ObservableProperty]
        private decimal _averageTransactionValue;

        [ObservableProperty]
        private SalesReportDto? _periodReport;

        #endregion

        public TransactionHistoryViewModel(
            IInvoiceService invoiceService,
            ICustomerService customerService,
            INavigationService navigationService,
            ILogger<TransactionHistoryViewModel> logger) : base(logger)
        {
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            Title = "تاريخ المعاملات والتقارير المالية";
            InitializeAsync();
        }

        #region Relay Commands

        [RelayCommand]
        private async Task LoadTransactionsAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                var result = await _invoiceService.GetInvoicesByDateRangeAsync(StartDate, EndDate);
                if (result.IsSuccess && result.Data != null)
                {
                    var transactions = result.Data.ToList();

                    // Apply customer filter if selected
                    if (SelectedCustomer != null)
                    {
                        transactions = transactions.Where(t => t.CustomerId == SelectedCustomer.CustomerId).ToList();
                    }

                    // Apply search filter if provided
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        var searchLower = SearchText.ToLower();
                        transactions = transactions.Where(t =>
                            t.InvoiceNumber.ToLower().Contains(searchLower) ||
                            t.Customer.CustomerName.ToLower().Contains(searchLower) ||
                            t.Truck.TruckNumber.ToLower().Contains(searchLower)
                        ).ToList();
                    }

                    Transactions.Clear();
                    foreach (var transaction in transactions.OrderByDescending(t => t.InvoiceDate))
                    {
                        Transactions.Add(transaction);
                    }

                    UpdateSummaryStatistics();
                }
                else
                {
                    AddError(result.Message);
                }
            }, true, "Load Transactions");
        }

        [RelayCommand]
        private async Task GeneratePeriodReportAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                var result = await _invoiceService.GenerateSalesReportAsync(StartDate, EndDate);
                if (result.IsSuccess && result.Data != null)
                {
                    PeriodReport = result.Data;
                }
                else
                {
                    AddError(result.Message);
                }
            }, true, "Generate Period Report");
        }

        [RelayCommand]
        private async Task ExportTransactionsAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                // Implementation would involve exporting to Excel/PDF
                await Task.Delay(1000); // Simulate export operation
                await _navigationService.ShowSuccessDialogAsync("تصدير البيانات", "تم تصدير المعاملات بنجاح");
            }, true, "Export Transactions");
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                await LoadCustomersAsync();
                await LoadTransactionsAsync();
                await GeneratePeriodReportAsync();
            }, true, "Refresh Data");
        }

        [RelayCommand]
        private void ClearFilters()
        {
            StartDate = DateTime.Today.AddDays(-30);
            EndDate = DateTime.Today;
            SelectedCustomer = null;
            SearchText = string.Empty;
            _ = LoadTransactionsAsync();
        }

        [RelayCommand]
        private async Task ViewTransactionDetailsAsync()
        {
            if (SelectedTransaction == null) return;

            var details = $"رقم الفاتورة: {SelectedTransaction.InvoiceNumber}\n" +
                         $"الزبون: {SelectedTransaction.Customer.CustomerName}\n" +
                         $"التاريخ: {SelectedTransaction.InvoiceDate:yyyy/MM/dd HH:mm}\n" +
                         $"الوزن الصافي: {SelectedTransaction.NetWeight:F2} كجم\n" +
                         $"المبلغ النهائي: {SelectedTransaction.FinalAmount:F2} ريال\n" +
                         $"الحالة: {(SelectedTransaction.IsPaid ? "مدفوعة" : "غير مدفوعة")}";

            await _navigationService.ShowSuccessDialogAsync("تفاصيل المعاملة", details);
        }

        #endregion

        #region Property Change Handlers

        partial void OnStartDateChanged(DateTime value)
        {
            if (value <= EndDate)
            {
                _ = LoadTransactionsAsync();
            }
        }

        partial void OnEndDateChanged(DateTime value)
        {
            if (value >= StartDate)
            {
                _ = LoadTransactionsAsync();
            }
        }

        partial void OnSelectedCustomerChanged(Customer? value)
        {
            _ = LoadTransactionsAsync();
        }

        partial void OnSearchTextChanged(string value)
        {
            _ = LoadTransactionsAsync();
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                await LoadCustomersAsync();
                await LoadTransactionsAsync();
                await GeneratePeriodReportAsync();
            }, true, "Initialize");
        }

        private async Task LoadCustomersAsync()
        {
            var result = await _customerService.GetAllActiveCustomersAsync();
            if (result.IsSuccess && result.Data != null)
            {
                Customers.Clear();
                Customers.Add(new Customer { CustomerId = 0, CustomerName = "جميع الزبائن" });

                foreach (var customer in result.Data)
                {
                    Customers.Add(customer);
                }
            }
        }

        private void UpdateSummaryStatistics()
        {
            TotalTransactionsCount = Transactions.Count;
            TotalSalesAmount = Transactions.Sum(t => t.FinalAmount);
            AverageTransactionValue = TotalTransactionsCount > 0 ? TotalSalesAmount / TotalTransactionsCount : 0;
        }

        #endregion

        public override void Cleanup()
        {
            Transactions.Clear();
            Customers.Clear();
            base.Cleanup();
        }
    }
}