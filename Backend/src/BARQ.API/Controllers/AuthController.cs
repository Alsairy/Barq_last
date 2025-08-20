using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using BARQ.Application.Interfaces;
using BARQ.Core.DTOs;
using BARQ.Core.Models.Responses;

namespace BARQ.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly AuthCookieOptions _cookieOptions;

        public AuthController(IUserService userService, IOptions<AuthCookieOptions> cookieOptions)
        {
            _userService = userService;
            _cookieOptions = cookieOptions.Value;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
        {
            try
            {
                var response = await _userService.LoginAsync(request);
                
                if (_cookieOptions.Enabled && !string.IsNullOrWhiteSpace(response.Token))
                {
                    Response.Cookies.Append(_cookieOptions.Name, response.Token, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = _cookieOptions.Secure,
                        SameSite = Enum.Parse<SameSiteMode>(_cookieOptions.SameSite),
                        Path = _cookieOptions.Path
                    });
                }
                
                return Ok(ApiResponse<LoginResponse>.Ok(response, "Login successful"));
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid username or password"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<LoginResponse>.Fail(ex.Message));
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
                if (Guid.TryParse(userIdClaim, out var userId))
                {
                    await _userService.LogoutAsync(userId);
                }
                
                if (_cookieOptions.Enabled)
                {
                    Response.Cookies.Delete(_cookieOptions.Name, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = _cookieOptions.Secure,
                        SameSite = Enum.Parse<SameSiteMode>(_cookieOptions.SameSite),
                        Path = _cookieOptions.Path
                    });
                }
                
                return Ok(ApiResponse<bool>.Ok(true, "Logout successful"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.Fail(ex.Message));
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<bool>>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(ApiResponse<bool>.Fail("Invalid user"));
                }

                var result = await _userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword);
                if (!result)
                    return BadRequest(ApiResponse<bool>.Fail("Failed to change password"));

                return Ok(ApiResponse<bool>.Ok(true, "Password changed successfully"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<bool>.Fail(ex.Message));
            }
        }

        [HttpPost("rotate-token")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<LoginResponse>>> RotateToken()
        {
            try
            {
                var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst("id")?.Value;
                if (!Guid.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(ApiResponse<LoginResponse>.Fail("Invalid user"));
                }

                return BadRequest(ApiResponse<LoginResponse>.Fail("Refresh token functionality not yet implemented"));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<LoginResponse>.Fail(ex.Message));
            }
        }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
