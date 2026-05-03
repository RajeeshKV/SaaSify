# Brevo Email Setup Guide

This guide explains how to configure Brevo email templates and environment variables for the SaaSify application.

## Environment Variables Configuration

You can configure the Brevo settings using environment variables instead of appsettings.json. This is recommended for production deployments.

### Required Environment Variables

```bash
# Brevo API Configuration
BREVO__ApiKey=your_brevo_api_key_here
BREVO__BaseUrl=https://api.brevo.com/v3

# Brevo Template IDs (get these from Brevo after creating templates)
BREVO__Templates__EmailVerification=1
BREVO__Templates__UserInvitation=2
BREVO__Templates__WelcomeEmail=3

# Frontend URL for email links
FRONTEND__Url=https://your-frontend-domain.com
```

### Docker Environment Variables

```dockerfile
# Add to your Dockerfile or docker-compose.yml
ENV BREVO__ApiKey=${BREVO_API_KEY}
ENV BREVO__BaseUrl=${BREVO_BASE_URL:-https://api.brevo.com/v3}
ENV BREVO__Templates__EmailVerification=${BREVO_EMAIL_VERIFICATION_TEMPLATE_ID}
ENV BREVO__Templates__UserInvitation=${BREVO_USER_INVITATION_TEMPLATE_ID}
ENV BREVO__Templates__WelcomeEmail=${BREVO_WELCOME_EMAIL_TEMPLATE_ID}
ENV FRONTEND__Url=${FRONTEND_URL}
```

## Brevo Template Setup

### Step 1: Create Templates in Brevo

1. Login to your Brevo account
2. Go to **Templates** → **Create Template**
3. Choose **HTML template**
4. Copy and paste the HTML code from the sections below

### Step 2: Template 1 - Email Verification

**Template Name:** `email-verification`

**Subject:** `Verify your email address`

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Verify your email address</title>
</head>
<body style="font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4;">
    <div style="max-width: 600px; margin: 0 auto; background-color: white; padding: 20px;">
        <div style="text-align: center; padding: 20px 0; border-bottom: 1px solid #eee;">
            <h1 style="color: #333; margin: 0;">SaaSify</h1>
        </div>
        
        <div style="padding: 30px 20px;">
            <h2 style="color: #333; margin-bottom: 20px;">Welcome to SaaSify{{#if tenantName}} - {{tenantName}}{{/if}}!</h2>
            
            <p style="color: #666; font-size: 16px; line-height: 1.5;">
                Hi{{#if userName}} {{userName}}{{/if}},
            </p>
            
            <p style="color: #666; font-size: 16px; line-height: 1.5;">
                Thank you for registering. Please verify your email address to complete your registration.
            </p>
            
            <div style="text-align: center; margin: 40px 0;">
                <a href="{{verificationUrl}}" 
                   style="background-color: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;">
                    Verify Email Address
                </a>
            </div>
            
            <div style="background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;">
                <p style="color: #666; font-size: 14px; margin: 0;">
                    <strong>Important:</strong> This link will expire in 24 hours. If you didn't request this verification, please ignore this email.
                </p>
            </div>
            
            <p style="color: #666; font-size: 14px; line-height: 1.5;">
                If the button doesn't work, copy and paste this link into your browser:<br>
                <a href="{{verificationUrl}}" style="color: #007bff; word-break: break-all;">{{verificationUrl}}</a>
            </p>
        </div>
        
        <div style="text-align: center; padding: 20px; border-top: 1px solid #eee; color: #999; font-size: 12px;">
            <p style="margin: 0;">© 2026 SaaSify. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
```

**Template Parameters:**
- `userName` - User's name (optional)
- `tenantName` - Tenant name (optional)
- `verificationUrl` - Email verification link

### Step 3: Template 2 - User Invitation

**Template Name:** `user-invitation`

**Subject:** `You are invited to join a SaaSify tenant!`

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>You are invited to join a SaaSify tenant!</title>
</head>
<body style="font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4;">
    <div style="max-width: 600px; margin: 0 auto; background-color: white; padding: 20px;">
        <div style="text-align: center; padding: 20px 0; border-bottom: 1px solid #eee;">
            <h1 style="color: #333; margin: 0;">SaaSify</h1>
        </div>
        
        <div style="padding: 30px 20px;">
            <h2 style="color: #333; margin-bottom: 20px;">You are invited by to be part of {{tenantName}}!</h2>
            
            <p style="color: #666; font-size: 16px; line-height: 1.5;">
                Hi{{#if userName}} {{userName}}{{/if}},
            </p>
            
            <p style="color: #666; font-size: 16px; line-height: 1.5;">
                You have been invited to join a SaaSify tenant. Please use the below URL to activate your account.
            </p>
            
            <div style="text-align: center; margin: 40px 0;">
                <a href="{{setPasswordUrl}}" 
                   style="background-color: #28a745; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;">
                    Activate Your Account
                </a>
            </div>
            
            <div style="background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;">
                <p style="color: #666; font-size: 14px; margin: 0;">
                    <strong>Important:</strong> This link will expire in 24 hours. If you didn't request this, please ignore this email.
                </p>
            </div>
            
            <p style="color: #666; font-size: 14px; line-height: 1.5;">
                If the button doesn't work, copy and paste this link into your browser:<br>
                <a href="{{setPasswordUrl}}" style="color: #28a745; word-break: break-all;">{{setPasswordUrl}}</a>
            </p>
        </div>
        
        <div style="text-align: center; padding: 20px; border-top: 1px solid #eee; color: #999; font-size: 12px;">
            <p style="margin: 0;">© 2026 SaaSify. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
```

**Template Parameters:**
- `userName` - User's name (optional)
- `tenantName` - Tenant name
- `setPasswordUrl` - Password set/activation link

### Step 4: Template 3 - Welcome Email

**Template Name:** `welcome-email`

**Subject:** `Welcome to {{tenantName}}!`

```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Welcome to {{tenantName}}!</title>
</head>
<body style="font-family: Arial, sans-serif; margin: 0; padding: 0; background-color: #f4f4f4;">
    <div style="max-width: 600px; margin: 0 auto; background-color: white; padding: 20px;">
        <div style="text-align: center; padding: 20px 0; border-bottom: 1px solid #eee;">
            <h1 style="color: #333; margin: 0;">SaaSify</h1>
        </div>
        
        <div style="padding: 30px 20px;">
            <h2 style="color: #333; margin-bottom: 20px;">Welcome to {{tenantName}}!</h2>
            
            <p style="color: #666; font-size: 16px; line-height: 1.5;">
                Hi{{#if userName}} {{userName}}{{/if}},
            </p>
            
            <p style="color: #666; font-size: 16px; line-height: 1.5;">
                Your email has been successfully verified. You can now start using SaaSify.
            </p>
            
            <div style="text-align: center; margin: 40px 0;">
                <a href="{{loginUrl}}" 
                   style="background-color: #007bff; color: white; padding: 12px 30px; text-decoration: none; border-radius: 5px; display: inline-block; font-weight: bold;">
                    Login to Your Account
                </a>
            </div>
            
            <div style="background-color: #e7f3ff; padding: 15px; border-radius: 5px; margin: 20px 0;">
                <p style="color: #0066cc; font-size: 14px; margin: 0;">
                    <strong>Next Steps:</strong> Log in to your account and start exploring the features available in your tenant.
                </p>
            </div>
            
            <p style="color: #666; font-size: 14px; line-height: 1.5;">
                If you have any questions, please contact our support team.
            </p>
        </div>
        
        <div style="text-align: center; padding: 20px; border-top: 1px solid #eee; color: #999; font-size: 12px;">
            <p style="margin: 0;">© 2026 SaaSify. All rights reserved.</p>
        </div>
    </div>
</body>
</html>
```

**Template Parameters:**
- `userName` - User's name (optional)
- `tenantName` - Tenant name
- `loginUrl` - Login page URL

### Step 5: Get Template IDs

After creating each template in Brevo:

1. Go to **Templates** in Brevo
2. Find your template in the list
3. Note the **Template ID** (numeric value)
4. Update your environment variables with these IDs

### Step 6: Test Configuration

Test your email configuration by:

1. Registering a new user (should trigger email verification)
2. Adding a user as tenant admin (should trigger user invitation)
3. Verifying email (should trigger welcome email)

## Troubleshooting

### Common Issues

1. **Template ID not found**: Ensure template IDs are correct and templates are active
2. **API Key invalid**: Verify your Brevo API key has proper permissions
3. **Parameters not working**: Check that parameter names match exactly in templates
4. **Email not sending**: Check Brevo logs and API rate limits

### Debug Mode

Enable debug logging to see detailed email sending information:

```bash
LOGGING__LOGLEVEL__DEFAULT=Debug
```

### Brevo API Documentation

For more information on Brevo's email API:
- [Brevo SMTP API Documentation](https://developers.brevo.com/reference/sendtransactional-email)
- [Brevo Template Documentation](https://help.brevo.com/hc/en-us/articles/360000669679-Create-and-manage-email-templates)
