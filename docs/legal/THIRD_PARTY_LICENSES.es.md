# Licencias de Terceros y Agradecimientos

**Control Peso Thiscloud** está construido con el apoyo de muchos proyectos de código abierto y servicios excepcionales. Estamos profundamente agradecidos a los desarrolladores y comunidades detrás de estas tecnologías.

Este documento enumera todas las dependencias de terceros, sus licencias y nuestros agradecimientos.

---

## 📦 Framework y Runtime Principal

### .NET 10

- **Proyecto**: [.NET](https://github.com/dotnet/runtime)
- **Licencia**: MIT License
- **Copyright**: © Microsoft Corporation
- **Propósito**: Runtime y framework de aplicación
- **Agradecimiento**: Gracias al equipo de .NET en Microsoft por proporcionar un framework potente, de código abierto y multiplataforma que hace posible el desarrollo moderno de aplicaciones.

**Texto de Licencia**: https://github.com/dotnet/runtime/blob/main/LICENSE.TXT

---

## 🎨 Framework de UI

### MudBlazor

- **Proyecto**: [MudBlazor](https://github.com/MudBlazor/MudBlazor)
- **Versión**: 9.0.0
- **Licencia**: MIT License
- **Copyright**: © MudBlazor Contributors
- **Propósito**: Biblioteca de Componentes Blazor (Material Design)
- **Agradecimiento**: MudBlazor es la columna vertebral de nuestra interfaz de usuario. Gracias al equipo y comunidad de MudBlazor por crear una biblioteca de componentes hermosa, rica en funciones y fácil de usar que lleva Material Design a Blazor.

**Texto de Licencia**: https://github.com/MudBlazor/MudBlazor/blob/dev/LICENSE

---

## 🗄️ Base de Datos y ORM

### Entity Framework Core

- **Proyecto**: [Entity Framework Core](https://github.com/dotnet/efcore)
- **Versión**: 10.0.3
- **Licencia**: MIT License
- **Copyright**: © Microsoft Corporation
- **Propósito**: Mapeador Objeto-Relacional (ORM) para .NET
- **Agradecimiento**: Gracias al equipo de EF Core por simplificar las interacciones con la base de datos y proporcionar un flujo de trabajo Database First robusto que impulsa nuestra capa de datos.

**Texto de Licencia**: https://github.com/dotnet/efcore/blob/main/LICENSE.txt

### Npgsql.EntityFrameworkCore.PostgreSQL

- **Proyecto**: [Proveedor Npgsql para EF Core](https://github.com/npgsql/efcore.pg)
- **Versión**: 10.0.0
- **Licencia**: PostgreSQL License
- **Copyright**: © Npgsql Contributors
- **Propósito**: Proveedor de base de datos PostgreSQL para Entity Framework Core (desarrollo y producción)
- **Agradecimiento**: Gracias al equipo de Npgsql por ofrecer un controlador y proveedor EF Core para PostgreSQL rápidos y confiables.

**Texto de Licencia**: https://github.com/npgsql/efcore.pg/blob/main/LICENSE

---

## 🖼️ Imágenes y Exportación de Datos

### SkiaSharp

- **Proyecto**: [SkiaSharp](https://github.com/mono/SkiaSharp)
- **Versión**: 4.157.0
- **Licencia**: MIT License
- **Copyright**: © Microsoft Corporation y Contribuidores
- **Propósito**: Biblioteca gráfica 2D multiplataforma usada para procesar imágenes de perfil (redimensionado y codificación WebP)
- **Agradecimiento**: Gracias al equipo de SkiaSharp por una biblioteca de imágenes con licencia permisiva.

**Texto de Licencia**: https://github.com/mono/SkiaSharp/blob/main/LICENSE.md

### CsvHelper

- **Proyecto**: [CsvHelper](https://github.com/JoshClose/CsvHelper)
- **Versión**: 33.1.0
- **Licencia**: Microsoft Public License (MS-PL) o Apache License 2.0 (doble licencia)
- **Copyright**: © Josh Close
- **Propósito**: Exportación CSV de datos de peso en el área de administración
- **Agradecimiento**: Gracias por una biblioteca CSV simple y rápida.

**Texto de Licencia**: https://github.com/JoshClose/CsvHelper/blob/master/LICENSE.txt

---

## 🔐 Autenticación

### Microsoft.AspNetCore.Authentication.Google

- **Proyecto**: ASP.NET Core Authentication - Proveedor Google
- **Licencia**: MIT License (parte de ASP.NET Core)
- **Copyright**: © Microsoft Corporation
- **Propósito**: Integración de autenticación Google OAuth 2.0
- **Agradecimiento**: Gracias por simplificar la autenticación segura con cuentas de Google.

### Google Identity Platform

- **Servicio**: [Google Identity](https://developers.google.com/identity)
- **Copyright**: © Google LLC
- **Propósito**: Proveedor de autenticación OAuth 2.0
- **Agradecimiento**: Gracias a Google por proporcionar un sistema de autenticación seguro y ampliamente adoptado que protege las cuentas de usuario sin requerir almacenamiento de contraseñas.

**Términos de Servicio**: https://developers.google.com/terms

---

## 📊 Logging y Observabilidad

### ThisCloud.Framework.Loggings

- **Paquete**: ThisCloud.Framework.Loggings
- **Versión**: 1.0.94
- **Licencia**: Propietaria (framework interno)
- **Copyright**: © ThisCloud
- **Propósito**: Logging empresarial con Serilog, redacción y correlación
- **Agradecimiento**: Gracias al equipo de ThisCloud por proporcionar capacidades de logging estructurado que mejoran la seguridad y el debugging.

### Serilog (vía ThisCloud.Framework.Loggings)

- **Proyecto**: [Serilog](https://github.com/serilog/serilog)
- **Licencia**: Apache License 2.0
- **Copyright**: © Serilog Contributors
- **Propósito**: Biblioteca de logging estructurado
- **Agradecimiento**: Gracias a la comunidad de Serilog por crear un framework de logging potente y flexible que facilita significativamente el análisis de logs y la resolución de problemas.

**Texto de Licencia**: https://github.com/serilog/serilog/blob/dev/LICENSE

---

## ✅ Validación

### FluentValidation

- **Proyecto**: [FluentValidation](https://github.com/FluentValidation/FluentValidation)
- **Versión**: 12.1.1
- **Licencia**: Apache License 2.0
- **Copyright**: © Jeremy Skinner and Contributors
- **Propósito**: Interfaz fluida para construir reglas de validación
- **Agradecimiento**: Gracias por proporcionar una forma elegante y expresiva de definir lógica de validación que mantiene nuestro código limpio y mantenible.

**Texto de Licencia**: https://github.com/FluentValidation/FluentValidation/blob/main/License.txt

---

## 📈 Análisis

### Google Analytics 4

- **Servicio**: [Google Analytics](https://analytics.google.com/)
- **Copyright**: © Google LLC
- **Propósito**: Análisis web y seguimiento de comportamiento del usuario
- **Agradecimiento**: Gracias a Google por proporcionar información sobre cómo los usuarios interactúan con nuestra aplicación, ayudándonos a mejorar la experiencia del usuario.

**Términos de Servicio**: https://marketingplatform.google.com/about/analytics/terms/es/

---

## 🧪 Testing

### xUnit.net

- **Proyecto**: [xUnit.net](https://github.com/xunit/xunit)
- **Versión**: 2.9.2
- **Licencia**: Apache License 2.0 y MIT License (licencia dual)
- **Copyright**: © .NET Foundation and Contributors
- **Propósito**: Framework de pruebas unitarias
- **Agradecimiento**: Gracias por proporcionar un framework de testing limpio y moderno que nos ayuda a mantener la calidad y confiabilidad del código.

**Texto de Licencia**: https://github.com/xunit/xunit/blob/main/LICENSE

### Moq

- **Proyecto**: [Moq](https://github.com/devlooped/moq)
- **Versión**: 4.20.72
- **Licencia**: BSD 3-Clause License
- **Copyright**: © Daniel Cazzulino and Contributors
- **Propósito**: Biblioteca de mocking para pruebas unitarias
- **Agradecimiento**: Gracias por simplificar las pruebas unitarias con un framework de mocking potente y fácil de usar.

**Texto de Licencia**: https://github.com/devlooped/moq/blob/main/LICENSE

### bUnit

- **Proyecto**: [bUnit](https://github.com/bUnit-dev/bUnit)
- **Versión**: 1.34.7
- **Licencia**: MIT License
- **Copyright**: © Egil Hansen and Contributors
- **Propósito**: Biblioteca de testing de componentes Blazor
- **Agradecimiento**: Gracias por permitir pruebas exhaustivas de componentes Blazor, asegurando que nuestra UI se comporte correctamente.

**Texto de Licencia**: https://github.com/bUnit-dev/bUnit/blob/main/LICENSE

### NSubstitute

- **Proyecto**: [NSubstitute](https://github.com/nsubstitute/NSubstitute)
- **Versión**: 5.3.0
- **Licencia**: BSD 3-Clause License
- **Copyright**: © Contribuidores de NSubstitute
- **Propósito**: Biblioteca de mocking para pruebas unitarias

### FluentAssertions

- **Proyecto**: [FluentAssertions](https://github.com/fluentassertions/fluentassertions)
- **Versión**: 7.0.0
- **Licencia**: Apache License 2.0
- **Copyright**: © Dennis Doomen y Contribuidores
- **Propósito**: Biblioteca de aserciones para pruebas

### Testcontainers for .NET

- **Proyecto**: [Testcontainers](https://github.com/testcontainers/testcontainers-dotnet)
- **Versión**: 4.17.0
- **Licencia**: MIT License
- **Copyright**: © Contribuidores de Testcontainers
- **Propósito**: Contenedores PostgreSQL descartables para pruebas de integración

### Microsoft.EntityFrameworkCore.Sqlite / InMemory

- **Licencia**: MIT License (parte de Entity Framework Core)
- **Copyright**: © Microsoft Corporation
- **Propósito**: Bases de datos en memoria y SQLite usadas solo por los proyectos de pruebas automatizadas (no forman parte del runtime de producción)

---

## 🐳 Infraestructura y Despliegue

### Docker

- **Proyecto**: [Docker](https://www.docker.com/)
- **Licencia**: Apache License 2.0
- **Copyright**: © Docker, Inc.
- **Propósito**: Plataforma de contenedorización
- **Agradecimiento**: Gracias por revolucionar el despliegue de aplicaciones con contenedores, haciendo nuestro entorno de producción portable y reproducible.

**Texto de Licencia**: https://github.com/moby/moby/blob/master/LICENSE

### PostgreSQL

- **Producto**: [PostgreSQL 15](https://www.postgresql.org/)
- **Licencia**: PostgreSQL License
- **Copyright**: © PostgreSQL Global Development Group
- **Propósito**: Base de datos de producción y desarrollo
- **Agradecimiento**: Gracias a la comunidad de PostgreSQL por una base de datos relacional robusta y de código abierto.

**Texto de Licencia**: https://www.postgresql.org/about/licence/

---

## 🔧 Herramientas de Desarrollo

### GitHub Actions

- **Servicio**: [GitHub Actions](https://github.com/features/actions)
- **Copyright**: © GitHub, Inc.
- **Propósito**: Workflows de CI/CD y automatización
- **Agradecimiento**: Gracias a GitHub por proporcionar integración CI/CD perfecta que automatiza nuestros procesos de build, test y release.

**Términos**: https://docs.github.com/es/site-policy/github-terms/github-terms-of-service

### mathieudutour/github-tag-action

- **Proyecto**: [github-tag-action](https://github.com/mathieudutour/github-tag-action)
- **Versión**: 6.2
- **Licencia**: MIT License
- **Copyright**: © Mathieu Dutour
- **Propósito**: Versionado semántico automático y creación de tags
- **Agradecimiento**: Gracias por simplificar la gestión de versiones con versionado semántico automático basado en conventional commits.

**Texto de Licencia**: https://github.com/mathieudutour/github-tag-action/blob/main/LICENSE

---

## 🌐 Hosting y Proxy

### Nginx Proxy Manager (NPM)

- **Proyecto**: [Nginx Proxy Manager](https://github.com/NginxProxyManager/nginx-proxy-manager)
- **Licencia**: MIT License
- **Copyright**: © Jamie Curnow and Contributors
- **Propósito**: Proxy inverso con gestión de SSL
- **Agradecimiento**: Gracias por proporcionar una interfaz intuitiva para gestionar proxies inversos y certificados SSL.

**Texto de Licencia**: https://github.com/NginxProxyManager/nginx-proxy-manager/blob/master/LICENSE

---

## 📖 Documentación y Estándares

### Markdown

- **Especificación**: [CommonMark](https://commonmark.org/)
- **Licencia**: Creative Commons CC-BY-SA 4.0 (especificación)
- **Propósito**: Formato de documentación
- **Agradecimiento**: Gracias a la comunidad de CommonMark por estandarizar Markdown y hacer accesible la creación de documentación.

---

## 🌍 Internacionalización

### ASP.NET Core Localization

- **Proyecto**: Parte de ASP.NET Core
- **Licencia**: MIT License
- **Copyright**: © Microsoft Corporation
- **Propósito**: Internacionalización y localización (i18n/l10n)
- **Agradecimiento**: Gracias por proporcionar soporte integrado para aplicaciones multi-idioma.

---

## 📜 Resúmenes de Licencias

### MIT License (Permisiva)

Usada por: .NET, EF Core, MudBlazor, SkiaSharp, Testcontainers, xUnit, bUnit, github-tag-action, NPM

**Resumen**: Permite libertad casi sin restricciones para usar, copiar, modificar, fusionar, publicar, distribuir, sublicenciar y vender copias. Requiere preservación del aviso de copyright y texto de licencia.

**Texto Completo**: https://opensource.org/licenses/MIT

### Apache License 2.0 (Permisiva)

Usada por: FluentValidation, FluentAssertions, Serilog, CsvHelper (licencia dual), xUnit (licencia dual), Docker

**Resumen**: Similar a MIT pero incluye concesiones explícitas de patentes. Requiere preservación del aviso de copyright, descargo de responsabilidad y avisos de atribución.

**Texto Completo**: https://www.apache.org/licenses/LICENSE-2.0

### BSD 3-Clause License (Permisiva)

Usada por: Moq, NSubstitute

**Resumen**: Licencia permisiva similar a MIT, con una cláusula adicional que prohíbe el uso del nombre del proyecto o de los contribuyentes para respaldo sin permiso.

**Texto Completo**: https://opensource.org/licenses/BSD-3-Clause

### PostgreSQL License (Permisiva)

Usada por: PostgreSQL, proveedor Npgsql para EF Core

**Resumen**: Licencia de código abierto liberal, similar a BSD/MIT. Permite usar, copiar, modificar y distribuir, siempre que se conserven el aviso de copyright y el texto de la licencia.

**Texto Completo**: https://www.postgresql.org/about/licence/

---

## 🙏 Agradecimientos Especiales

Extendemos nuestra gratitud a:

- **La .NET Foundation** por administrar el ecosistema .NET
- **La comunidad de código abierto** por incontables horas de trabajo voluntario
- **Microsoft** por abrazar el código abierto y contribuir .NET, EF Core y ASP.NET Core
- **Google** por los servicios de autenticación y análisis
- **Todos los contribuyentes** a las bibliotecas listadas arriba

Sin sus contribuciones, este proyecto no sería posible.

---

## ⚖️ Declaración de Cumplimiento

Control Peso Thiscloud cumple con todos los términos de licencia del software y servicios de terceros listados arriba. Nosotros:

- ✅ Incluimos todos los avisos de copyright requeridos
- ✅ Proporcionamos atribución a los autores originales
- ✅ Conservamos los textos de licencia originales (enlazados arriba)
- ✅ Cumplimos con los requisitos de redistribución
- ✅ No tergiversamos el origen del código de terceros
- ✅ No usamos nombres de proyectos para respaldo sin permiso

---

## 📬 ¿Preguntas o Inquietudes?

Si cree que hemos tergiversado o no hemos cumplido con algún término de licencia, contáctenos inmediatamente:

**Correo Electrónico:** [correo de contacto por configurar]  
**Sitio Web:** https://controlpeso.thiscloud.com.ar

Tomamos el cumplimiento de licencias en serio y abordaremos cualquier problema de inmediato.

---

**Última Actualización:** 19 de noviembre de 2026

**Gracias a todos los que contribuyen al código abierto. Hacen del mundo un lugar mejor. 💙**
