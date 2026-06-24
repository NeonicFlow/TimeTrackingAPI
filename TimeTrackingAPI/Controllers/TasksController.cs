using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackingAPI.Data;
using TimeTrackingAPI.DTOs;
using TimeTrackingAPI.Models;

namespace TimeTrackingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TasksController> _logger;

        public TasksController(ApplicationDbContext context, ILogger<TasksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetTasks([FromQuery] bool? activeOnly)
        {
            try
            {
                var query = _context.Tasks
                    .Include(t => t.Project)
                    .AsQueryable();

                if (activeOnly.HasValue && activeOnly.Value)
                {
                    query = query.Where(t => t.IsActive);
                }

                var tasks = await query
                    .Select(t => new TaskDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        ProjectId = t.ProjectId,
                        ProjectName = t.Project != null ? t.Project.Name : null,
                        IsActive = t.IsActive
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} tasks", tasks.Count);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TaskDto>> GetTask(int id)
        {
            try
            {
                var task = await _context.Tasks
                    .Include(t => t.Project)
                    .Where(t => t.Id == id)
                    .Select(t => new TaskDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        ProjectId = t.ProjectId,
                        ProjectName = t.Project != null ? t.Project.Name : null,
                        IsActive = t.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (task == null)
                {
                    _logger.LogWarning("Task with ID {Id} not found", id);
                    return NotFound($"Task with ID {id} not found");
                }

                return Ok(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting task with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<ActionResult<TaskDto>> CreateTask(CreateTaskDto createDto)
        {
            try
            {
                var project = await _context.Projects.FindAsync(createDto.ProjectId);
                if (project == null)
                {
                    return BadRequest($"Project with ID {createDto.ProjectId} not found");
                }

                if (!project.IsActive)
                {
                    return BadRequest("Cannot create task for inactive project");
                }

                var task = new TimeTrackingAPI.Models.Task
                {
                    Name = createDto.Name,
                    ProjectId = createDto.ProjectId,
                    IsActive = createDto.IsActive
                };

                _context.Tasks.Add(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new task with ID {Id} for project ID {ProjectId}",
                    task.Id, task.ProjectId);

                var taskDto = new TaskDto
                {
                    Id = task.Id,
                    Name = task.Name,
                    ProjectId = task.ProjectId,
                    ProjectName = project.Name,
                    IsActive = task.IsActive
                };

                return CreatedAtAction(nameof(GetTask), new { id = task.Id }, taskDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, UpdateTaskDto updateDto)
        {
            try
            {
                var task = await _context.Tasks
                    .Include(t => t.Project)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task == null)
                {
                    _logger.LogWarning("Task with ID {Id} not found for update", id);
                    return NotFound($"Task with ID {id} not found");
                }

                var project = await _context.Projects.FindAsync(updateDto.ProjectId);
                if (project == null)
                {
                    return BadRequest($"Project with ID {updateDto.ProjectId} not found");
                }

                if (!project.IsActive)
                {
                    return BadRequest("Cannot assign task to inactive project");
                }

                task.Name = updateDto.Name;
                task.ProjectId = updateDto.ProjectId;
                task.IsActive = updateDto.IsActive;
                task.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Updated task with ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            try
            {
                var task = await _context.Tasks.FindAsync(id);
                if (task == null)
                {
                    _logger.LogWarning("Task with ID {Id} not found for deletion", id);
                    return NotFound($"Task with ID {id} not found");
                }

                var hasEntries = await _context.TimeEntries.AnyAsync(t => t.TaskId == id);
                if (hasEntries)
                {
                    return BadRequest("Cannot delete task with existing time entries. Deactivate the task instead.");
                }

                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted task with ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("active")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetActiveTasks()
        {
            try
            {
                var tasks = await _context.Tasks
                    .Include(t => t.Project)
                    .Where(t => t.IsActive && t.Project!.IsActive)
                    .Select(t => new TaskDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        ProjectId = t.ProjectId,
                        ProjectName = t.Project != null ? t.Project.Name : null,
                        IsActive = t.IsActive
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} active tasks", tasks.Count);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active tasks");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}