using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Models.Responses;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        private Guid GetCurrentTenantId()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : Guid.Empty;
        }

        [HttpGet]
        public async Task<ActionResult<ApiResponse<PagedResult<TaskDto>>>> GetTasks([FromQuery] TaskListRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var result = await _taskService.GetTasksAsync(tenantId, request);
                return Ok(ApiResponse<PagedResult<TaskDto>>.SuccessResponse(result));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<PagedResult<TaskDto>>.ErrorResponse(ex.Message));
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<TaskDto>>> GetTask(Guid id)
        {
            try
            {
                var task = await _taskService.GetTaskByIdAsync(id);
                if (task == null)
                    return NotFound(ApiResponse<TaskDto>.ErrorResponse("Task not found"));

                return Ok(ApiResponse<TaskDto>.SuccessResponse(task));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<TaskDto>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<TaskDto>>> CreateTask([FromBody] CreateTaskRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var task = await _taskService.CreateTaskAsync(tenantId, request);
                return CreatedAtAction(nameof(GetTask), new { id = task.Id }, 
                    ApiResponse<TaskDto>.SuccessResponse(task, "Task created successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<TaskDto>.ErrorResponse(ex.Message));
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<TaskDto>>> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
        {
            try
            {
                var task = await _taskService.UpdateTaskAsync(id, request);
                return Ok(ApiResponse<TaskDto>.SuccessResponse(task, "Task updated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<TaskDto>.ErrorResponse(ex.Message));
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteTask(Guid id)
        {
            try
            {
                var result = await _taskService.DeleteTaskAsync(id);
                if (!result)
                    return NotFound(ApiResponse<bool>.ErrorResponse("Task not found"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Task deleted successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost("{id}/assign")]
        public async Task<ActionResult<ApiResponse<bool>>> AssignTask(Guid id, [FromBody] AssignTaskRequest request)
        {
            try
            {
                var result = await _taskService.AssignTaskAsync(id, request.UserId);
                if (!result)
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Failed to assign task"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Task assigned successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost("{id}/status")]
        public async Task<ActionResult<ApiResponse<bool>>> UpdateTaskStatus(Guid id, [FromBody] UpdateTaskStatusRequest request)
        {
            try
            {
                var result = await _taskService.UpdateTaskStatusAsync(id, request.Status);
                if (!result)
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Failed to update task status"));

                return Ok(ApiResponse<bool>.SuccessResponse(true, "Task status updated successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.ErrorResponse(ex.Message));
            }
        }

        [HttpPost("bulk-delete")]
        public async Task<ActionResult<ApiResponse<BulkOperationResult>>> BulkDeleteTasks([FromBody] BulkDeleteRequest request)
        {
            try
            {
                var result = await _taskService.BulkDeleteTasksAsync(request);
                return Ok(ApiResponse<BulkOperationResult>.SuccessResponse(result, "Bulk delete completed"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<BulkOperationResult>.ErrorResponse(ex.Message));
            }
        }
    }

    public class AssignTaskRequest
    {
        public Guid UserId { get; set; }
    }

    public class UpdateTaskStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }
}
