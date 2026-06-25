using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackingAPI.Data;
using TimeTrackingAPI.DTOs;
using TimeTrackingAPI.Models;
using TimeTrackingAPI.Services;

namespace TimeTrackingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeEntriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ITimeEntryService _timeEntryService;
        private readonly ILogger<TimeEntriesController> _logger;

        public TimeEntriesController(
            ApplicationDbContext context,
            ITimeEntryService timeEntryService,
            ILogger<TimeEntriesController> logger)
        {
            _context = context;
            _timeEntryService = timeEntryService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TimeEntryDto>>> GetTimeEntries()
        {
            try
            {
                var entries = await _context.TimeEntries
                    .Include(t => t.Task)
                        .ThenInclude(t => t!.Project)
                    .OrderByDescending(t => t.EntryDate)
                    .Select(t => new TimeEntryDto
                    {
                        Id = t.Id,
                        EntryDate = t.EntryDate,
                        Hours = t.Hours,
                        Description = t.Description,
                        TaskId = t.TaskId,
                        TaskName = t.Task != null ? t.Task.Name : null,
                        ProjectName = t.Task != null && t.Task.Project != null ? t.Task.Project.Name : null
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} time entries", entries.Count);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time entries");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("by-date")]
        public async Task<ActionResult<IEnumerable<TimeEntryDto>>> GetTimeEntriesByDate([FromQuery] DateTime date)
        {
            try
            {
                var entries = await _context.TimeEntries
                    .Include(t => t.Task)
                        .ThenInclude(t => t!.Project)
                    .Where(t => t.EntryDate.Date == date.Date)
                    .OrderBy(t => t.EntryDate)
                    .Select(t => new TimeEntryDto
                    {
                        Id = t.Id,
                        EntryDate = t.EntryDate,
                        Hours = t.Hours,
                        Description = t.Description,
                        TaskId = t.TaskId,
                        TaskName = t.Task != null ? t.Task.Name : null,
                        ProjectName = t.Task != null && t.Task.Project != null ? t.Task.Project.Name : null
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} time entries for date {Date}", entries.Count, date);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time entries for date {Date}", date);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("by-month")]
        public async Task<ActionResult<IEnumerable<TimeEntryDto>>> GetTimeEntriesByMonth([FromQuery] string month)
        {
            try
            {
                if (!DateTime.TryParseExact(month + "-01", "yyyy-MM-dd", null,
                    System.Globalization.DateTimeStyles.None, out DateTime firstDay))
                {
                    return BadRequest("Invalid month format. Use yyyy-MM");
                }

                var lastDay = firstDay.AddMonths(1).AddDays(-1);

                var entries = await _context.TimeEntries
                    .Include(t => t.Task)
                        .ThenInclude(t => t!.Project)
                    .Where(t => t.EntryDate >= firstDay && t.EntryDate <= lastDay)
                    .OrderBy(t => t.EntryDate)
                    .Select(t => new TimeEntryDto
                    {
                        Id = t.Id,
                        EntryDate = t.EntryDate,
                        Hours = t.Hours,
                        Description = t.Description,
                        TaskId = t.TaskId,
                        TaskName = t.Task != null ? t.Task.Name : null,
                        ProjectName = t.Task != null && t.Task.Project != null ? t.Task.Project.Name : null
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} time entries for month {Month}", entries.Count, month);
                return Ok(entries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time entries for month {Month}", month);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TimeEntryDto>> GetTimeEntry(int id)
        {
            try
            {
                var entry = await _context.TimeEntries
                    .Include(t => t.Task)
                        .ThenInclude(t => t!.Project)
                    .Where(t => t.Id == id)
                    .Select(t => new TimeEntryDto
                    {
                        Id = t.Id,
                        EntryDate = t.EntryDate,
                        Hours = t.Hours,
                        Description = t.Description,
                        TaskId = t.TaskId,
                        TaskName = t.Task != null ? t.Task.Name : null,
                        ProjectName = t.Task != null && t.Task.Project != null ? t.Task.Project.Name : null
                    })
                    .FirstOrDefaultAsync();

                if (entry == null)
                {
                    _logger.LogWarning("Time entry with ID {Id} not found", id);
                    return NotFound($"Time entry with ID {id} not found");
                }

                return Ok(entry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting time entry with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<TimeEntryDto>> CreateTimeEntry(CreateTimeEntryDto createDto)
        {
            try
            {
                var task = await _context.Tasks
                    .Include(t => t.Project)
                    .FirstOrDefaultAsync(t => t.Id == createDto.TaskId);

                if (task == null)
                {
                    return BadRequest($"Task with ID {createDto.TaskId} not found");
                }

                if (!task.IsActive)
                {
                    return BadRequest("Cannot create time entry for inactive task");
                }

                if (task.Project != null && !task.Project.IsActive)
                {
                    return BadRequest("Cannot create time entry for task in inactive project");
                }

                var isValid = await _timeEntryService.ValidateDailyHours(
                    createDto.TaskId,
                    createDto.EntryDate,
                    createDto.Hours
                );

                if (!isValid)
                {
                    return BadRequest("Total hours for this day would exceed 24 hours");
                }

                var timeEntry = new TimeEntry
                {
                    EntryDate = createDto.EntryDate,
                    Hours = createDto.Hours,
                    Description = createDto.Description ?? string.Empty,
                    TaskId = createDto.TaskId
                };

                _context.TimeEntries.Add(timeEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new time entry with ID {Id} for task ID {TaskId}",
                    timeEntry.Id, timeEntry.TaskId);

                var entryDto = new TimeEntryDto
                {
                    Id = timeEntry.Id,
                    EntryDate = timeEntry.EntryDate,
                    Hours = timeEntry.Hours,
                    Description = timeEntry.Description,
                    TaskId = timeEntry.TaskId,
                    TaskName = task.Name,
                    ProjectName = task.Project != null ? task.Project.Name : null
                };

                return CreatedAtAction(nameof(GetTimeEntry), new { id = timeEntry.Id }, entryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating time entry");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTimeEntry(int id, UpdateTimeEntryDto updateDto)
        {
            try
            {
                var timeEntry = await _context.TimeEntries
                    .Include(t => t.Task)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (timeEntry == null)
                {
                    _logger.LogWarning("Time entry with ID {Id} not found for update", id);
                    return NotFound($"Time entry with ID {id} not found");
                }

                if (timeEntry.Task != null && !timeEntry.Task.IsActive)
                {
                    if (timeEntry.TaskId != updateDto.TaskId)
                    {
                        return BadRequest("Cannot change task for this time entry because the task is inactive");
                    }
                }

                var newTask = await _context.Tasks
                    .Include(t => t.Project)
                    .FirstOrDefaultAsync(t => t.Id == updateDto.TaskId);

                if (newTask == null)
                {
                    return BadRequest($"Task with ID {updateDto.TaskId} not found");
                }

                if (!newTask.IsActive)
                {
                    return BadRequest("Cannot assign time entry to inactive task");
                }

                if (newTask.Project != null && !newTask.Project.IsActive)
                {
                    return BadRequest("Cannot assign time entry to task in inactive project");
                }

                var existingHours = await _context.TimeEntries
                    .Where(t => t.EntryDate.Date == updateDto.EntryDate.Date && t.Id != id)
                    .SumAsync(t => t.Hours); 

                if (existingHours + updateDto.Hours > 24)
                {
                    return BadRequest("Total hours for this day would exceed 24 hours");
                }

                timeEntry.EntryDate = updateDto.EntryDate;
                timeEntry.Hours = updateDto.Hours;
                timeEntry.Description = updateDto.Description ?? string.Empty;
                timeEntry.TaskId = updateDto.TaskId;
                timeEntry.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated time entry with ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating time entry with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTimeEntry(int id)
        {
            try
            {
                var timeEntry = await _context.TimeEntries.FindAsync(id);
                if (timeEntry == null)
                {
                    _logger.LogWarning("Time entry with ID {Id} not found for deletion", id);
                    return NotFound($"Time entry with ID {id} not found");
                }

                _context.TimeEntries.Remove(timeEntry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted time entry with ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting time entry with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("daily-summary")]
        public async Task<ActionResult<DailySummaryDto>> GetDailySummary([FromQuery] DateTime date)
        {
            try
            {
                var summary = await _timeEntryService.GetDailySummary(date);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily summary for date {Date}", date);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}