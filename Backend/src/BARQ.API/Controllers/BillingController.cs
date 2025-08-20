using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.DTOs.Common;
using BARQ.Core.Models.Responses;
using System.Security.Claims;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BillingController : ControllerBase
    {
        private readonly IBillingService _billingService;
        private readonly ILogger<BillingController> _logger;

        public BillingController(IBillingService billingService, ILogger<BillingController> logger)
        {
            _billingService = billingService;
            _logger = logger;
        }

        [HttpGet("plans")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<PagedResult<BillingPlanDto>>>> GetBillingPlans([FromQuery] bool includeInactive = false)
        {
            try
            {
                var plans = await _billingService.GetBillingPlansAsync(includeInactive);

                return Ok(new ApiResponse<PagedResult<BillingPlanDto>>
                {
                    Success = true,
                    Data = plans
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving billing plans");
                return StatusCode(500, new ApiResponse<PagedResult<BillingPlanDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving billing plans"
                });
            }
        }

        [HttpGet("plans/{planId}")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<BillingPlanDto>>> GetBillingPlan(Guid planId)
        {
            try
            {
                var plan = await _billingService.GetBillingPlanAsync(planId);

                if (plan == null)
                {
                    return NotFound(new ApiResponse<BillingPlanDto>
                    {
                        Success = false,
                        Message = "Billing plan not found"
                    });
                }

                return Ok(new ApiResponse<BillingPlanDto>
                {
                    Success = true,
                    Data = plan
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving billing plan {PlanId}", planId);
                return StatusCode(500, new ApiResponse<BillingPlanDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the billing plan"
                });
            }
        }

        [HttpPost("plans")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BillingPlanDto>>> CreateBillingPlan([FromBody] CreateBillingPlanRequest request)
        {
            try
            {
                var plan = await _billingService.CreateBillingPlanAsync(request);

                return CreatedAtAction(nameof(GetBillingPlan), new { planId = plan.Id }, new ApiResponse<BillingPlanDto>
                {
                    Success = true,
                    Data = plan,
                    Message = "Billing plan created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating billing plan");
                return StatusCode(500, new ApiResponse<BillingPlanDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the billing plan"
                });
            }
        }

        [HttpPut("plans/{planId}")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<ActionResult<ApiResponse<BillingPlanDto>>> UpdateBillingPlan(Guid planId, [FromBody] UpdateBillingPlanRequest request)
        {
            try
            {
                var plan = await _billingService.UpdateBillingPlanAsync(planId, request);

                if (plan == null)
                {
                    return NotFound(new ApiResponse<BillingPlanDto>
                    {
                        Success = false,
                        Message = "Billing plan not found"
                    });
                }

                return Ok(new ApiResponse<BillingPlanDto>
                {
                    Success = true,
                    Data = plan,
                    Message = "Billing plan updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating billing plan {PlanId}", planId);
                return StatusCode(500, new ApiResponse<BillingPlanDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the billing plan"
                });
            }
        }

        [HttpDelete("plans/{planId}")]
        [Authorize(Roles = "SystemAdmin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteBillingPlan(Guid planId)
        {
            try
            {
                var result = await _billingService.DeleteBillingPlanAsync(planId);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Billing plan not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Billing plan deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting billing plan {PlanId}", planId);
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while deleting the billing plan"
                });
            }
        }

        [HttpGet("subscription")]
        public async Task<ActionResult<ApiResponse<TenantSubscriptionDto>>> GetSubscription()
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (!tenantId.HasValue)
                {
                    return BadRequest(new ApiResponse<TenantSubscriptionDto>
                    {
                        Success = false,
                        Message = "Tenant context required"
                    });
                }

                var subscription = await _billingService.GetTenantSubscriptionAsync(tenantId.Value);

                return Ok(new ApiResponse<TenantSubscriptionDto>
                {
                    Success = true,
                    Data = subscription
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subscription");
                return StatusCode(500, new ApiResponse<TenantSubscriptionDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the subscription"
                });
            }
        }

        [HttpPost("subscription")]
        public async Task<ActionResult<ApiResponse<TenantSubscriptionDto>>> CreateSubscription([FromBody] CreateSubscriptionRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                var userId = GetCurrentUserId();

                if (!tenantId.HasValue)
                {
                    return BadRequest(new ApiResponse<TenantSubscriptionDto>
                    {
                        Success = false,
                        Message = "Tenant context required"
                    });
                }

                var subscription = await _billingService.CreateSubscriptionAsync(tenantId.Value, userId, request);

                return CreatedAtAction(nameof(GetSubscription), new ApiResponse<TenantSubscriptionDto>
                {
                    Success = true,
                    Data = subscription,
                    Message = "Subscription created successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<TenantSubscriptionDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<TenantSubscriptionDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subscription");
                return StatusCode(500, new ApiResponse<TenantSubscriptionDto>
                {
                    Success = false,
                    Message = "An error occurred while creating the subscription"
                });
            }
        }

        [HttpPut("subscription")]
        public async Task<ActionResult<ApiResponse<TenantSubscriptionDto>>> UpdateSubscription([FromBody] UpdateSubscriptionRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (!tenantId.HasValue)
                {
                    return BadRequest(new ApiResponse<TenantSubscriptionDto>
                    {
                        Success = false,
                        Message = "Tenant context required"
                    });
                }

                var subscription = await _billingService.UpdateSubscriptionAsync(tenantId.Value, request);

                if (subscription == null)
                {
                    return NotFound(new ApiResponse<TenantSubscriptionDto>
                    {
                        Success = false,
                        Message = "Subscription not found"
                    });
                }

                return Ok(new ApiResponse<TenantSubscriptionDto>
                {
                    Success = true,
                    Data = subscription,
                    Message = "Subscription updated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subscription");
                return StatusCode(500, new ApiResponse<TenantSubscriptionDto>
                {
                    Success = false,
                    Message = "An error occurred while updating the subscription"
                });
            }
        }

        [HttpPost("subscription/cancel")]
        public async Task<ActionResult<ApiResponse<bool>>> CancelSubscription([FromBody] CancelSubscriptionRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (!tenantId.HasValue)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Tenant context required"
                    });
                }

                var result = await _billingService.CancelSubscriptionAsync(tenantId.Value, request);

                if (!result)
                {
                    return NotFound(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Subscription not found"
                    });
                }

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = true,
                    Message = "Subscription cancelled successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling subscription");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while cancelling the subscription"
                });
            }
        }

        [HttpPost("subscription/upgrade-downgrade")]
        public async Task<ActionResult<ApiResponse<TenantSubscriptionDto>>> UpgradeDowngrade([FromBody] UpgradeDowngradeRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (!tenantId.HasValue)
                {
                    return BadRequest(new ApiResponse<TenantSubscriptionDto>
                    {
                        Success = false,
                        Message = "Tenant context required"
                    });
                }

                var subscription = await _billingService.UpgradeDowngradeAsync(tenantId.Value, request);

                if (subscription == null)
                {
                    return NotFound(new ApiResponse<TenantSubscriptionDto>
                    {
                        Success = false,
                        Message = "Subscription not found"
                    });
                }

                return Ok(new ApiResponse<TenantSubscriptionDto>
                {
                    Success = true,
                    Data = subscription,
                    Message = "Subscription plan changed successfully"
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<TenantSubscriptionDto>
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upgrading/downgrading subscription");
                return StatusCode(500, new ApiResponse<TenantSubscriptionDto>
                {
                    Success = false,
                    Message = "An error occurred while changing the subscription plan"
                });
            }
        }

        [HttpGet("quotas")]
        public async Task<ActionResult<ApiResponse<PagedResult<UsageQuotaDto>>>> GetUsageQuotas()
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (!tenantId.HasValue)
                {
                    return BadRequest(new ApiResponse<PagedResult<UsageQuotaDto>>
                    {
                        Success = false,
                        Message = "Tenant context required"
                    });
                }

                var quotas = await _billingService.GetUsageQuotasAsync(tenantId.Value);

                return Ok(new ApiResponse<PagedResult<UsageQuotaDto>>
                {
                    Success = true,
                    Data = quotas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage quotas");
                return StatusCode(500, new ApiResponse<PagedResult<UsageQuotaDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving usage quotas"
                });
            }
        }

        [HttpPost("usage")]
        public async Task<ActionResult<ApiResponse<bool>>> RecordUsage([FromBody] RecordUsageRequest request)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (!tenantId.HasValue)
                {
                    return BadRequest(new ApiResponse<bool>
                    {
                        Success = false,
                        Message = "Tenant context required"
                    });
                }

                var result = await _billingService.RecordUsageAsync(tenantId.Value, request);

                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    Data = result,
                    Message = "Usage recorded successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording usage");
                return StatusCode(500, new ApiResponse<bool>
                {
                    Success = false,
                    Message = "An error occurred while recording usage"
                });
            }
        }

        [HttpGet("invoices")]
        public async Task<ActionResult<ApiResponse<PagedResult<InvoiceDto>>>> GetInvoices([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (!tenantId.HasValue)
                {
                    return BadRequest(new ApiResponse<PagedResult<InvoiceDto>>
                    {
                        Success = false,
                        Message = "Tenant context required"
                    });
                }

                var invoices = await _billingService.GetInvoicesAsync(tenantId.Value, startDate, endDate);

                return Ok(new ApiResponse<PagedResult<InvoiceDto>>
                {
                    Success = true,
                    Data = invoices
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices");
                return StatusCode(500, new ApiResponse<PagedResult<InvoiceDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving invoices"
                });
            }
        }

        [HttpGet("invoices/{invoiceId}")]
        public async Task<ActionResult<ApiResponse<InvoiceDto>>> GetInvoice(Guid invoiceId)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (!tenantId.HasValue)
                {
                    return BadRequest(new ApiResponse<InvoiceDto>
                    {
                        Success = false,
                        Message = "Tenant context required"
                    });
                }

                var invoice = await _billingService.GetInvoiceAsync(tenantId.Value, invoiceId);

                if (invoice == null)
                {
                    return NotFound(new ApiResponse<InvoiceDto>
                    {
                        Success = false,
                        Message = "Invoice not found"
                    });
                }

                return Ok(new ApiResponse<InvoiceDto>
                {
                    Success = true,
                    Data = invoice
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice {InvoiceId}", invoiceId);
                return StatusCode(500, new ApiResponse<InvoiceDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the invoice"
                });
            }
        }

        [HttpGet("invoices/{invoiceId}/download")]
        public async Task<IActionResult> DownloadInvoice(Guid invoiceId)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (!tenantId.HasValue)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Tenant context required"
                    });
                }

                var stream = await _billingService.DownloadInvoiceAsync(tenantId.Value, invoiceId);

                if (stream == null)
                {
                    return NotFound(new ApiResponse<object>
                    {
                        Success = false,
                        Message = "Invoice not found"
                    });
                }

                return File(stream, "application/pdf", $"invoice_{invoiceId}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading invoice {InvoiceId}", invoiceId);
                return StatusCode(500, new ApiResponse<object>
                {
                    Success = false,
                    Message = "An error occurred while downloading the invoice"
                });
            }
        }

        [HttpGet("dashboard")]
        public async Task<ActionResult<ApiResponse<BillingDashboardDto>>> GetBillingDashboard()
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (!tenantId.HasValue)
                {
                    return BadRequest(new ApiResponse<BillingDashboardDto>
                    {
                        Success = false,
                        Message = "Tenant context required"
                    });
                }

                var dashboard = await _billingService.GetBillingDashboardAsync(tenantId.Value);

                return Ok(new ApiResponse<BillingDashboardDto>
                {
                    Success = true,
                    Data = dashboard
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving billing dashboard");
                return StatusCode(500, new ApiResponse<BillingDashboardDto>
                {
                    Success = false,
                    Message = "An error occurred while retrieving the billing dashboard"
                });
            }
        }

        [HttpGet("usage-history")]
        public async Task<ActionResult<ApiResponse<PagedResult<UsageRecordDto>>>> GetUsageHistory([FromQuery] string? usageType = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var tenantId = GetCurrentTenantId();
                if (!tenantId.HasValue)
                {
                    return BadRequest(new ApiResponse<PagedResult<UsageRecordDto>>
                    {
                        Success = false,
                        Message = "Tenant context required"
                    });
                }

                var usage = await _billingService.GetUsageHistoryAsync(tenantId.Value, usageType, startDate, endDate);

                return Ok(new ApiResponse<PagedResult<UsageRecordDto>>
                {
                    Success = true,
                    Data = usage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving usage history");
                return StatusCode(500, new ApiResponse<PagedResult<UsageRecordDto>>
                {
                    Success = false,
                    Message = "An error occurred while retrieving usage history"
                });
            }
        }

        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        private Guid? GetCurrentTenantId()
        {
            var tenantIdClaim = User.FindFirst("TenantId")?.Value;
            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : null;
        }
    }
}
