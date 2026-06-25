using Microsoft.EntityFrameworkCore;
using TimeTrackingAPI.Data;
using TimeTrackingAPI.DTOs;
using TimeTrackingAPI.Models;

namespace TimeTrackingAPI.Services
{
    public class TimeEntryService : ITimeEntryService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TimeEntryService> _logger;

        public TimeEntryService(ApplicationDbContext context, ILogger<TimeEntryService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> ValidateDailyHours(int taskId, DateTime date, decimal hours)
        {
            try
            {
                var dailyTotal = await _context.TimeEntries
                    .Where(t => t.EntryDate.Date == date.Date)
                    .SumAsync(t => t.Hours);

                var newTotal = dailyTotal + hours;
                var isValid = newTotal <= 24;

                _logger.LogInformation(
                    "Daily hours validation for date {Date}: current total = {DailyTotal}, adding {Hours}, new total = {NewTotal}, IsValid = {IsValid}",
                    date, dailyTotal, hours, newTotal, isValid
                );

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating daily hours for date {Date}", date);
                throw;
            }
        }

        public async Task<DailySummaryDto> GetDailySummary(DateTime date)
        {
            try
            {
                var entries = await _context.TimeEntries
                    .Include(t => t.Task)
                        .ThenInclude(t => t!.Project)
                    .Where(t => t.EntryDate.Date == date.Date)
                    .ToListAsync();

                var totalHours = entries.Sum(t => t.Hours);

                var status = totalHours switch
                {
                    < 8 => "Yellow",
                    8 => "Green",
                    > 8 => "Red"
                };

                var entryDtos = entries.Select(e => new TimeEntryDto
                {
                    Id = e.Id,
                    EntryDate = e.EntryDate,
                    Hours = e.Hours,
                    Description = e.Description,
                    TaskId = e.TaskId,
                    TaskName = e.Task?.Name,
                    ProjectName = e.Task?.Project?.Name
                }).ToList();

                return new DailySummaryDto
                {
                    Date = date,
                    TotalHours = totalHours,
                    Status = status,
                    Entries = entryDtos
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily summary for date: {Date}", date);
                throw;
            }
        }

        public async Task<bool> CanEditTask(int timeEntryId)
        {
            try
            {
                var timeEntry = await _context.TimeEntries
                    .Include(t => t.Task)
                    .FirstOrDefaultAsync(t => t.Id == timeEntryId);

                if (timeEntry == null)
                    return false;

                return timeEntry.Task?.IsActive ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if task can be edited for TimeEntryId: {TimeEntryId}", timeEntryId);
                throw;
            }
        }
    }
}