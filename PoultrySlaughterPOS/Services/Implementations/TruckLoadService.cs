using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models.DTOs;
using PoultrySlaughterPOS.Models.Entities;
using PoultrySlaughterPOS.Services.Interfaces;
using PoultrySlaughterPOS.Utils.Extensions;

namespace PoultrySlaughterPOS.Services.Implementations
{
    /// <summary>
    /// Business service implementation for truck load management operations
    /// Provides comprehensive truck loading, tracking, and weight comparison functionality
    /// </summary>
    public class TruckLoadService : ITruckLoadService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TruckLoadService> _logger;

        public TruckLoadService(IUnitOfWork unitOfWork, ILogger<TruckLoadService> logger)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ServiceResult<TruckLoad>> CreateTruckLoadAsync(CreateTruckLoadDto dto)
        {
            try
            {
                _logger.LogInformation("Creating truck load for Truck ID: {TruckId}", dto.TruckId);

                // Validate input data
                var validationResult = await ValidateTruckLoadDataAsync(dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<TruckLoad>.Failure(validationResult.Errors, "Validation failed");
                }

                // Check if truck exists and is active
                var truck = await _unitOfWork.Trucks.GetByIdAsync(dto.TruckId);
                if (truck == null || !truck.IsActive)
                {
                    return ServiceResult<TruckLoad>.Failure("Selected truck is not available", "TRUCK_NOT_FOUND");
                }

                // Check for existing load on the same date
                var existingLoad = await _unitOfWork.TruckLoads.FirstOrDefaultAsync(
                    tl => tl.TruckId == dto.TruckId && tl.LoadDate.Date == dto.LoadDate.Date);

                if (existingLoad != null)
                {
                    return ServiceResult<TruckLoad>.Failure(
                        $"Truck {truck.TruckNumber} already has a load for {dto.LoadDate:yyyy-MM-dd}",
                        "DUPLICATE_LOAD");
                }

                // Create new truck load entity
                var truckLoad = new TruckLoad
                {
                    TruckId = dto.TruckId,
                    LoadDate = dto.LoadDate,
                    TotalWeight = dto.TotalWeight,
                    CagesCount = dto.CagesCount,
                    CagesWeight = dto.CagesWeight,
                    Notes = dto.Notes?.Trim(),
                    Status = "Loaded",
                    IsCompleted = false,
                    CreatedDate = DateTime.Now
                };

                await _unitOfWork.TruckLoads.AddAsync(truckLoad);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully created truck load ID: {LoadId} for Truck: {TruckNumber}",
                    truckLoad.LoadId, truck.TruckNumber);

                return ServiceResult<TruckLoad>.Success(truckLoad, "Truck load created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating truck load for Truck ID: {TruckId}", dto.TruckId);
                return ServiceResult<TruckLoad>.Failure("An error occurred while creating the truck load", "CREATION_ERROR", ex);
            }
        }

        public async Task<ServiceResult<WeightComparisonReportDto>> GenerateWeightComparisonReportAsync(int truckId, DateTime date)
        {
            try
            {
                _logger.LogInformation("Generating weight comparison report for Truck ID: {TruckId}, Date: {Date:yyyy-MM-dd}",
                    truckId, date);

                // Get truck with load information
                var truck = await _unitOfWork.Trucks.GetByIdAsync(truckId, t => t.TruckLoads);
                if (truck == null)
                {
                    return ServiceResult<WeightComparisonReportDto>.Failure("Truck not found", "TRUCK_NOT_FOUND");
                }

                // Get truck load for the specified date
                var truckLoad = await _unitOfWork.TruckLoads.FirstOrDefaultAsync(
                    tl => tl.TruckId == truckId && tl.LoadDate.Date == date.Date);

                if (truckLoad == null)
                {
                    return ServiceResult<WeightComparisonReportDto>.Failure(
                        $"No load found for truck {truck.TruckNumber} on {date:yyyy-MM-dd}", "LOAD_NOT_FOUND");
                }

                // Get invoices for the truck on the specified date
                var invoices = await _unitOfWork.Invoices.GetInvoicesByTruckAsync(truckId, date);

                // Calculate totals
                var totalSalesWeight = invoices.Sum(i => i.NetWeight);
                var totalSalesAmount = invoices.Sum(i => i.FinalAmount);
                var weightDifference = truckLoad.TotalWeight - totalSalesWeight;
                var lossPercentage = truckLoad.TotalWeight > 0 ? (weightDifference / truckLoad.TotalWeight) * 100 : 0;

                // Create invoice summaries
                var invoiceDetails = invoices.Select(i => new InvoiceSummaryDto
                {
                    InvoiceId = i.InvoiceId,
                    InvoiceNumber = i.InvoiceNumber,
                    CustomerName = i.Customer.CustomerName,
                    InvoiceDate = i.InvoiceDate,
                    NetWeight = i.NetWeight,
                    FinalAmount = i.FinalAmount,
                    IsPaid = i.IsPaid
                }).ToList();

                var report = new WeightComparisonReportDto
                {
                    TruckId = truck.TruckId,
                    TruckNumber = truck.TruckNumber,
                    DriverName = truck.DriverName,
                    ReportDate = date,
                    InitialLoadWeight = truckLoad.TotalWeight,
                    TotalSalesWeight = totalSalesWeight,
                    WeightDifference = weightDifference,
                    LossPercentage = Math.Round(lossPercentage, 2),
                    TotalInvoicesCount = invoices.Count(),
                    TotalSalesAmount = totalSalesAmount,
                    InvoiceDetails = invoiceDetails
                };

                _logger.LogInformation("Successfully generated weight comparison report for Truck: {TruckNumber}, Loss: {LossPercentage}%",
                    truck.TruckNumber, report.LossPercentage);

                return ServiceResult<WeightComparisonReportDto>.Success(report, "Weight comparison report generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating weight comparison report for Truck ID: {TruckId}", truckId);
                return ServiceResult<WeightComparisonReportDto>.Failure(
                    "An error occurred while generating the weight comparison report", "REPORT_ERROR", ex);
            }
        }

        public async Task<ServiceResult<DailySummaryReportDto>> GenerateDailySummaryReportAsync(DateTime date)
        {
            try
            {
                _logger.LogInformation("Generating daily summary report for Date: {Date:yyyy-MM-dd}", date);

                // Get all trucks with loads for the specified date
                var trucksWithLoads = await _unitOfWork.Trucks.GetTrucksWithCurrentLoadsAsync(date);

                var truckReports = new List<WeightComparisonReportDto>();

                foreach (var truck in trucksWithLoads.Where(t => t.TruckLoads.Any()))
                {
                    var reportResult = await GenerateWeightComparisonReportAsync(truck.TruckId, date);
                    if (reportResult.IsSuccess && reportResult.Data != null)
                    {
                        truckReports.Add(reportResult.Data);
                    }
                }

                // Calculate summary totals
                var totalInitialWeight = truckReports.Sum(tr => tr.InitialLoadWeight);
                var totalSalesWeight = truckReports.Sum(tr => tr.TotalSalesWeight);
                var totalWeightLoss = totalInitialWeight - totalSalesWeight;
                var averageLossPercentage = truckReports.Count > 0 ? truckReports.Average(tr => tr.LossPercentage) : 0;
                var totalSalesAmount = truckReports.Sum(tr => tr.TotalSalesAmount);
                var totalInvoicesCount = truckReports.Sum(tr => tr.TotalInvoicesCount);

                var dailySummary = new DailySummaryReportDto
                {
                    ReportDate = date,
                    TruckReports = truckReports,
                    TotalInitialWeight = totalInitialWeight,
                    TotalSalesWeight = totalSalesWeight,
                    TotalWeightLoss = totalWeightLoss,
                    AverageLossPercentage = Math.Round(averageLossPercentage, 2),
                    TotalSalesAmount = totalSalesAmount,
                    TotalInvoicesCount = totalInvoicesCount
                };

                _logger.LogInformation("Successfully generated daily summary report for {Date:yyyy-MM-dd}: {TruckCount} trucks, {TotalSales:C}",
                    date, truckReports.Count, totalSalesAmount);

                return ServiceResult<DailySummaryReportDto>.Success(dailySummary, "Daily summary report generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating daily summary report for Date: {Date:yyyy-MM-dd}", date);
                return ServiceResult<DailySummaryReportDto>.Failure(
                    "An error occurred while generating the daily summary report", "REPORT_ERROR", ex);
            }
        }

        public async Task<ServiceResult<bool>> ValidateTruckLoadDataAsync(CreateTruckLoadDto dto)
        {
            var errors = new List<string>();

            try
            {
                // Validate truck ID
                if (dto.TruckId <= 0)
                {
                    errors.Add("Invalid truck selection");
                }

                // Validate load date
                if (dto.LoadDate.Date > DateTime.Now.Date)
                {
                    errors.Add("Load date cannot be in the future");
                }

                // Validate weights
                if (dto.TotalWeight <= 0)
                {
                    errors.Add("Total weight must be greater than zero");
                }

                if (dto.CagesWeight < 0)
                {
                    errors.Add("Cages weight cannot be negative");
                }

                if (dto.CagesWeight >= dto.TotalWeight)
                {
                    errors.Add("Cages weight cannot be greater than or equal to total weight");
                }

                // Validate cages count
                if (dto.CagesCount <= 0)
                {
                    errors.Add("Cages count must be greater than zero");
                }

                // Validate logical consistency
                var netWeight = dto.TotalWeight - dto.CagesWeight;
                if (netWeight <= 0)
                {
                    errors.Add("Net weight (Total - Cages) must be greater than zero");
                }

                if (errors.Any())
                {
                    return ServiceResult<bool>.Failure(errors, "Validation failed");
                }

                return ServiceResult<bool>.Success(true, "Validation passed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating truck load data");
                return ServiceResult<bool>.Failure("Validation error occurred", "VALIDATION_ERROR", ex);
            }
        }

        public async Task<ServiceResult<TruckLoad>> UpdateTruckLoadAsync(int loadId, UpdateTruckLoadDto dto)
        {
            try
            {
                _logger.LogInformation("Updating truck load ID: {LoadId}", loadId);

                var existingLoad = await _unitOfWork.TruckLoads.GetByIdAsync(loadId);
                if (existingLoad == null)
                {
                    return ServiceResult<TruckLoad>.Failure("Truck load not found", "LOAD_NOT_FOUND");
                }

                // Validate input data
                var validationResult = await ValidateTruckLoadDataAsync(dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<TruckLoad>.Failure(validationResult.Errors, "Validation failed");
                }

                // Update entity properties
                existingLoad.TotalWeight = dto.TotalWeight;
                existingLoad.CagesCount = dto.CagesCount;
                existingLoad.CagesWeight = dto.CagesWeight;
                existingLoad.Notes = dto.Notes?.Trim();
                existingLoad.Status = dto.Status;

                _unitOfWork.TruckLoads.Update(existingLoad);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully updated truck load ID: {LoadId}", loadId);
                return ServiceResult<TruckLoad>.Success(existingLoad, "Truck load updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating truck load ID: {LoadId}", loadId);
                return ServiceResult<TruckLoad>.Failure("An error occurred while updating the truck load", "UPDATE_ERROR", ex);
            }
        }

        public async Task<ServiceResult<bool>> DeleteTruckLoadAsync(int loadId)
        {
            try
            {
                _logger.LogInformation("Deleting truck load ID: {LoadId}", loadId);

                var truckLoad = await _unitOfWork.TruckLoads.GetByIdAsync(loadId);
                if (truckLoad == null)
                {
                    return ServiceResult<bool>.Failure("Truck load not found", "LOAD_NOT_FOUND");
                }

                // Check if there are related invoices
                var hasInvoices = await _unitOfWork.Invoices.AnyAsync(
                    i => i.TruckId == truckLoad.TruckId && i.InvoiceDate.Date == truckLoad.LoadDate.Date);

                if (hasInvoices)
                {
                    return ServiceResult<bool>.Failure(
                        "Cannot delete truck load with existing invoices", "HAS_RELATED_INVOICES");
                }

                _unitOfWork.TruckLoads.Remove(truckLoad);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully deleted truck load ID: {LoadId}", loadId);
                return ServiceResult<bool>.Success(true, "Truck load deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting truck load ID: {LoadId}", loadId);
                return ServiceResult<bool>.Failure("An error occurred while deleting the truck load", "DELETE_ERROR", ex);
            }
        }

        public async Task<ServiceResult<TruckLoad?>> GetTruckLoadByIdAsync(int loadId)
        {
            try
            {
                var truckLoad = await _unitOfWork.TruckLoads.GetByIdAsync(loadId, tl => tl.Truck);
                if (truckLoad == null)
                {
                    return ServiceResult<TruckLoad?>.Failure("Truck load not found", "LOAD_NOT_FOUND");
                }

                return ServiceResult<TruckLoad?>.Success(truckLoad, "Truck load retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving truck load ID: {LoadId}", loadId);
                return ServiceResult<TruckLoad?>.Failure("An error occurred while retrieving the truck load", "RETRIEVAL_ERROR", ex);
            }
        }

        public async Task<ServiceResult<IEnumerable<TruckLoad>>> GetTruckLoadsByDateAsync(DateTime date)
        {
            try
            {
                var truckLoads = await _unitOfWork.TruckLoads.FindAsync(
                    tl => tl.LoadDate.Date == date.Date, tl => tl.Truck);

                return ServiceResult<IEnumerable<TruckLoad>>.Success(truckLoads, "Truck loads retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving truck loads for date: {Date:yyyy-MM-dd}", date);
                return ServiceResult<IEnumerable<TruckLoad>>.Failure(
                    "An error occurred while retrieving truck loads", "RETRIEVAL_ERROR", ex);
            }
        }

        public async Task<ServiceResult<IEnumerable<TruckLoad>>> GetTruckLoadsByTruckAsync(int truckId, DateTime date)
        {
            try
            {
                var truckLoads = await _unitOfWork.TruckLoads.FindAsync(
                    tl => tl.TruckId == truckId && tl.LoadDate.Date == date.Date, tl => tl.Truck);

                return ServiceResult<IEnumerable<TruckLoad>>.Success(truckLoads, "Truck loads retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving truck loads for Truck ID: {TruckId}, Date: {Date:yyyy-MM-dd}", truckId, date);
                return ServiceResult<IEnumerable<TruckLoad>>.Failure(
                    "An error occurred while retrieving truck loads", "RETRIEVAL_ERROR", ex);
            }
        }

        public async Task<ServiceResult<bool>> CompleteTruckLoadAsync(int loadId)
        {
            try
            {
                _logger.LogInformation("Completing truck load ID: {LoadId}", loadId);

                var truckLoad = await _unitOfWork.TruckLoads.GetByIdAsync(loadId);
                if (truckLoad == null)
                {
                    return ServiceResult<bool>.Failure("Truck load not found", "LOAD_NOT_FOUND");
                }

                truckLoad.Status = "Completed";
                truckLoad.IsCompleted = true;

                _unitOfWork.TruckLoads.Update(truckLoad);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Successfully completed truck load ID: {LoadId}", loadId);
                return ServiceResult<bool>.Success(true, "Truck load marked as completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing truck load ID: {LoadId}", loadId);
                return ServiceResult<bool>.Failure("An error occurred while completing the truck load", "COMPLETION_ERROR", ex);
            }
        }
    }
}