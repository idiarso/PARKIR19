using System;

namespace ParkIRC.Services
{
    public interface IEmailTemplateService
    {
        string GetPasswordResetTemplate(string resetLink, string userName);
    }

    public class EmailTemplateService : IEmailTemplateService
    {
        public string GetPasswordResetTemplate(string resetLink, string userName)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Reset Your Password</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #333;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
        }}
        .header {{
            background-color: #007bff;
            color: white;
            padding: 20px;
            text-align: center;
            border-radius: 5px 5px 0 0;
        }}
        .content {{
            background-color: #ffffff;
            padding: 20px;
            border: 1px solid #ddd;
            border-radius: 0 0 5px 5px;
        }}
        .button {{
            display: inline-block;
            padding: 12px 24px;
            background-color: #007bff;
            color: white;
            text-decoration: none;
            border-radius: 5px;
            margin: 20px 0;
        }}
        .footer {{
            text-align: center;
            margin-top: 20px;
            color: #666;
            font-size: 12px;
        }}
        @media only screen and (max-width: 480px) {{
            .container {{
                padding: 10px;
            }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Password Reset Request</h1>
        </div>
        <div class='content'>
            <p>Hello{(string.IsNullOrEmpty(userName) ? "" : $" {userName}")},</p>
            <p>We received a request to reset your password for your ParkIRC account. Click the button below to reset it:</p>
            
            <div style='text-align: center;'>
                <a href='{resetLink}' class='button'>Reset Password</a>
            </div>
            
            <p>If you didn't request this password reset, you can safely ignore this email. Your password will not be changed.</p>
            
            <p><strong>Important:</strong></p>
            <ul>
                <li>This link will expire in 24 hours</li>
                <li>For security, this link can only be used once</li>
                <li>If you need help, contact our support team</li>
            </ul>
        </div>
        <div class='footer'>
            <p>This is an automated message from ParkIRC. Please do not reply to this email.</p>
            <p>&copy; {DateTime.Now.Year} ParkIRC. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
} 