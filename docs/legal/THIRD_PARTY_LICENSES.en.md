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
- **Version**: 9.0.0
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

### Npgsql.EntityFrameworkCore.PostgreSQL

- **Project**: [Npgsql EF Core Provider](https://github.com/npgsql/efcore.pg)
- **Version**: 10.0.0
- **License**: PostgreSQL License
- **Copyright**: © Npgsql Contributors
- **Purpose**: PostgreSQL database provider for Entity Framework Core (development and production)
- **Acknowledgment**: Thank you to the Npgsql team for providing a fast, reliable PostgreSQL driver and EF Core provider.

**License Text**: https://github.com/npgsql/efcore.pg/blob/main/LICENSE

---

## 🖼️ Imaging and Data Export

### SkiaSharp

- **Project**: [SkiaSharp](https://github.com/mono/SkiaSharp)
- **Version**: 4.157.0
- **License**: MIT License
- **Copyright**: © Microsoft Corporation and Contributors
- **Purpose**: Cross-platform 2D graphics library used to process profile images (resize and WebP encoding)
- **Acknowledgment**: Thank you to the SkiaSharp team for a permissively licensed imaging library.

**License Text**: https://github.com/mono/SkiaSharp/blob/main/LICENSE.md

### CsvHelper

- **Project**: [CsvHelper](https://github.com/JoshClose/CsvHelper)
- **Version**: 33.1.0
- **License**: Microsoft Public License (MS-PL) or Apache License 2.0 (dual-licensed)
- **Copyright**: © Josh Close
- **Purpose**: CSV export of weight data in the administration area
- **Acknowledgment**: Thank you for a simple and fast CSV library.

**License Text**: https://github.com/JoshClose/CsvHelper/blob/master/LICENSE.txt

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
- **Version**: 1.0.94
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
- **Version**: 12.1.1
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

### NSubstitute

- **Project**: [NSubstitute](https://github.com/nsubstitute/NSubstitute)
- **Version**: 5.3.0
- **License**: BSD 3-Clause License
- **Copyright**: © NSubstitute Contributors
- **Purpose**: Mocking library for unit tests

### FluentAssertions

- **Project**: [FluentAssertions](https://github.com/fluentassertions/fluentassertions)
- **Version**: 7.0.0
- **License**: Apache License 2.0
- **Copyright**: © Dennis Doomen and Contributors
- **Purpose**: Assertion library for tests

### Testcontainers for .NET

- **Project**: [Testcontainers](https://github.com/testcontainers/testcontainers-dotnet)
- **Version**: 4.17.0
- **License**: MIT License
- **Copyright**: © Testcontainers Contributors
- **Purpose**: Disposable PostgreSQL containers for integration tests

### Microsoft.EntityFrameworkCore.Sqlite / InMemory

- **License**: MIT License (part of Entity Framework Core)
- **Copyright**: © Microsoft Corporation
- **Purpose**: In-memory and SQLite databases used only by the automated test projects (not part of the production runtime)

---

## 🐳 Infrastructure and Deployment

### Docker

- **Project**: [Docker](https://www.docker.com/)
- **License**: Apache License 2.0
- **Copyright**: © Docker, Inc.
- **Purpose**: Containerization platform
- **Acknowledgment**: Thank you for revolutionizing application deployment with containers, making our production environment portable and reproducible.

**License Text**: https://github.com/moby/moby/blob/master/LICENSE

### PostgreSQL

- **Product**: [PostgreSQL 15](https://www.postgresql.org/)
- **License**: PostgreSQL License
- **Copyright**: © PostgreSQL Global Development Group
- **Purpose**: Production and development database
- **Acknowledgment**: Thank you to the PostgreSQL community for a robust, open-source relational database.

**License Text**: https://www.postgresql.org/about/licence/

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

Used by: .NET, EF Core, MudBlazor, SkiaSharp, Testcontainers, xUnit, bUnit, github-tag-action, NPM

**Summary**: Allows almost unrestricted freedom to use, copy, modify, merge, publish, distribute, sublicense, and sell copies. Requires preservation of copyright notice and license text.

**Full Text**: https://opensource.org/licenses/MIT

### Apache License 2.0 (Permissive)

Used by: FluentValidation, FluentAssertions, Serilog, CsvHelper (dual-licensed), xUnit (dual-licensed), Docker

**Summary**: Similar to MIT but includes explicit patent grants. Requires preservation of copyright notice, disclaimer, and attribution notices.

**Full Text**: https://www.apache.org/licenses/LICENSE-2.0

### BSD 3-Clause License (Permissive)

Used by: Moq, NSubstitute

**Summary**: Permissive license similar to MIT, with an additional clause prohibiting use of the project name or contributors' names for endorsement without permission.

**Full Text**: https://opensource.org/licenses/BSD-3-Clause

### PostgreSQL License (Permissive)

Used by: PostgreSQL, Npgsql EF Core provider

**Summary**: Liberal open-source license similar to BSD/MIT. Allows use, copy, modification and distribution, provided the copyright notice and license text are retained.

**Full Text**: https://www.postgresql.org/about/licence/

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

**Last Updated:** November 19, 2026

**Thank you to everyone who contributes to open source. You make the world a better place. 💙**
