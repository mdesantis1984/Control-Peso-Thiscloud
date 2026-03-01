# Third-Party Licenses and Acknowledgments

**Control Peso Thiscloud** is built with the support of many outstanding open-source projects and services. We are deeply grateful to the developers and communities behind these technologies.

This document lists all third-party dependencies, their licenses, and our acknowledgments.

---

## 📦 Core Framework and Runtime

### .NET 10

- **Project**: [.NET](https://github.com/dotnet/runtime)
- **License**: MIT License
- **Copyright**: © Microsoft Corporation
- **Purpose**: Application runtime and framework
- **Acknowledgment**: Thank you to the .NET team at Microsoft for providing a powerful, open-source, cross-platform framework that makes modern application development possible.

**License Text**: https://github.com/dotnet/runtime/blob/main/LICENSE.TXT

---

## 🎨 UI Framework

### MudBlazor

- **Project**: [MudBlazor](https://github.com/MudBlazor/MudBlazor)
- **Version**: 8.0.0
- **License**: MIT License
- **Copyright**: © MudBlazor Contributors
- **Purpose**: Blazor Component Library (Material Design)
- **Acknowledgment**: MudBlazor is the backbone of our user interface. Thank you to the MudBlazor team and community for creating a beautiful, feature-rich, and easy-to-use component library that brings Material Design to Blazor.

**License Text**: https://github.com/MudBlazor/MudBlazor/blob/dev/LICENSE

---

## 🗄️ Database and ORM

### Entity Framework Core

- **Project**: [Entity Framework Core](https://github.com/dotnet/efcore)
- **Version**: 10.0.3
- **License**: MIT License
- **Copyright**: © Microsoft Corporation
- **Purpose**: Object-Relational Mapper (ORM) for .NET
- **Acknowledgment**: Thank you to the EF Core team for simplifying database interactions and providing a robust Database First workflow that powers our data layer.

**License Text**: https://github.com/dotnet/efcore/blob/main/LICENSE.txt

### Microsoft.EntityFrameworkCore.Sqlite

- **Project**: EF Core SQLite Provider
- **Version**: 10.0.3
- **License**: MIT License
- **Copyright**: © Microsoft Corporation
- **Purpose**: SQLite database provider for development and prototyping
- **Acknowledgment**: Thank you for enabling rapid development with a lightweight embedded database.

### Microsoft.EntityFrameworkCore.SqlServer

- **Project**: EF Core SQL Server Provider
- **Version**: 10.0.3
- **License**: MIT License
- **Copyright**: © Microsoft Corporation
- **Purpose**: SQL Server database provider for production
- **Acknowledgment**: Thank you for providing enterprise-grade database connectivity for production deployments.

---

## 🔐 Authentication

### Microsoft.AspNetCore.Authentication.Google

- **Project**: ASP.NET Core Authentication - Google Provider
- **License**: MIT License (part of ASP.NET Core)
- **Copyright**: © Microsoft Corporation
- **Purpose**: Google OAuth 2.0 authentication integration
- **Acknowledgment**: Thank you for simplifying secure authentication with Google accounts.

### Google Identity Platform

- **Service**: [Google Identity](https://developers.google.com/identity)
- **Copyright**: © Google LLC
- **Purpose**: OAuth 2.0 authentication provider
- **Acknowledgment**: Thank you to Google for providing a secure, widely-adopted authentication system that protects user accounts without requiring password storage.

**Terms of Service**: https://developers.google.com/terms

---

## 📊 Logging and Observability

### ThisCloud.Framework.Loggings

- **Package**: ThisCloud.Framework.Loggings
- **Version**: 1.0.86
- **License**: Proprietary (internal framework)
- **Copyright**: © ThisCloud
- **Purpose**: Enterprise logging with Serilog, redaction, and correlation
- **Acknowledgment**: Thank you to the ThisCloud team for providing structured logging capabilities that enhance security and debugging.

### Serilog (via ThisCloud.Framework.Loggings)

- **Project**: [Serilog](https://github.com/serilog/serilog)
- **License**: Apache License 2.0
- **Copyright**: © Serilog Contributors
- **Purpose**: Structured logging library
- **Acknowledgment**: Thank you to the Serilog community for creating a powerful, flexible logging framework that makes log analysis and troubleshooting significantly easier.

**License Text**: https://github.com/serilog/serilog/blob/dev/LICENSE

---

## ✅ Validation

### FluentValidation

- **Project**: [FluentValidation](https://github.com/FluentValidation/FluentValidation)
- **Version**: 11.11.0
- **License**: Apache License 2.0
- **Copyright**: © Jeremy Skinner and Contributors
- **Purpose**: Fluent interface for building validation rules
- **Acknowledgment**: Thank you for providing an elegant, expressive way to define validation logic that keeps our code clean and maintainable.

**License Text**: https://github.com/FluentValidation/FluentValidation/blob/main/License.txt

---

## 📈 Analytics

### Google Analytics 4

- **Service**: [Google Analytics](https://analytics.google.com/)
- **Copyright**: © Google LLC
- **Purpose**: Web analytics and user behavior tracking
- **Acknowledgment**: Thank you to Google for providing insights into how users interact with our application, helping us improve the user experience.

**Terms of Service**: https://marketingplatform.google.com/about/analytics/terms/us/

---

## 🧪 Testing

### xUnit.net

- **Project**: [xUnit.net](https://github.com/xunit/xunit)
- **Version**: 2.9.2
- **License**: Apache License 2.0 and MIT License (dual-licensed)
- **Copyright**: © .NET Foundation and Contributors
- **Purpose**: Unit testing framework
- **Acknowledgment**: Thank you for providing a clean, modern testing framework that helps us maintain code quality and reliability.

**License Text**: https://github.com/xunit/xunit/blob/main/LICENSE

### Moq

- **Project**: [Moq](https://github.com/devlooped/moq)
- **Version**: 4.20.72
- **License**: BSD 3-Clause License
- **Copyright**: © Daniel Cazzulino and Contributors
- **Purpose**: Mocking library for unit tests
- **Acknowledgment**: Thank you for simplifying unit testing with a powerful, easy-to-use mocking framework.

**License Text**: https://github.com/devlooped/moq/blob/main/LICENSE

### bUnit

- **Project**: [bUnit](https://github.com/bUnit-dev/bUnit)
- **Version**: 1.34.7
- **License**: MIT License
- **Copyright**: © Egil Hansen and Contributors
- **Purpose**: Blazor component testing library
- **Acknowledgment**: Thank you for enabling comprehensive testing of Blazor components, ensuring our UI behaves correctly.

**License Text**: https://github.com/bUnit-dev/bUnit/blob/main/LICENSE

---

## 🐳 Infrastructure and Deployment

### Docker

- **Project**: [Docker](https://www.docker.com/)
- **License**: Apache License 2.0
- **Copyright**: © Docker, Inc.
- **Purpose**: Containerization platform
- **Acknowledgment**: Thank you for revolutionizing application deployment with containers, making our production environment portable and reproducible.

**License Text**: https://github.com/moby/moby/blob/master/LICENSE

### Microsoft SQL Server Express

- **Product**: [SQL Server Express 2022](https://www.microsoft.com/sql-server/sql-server-downloads)
- **License**: Microsoft Software License Terms
- **Copyright**: © Microsoft Corporation
- **Purpose**: Production database
- **Acknowledgment**: Thank you to Microsoft for providing a free, powerful database solution for production deployments.

**License Terms**: https://www.microsoft.com/useterms

---

## 🔧 Development Tools

### GitHub Actions

- **Service**: [GitHub Actions](https://github.com/features/actions)
- **Copyright**: © GitHub, Inc.
- **Purpose**: CI/CD workflows and automation
- **Acknowledgment**: Thank you to GitHub for providing seamless CI/CD integration that automates our build, test, and release processes.

**Terms**: https://docs.github.com/en/site-policy/github-terms/github-terms-of-service

### mathieudutour/github-tag-action

- **Project**: [github-tag-action](https://github.com/mathieudutour/github-tag-action)
- **Version**: 6.2
- **License**: MIT License
- **Copyright**: © Mathieu Dutour
- **Purpose**: Automated semantic versioning and tag creation
- **Acknowledgment**: Thank you for simplifying version management with automatic semantic versioning based on conventional commits.

**License Text**: https://github.com/mathieudutour/github-tag-action/blob/main/LICENSE

---

## 🌐 Hosting and Proxy

### Nginx Proxy Manager (NPM)

- **Project**: [Nginx Proxy Manager](https://github.com/NginxProxyManager/nginx-proxy-manager)
- **License**: MIT License
- **Copyright**: © Jamie Curnow and Contributors
- **Purpose**: Reverse proxy with SSL management
- **Acknowledgment**: Thank you for providing an intuitive interface for managing reverse proxies and SSL certificates.

**License Text**: https://github.com/NginxProxyManager/nginx-proxy-manager/blob/master/LICENSE

---

## 📖 Documentation and Standards

### Markdown

- **Specification**: [CommonMark](https://commonmark.org/)
- **License**: Creative Commons CC-BY-SA 4.0 (spec)
- **Purpose**: Documentation format
- **Acknowledgment**: Thank you to the CommonMark community for standardizing Markdown and making documentation authoring accessible.

---

## 🌍 Internationalization

### ASP.NET Core Localization

- **Project**: Part of ASP.NET Core
- **License**: MIT License
- **Copyright**: © Microsoft Corporation
- **Purpose**: Internationalization and localization (i18n/l10n)
- **Acknowledgment**: Thank you for providing built-in support for multi-language applications.

---

## 📜 License Summaries

### MIT License (Permissive)

Used by: .NET, EF Core, MudBlazor, xUnit, bUnit, github-tag-action, NPM

**Summary**: Allows almost unrestricted freedom to use, copy, modify, merge, publish, distribute, sublicense, and sell copies. Requires preservation of copyright notice and license text.

**Full Text**: https://opensource.org/licenses/MIT

### Apache License 2.0 (Permissive)

Used by: FluentValidation, Serilog, xUnit (dual-licensed), Docker

**Summary**: Similar to MIT but includes explicit patent grants. Requires preservation of copyright notice, disclaimer, and attribution notices.

**Full Text**: https://www.apache.org/licenses/LICENSE-2.0

### BSD 3-Clause License (Permissive)

Used by: Moq

**Summary**: Permissive license similar to MIT, with an additional clause prohibiting use of the project name or contributors' names for endorsement without permission.

**Full Text**: https://opensource.org/licenses/BSD-3-Clause

---

## 🙏 Special Thanks

We extend our gratitude to:

- **The .NET Foundation** for stewarding the .NET ecosystem
- **The open-source community** for countless hours of volunteer work
- **Microsoft** for embracing open source and contributing .NET, EF Core, and ASP.NET Core
- **Google** for authentication and analytics services
- **All contributors** to the libraries listed above

Without your contributions, this project would not be possible.

---

## ⚖️ Compliance Statement

Control Peso Thiscloud complies with all license terms of the third-party software and services listed above. We:

- ✅ Include all required copyright notices
- ✅ Provide attribution to original authors
- ✅ Retain original license texts (linked above)
- ✅ Comply with redistribution requirements
- ✅ Do not misrepresent the origin of third-party code
- ✅ Do not use project names for endorsement without permission

---

## 📬 Questions or Concerns?

If you believe we have misrepresented or failed to comply with any license terms, please contact us immediately:

**Email:** [contact email to be configured]  
**Website:** https://controlpeso.thiscloud.com.ar

We take license compliance seriously and will address any issues promptly.

---

**Last Updated:** February 25, 2026

**Thank you to everyone who contributes to open source. You make the world a better place. 💙**
