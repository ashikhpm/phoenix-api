# Email Logo System Guide

## Overview
The email system now supports displaying logos in all email templates. The logo is automatically embedded in HTML emails and displayed in the header section.

## Logo Configuration

### 1. Logo File
- Place your logo file as `logo.png` in the `wwwroot/images/` folder
- The system will automatically embed this logo in all HTML emails

### 2. Configuration Settings
In `appsettings.json`, you can configure the logo settings under `EmailSettings`:

```json
"EmailSettings": {
  "SmtpServer": "smtp.gmail.com",
  "SmtpPort": 587,
  "SmtpUsername": "your-email@gmail.com",
  "SmtpPassword": "your-app-password",
  "SenderName": "Phoenix Sangam",
  "SenderEmail": "noreply@phoenixsangam.com",
  "EnableSsl": true,
  "LogoUrl": "",                    // External URL for logo (optional)
  "LogoAltText": "Phoenix Sangam Logo",
  "LogoWidth": 200,                 // Logo width in pixels
  "LogoHeight": 60,                 // Logo height in pixels
  "CompanyName": "Phoenix Sangam",
  "CompanyWebsite": ""              // Company website URL (optional)
}
```

### 3. Logo Display Options

#### Option A: Local Logo File (Recommended)
- Set `LogoUrl` to empty string `""`
- Place `logo.png` in `wwwroot/images/` folder
- The logo will be embedded directly in the email

#### Option B: External Logo URL
- Set `LogoUrl` to a public URL (e.g., `"https://yourdomain.com/logo.png"`)
- The logo will be loaded from the external URL

## Email Templates with Logo

The system includes the following email templates with logo support:

1. **Welcome Email** - Sent when a new user is created
2. **Loan Created Email** - Sent when a loan is created
3. **Loan Approved Email** - Sent when a loan request is approved
4. **Loan Rejected Email** - Sent when a loan request is rejected
5. **Payment Reminder Email** - Sent as a reminder for loan payments

## Testing the Logo

### Test Endpoint
Use the test endpoint to verify logo display:

```http
POST /api/User/test-email
Content-Type: application/json

{
  "email": "test@example.com",
  "name": "Test User"
}
```

### Manual Testing
1. Create a new user through the API
2. Check the welcome email received
3. Verify the logo appears in the email header

## Technical Implementation

### Files Modified/Created:
1. **EmailSettings.cs** - Added logo configuration properties
2. **EmailTemplateService.cs** - New service for HTML email templates
3. **EmailService.cs** - Updated to use HTML templates and embed logo
4. **Program.cs** - Added static files support
5. **appsettings.json** - Added logo configuration
6. **UserController.cs** - Added test endpoint

### Logo Embedding Process:
1. When sending HTML emails, the system checks for `logo.png` in `wwwroot/images/`
2. If found, it embeds the logo as a linked resource with Content-ID "logo"
3. The HTML template references the logo using `cid:logo`
4. Email clients display the embedded logo in the email header

## Troubleshooting

### Logo Not Displaying
1. Verify `logo.png` exists in `wwwroot/images/` folder
2. Check that `LogoUrl` is empty in appsettings.json
3. Ensure the email client supports embedded images
4. Test with the test endpoint

### Logo Size Issues
1. Adjust `LogoWidth` and `LogoHeight` in appsettings.json
2. Ensure the logo file has appropriate dimensions
3. Consider using a PNG with transparent background

### External URL Issues
1. Ensure the external URL is publicly accessible
2. Check that the URL returns a valid image
3. Consider using HTTPS URLs for better compatibility

## Best Practices

1. **Logo Format**: Use PNG format with transparent background
2. **Logo Size**: Keep logo under 200KB for better email delivery
3. **Dimensions**: Recommended size is 200x60 pixels
4. **Testing**: Always test with multiple email clients
5. **Fallback**: Provide alt text for accessibility

## Email Client Compatibility

The logo system works with most modern email clients:
- ✅ Gmail
- ✅ Outlook (desktop and web)
- ✅ Apple Mail
- ✅ Thunderbird
- ✅ Mobile email apps

Note: Some older email clients may not display embedded images, but the email will still be delivered with the logo as an attachment. 