using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using phoenix_sangam_api.Configuration;

namespace phoenix_sangam_api.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;
    private readonly EmailTemplateService _emailTemplateService;

    public EmailService(EmailSettings emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings;
        _logger = logger;
        _emailTemplateService = new EmailTemplateService(emailSettings);
    }

    public async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string body, bool isHtml = false)
    {
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            email.To.Add(new MailboxAddress(toName, toEmail));
            email.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
            {
                bodyBuilder.HtmlBody = body;
                
                // Add logo as embedded image if it exists and no external URL is provided
                if (string.IsNullOrEmpty(_emailSettings.LogoUrl))
                {
                    var logoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "logo.png");
                    if (File.Exists(logoPath))
                    {
                        var logo = bodyBuilder.LinkedResources.Add(logoPath);
                        logo.ContentId = "logo";
                    }
                }
            }
            else
            {
                bodyBuilder.TextBody = body;
            }
            email.Body = bodyBuilder.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
            await smtp.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent successfully to {ToEmail} with subject: {Subject}", toEmail, subject);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {ToEmail} with subject: {Subject}", toEmail, subject);
            return false;
        }
    }

    public async Task<bool> SendUserWelcomeEmailAsync(string toEmail, string userName)
    {
        var subject = "Welcome to Phoenix Sangam!";
        var body = _emailTemplateService.CreateWelcomeEmailTemplate(userName);

        return await SendEmailAsync(toEmail, userName, subject, body, true);
    }

    public async Task<bool> SendLoanCreatedEmailAsync(string toEmail, string userName, decimal amount, string loanType, DateTime dueDate, double interestRate, decimal expectedInterest)
    {
        var subject = "Loan Created Successfully";
        var body = _emailTemplateService.CreateLoanCreatedEmailTemplate(userName, amount, loanType, dueDate, interestRate, expectedInterest);

        return await SendEmailAsync(toEmail, userName, subject, body, true);
    }

    public async Task<bool> SendLoanRequestApprovedEmailAsync(string toEmail, string userName, decimal amount, string loanType, DateTime dueDate, double interestRate, decimal expectedInterest)
    {
        var subject = "Loan Request Approved";
        var body = _emailTemplateService.CreateLoanApprovedEmailTemplate(userName, amount, loanType, dueDate, interestRate, expectedInterest);

        return await SendEmailAsync(toEmail, userName, subject, body, true);
    }

    public async Task<bool> SendLoanRequestRejectedEmailAsync(string toEmail, string userName, decimal amount, string loanType, string reason = "")
    {
        var subject = "Loan Request Status Update";
        var body = _emailTemplateService.CreateLoanRejectedEmailTemplate(userName, amount, loanType, reason);

        return await SendEmailAsync(toEmail, userName, subject, body, true);
    }

    public async Task<bool> SendLoanDueReminderEmailAsync(string toEmail, string userName, decimal amount, DateTime dueDate, int daysUntilDue)
    {
        var subject = $"Loan Payment Reminder - Due in {daysUntilDue} day{(daysUntilDue == 1 ? "" : "s")}";
        var body = _emailTemplateService.CreateLoanDueReminderEmailTemplate(userName, amount, dueDate, daysUntilDue);

        return await SendEmailAsync(toEmail, userName, subject, body, true);
    }
} 