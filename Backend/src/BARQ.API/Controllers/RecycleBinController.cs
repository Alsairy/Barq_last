using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BARQ.Core.Models.Responses;
using BARQ.API.Controllers;
using BARQ.Application.Services.RecycleBin;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/recycle-bin")]
    [Authorize]
    public class RecycleBinController : ControllerBase
    {
        private readonly IRecycleBinService _svc;
        public RecycleBinController(IRecycleBinService svc) => _svc = svc;

        [HttpGet]
        public async Task<IActionResult> List([FromQuery] string entity, [FromQuery] int page = 1, [FromQuery] int pageSize = 25)
        {
            var result = await _svc.ListDeletedAsync(entity, page, pageSize);
            return Ok(ApiResponse<object>.Ok(result, "Recycle bin list fetched"));
        }

        [HttpPost("{entity}/{id:guid}/restore")]
        public async Task<IActionResult> Restore([FromRoute] string entity, [FromRoute] Guid id)
        {
            var ok = await _svc.RestoreAsync(entity, id);
            if (!ok) return NotFound(ApiResponse<object>.Fail("Item not found or cannot be restored"));
            return Ok(ApiResponse<object>.Ok(null, "Restored"));
        }
    }
}
