using BARQ.Application.Interfaces;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BARQ.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SlaController : ControllerBase
{
    private readonly ISlaService _slaService;
    private readonly IEscalationService _escalationService;
    private readonly ILogger<SlaController> _logger;

    public SlaController(ISlaService slaService, IEscalationService escalationService, ILogger<SlaController> logger)
    {
        _slaService = slaService;
        _escalationService = escalationService;
        _logger = logger;
    }

    [HttpGet("policies")]
    public async Task<ActionResult<PagedResult<SlaPolicy>>> GetSlaPolicies(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _slaService.GetSlaPoliciesAsync(page, pageSize, search, cancellationToken);
        return Ok(result);
    }

    [HttpGet("policies/{id}")]
    public async Task<ActionResult<SlaPolicy>> GetSlaPolicyById(Guid id, CancellationToken cancellationToken = default)
    {
        var policy = await _slaService.GetSlaPolicyByIdAsync(id, cancellationToken);
        if (policy == null)
            return NotFound();

        return Ok(policy);
    }

    [HttpPost("policies")]
    public async Task<ActionResult<SlaPolicy>> CreateSlaPolicy([FromBody] SlaPolicy slaPolicy, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _slaService.CreateSlaPolicyAsync(slaPolicy, cancellationToken);
        return CreatedAtAction(nameof(GetSlaPolicyById), new { id = created.Id }, created);
    }

    [HttpPut("policies/{id}")]
    public async Task<ActionResult<SlaPolicy>> UpdateSlaPolicy(Guid id, [FromBody] SlaPolicy slaPolicy, CancellationToken cancellationToken = default)
    {
        if (id != slaPolicy.Id)
            return BadRequest("ID mismatch");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _slaService.UpdateSlaPolicyAsync(slaPolicy, cancellationToken);
            return Ok(updated);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpDelete("policies/{id}")]
    public async Task<IActionResult> DeleteSlaPolicy(Guid id, CancellationToken cancellationToken = default)
    {
        await _slaService.DeleteSlaPolicyAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("violations")]
    public async Task<ActionResult<PagedResult<SlaViolation>>> GetSlaViolations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _slaService.GetSlaViolationsAsync(page, pageSize, status, cancellationToken);
        return Ok(result);
    }

    [HttpGet("violations/{id}")]
    public async Task<ActionResult<SlaViolation>> GetSlaViolationById(Guid id, CancellationToken cancellationToken = default)
    {
        var violation = await _slaService.GetSlaViolationByIdAsync(id, cancellationToken);
        if (violation == null)
            return NotFound();

        return Ok(violation);
    }

    [HttpPost("violations/{id}/resolve")]
    public async Task<ActionResult<SlaViolation>> ResolveSlaViolation(Guid id, [FromBody] string resolution, CancellationToken cancellationToken = default)
    {
        var violation = await _slaService.GetSlaViolationByIdAsync(id, cancellationToken);
        if (violation == null)
            return NotFound();

        violation.Status = "Resolved";
        violation.Resolution = resolution;
        violation.ResolvedTime = DateTime.UtcNow;

        var updated = await _slaService.UpdateSlaViolationAsync(violation, cancellationToken);
        return Ok(updated);
    }

    [HttpGet("escalation-rules")]
    public async Task<ActionResult<PagedResult<EscalationRule>>> GetEscalationRules(
        [FromQuery] Guid? slaPolicyId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _escalationService.GetEscalationRulesAsync(slaPolicyId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("escalation-rules/{id}")]
    public async Task<ActionResult<EscalationRule>> GetEscalationRuleById(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _escalationService.GetEscalationRuleByIdAsync(id, cancellationToken);
        if (rule == null)
            return NotFound();

        return Ok(rule);
    }

    [HttpPost("escalation-rules")]
    public async Task<ActionResult<EscalationRule>> CreateEscalationRule([FromBody] EscalationRule rule, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _escalationService.CreateEscalationRuleAsync(rule, cancellationToken);
        return CreatedAtAction(nameof(GetEscalationRuleById), new { id = created.Id }, created);
    }

    [HttpPut("escalation-rules/{id}")]
    public async Task<ActionResult<EscalationRule>> UpdateEscalationRule(Guid id, [FromBody] EscalationRule rule, CancellationToken cancellationToken = default)
    {
        if (id != rule.Id)
            return BadRequest("ID mismatch");

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var updated = await _escalationService.UpdateEscalationRuleAsync(rule, cancellationToken);
            return Ok(updated);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpDelete("escalation-rules/{id}")]
    public async Task<IActionResult> DeleteEscalationRule(Guid id, CancellationToken cancellationToken = default)
    {
        await _escalationService.DeleteEscalationRuleAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("escalation-actions")]
    public async Task<ActionResult<PagedResult<EscalationAction>>> GetEscalationActions(
        [FromQuery] Guid? violationId = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _escalationService.GetEscalationActionsAsync(violationId, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost("escalation-actions/{id}/retry")]
    public async Task<IActionResult> RetryEscalationAction(Guid id, CancellationToken cancellationToken = default)
    {
        await _escalationService.ExecuteEscalationActionAsync(id, cancellationToken);
        return Ok();
    }

    [HttpPost("check-violations")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> CheckViolations(CancellationToken cancellationToken = default)
    {
        await _slaService.CheckAndCreateViolationsAsync(cancellationToken);
        return Ok(new { Message = "SLA violation check completed" });
    }

    [HttpPost("process-escalations")]
    [Authorize(Roles = "Administrator")]
    public async Task<IActionResult> ProcessEscalations(CancellationToken cancellationToken = default)
    {
        await _escalationService.ProcessEscalationsAsync(cancellationToken);
        return Ok(new { Message = "Escalation processing completed" });
    }
}
