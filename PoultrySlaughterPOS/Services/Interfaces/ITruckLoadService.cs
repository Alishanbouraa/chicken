using PoultrySlaughterPOS.Models.Entities;
using PoultrySlaughterPOS.Models.DTOs;

namespace PoultrySlaughterPOS.Services.Interfaces
{
    /// <summary>
    /// Business service interface for truck load management operations
    /// Handles initial truck loading, distribution tracking, and weight comparison analytics
    /// </summary>
    public interface ITruckLoadService
    {
        Task<ServiceResult<TruckLoad>> CreateTruckLoadAsync(CreateTruckLoadDto dto);
        Task<ServiceResult<TruckLoad>> UpdateTruckLoadAsync(int loadId, UpdateTruckLoadDto dto);
        Task<ServiceResult<bool>> DeleteTruckLoadAsync(int loadId);
        Task<ServiceResult<TruckLoad?>> GetTruckLoadByIdAsync(int loadId);
        Task<ServiceResult<IEnumerable<TruckLoad>>> GetTruckLoadsByDateAsync(DateTime date);
        Task<ServiceResult<IEnumerable<TruckLoad>>> GetTruckLoadsByTruckAsync(int truckId, DateTime date);
        Task<ServiceResult<bool>> CompleteTruckLoadAsync(int loadId);
        Task<ServiceResult<WeightComparisonReportDto>> GenerateWeightComparisonReportAsync(int truckId, DateTime date);
        Task<ServiceResult<DailySummaryReportDto>> GenerateDailySummaryReportAsync(DateTime date);
        Task<ServiceResult<bool>> ValidateTruckLoadDataAsync(CreateTruckLoadDto dto);
    }
}