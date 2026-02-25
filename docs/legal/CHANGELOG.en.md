# Changelog

All notable changes to **Control Peso Thiscloud** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [Unreleased]

### Planned Features

- 🌐 Additional language support (Portuguese, French)
- 📊 Advanced analytics dashboard with custom date ranges
- 📱 Progressive Web App (PWA) support for offline access
- 🔔 Email notifications for weight goals and milestones
- 📈 Export data to CSV/Excel
- 🎯 Weight goal reminders and motivational messages
- 🔄 Data import from other weight tracking apps
- 🏆 Achievement badges and gamification
- 👥 Social features (optional sharing with friends/family)
- 🩺 Integration with health platforms (Apple Health, Google Fit)

---

## [1.3.0] - 2026-02-25

### Added

- ✅ **Production Deployment Infrastructure**:
  - Docker Compose production configuration with SQL Server Express 2022
  - Automated daily backup system with 30-day retention
  - Backup scripts: backup-sqlserver.sh, restore-backup.sh, backup-now.sh
  - SQL Server migration script (migrate-sqlite-to-sqlserver.sh)
  - Custom SQL Server Docker image with cron daemon for scheduled backups
  
- 🔐 **Application Security Hardening**:
  - HTTP Strict Transport Security (HSTS) with 1-year max-age policy
  - Permanent HTTPS redirection (308 status code) for production
  - Forwarded headers middleware for reverse proxy (NPM Plus) support
  - Content Security Policy (CSP) with restrictive headers
  - Secrets management documentation and templates

- 🌐 **SEO and Accessibility**:
  - Comprehensive meta tags (Open Graph, Twitter Cards)
  - Updated robots.txt and sitemap.xml
  - HTML lang attribute set to es-AR (Spanish - Argentina)
  - Improved semantic HTML structure

- 📄 **Legal Documentation** (Bilingual - EN/ES):
  - Privacy Policy (GDPR-inspired, Argentine context)
  - Terms and Conditions with comprehensive disclaimer
  - Third-Party Licenses and acknowledgments
  - Changelog (this file)

- 🚀 **CI/CD Automation**:
  - Automated semantic versioning workflow (release.yml)
  - Automatic GitHub tag and release creation on push to main
  - Version calculation based on conventional commits (feat/fix/BREAKING CHANGE)

- 📊 **Logging Infrastructure**:
  - Integration with ThisCloud.Framework.Loggings (Serilog-based)
  - Structured logging with redaction and correlation IDs
  - Log rotation and retention policies

### Changed

- 🗄️ **Database**: Added support for SQL Server Express 2022 (production)
- 🐳 **Docker**: Multi-container orchestration with separate database and web services
- 📦 **NuGet**: Added Microsoft.EntityFrameworkCore.SqlServer 10.0.3
- 🔒 **Configuration**: Environment variable pattern for sensitive data
- 🌍 **Hosting**: Prepared for reverse proxy deployment behind NPM Plus

### Fixed

- 🔧 Removed hardcoded connection string warning from DbContext
- 🔐 Sanitized internal IP addresses, ports, and paths from documentation
- 📝 .gitignore now properly excludes docker-compose.override.yml and .env files

### Security

- 🔒 All secrets moved to environment variables and docker-compose override files
- 🛡️ Database passwords encrypted with strong requirements
- 🔐 Google OAuth ClientSecret managed via User Secrets (dev) and env vars (prod)
- 📊 Google Analytics configured with IP anonymization and Secure cookies
- 🚫 No sensitive infrastructure details in public repository

### Documentation

- 📚 Complete production deployment guide (13-step plan)
- 📖 Google OAuth production setup documentation
- 💾 Comprehensive backup and restore procedures
- 🔑 Secrets management best practices
- 🏗️ Infrastructure sanitization (placeholders for IPs, ports, paths)
- 📄 Legal compliance documentation (Privacy, Terms, Licenses)

---

## [1.2.0] - 2026-02-15

### Added

- 🎨 **Theme Management**:
  - MudBlazor ThemeManager integration
  - Dark/Light theme toggle
  - Customizable color palette
  - Typography adjustments
  - Real-time theme preview

- 📱 **Responsive Design**:
  - Mobile-first layout optimization
  - Adaptive components for different screen sizes
  - Touch-friendly controls

- 🌐 **Internationalization**:
  - Full bilingual support (Spanish/English)
  - Language selector component
  - Localized date and number formatting
  - Resource files for UI strings

### Changed

- 🎨 Updated MudBlazor to 8.0.0
- 📊 Improved chart rendering performance
- 🖼️ Enhanced visual hierarchy in dashboard

---

## [1.1.0] - 2026-02-01

### Added

- 📈 **Trends and Analytics**:
  - Weight trend analysis (up/down/neutral)
  - Statistical calculations (average, min, max)
  - Weight projection based on historical data
  - Weekly and monthly trend summaries

- 🔍 **Advanced Filtering**:
  - Date range filters
  - Search by notes
  - Sort by date, weight, or trend
  - Pagination for large datasets

- 💬 **Notes Feature**:
  - Add personal notes to weight entries
  - Maximum 500 characters per note
  - Edit and delete notes

### Fixed

- 🐛 Weight chart not updating after adding new entry
- 🔄 Refresh issue after editing weight log
- 📅 Date picker locale not respecting user language

---

## [1.0.0] - 2026-01-15

### Added - Initial Release

- ✅ **Core Features**:
  - Google OAuth 2.0 authentication
  - Weight entry creation, editing, and deletion
  - Weight history table with date, time, weight, and notes
  - Basic weight statistics (current, starting, goal, progress)
  - Line chart visualization of weight over time

- 🎨 **User Interface**:
  - MudBlazor Material Design components
  - Dark theme by default
  - Responsive layout
  - Intuitive navigation menu

- 🗄️ **Database**:
  - SQLite database (Database First approach)
  - Entity Framework Core 9.0.1
  - Entities: User, WeightLog, UserPreference, AuditLog

- 🔐 **Security**:
  - Google OAuth authentication (no password storage)
  - Encrypted HTTPS communication
  - HttpOnly + Secure + SameSite cookies
  - Input validation with FluentValidation

- 📊 **Data Model**:
  - User management with roles (User, Administrator)
  - Weight logs with date, time, weight (kg), unit display, notes
  - User preferences (dark mode, notifications, timezone)
  - Audit log for tracking changes

- 🌐 **Configuration**:
  - Environment-based configuration (Development/Production)
  - User Secrets for sensitive data (development)
  - Google Analytics 4 integration

### Technical Stack

- 🚀 **.NET 10** - Latest .NET framework
- 🔥 **Blazor Server** - Interactive server-side rendering
- 🎨 **MudBlazor 8.0.0** - Material Design component library
- 🗄️ **Entity Framework Core 9.0.1** - ORM with Database First
- 🔐 **Google OAuth 2.0** - Authentication provider
- 📊 **Google Analytics 4** - Web analytics
- ✅ **FluentValidation 11.11.0** - Input validation
- 📝 **Serilog** (via ThisCloud.Framework.Loggings) - Structured logging

---

## Version Numbering

This project uses [Semantic Versioning](https://semver.org/):

- **MAJOR** version (X.0.0): Incompatible API changes or significant architectural shifts
- **MINOR** version (0.X.0): New features added in a backwards-compatible manner
- **PATCH** version (0.0.X): Backwards-compatible bug fixes

---

## Release Notes

### How to Read This Changelog

- 🚀 **Added**: New features or functionality
- 🔄 **Changed**: Changes in existing functionality
- 🗑️ **Deprecated**: Features that will be removed in future releases
- ❌ **Removed**: Features that have been removed
- 🐛 **Fixed**: Bug fixes
- 🔒 **Security**: Security improvements or vulnerability patches

---

## Contributing

We welcome contributions! If you'd like to contribute to this project, please:

1. Fork the repository
2. Create a feature branch (`feature/your-feature-name`)
3. Follow [Conventional Commits](https://www.conventionalcommits.org/) for commit messages
4. Submit a pull request

Commit message format:
```
<type>(<scope>): <subject>

Types: feat, fix, docs, style, refactor, test, chore, ci
```

Examples:
- `feat(dashboard): add weight projection chart`
- `fix(auth): resolve Google OAuth redirect issue`
- `docs(readme): update installation instructions`

---

## Support

For questions, issues, or feature requests:

- **Issues**: [GitHub Issues](https://github.com/mdesantis1984/Control-Peso-Thiscloud/issues)
- **Email**: [contact email to be configured]
- **Website**: https://controlpeso.thiscloud.com.ar

---

## License

This project is licensed under the **MIT License** - see the LICENSE file for details.

---

**Thank you for using Control Peso Thiscloud!**

Track your weight. Achieve your goals. Stay healthy. 💪
