using phoenix_sangam_api.Configuration;

namespace phoenix_sangam_api.Services;

public class EmailTemplateService
{
    private readonly EmailSettings _emailSettings;

    public EmailTemplateService(EmailSettings emailSettings)
    {
        _emailSettings = emailSettings;
    }

    private string GetLogoHtml()
    {
        if (string.IsNullOrEmpty(_emailSettings.LogoUrl))
        {
            // Use the local logo file
            return $@"<img src=""cid:logo"" alt=""{_emailSettings.LogoAltText}"" width=""{_emailSettings.LogoWidth}"" height=""{_emailSettings.LogoHeight}"" style=""display: block; margin: 0 auto;"" />";
        }
        
        return $@"<img src=""{_emailSettings.LogoUrl}"" alt=""{_emailSettings.LogoAltText}"" width=""{_emailSettings.LogoWidth}"" height=""{_emailSettings.LogoHeight}"" style=""display: block; margin: 0 auto;"" />";
    }

    private string GetEmailHeader()
    {
        return $@"
            <div style=""background-color: #f8f9fa; padding: 20px; text-align: center; border-bottom: 3px solid #007bff;"">
                {GetLogoHtml()}
                <h1 style=""color: #007bff; margin: 10px 0; font-family: Arial, sans-serif;"">{_emailSettings.CompanyName}</h1>
            </div>";
    }

    private string GetEmailFooter()
    {
        var websiteLink = string.IsNullOrEmpty(_emailSettings.CompanyWebsite) 
            ? "" 
            : $@"<p style=""margin: 10px 0;""><a href=""{_emailSettings.CompanyWebsite}"" style=""color: #007bff; text-decoration: none;"">Visit our website</a></p>";
        
        return $@"
            <div style=""background-color: #f8f9fa; padding: 20px; text-align: center; border-top: 3px solid #007bff; margin-top: 30px;"">
                <p style=""color: #666; margin: 5px 0; font-family: Arial, sans-serif;"">Best regards,</p>
                <p style=""color: #007bff; margin: 5px 0; font-weight: bold; font-family: Arial, sans-serif;"">{_emailSettings.CompanyName} Team</p>
                {websiteLink}
                <p style=""color: #999; font-size: 12px; margin: 10px 0; font-family: Arial, sans-serif;"">This is an automated message. Please do not reply to this email.</p>
            </div>";
    }

    public string CreateWelcomeEmailTemplate(string userName)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset=""utf-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Welcome to {_emailSettings.CompanyName}</title>
            </head>
            <body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #ffffff;"">
                <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff;"">
                    {GetEmailHeader()}
                    
                    <div style=""padding: 30px 20px; background-color: #ffffff;"">
                        <h2 style=""color: #333; margin-bottom: 20px; font-family: Arial, sans-serif;"">Welcome to {_emailSettings.CompanyName}!</h2>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 15px; font-family: Arial, sans-serif;"">
                            Dear <strong>{userName}</strong>,
                        </p>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 15px; font-family: Arial, sans-serif;"">
                            Welcome to {_emailSettings.CompanyName}! Your account has been successfully created.
                        </p>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 15px; font-family: Arial, sans-serif;"">
                            We're excited to have you as a member of our community. You can now access all the features of our platform.
                        </p>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 15px; font-family: Arial, sans-serif;"">
                            If you have any questions or need assistance, please don't hesitate to contact us.
                        </p>
                    </div>
                    
                    {GetEmailFooter()}
                </div>
            </body>
            </html>";
    }

    public string CreateLoanCreatedEmailTemplate(string userName, decimal amount, string loanType, DateTime dueDate, double interestRate, decimal expectedInterest)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset=""utf-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Loan Created Successfully</title>
            </head>
            <body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #ffffff;"">
                <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff;"">
                    {GetEmailHeader()}
                    
                    <div style=""padding: 30px 20px; background-color: #ffffff;"">
                        <h2 style=""color: #333; margin-bottom: 20px; font-family: Arial, sans-serif;"">Loan Created Successfully</h2>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 15px; font-family: Arial, sans-serif;"">
                            Dear <strong>{userName}</strong>,
                        </p>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 20px; font-family: Arial, sans-serif;"">
                            Your loan has been successfully created with the following details:
                        </p>
                        
                        <div style=""background-color: #f8f9fa; padding: 20px; border-radius: 5px; margin-bottom: 20px;"">
                            <table style=""width: 100%; border-collapse: collapse;"">
                                <tr>
                                    <td style=""padding: 8px 0; color: #333; font-weight: bold;"">Loan Type:</td>
                                    <td style=""padding: 8px 0; color: #555;"">{loanType}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 8px 0; color: #333; font-weight: bold;"">Amount:</td>
                                    <td style=""padding: 8px 0; color: #555;"">₹{amount:N2}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 8px 0; color: #333; font-weight: bold;"">Due Date:</td>
                                    <td style=""padding: 8px 0; color: #555;"">{dueDate:dd/MM/yyyy}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 8px 0; color: #333; font-weight: bold;"">Interest Rate:</td>
                                    <td style=""padding: 8px 0; color: #555;"">{interestRate}% per month</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 8px 0; color: #333; font-weight: bold;"">Expected Interest:</td>
                                    <td style=""padding: 8px 0; color: #555;"">₹{expectedInterest:N2}</td>
                                </tr>
                            </table>
                        </div>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 15px; font-family: Arial, sans-serif;"">
                            Your loan is now active and you can track its status through your account.
                        </p>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 15px; font-family: Arial, sans-serif;"">
                            Please ensure timely repayment to maintain a good standing.
                        </p>
                    </div>
                    
                    {GetEmailFooter()}
                </div>
            </body>
            </html>";
    }

    public string CreateLoanApprovedEmailTemplate(string userName, decimal amount, string loanType, DateTime dueDate, double interestRate, decimal expectedInterest)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset=""utf-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Loan Request Approved</title>
            </head>
            <body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #ffffff;"">
                <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff;"">
                    {GetEmailHeader()}
                    
                    <div style=""padding: 30px 20px; background-color: #ffffff;"">
                        <h2 style=""color: #28a745; margin-bottom: 20px; font-family: Arial, sans-serif;"">🎉 Loan Request Approved!</h2>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 15px; font-family: Arial, sans-serif;"">
                            Dear <strong>{userName}</strong>,
                        </p>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 20px; font-family: Arial, sans-serif;"">
                            Great news! Your loan request has been approved.
                        </p>
                        
                        <div style=""background-color: #d4edda; padding: 20px; border-radius: 5px; margin-bottom: 20px; border-left: 4px solid #28a745;"">
                            <h3 style=""color: #155724; margin-top: 0; font-family: Arial, sans-serif;"">Loan Details:</h3>
                            <table style=""width: 100%; border-collapse: collapse;"">
                                <tr>
                                    <td style=""padding: 8px 0; color: #155724; font-weight: bold;"">Loan Type:</td>
                                    <td style=""padding: 8px 0; color: #155724;"">{loanType}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 8px 0; color: #155724; font-weight: bold;"">Amount:</td>
                                    <td style=""padding: 8px 0; color: #155724;"">₹{amount:N2}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 8px 0; color: #155724; font-weight: bold;"">Due Date:</td>
                                    <td style=""padding: 8px 0; color: #155724;"">{dueDate:dd/MM/yyyy}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 8px 0; color: #155724; font-weight: bold;"">Interest Rate:</td>
                                    <td style=""padding: 8px 0; color: #155724;"">{interestRate}% per month</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 8px 0; color: #155724; font-weight: bold;"">Expected Interest:</td>
                                    <td style=""padding: 8px 0; color: #155724;"">₹{expectedInterest:N2}</td>
                                </tr>
                            </table>
                        </div>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 15px; font-family: Arial, sans-serif;"">
                            Your loan is now active and you can access the funds. Please ensure timely repayment.
                        </p>
                    </div>
                    
                    {GetEmailFooter()}
                </div>
            </body>
            </html>";
    }

    public string CreateLoanRejectedEmailTemplate(string userName, decimal amount, string loanType, string reason = "")
    {
        var reasonSection = string.IsNullOrEmpty(reason) 
            ? "" 
            : $@"
                            <tr>
                                <td style=""padding: 8px 0; color: #721c24; font-weight: bold;"">Reason:</td>
                                <td style=""padding: 8px 0; color: #721c24;"">{reason}</td>
                            </tr>";

        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset=""utf-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Loan Request Status Update</title>
            </head>
            <body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #ffffff;"">
                <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff;"">
                    {GetEmailHeader()}
                    
                    <div style=""padding: 30px 20px; background-color: #ffffff;"">
                        <h2 style=""color: #dc3545; margin-bottom: 20px; font-family: Arial, sans-serif;"">Loan Request Status Update</h2>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 15px; font-family: Arial, sans-serif;"">
                            Dear <strong>{userName}</strong>,
                        </p>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 20px; font-family: Arial, sans-serif;"">
                            We regret to inform you that your loan request has been rejected.
                        </p>
                        
                        <div style=""background-color: #f8d7da; padding: 20px; border-radius: 5px; margin-bottom: 20px; border-left: 4px solid #dc3545;"">
                            <h3 style=""color: #721c24; margin-top: 0; font-family: Arial, sans-serif;"">Loan Details:</h3>
                            <table style=""width: 100%; border-collapse: collapse;"">
                                <tr>
                                    <td style=""padding: 8px 0; color: #721c24; font-weight: bold;"">Loan Type:</td>
                                    <td style=""padding: 8px 0; color: #721c24;"">{loanType}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 8px 0; color: #721c24; font-weight: bold;"">Amount:</td>
                                    <td style=""padding: 8px 0; color: #721c24;"">₹{amount:N2}</td>
                                </tr>{reasonSection}
                            </table>
                        </div>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 15px; font-family: Arial, sans-serif;"">
                            You may apply for a new loan request in the future. Please ensure all required information is provided accurately.
                        </p>
                    </div>
                    
                    {GetEmailFooter()}
                </div>
            </body>
            </html>";
    }

    public string CreateLoanDueReminderEmailTemplate(string userName, decimal amount, DateTime dueDate, int daysUntilDue)
    {
        return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset=""utf-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Loan Payment Reminder</title>
            </head>
            <body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #ffffff;"">
                <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff;"">
                    {GetEmailHeader()}
                    
                    <div style=""padding: 30px 20px; background-color: #ffffff;"">
                        <h2 style=""color: #ffc107; margin-bottom: 20px; font-family: Arial, sans-serif;"">⚠️ Payment Reminder</h2>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 15px; font-family: Arial, sans-serif;"">
                            Dear <strong>{userName}</strong>,
                        </p>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 20px; font-family: Arial, sans-serif;"">
                            This is a friendly reminder that your loan payment is due soon.
                        </p>
                        
                        <div style=""background-color: #fff3cd; padding: 20px; border-radius: 5px; margin-bottom: 20px; border-left: 4px solid #ffc107;"">
                            <h3 style=""color: #856404; margin-top: 0; font-family: Arial, sans-serif;"">Payment Details:</h3>
                            <table style=""width: 100%; border-collapse: collapse;"">
                                <tr>
                                    <td style=""padding: 8px 0; color: #856404; font-weight: bold;"">Amount:</td>
                                    <td style=""padding: 8px 0; color: #856404;"">₹{amount:N2}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 8px 0; color: #856404; font-weight: bold;"">Due Date:</td>
                                    <td style=""padding: 8px 0; color: #856404;"">{dueDate:dd/MM/yyyy}</td>
                                </tr>
                                <tr>
                                    <td style=""padding: 8px 0; color: #856404; font-weight: bold;"">Days Remaining:</td>
                                    <td style=""padding: 8px 0; color: #856404;"">{daysUntilDue} day{(daysUntilDue == 1 ? "" : "s")}</td>
                                </tr>
                            </table>
                        </div>
                        
                        <p style=""color: #555; line-height: 1.6; margin-bottom: 15px; font-family: Arial, sans-serif;"">
                            Please ensure timely payment to avoid any late fees or penalties.
                        </p>
                    </div>
                    
                    {GetEmailFooter()}
                </div>
            </body>
            </html>";
    }
} 