using TimeTrackingAPI.DTOs;

namespace TimeTrackingAPI.Services
{
    public interface ITimeEntryService
    {
        Task<bool> ValidateDailyHours(int taskId, DateTime date, decimal hours);
        Task<DailySummaryDto> GetDailySummary(DateTime date);
        Task<bool> CanEditTask(int timeEntryId);
    }
}