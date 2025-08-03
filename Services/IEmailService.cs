namespace phoenix_sangam_api.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string body, bool isHtml = false);
    Task<bool> SendUserWelcomeEmailAsync(string toEmail, string userName);
    Task<bool> SendLoanCreatedEmailAsync(string toEmail, string userName, decimal amount, string loanType, DateTime dueDate, double interestRate, decimal expectedInterest);
    Task<bool> SendLoanRequestApprovedEmailAsync(string toEmail, string userName, decimal amount, string loanType, DateTime dueDate, double interestRate, decimal expectedInterest);
    Task<bool> SendLoanRequestRejectedEmailAsync(string toEmail, string userName, decimal amount, string loanType, string reason = "");
    Task<bool> SendLoanDueReminderEmailAsync(string toEmail, string userName, decimal amount, DateTime dueDate, int daysUntilDue);
} 