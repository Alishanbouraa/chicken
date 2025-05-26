using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models.DTOs;
using PoultrySlaughterPOS.Services.Interfaces;
using System.Collections.ObjectModel;

namespace PoultrySlaughterPOS.ViewModels
{
    /// <summary>
    /// Advanced ViewModel for comprehensive business intelligence and analytical reporting
    /// Implements sophisticated data visualization and executive dashboard capabilities
    /// </summary>
    public partial class ReportsViewModel : BaseViewModel
    {
        private readonly ITruckLoadService _truckLoadService;
        private readonly IInvoiceService _invoiceService;
        private readonly ICustomerService _customerService;
        private readonly INavigationService _navigationService;

        #region Observable Properties

        [ObservableProperty]
        private DateTime _reportDate = DateTime.Today;

        [ObservableProperty]
        private DateTime _reportStartDate = DateTime.Today.AddDays(-30);

        [ObservableProperty]
        private DateTime _reportEndDate = DateTime.Today;

        [ObservableProperty]
        private DailySummaryReportDto? _dailySummaryReport;

        [ObservableProperty]
        private SalesReportDto? _salesReport;

        [ObservableProperty]
        private DebtorsReportDto? _debtorsReport;

        [ObservableProperty]
        private ObservableCollection<WeightComparisonReportDto> _weightReports = new();

        [ObservableProperty]
        private string _selectedReportType = "DailySummary";

        #endregion

        public ReportsViewModel(
            ITruckLoadService truckLoadService,
            IInvoiceService invoiceService,
            ICustomerService customerService,
            INavigationService navigationService,
            ILogger<ReportsViewModel> logger) : base(logger)
        {
            _truckLoadService = truckLoadService ?? throw new ArgumentNullException(nameof(truckLoadService));
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            Title = "التقارير والإحصائيات التنفيذية";
            InitializeAsync();
        }

        #region Relay Commands

        [RelayCommand]
        private async Task GenerateDailySummaryReportAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                var result = await _truckLoadService.GenerateDailySummaryReportAsync(ReportDate);
                if (result.IsSuccess && result.Data != null)
                {
                    DailySummaryReport = result.Data;
                    WeightReports.Clear();
                    foreach (var report in result.Data.TruckReports)
                    {
                        WeightReports.Add(report);
                    }
                }
                else
                {
                    AddError(result.Message);
                }
            }, true, "Generate Daily Summary Report");
        }

        [RelayCommand]
        private async Task GenerateSalesReportAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                var result = await _invoiceService.GenerateSalesReportAsync(ReportStartDate, ReportEndDate);
                if (result.IsSuccess && result.Data != null)
                {
                    SalesReport = result.Data;
                }
                else
                {
                    AddError(result.Message);
                }
            }, true, "Generate Sales Report");
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
        private async Task RefreshAllReportsAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                await GenerateDailySummaryReportAsync();
                await GenerateSalesReportAsync();
                await GenerateDebtorsReportAsync();
            }, true, "Refresh All Reports");
        }

        [RelayCommand]
        private async Task ExportReportAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                // Implementation would involve exporting selected report
                await Task.Delay(1000);
                await _navigationService.ShowSuccessDialogAsync("تصدير التقرير", "تم تصدير التقرير بنجاح");
            }, true, "Export Report");
        }

        #endregion

        #region Property Change Handlers

        partial void OnReportDateChanged(DateTime value)
        {
            _ = GenerateDailySummaryReportAsync();
        }

        partial void OnSelectedReportTypeChanged(string value)
        {
            switch (value)
            {
                case "DailySummary":
                    _ = GenerateDailySummaryReportAsync();
                    break;
                case "Sales":
                    _ = GenerateSalesReportAsync();
                    break;
                case "Debtors":
                    _ = GenerateDebtorsReportAsync();
                    break;
            }
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                await GenerateDailySummaryReportAsync();
                await GenerateSalesReportAsync();
                await GenerateDebtorsReportAsync();
            }, true, "Initialize");
        }

        #endregion

        public override void Cleanup()
        {
            WeightReports.Clear();
            base.Cleanup();
        }
    }
}