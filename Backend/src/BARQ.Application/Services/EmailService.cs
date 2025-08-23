using BARQ.Application.Interfaces;
using BARQ.Core.Entities;
using BARQ.Core.Services;
using BARQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;
using System.Text.Json;

namespace BARQ.Application.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        private readonly BarqDbContext _context;
        private readonly ITenantProvider _tenantProvider;
        private readonly SmtpClient _smtpClient;

        public EmailService(ILogger<EmailService> logger, IConfiguration configuration, BarqDbContext context, ITenantProvider tenantProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
            _tenantProvider = tenantProvider;
            
            _smtpClient = new SmtpClient
            {
                Host = _configuration["Email:SmtpHost"] ?? "localhost",
                Port = int.Parse(_configuration["Email:SmtpPort"] ?? "587"),
                EnableSsl = bool.Parse(_configuration["Email:EnableSsl"] ?? "true"),
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    _configuration["Email:Username"] ?? "",
                    _configuration["Email:Password"] ?? ""
                )
            };
        }

        public async System.Threading.Tasks.Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null)
        {
            return await SendEmailAsync(new List<string> { to }, subject, htmlBody, textBody);
        }

        public async System.Threading.Tasks.Task<bool> SendEmailAsync(List<string> to, string subject, string htmlBody, string? textBody = null)
        {
            try
            {
                var fromAddress = _configuration["Email:FromAddress"] ?? "noreply@barq.ai";
                var fromName = _configuration["Email:FromName"] ?? "BARQ Platform";

                using var message = new MailMessage();
                message.From = new MailAddress(fromAddress, fromName);
                message.Subject = subject;
                message.Body = htmlBody;
                message.IsBodyHtml = true;

                if (!string.IsNullOrEmpty(textBody))
                {
                    var textView = AlternateView.CreateAlternateViewFromString(textBody, null, "text/plain");
                    message.AlternateViews.Add(textView);
                }

                foreach (var recipient in to)
                {
                    if (await ValidateEmailAsync(recipient))
                    {
                        message.To.Add(recipient);
                    }
                }

                if (message.To.Count == 0)
                {
                    _logger.LogWarning("No valid recipients found for email: {Subject}", subject);
                    return false;
                }

                await _smtpClient.SendMailAsync(message);
                _logger.LogInformation("Email sent successfully to {Recipients}: {Subject}", 
                    string.Join(", ", message.To.Select(t => t.Address)), subject);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipients}: {Subject}", 
                    string.Join(", ", to), subject);
                return false;
            }
        }

        public async System.Threading.Tasks.Task<bool> SendTemplatedEmailAsync(string to, string templateName, object templateData, string? language = "en")
        {
            return await SendTemplatedEmailAsync(new List<string> { to }, templateName, templateData, language);
        }

        public async System.Threading.Tasks.Task<bool> SendTemplatedEmailAsync(List<string> to, string templateName, object templateData, string? language = "en")
        {
            try
            {
                var template = await _context.EmailTemplates
                    .Where(t => t.TenantId == _tenantProvider.GetTenantId())
                    .FirstOrDefaultAsync(t => t.Name == templateName && t.Language == language && t.IsActive);

                if (template == null)
                {
                    _logger.LogWarning("Email template not found: {TemplateName} ({Language})", templateName, language);
                    return false;
                }

                var subject = await RenderTemplateContentAsync(template.Subject, templateData);
                var htmlBody = await RenderTemplateContentAsync(template.HtmlBody, templateData);
                var textBody = !string.IsNullOrEmpty(template.TextBody) 
                    ? await RenderTemplateContentAsync(template.TextBody, templateData) 
                    : null;

                return await SendEmailAsync(to, subject, htmlBody, textBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send templated email: {TemplateName}", templateName);
                return false;
            }
        }

        public async System.Threading.Tasks.Task<string> RenderTemplateAsync(string templateName, object templateData, string? language = "en")
        {
            var template = await _context.EmailTemplates
                .Where(t => t.TenantId == _tenantProvider.GetTenantId())
                .FirstOrDefaultAsync(t => t.Name == templateName && t.Language == language && t.IsActive);

            if (template == null)
            {
                throw new ArgumentException($"Email template not found: {templateName} ({language})");
            }

            return await RenderTemplateContentAsync(template.HtmlBody, templateData);
        }

        public System.Threading.Tasks.Task<bool> ValidateEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return System.Threading.Tasks.Task.FromResult(false);

            var trimmed = email.Trim();
            try
            {
                var addr = new MailAddress(trimmed);
                return System.Threading.Tasks.Task.FromResult(string.Equals(addr.Address, trimmed, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return System.Threading.Tasks.Task.FromResult(false);
            }
        }

        private System.Threading.Tasks.Task<string> RenderTemplateContentAsync(string template, object data)
        {
            var result = template;
            var properties = data.GetType().GetProperties();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(data)?.ToString() ?? "";
                result = result.Replace($"{{{{{prop.Name}}}}}", value);
            }

            return System.Threading.Tasks.Task.FromResult(result);
        }

        public void Dispose()
        {
            _smtpClient?.Dispose();
        }
    }
}
