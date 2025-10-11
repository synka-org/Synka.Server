# Security Policy

## Supported Versions

We release patches for security vulnerabilities for the following versions:

| Version | Supported          |
| ------- | ------------------ |
| 0.1.x   | :white_check_mark: |

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues.**

If you discover a security vulnerability in Synka.Server, please report it to us privately. We take all security vulnerabilities seriously.

### How to Report

1. **Email**: Send details to [security@nilzen.se](mailto:security@nilzen.se)
2. **Subject Line**: Use "SECURITY: [Brief Description]"
3. **Include**:
   - Description of the vulnerability
   - Steps to reproduce the issue
   - Potential impact
   - Suggested fix (if available)

### What to Expect

- **Acknowledgment**: We will acknowledge receipt of your report within 7 days
- **Assessment**: We will investigate and assess the severity within 30 days
- **Updates**: We will keep you informed about our progress
- **Fix**: We will work on a fix and aim to release a patch as soon as possible
- **Credit**: We will credit you in the security advisory (unless you prefer to remain anonymous)

## Security Best Practices

When deploying Synka.Server, follow these security best practices:

### Database Security

- **Production Database**: Use PostgreSQL instead of SQLite for production deployments
- **Connection Strings**: Store connection strings in environment variables or secure configuration systems
- **Database Credentials**: Use strong, randomly generated passwords
- **Network Access**: Restrict database access to the application server only

### Authentication & Authorization

- **Admin Registration**: The `/auth/register` endpoint is admin-only by default
- **Password Policy**: Configure strong password requirements via ASP.NET Identity options
- **HTTPS**: Always use HTTPS in production (configure reverse proxy)
- **Cookie Security**: Ensure cookies are marked as `Secure` and `HttpOnly`

### OIDC Integration

When using OpenID Connect:

- **Client Secrets**: Store `Authentication:OIDC:ClientSecret` securely (e.g., Azure Key Vault, AWS Secrets Manager)
- **HTTPS Callback**: Ensure callback URLs use HTTPS
- **Token Validation**: Verify the `Authority` URL is correct and trusted
- **Scopes**: Request only the minimum required scopes

### API Security

- **OpenAPI Exposure**: Keep `OpenApi:Expose` set to `false` in production unless necessary
- **CORS**: Configure CORS policies appropriately (not enabled by default)
- **Rate Limiting**: Implement rate limiting at the reverse proxy level
- **Input Validation**: All DTOs use data annotations for validation

### Deployment Security

- **Environment Variables**: Use secure configuration providers (Azure App Configuration, AWS Parameter Store, etc.)
- **Secrets Management**: Never commit secrets to version control
- **TLS/SSL**: Use TLS 1.2 or higher
- **Security Headers**: Configure security headers via reverse proxy:
  - `Strict-Transport-Security`
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `Content-Security-Policy`

### Docker Security (if applicable)

- **Base Images**: Use official, updated base images
- **User Privileges**: Run containers as non-root user
- **Secrets**: Use Docker secrets or orchestration secret management
- **Image Scanning**: Scan images for vulnerabilities before deployment

## Known Security Considerations

### SQLite in Production

⚠️ **SQLite is not recommended for production use**:

- Limited concurrent write operations
- No network access controls
- File-based permissions only

**Recommendation**: Use PostgreSQL for production deployments.

### Default Configuration

The default `appsettings.json` is designed for **development only**:

- SQLite with local file storage
- Permissive logging levels
- No external authentication

**Always** override configuration for production deployments.

## Security Updates

We will announce security updates through:

- GitHub Security Advisories
- Release notes with `[SECURITY]` prefix
- Email to reporters (if applicable)

## Responsible Disclosure Policy

We kindly ask security researchers to:

- Give us reasonable time to respond to your report before public disclosure
- Make a good faith effort to avoid privacy violations, data destruction, and service interruption
- Not exploit the vulnerability beyond what is necessary to demonstrate the issue

We commit to:

- Respond to your report promptly
- Keep you informed of our progress
- Credit you for your discovery (unless you prefer anonymity)
- Not pursue legal action against researchers who follow this policy

## Contact

For security concerns, contact:

- **Email**: [security@nilzen.se](mailto:security@nilzen.se)
- **Organization**: [Synka](https://github.com/synka-org)

## Last Updated

October 11, 2025
