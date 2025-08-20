namespace BARQ.Application.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string htmlBody, string? textBody = null);
        Task<bool> SendEmailAsync(List<string> to, string subject, string htmlBody, string? textBody = null);
        Task<bool> SendTemplatedEmailAsync(string to, string templateName, object templateData, string? language = "en");
        Task<bool> SendTemplatedEmailAsync(List<string> to, string templateName, object templateData, string? language = "en");
        Task<string> RenderTemplateAsync(string templateName, object templateData, string? language = "en");
        Task<bool> ValidateEmailAsync(string email);
    }
}
