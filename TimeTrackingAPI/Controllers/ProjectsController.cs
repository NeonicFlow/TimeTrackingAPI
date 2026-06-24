using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimeTrackingAPI.Data;
using TimeTrackingAPI.DTOs;
using TimeTrackingAPI.Models;

namespace TimeTrackingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProjectsController> _logger;

        public ProjectsController(ApplicationDbContext context, ILogger<ProjectsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==================== GET: api/projects ====================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProjectDto>>> GetProjects()
        {
            try
            {
                var projects = await _context.Projects
                    .Select(p => new ProjectDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Code = p.Code,
                        IsActive = p.IsActive
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} projects", projects.Count);
                return Ok(projects);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting projects");
                return StatusCode(500, "Internal server error");
            }
        }

        // ==================== GET: api/projects/{id} ====================
        [HttpGet("{id}")]
        public async Task<ActionResult<ProjectDto>> GetProject(int id)
        {
            try
            {
                var project = await _context.Projects
                    .Where(p => p.Id == id)
                    .Select(p => new ProjectDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Code = p.Code,
                        IsActive = p.IsActive
                    })
                    .FirstOrDefaultAsync();

                if (project == null)
                {
                    _logger.LogWarning("Project with ID {Id} not found", id);
                    return NotFound($"Project with ID {id} not found");
                }

                return Ok(project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting project with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // ==================== POST: api/projects ====================
        [HttpPost]
        public async Task<ActionResult<ProjectDto>> CreateProject(CreateProjectDto createDto)
        {
            try
            {
                // Проверяем уникальность кода
                var existingProject = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Code == createDto.Code);

                if (existingProject != null)
                {
                    return BadRequest($"Project with code '{createDto.Code}' already exists");
                }

                var project = new Project
                {
                    Name = createDto.Name,
                    Code = createDto.Code,
                    IsActive = createDto.IsActive
                };

                _context.Projects.Add(project);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new project with ID {Id}, Code: {Code}", project.Id, project.Code);

                var projectDto = new ProjectDto
                {
                    Id = project.Id,
                    Name = project.Name,
                    Code = project.Code,
                    IsActive = project.IsActive
                };

                return CreatedAtAction(nameof(GetProject), new { id = project.Id }, projectDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project");
                return StatusCode(500, "Internal server error");
            }
        }

        // ==================== PUT: api/projects/{id} ====================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, UpdateProjectDto updateDto)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null)
                {
                    _logger.LogWarning("Project with ID {Id} not found for update", id);
                    return NotFound($"Project with ID {id} not found");
                }

                // Проверяем уникальность кода (исключая текущий проект)
                var existingProject = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Code == updateDto.Code && p.Id != id);

                if (existingProject != null)
                {
                    return BadRequest($"Project with code '{updateDto.Code}' already exists");
                }

                // Сохраняем старое состояние для проверки
                var oldStatus = project.IsActive;

                // Обновляем проект
                project.Name = updateDto.Name;
                project.Code = updateDto.Code;
                project.IsActive = updateDto.IsActive;
                project.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // ✅ Если статус изменился, обновляем все задачи
                if (oldStatus != updateDto.IsActive)
                {
                    await UpdateTaskStatusByProject(id, updateDto.IsActive);
                }

                _logger.LogInformation("Updated project with ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating project with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // ==================== DELETE: api/projects/{id} ====================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            try
            {
                var project = await _context.Projects.FindAsync(id);
                if (project == null)
                {
                    _logger.LogWarning("Project with ID {Id} not found for deletion", id);
                    return NotFound($"Project with ID {id} not found");
                }

                // Проверяем, есть ли связанные задачи
                var hasTasks = await _context.Tasks.AnyAsync(t => t.ProjectId == id);
                if (hasTasks)
                {
                    return BadRequest("Cannot delete project with existing tasks. Deactivate the project instead.");
                }

                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted project with ID {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting project with ID {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // ==================== GET: api/projects/{id}/tasks ====================
        [HttpGet("{id}/tasks")]
        public async Task<ActionResult<IEnumerable<TaskDto>>> GetProjectTasks(int id)
        {
            try
            {
                var projectExists = await _context.Projects.AnyAsync(p => p.Id == id);
                if (!projectExists)
                {
                    return NotFound($"Project with ID {id} not found");
                }

                var tasks = await _context.Tasks
                    .Where(t => t.ProjectId == id)
                    .Select(t => new TaskDto
                    {
                        Id = t.Id,
                        Name = t.Name,
                        ProjectId = t.ProjectId,
                        ProjectName = t.Project != null ? t.Project.Name : null,
                        IsActive = t.IsActive
                    })
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} tasks for project ID {ProjectId}", tasks.Count, id);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks for project ID {ProjectId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // ==================== НОВЫЙ МЕТОД: Обновление статуса задач ====================
        /// <summary>
        /// Обновляет статус всех задач проекта при изменении статуса проекта
        /// </summary>
        private async System.Threading.Tasks.Task UpdateTaskStatusByProject(int projectId, bool isActive)
        {
            try
            {
                // Находим все задачи проекта (используем полное имя для модели Task)
                var tasks = await _context.Tasks
                    .Where(t => t.ProjectId == projectId)
                    .ToListAsync();

                if (!tasks.Any())
                {
                    _logger.LogInformation("No tasks found for project ID {ProjectId}", projectId);
                    return;
                }

                // Обновляем статус каждой задачи
                foreach (var task in tasks)
                {
                    task.IsActive = isActive;
                    task.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated {Count} tasks status to {IsActive} for project ID {ProjectId}",
                    tasks.Count,
                    isActive,
                    projectId
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tasks status for project ID {ProjectId}", projectId);
                throw;
            }
        }
    }
}