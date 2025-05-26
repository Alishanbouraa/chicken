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
    /// Advanced ViewModel for comprehensive truck load management operations
    /// Implements sophisticated data binding, validation, and command patterns for truck loading workflows
    /// </summary>
    public partial class TruckLoadViewModel : BaseViewModel
    {
        private readonly ITruckLoadService _truckLoadService;
        private readonly INavigationService _navigationService;

        #region Observable Properties

        [ObservableProperty]
        private ObservableCollection<Truck> _availableTrucks = new();

        [ObservableProperty]
        private ObservableCollection<TruckLoad> _todaysTruckLoads = new();

        [ObservableProperty]
        private ObservableCollection<WeightComparisonReportDto> _weightComparisonReports = new();

        [ObservableProperty]
        private Truck? _selectedTruck;

        [ObservableProperty]
        private TruckLoad? _selectedTruckLoad;

        [ObservableProperty]
        private DateTime _selectedDate = DateTime.Today;

        [ObservableProperty]
        [Required(ErrorMessage = "يجب اختيار الشاحنة")]
        private int _truckId;

        [ObservableProperty]
        [Required(ErrorMessage = "تاريخ التحميل مطلوب")]
        private DateTime _loadDate = DateTime.Now;

        [ObservableProperty]
        [Required(ErrorMessage = "الوزن الإجمالي مطلوب")]
        [Range(0.001, 9999.999, ErrorMessage = "الوزن الإجمالي يجب أن يكون بين 0.001 و 9999.999 كيلو")]
        private decimal _totalWeight;

        [ObservableProperty]
        [Required(ErrorMessage = "عدد الأقفاص مطلوب")]
        [Range(1, 999, ErrorMessage = "عدد الأقفاص يجب أن يكون بين 1 و 999")]
        private int _cagesCount;

        [ObservableProperty]
        [Range(0, 999.999, ErrorMessage = "وزن الأقفاص يجب أن يكون بين 0 و 999.999 كيلو")]
        private decimal _cagesWeight;

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private bool _isEditMode;

        [ObservableProperty]
        private DailySummaryReportDto? _dailySummaryReport;

        [ObservableProperty]
        private decimal _totalInitialWeight;

        [ObservableProperty]
        private decimal _totalSalesWeight;

        [ObservableProperty]
        private decimal _totalWeightLoss;

        [ObservableProperty]
        private decimal _averageLossPercentage;

        #endregion

        public TruckLoadViewModel(
            ITruckLoadService truckLoadService,
            INavigationService navigationService,
            ILogger<TruckLoadViewModel> logger) : base(logger)
        {
            _truckLoadService = truckLoadService ?? throw new ArgumentNullException(nameof(truckLoadService));
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            Title = "إدارة تحميل الشاحنات";
            InitializeAsync();
        }

        #region Async Relay Commands

        [RelayCommand(CanExecute = nameof(CanCreateTruckLoad))]
        private async Task CreateTruckLoadAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                ValidateAllProperties();
                if (HasErrors) return;

                var dto = new CreateTruckLoadDto
                {
                    TruckId = TruckId,
                    LoadDate = LoadDate,
                    TotalWeight = TotalWeight,
                    CagesCount = CagesCount,
                    CagesWeight = CagesWeight,
                    Notes = Notes
                };

                var result = await _truckLoadService.CreateTruckLoadAsync(dto);
                if (result.IsSuccess && result.Data != null)
                {
                    await RefreshTodaysTruckLoadsAsync();
                    ClearForm();
                    await _navigationService.ShowSuccessDialogAsync("نجح الحفظ", "تم إنشاء تحميل الشاحنة بنجاح");
                }
                else
                {
                    AddError(result.Message);
                    await _navigationService.ShowErrorDialogAsync("خطأ في الحفظ", result.Message);
                }
            }, true, "Create Truck Load");
        }

        [RelayCommand(CanExecute = nameof(CanUpdateTruckLoad))]
        private async Task UpdateTruckLoadAsync()
        {
            if (SelectedTruckLoad == null) return;

            await ExecuteAsyncOperation(async () =>
            {
                ValidateAllProperties();
                if (HasErrors) return;

                var dto = new UpdateTruckLoadDto
                {
                    LoadId = SelectedTruckLoad.LoadId,
                    TruckId = TruckId,
                    LoadDate = LoadDate,
                    TotalWeight = TotalWeight,
                    CagesCount = CagesCount,
                    CagesWeight = CagesWeight,
                    Notes = Notes,
                    Status = SelectedTruckLoad.Status
                };

                var result = await _truckLoadService.UpdateTruckLoadAsync(SelectedTruckLoad.LoadId, dto);
                if (result.IsSuccess)
                {
                    await RefreshTodaysTruckLoadsAsync();
                    ExitEditMode();
                    await _navigationService.ShowSuccessDialogAsync("نجح التحديث", "تم تحديث تحميل الشاحنة بنجاح");
                }
                else
                {
                    AddError(result.Message);
                    await _navigationService.ShowErrorDialogAsync("خطأ في التحديث", result.Message);
                }
            }, true, "Update Truck Load");
        }

        [RelayCommand(CanExecute = nameof(CanDeleteTruckLoad))]
        private async Task DeleteTruckLoadAsync()
        {
            if (SelectedTruckLoad == null) return;

            var confirmed = await _navigationService.ShowConfirmationDialogAsync(
                "تأكيد الحذف",
                $"هل أنت متأكد من حذف تحميل الشاحنة {SelectedTruckLoad.Truck?.TruckNumber}؟");

            if (!confirmed) return;

            await ExecuteAsyncOperation(async () =>
            {
                var result = await _truckLoadService.DeleteTruckLoadAsync(SelectedTruckLoad.LoadId);
                if (result.IsSuccess)
                {
                    await RefreshTodaysTruckLoadsAsync();
                    SelectedTruckLoad = null;
                    await _navigationService.ShowSuccessDialogAsync("نجح الحذف", "تم حذف تحميل الشاحنة بنجاح");
                }
                else
                {
                    AddError(result.Message);
                    await _navigationService.ShowErrorDialogAsync("خطأ في الحذف", result.Message);
                }
            }, true, "Delete Truck Load");
        }

        [RelayCommand]
        private async Task RefreshDataAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                await LoadAvailableTrucksAsync();
                await RefreshTodaysTruckLoadsAsync();
                await GenerateDailySummaryReportAsync();
            }, true, "Refresh Data");
        }

        [RelayCommand]
        private async Task GenerateWeightComparisonReportAsync()
        {
            if (SelectedTruckLoad == null) return;

            await ExecuteAsyncOperation(async () =>
            {
                var result = await _truckLoadService.GenerateWeightComparisonReportAsync(
                    SelectedTruckLoad.TruckId, SelectedDate);

                if (result.IsSuccess && result.Data != null)
                {
                    // Navigate to report view or display in dialog
                    // Implementation depends on UI requirements
                    await _navigationService.ShowSuccessDialogAsync("تقرير المقارنة",
                        $"الوزن المحمل: {result.Data.InitialLoadWeight} كجم\n" +
                        $"الوزن المباع: {result.Data.TotalSalesWeight} كجم\n" +
                        $"الفقد: {result.Data.WeightDifference} كجم ({result.Data.LossPercentage}%)");
                }
                else
                {
                    AddError(result.Message);
                    await _navigationService.ShowErrorDialogAsync("خطأ في التقرير", result.Message);
                }
            }, true, "Generate Weight Comparison Report");
        }

        [RelayCommand]
        private async Task GenerateDailySummaryReportAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                var result = await _truckLoadService.GenerateDailySummaryReportAsync(SelectedDate);
                if (result.IsSuccess && result.Data != null)
                {
                    DailySummaryReport = result.Data;
                    TotalInitialWeight = result.Data.TotalInitialWeight;
                    TotalSalesWeight = result.Data.TotalSalesWeight;
                    TotalWeightLoss = result.Data.TotalWeightLoss;
                    AverageLossPercentage = result.Data.AverageLossPercentage;

                    WeightComparisonReports.Clear();
                    foreach (var report in result.Data.TruckReports)
                    {
                        WeightComparisonReports.Add(report);
                    }
                }
                else
                {
                    AddError(result.Message);
                }
            }, true, "Generate Daily Summary Report");
        }

        [RelayCommand]
        private void EditTruckLoad()
        {
            if (SelectedTruckLoad == null) return;

            TruckId = SelectedTruckLoad.TruckId;
            LoadDate = SelectedTruckLoad.LoadDate;
            TotalWeight = SelectedTruckLoad.TotalWeight;
            CagesCount = SelectedTruckLoad.CagesCount;
            CagesWeight = SelectedTruckLoad.CagesWeight;
            Notes = SelectedTruckLoad.Notes ?? string.Empty;
            IsEditMode = true;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            ExitEditMode();
            ClearForm();
        }

        [RelayCommand]
        private async Task CompleteTruckLoadAsync()
        {
            if (SelectedTruckLoad == null) return;

            var confirmed = await _navigationService.ShowConfirmationDialogAsync(
                "تأكيد الإكمال",
                $"هل أنت متأكد من إكمال تحميل الشاحنة {SelectedTruckLoad.Truck?.TruckNumber}؟");

            if (!confirmed) return;

            await ExecuteAsyncOperation(async () =>
            {
                var result = await _truckLoadService.CompleteTruckLoadAsync(SelectedTruckLoad.LoadId);
                if (result.IsSuccess)
                {
                    await RefreshTodaysTruckLoadsAsync();
                    await _navigationService.ShowSuccessDialogAsync("تم الإكمال", "تم إكمال تحميل الشاحنة بنجاح");
                }
                else
                {
                    AddError(result.Message);
                    await _navigationService.ShowErrorDialogAsync("خطأ في الإكمال", result.Message);
                }
            }, true, "Complete Truck Load");
        }

        #endregion

        #region Command Can Execute Methods

        private bool CanCreateTruckLoad() => !IsBusy && !IsEditMode && TruckId > 0;
        private bool CanUpdateTruckLoad() => !IsBusy && IsEditMode && SelectedTruckLoad != null;
        private bool CanDeleteTruckLoad() => !IsBusy && SelectedTruckLoad != null && !SelectedTruckLoad.IsCompleted;

        #endregion

        #region Property Change Handlers

        partial void OnSelectedDateChanged(DateTime value)
        {
            _ = Task.Run(async () =>
            {
                await RefreshTodaysTruckLoadsAsync();
                await GenerateDailySummaryReportAsync();
            });
        }

        partial void OnSelectedTruckChanged(Truck? value)
        {
            if (value != null)
            {
                TruckId = value.TruckId;
            }
        }

        partial void OnTotalWeightChanged(decimal value)
        {
            ValidateProperty(value, nameof(TotalWeight));
            CalculateNetWeight();
        }

        partial void OnCagesWeightChanged(decimal value)
        {
            ValidateProperty(value, nameof(CagesWeight));
            CalculateNetWeight();
        }

        #endregion

        #region Private Methods

        private async Task InitializeAsync()
        {
            await ExecuteAsyncOperation(async () =>
            {
                await LoadAvailableTrucksAsync();
                await RefreshTodaysTruckLoadsAsync();
                await GenerateDailySummaryReportAsync();
            }, true, "Initialize");
        }

        private async Task LoadAvailableTrucksAsync()
        {
            // Implementation would use ITruckRepository or ITruckService
            // For now, placeholder implementation
            AvailableTrucks.Clear();
            // var trucks = await _truckService.GetActiveTrucksAsync();
            // foreach (var truck in trucks) { AvailableTrucks.Add(truck); }
        }

        private async Task RefreshTodaysTruckLoadsAsync()
        {
            var result = await _truckLoadService.GetTruckLoadsByDateAsync(SelectedDate);
            if (result.IsSuccess && result.Data != null)
            {
                TodaysTruckLoads.Clear();
                foreach (var truckLoad in result.Data)
                {
                    TodaysTruckLoads.Add(truckLoad);
                }
            }
        }

        private void ClearForm()
        {
            TruckId = 0;
            SelectedTruck = null;
            LoadDate = DateTime.Now;
            TotalWeight = 0;
            CagesCount = 0;
            CagesWeight = 0;
            Notes = string.Empty;
            ClearErrors();
        }

        private void ExitEditMode()
        {
            IsEditMode = false;
            SelectedTruckLoad = null;
        }

        private void CalculateNetWeight()
        {
            // This could be exposed as a computed property if needed
            var netWeight = TotalWeight - CagesWeight;
            // Update UI or perform additional calculations
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
            AvailableTrucks.Clear();
            TodaysTruckLoads.Clear();
            WeightComparisonReports.Clear();
            base.Cleanup();
        }
    }
}