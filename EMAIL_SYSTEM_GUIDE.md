# Email System Guide

## Overview

The Phoenix Sangam API now includes a comprehensive email notification system that automatically sends emails for various events. The system uses MailKit for SMTP email delivery and is fully integrated with the application's business logic.

## Email Configuration

### AppSettings Configuration

```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "SenderName": "Phoenix Sangam",
    "SenderEmail": "noreply@phoenixsangam.com",
    "EnableSsl": true
  }
}
```

### Gmail Setup Instructions

1. **Enable 2-Factor Authentication** on your Gmail account
2. **Generate App Password**:
   - Go to Google Account settings
   - Security → 2-Step Verification → App passwords
   - Generate a new app password for "Mail"
3. **Update Configuration**:
   - Replace `your-email@gmail.com` with your Gmail address
   - Replace `your-app-password` with the generated app password

## Email Events

### 1. User Registration Welcome Email
- **Trigger**: When a new user is created
- **Endpoint**: `POST /api/user`
- **Email Type**: Welcome email with account details
- **Template**: Personalized welcome message

### 2. Loan Creation Notification
- **Trigger**: When a new loan is created
- **Endpoint**: `POST /api/loan`
- **Email Type**: Loan confirmation with details
- **Template**: Loan amount, type, due date, and terms

### 3. Loan Request Approval
- **Trigger**: When a loan request is approved
- **Endpoint**: `PUT /api/dashboard/loan-requests/{id}/action`
- **Email Type**: Approval notification
- **Template**: Loan details and next steps

### 4. Loan Request Rejection
- **Trigger**: When a loan request is rejected
- **Endpoint**: `PUT /api/dashboard/loan-requests/{id}/action`
- **Email Type**: Rejection notification
- **Template**: Rejection reason and future application guidance

### 5. Loan Due Reminder (Background Job)
- **Trigger**: Weekly background job (every Saturday at 9:00 AM)
- **Condition**: Loans due within 5 days
- **Email Type**: Payment reminder
- **Template**: Due date, amount, and days remaining

## Email Templates

### Welcome Email
```
Subject: Welcome to Phoenix Sangam!

Dear {UserName},

Welcome to Phoenix Sangam! Your account has been successfully created.

We're excited to have you as a member of our community. You can now access all the features of our platform.

If you have any questions or need assistance, please don't hesitate to contact us.

Best regards,
Phoenix Sangam Team
```

### Loan Created Email
```
Subject: Loan Created Successfully

Dear {UserName},

Your loan has been successfully created with the following details:

Loan Type: {LoanType}
Amount: ₹{Amount}
Due Date: {DueDate}
Interest Rate: {InterestRate}% per month
Expected Interest: ₹{ExpectedInterest}

Your loan is now active and you can track its status through your account.

Please ensure timely repayment to maintain a good standing.

Best regards,
Phoenix Sangam Team
```

### Loan Request Approved Email
```
Subject: Loan Request Approved

Dear {UserName},

Great news! Your loan request has been approved.

Loan Details:
- Loan Type: {LoanType}
- Amount: ₹{Amount}
- Due Date: {DueDate}
- Interest Rate: {InterestRate}% per month
- Expected Interest: ₹{ExpectedInterest}

Your loan is now active and you can access the funds. Please ensure timely repayment.

Best regards,
Phoenix Sangam Team
```

### Loan Request Rejected Email
```
Subject: Loan Request Status Update

Dear {UserName},

We regret to inform you that your loan request has been rejected.

Loan Details:
- Loan Type: {LoanType}
- Amount: ₹{Amount}
- Reason: {Reason}

You may apply for a new loan request in the future. Please ensure all required information is provided accurately.

Best regards,
Phoenix Sangam Team
```

### Loan Due Reminder Email
```
Subject: Loan Payment Reminder - Due in {Days} day(s)

Dear {UserName},

This is a friendly reminder that your loan payment is due soon.

Loan Details:
- Amount: ₹{Amount}
- Due Date: {DueDate}
- Days Remaining: {DaysUntilDue}

Please ensure timely payment to avoid any late fees or penalties.

Best regards,
Phoenix Sangam Team
```

## Background Job Email System

### Weekly Job Schedule
- **Frequency**: Every Saturday at 9:00 AM
- **Tasks**:
  1. Check overdue loans
  2. Generate weekly reports
  3. Clean up old data
  4. **Send loan due reminders** (new feature)

### Loan Due Reminder Logic
```csharp
// Find loans due within 5 days
var fiveDaysFromNow = DateTime.Today.AddDays(5);
var loansDueSoon = await _context.Loans
    .Include(l => l.User)
    .Include(l => l.LoanType)
    .Where(l => l.DueDate.Date <= fiveDaysFromNow && 
               l.DueDate.Date >= DateTime.Today &&
               l.ClosedDate == null && 
               l.Status.ToLower() != "closed")
    .ToListAsync();
```

## Error Handling

### Email Failure Handling
- **Non-blocking**: Email failures don't prevent main operations
- **Logging**: All email attempts are logged with success/failure status
- **Graceful Degradation**: Application continues to function even if emails fail

### Example Error Handling
```csharp
try
{
    var emailSent = await _emailService.SendUserWelcomeEmailAsync(user.Email, user.Name);
    if (emailSent)
    {
        _logger.LogInformation("Welcome email sent successfully to {Email}", user.Email);
    }
    else
    {
        _logger.LogWarning("Failed to send welcome email to {Email}", user.Email);
    }
}
catch (Exception emailEx)
{
    _logger.LogError(emailEx, "Error sending welcome email to {Email}", user.Email);
    // Don't fail the user creation if email fails
}
```

## Email Service Interface

### IEmailService Methods
```csharp
public interface IEmailService
{
    Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string body, bool isHtml = false);
    Task<bool> SendUserWelcomeEmailAsync(string toEmail, string userName);
    Task<bool> SendLoanCreatedEmailAsync(string toEmail, string userName, decimal amount, string loanType, DateTime dueDate, double interestRate, decimal expectedInterest);
    Task<bool> SendLoanRequestApprovedEmailAsync(string toEmail, string userName, decimal amount, string loanType, DateTime dueDate, double interestRate, decimal expectedInterest);
    Task<bool> SendLoanRequestRejectedEmailAsync(string toEmail, string userName, decimal amount, string loanType, string reason = "");
    Task<bool> SendLoanDueReminderEmailAsync(string toEmail, string userName, decimal amount, DateTime dueDate, int daysUntilDue);
}
```

## Configuration Management

### EmailSettings Class
```csharp
public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}
```

### Service Registration
```csharp
// Configure email settings
var emailSettings = builder.Configuration.GetSection("EmailSettings").Get<EmailSettings>();
if (emailSettings == null)
{
    // Fallback to default settings if configuration is missing
    emailSettings = new EmailSettings { /* default values */ };
}
builder.Services.AddSingleton(emailSettings);
builder.Services.AddScoped<IEmailService, EmailService>();
```

## Testing Email System

### 1. Test Email Configuration
```bash
# Check if email settings are loaded correctly
curl -X GET "http://localhost:5276/api/backgroundjob/jobs" \
  -H "Authorization: Bearer your-token"
```

### 2. Test User Creation Email
```bash
# Create a new user (will trigger welcome email)
curl -X POST "http://localhost:5276/api/user" \
  -H "Authorization: Bearer your-token" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "email": "test@example.com",
    "address": "Test Address",
    "phone": "1234567890"
  }'
```

### 3. Test Loan Creation Email
```bash
# Create a new loan (will trigger loan created email)
curl -X POST "http://localhost:5276/api/loan" \
  -H "Authorization: Bearer your-token" \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "date": "2024-01-01",
    "dueDate": "2024-02-01",
    "loanTypeId": 1,
    "amount": 10000,
    "loanTerm": 12,
    "chequeNumber": "CHQ001"
  }'
```

### 4. Test Background Job Email
```bash
# Manually trigger the weekly job (will send due reminders)
curl -X POST "http://localhost:5276/api/backgroundjob/trigger-weekly" \
  -H "Authorization: Bearer your-token"
```

## Monitoring and Logs

### Email Log Messages
```
info: EmailService[0]
      Email sent successfully to user@example.com with subject: Welcome to Phoenix Sangam!

warn: EmailService[0]
      Failed to send welcome email to user@example.com

error: EmailService[0]
      Error sending email to user@example.com with subject: Welcome to Phoenix Sangam!
```

### Background Job Email Logs
```
info: WeeklyJob[0]
      Found 3 loans due within 5 days

info: WeeklyJob[0]
      Due reminder email sent to user@example.com for loan 123

warn: WeeklyJob[0]
      Failed to send due reminder email to user@example.com for loan 123
```

## Security Considerations

### Email Security
- **SSL/TLS**: All emails sent over encrypted connections
- **App Passwords**: Uses Gmail app passwords instead of regular passwords
- **No Sensitive Data**: Emails don't contain sensitive financial information
- **Rate Limiting**: Built-in error handling prevents email spam

### Configuration Security
- **Environment Variables**: Consider using environment variables for sensitive data
- **Secrets Management**: Use Azure Key Vault or similar for production
- **Access Control**: Email service only accessible to authorized operations

## Troubleshooting

### Common Issues

1. **Email Not Sending**
   - Check SMTP configuration in appsettings.json
   - Verify Gmail app password is correct
   - Check firewall/network connectivity
   - Review application logs for specific errors

2. **Authentication Failed**
   - Ensure 2-factor authentication is enabled
   - Verify app password is generated correctly
   - Check username (email) is correct

3. **Background Job Not Running**
   - Check if Quartz.NET scheduler is started
   - Verify job is scheduled correctly
   - Review weekly job logs

4. **Email Templates Not Working**
   - Check string interpolation syntax
   - Verify all required parameters are passed
   - Review email service implementation

### Debug Steps

1. **Check Email Configuration**
   ```bash
   # Verify email settings are loaded
   curl -X GET "http://localhost:5276/api/backgroundjob/jobs"
   ```

2. **Test Email Service**
   ```bash
   # Create a test user to trigger welcome email
   curl -X POST "http://localhost:5276/api/user" -H "Content-Type: application/json" -d '{"name":"Test","email":"test@example.com"}'
   ```

3. **Review Application Logs**
   ```bash
   # Check for email-related log messages
   tail -f logs/application.log | grep -i email
   ```

## Best Practices

### 1. Email Content
- Keep emails concise and professional
- Include all relevant information
- Use clear call-to-action when needed
- Maintain consistent branding

### 2. Error Handling
- Never fail main operations due to email failures
- Log all email attempts for monitoring
- Implement retry logic for critical emails

### 3. Performance
- Send emails asynchronously
- Don't block user operations for email sending
- Use background jobs for bulk email operations

### 4. Monitoring
- Monitor email delivery success rates
- Track email open rates (if possible)
- Set up alerts for email service failures

## Future Enhancements

### 1. HTML Email Templates
- Implement rich HTML email templates
- Add branding and styling
- Include clickable links and buttons

### 2. Email Preferences
- Allow users to opt-out of certain email types
- Implement email frequency controls
- Add email preference management

### 3. Advanced Notifications
- SMS notifications for critical alerts
- Push notifications for mobile apps
- In-app notification system

### 4. Email Analytics
- Track email delivery and open rates
- Implement email engagement metrics
- Add email performance reporting 