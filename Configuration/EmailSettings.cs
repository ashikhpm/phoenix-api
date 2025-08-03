namespace phoenix_sangam_api.Configuration;

public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    
    // Logo configuration
    public string LogoUrl { get; set; } = string.Empty;
    public string LogoAltText { get; set; } = "Phoenix Sangam Logo";
    public int LogoWidth { get; set; } = 200;
    public int LogoHeight { get; set; } = 60;
    public string CompanyName { get; set; } = "Phoenix Sangam";
    public string CompanyWebsite { get; set; } = string.Empty;
} 