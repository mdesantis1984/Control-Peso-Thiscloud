# AGENTS — ThisCloud.ControlPeso (Copilot/Claude Sonnet en modo agente)

Este archivo define cómo deben operar los **agentes** (Copilot Agent / Claude Sonnet) en este repositorio.

## Principios

- **No alucinar**: si falta información o acceso (p.ej. sitio de referencia), crear una tarea `PENDIENTE` en el plan y **no inventar UI/copy/entidades**.
- **Minimalismo**: priorizar el MVP. Menos pantallas, menos entidades, mejor UX.
- **Arquitectura cebolla**: respetar dependencias (Domain → Application → Infrastructure → Presentation).
- **MudBlazor only**: UI solo con MudBlazor.
- **Database First**: schema SQL es la fuente de verdad; el código se scaffoldea desde DB.

## Flujo de trabajo recomendado (1 PR = 1 objetivo)

1) Elegir una tarea del plan (`CPx.y`).
2) Implementar cambios mínimos que cumplan el criterio de aceptación.
3) Agregar tests (cuando aplique).
4) Actualizar docs si se cambia comportamiento/config/esquema.

## Validaciones antes de finalizar

- `dotnet build -c Release`
- `dotnet test -c Release` (si hay tests)
- Confirmar:
  - No se agregaron secretos al repo
  - No se introdujeron librerías UI ajenas a MudBlazor
  - Se mantuvo la separación de capas

## Artefactos contractuales

- `.github/copilot-instructions.md`
- `PLAN_ThisCloud.ControlPeso_v1.0.md`
- `docs/db/sqlite/schema_v1.sql` (o equivalente)

