# Plan Fase 9 - Frontend Pixel Perfect

**Fecha creación**: 2026-02-18  
**Estado**: ⏳ **PENDIENTE** (0/35 tareas)  
**Objetivo**: Refinar UI/UX hasta lograr diseño "pixel perfect" según prototipo Google AI Studio

---

## Contexto

El proyecto tiene **backend 100% completo** (arquitectura, lógica de negocio, tests, seguridad, SEO), pero el **frontend está al ~50%**: funcional pero sin el polish visual necesario.

**Problemas actuales**:
- Espaciados inconsistentes entre componentes
- Tamaños de fuentes no optimizados
- Responsive design mejorable (especialmente mobile)
- Transiciones abruptas (falta suavidad)
- Iconografía inconsistente
- Colores no totalmente alineados al prototipo
- Algunos componentes MudBlazor sin configurar parámetros óptimos

**Meta**: Lograr que la UI sea **indistinguible** del prototipo de referencia (Google AI Studio), con atención extrema al detalle.

---

## Fase 9 - Tareas

### 9.1 - Auditoría Visual Completa (3 tareas)

- [ ] **P9.1.1** Capturar screenshots del prototipo (Google AI Studio) para cada página
  - Login.razor → Captura de referencia
  - Dashboard.razor → Captura de referencia
  - Profile.razor → Captura de referencia
  - History.razor → Captura de referencia
  - Trends.razor → Captura de referencia
  - Admin.razor → Captura de referencia
  - Crear carpeta `docs/screenshots/prototype/` con imágenes

- [ ] **P9.1.2** Capturar screenshots actuales de la app Blazor (mismo viewport)
  - Usar mismas páginas que P9.1.1
  - Crear carpeta `docs/screenshots/current/` con imágenes
  - Usar viewport estándar: 1920x1080 (desktop), 768x1024 (tablet), 375x667 (mobile)

- [ ] **P9.1.3** Crear documento comparativo con discrepancias
  - Crear `docs/UI_DISCREPANCIES.md`
  - Tabla: Página | Elemento | Prototipo | Actual | Acción requerida
  - Priorizar por severidad: CRÍTICO > ALTA > MEDIA > BAJA
  - Ejemplo: "Dashboard - StatsCard padding: 24px → 16px (Actual)"

### 9.2 - Typography System (4 tareas)

- [ ] **P9.2.1** Definir sistema de tipografía en `ControlPesoTheme.cs`
  - Revisar prototipo: tamaños, pesos, line-heights
  - Actualizar `MudTheme.Typography` con valores exactos
  - Ejemplo: H1 → 32px/400, H2 → 24px/500, Body1 → 14px/400
  - Definir jerarquía: Display, Headline, Title, Body, Caption

- [ ] **P9.2.2** Aplicar tipografía consistente en todos los componentes
  - MainLayout.razor → Usar `Typo.` de MudBlazor
  - Dashboard.razor → Revisar títulos y labels
  - Profile.razor → Consistencia en forms
  - History.razor → Tabla headers y cells
  - Trends.razor → Charts labels y legends
  - Admin.razor → Tabla y stats cards

- [ ] **P9.2.3** Configurar font weights correctos
  - Regular (400) → Body text
  - Medium (500) → Subtítulos
  - SemiBold (600) → Titles
  - Bold (700) → Headlines importantes
  - Verificar carga de Google Fonts en `App.razor` si necesario

- [ ] **P9.2.4** Ajustar line-heights y letter-spacing
  - Revisar legibilidad en textos largos
  - Ajustar `LineHeight` en theme
  - Configurar `LetterSpacing` para headings (-0.5px típico)

### 9.3 - Spacing System (5 tareas)

- [ ] **P9.3.1** Definir sistema de espaciados en `ControlPesoTheme.cs`
  - Crear constantes: XS=4px, SM=8px, MD=16px, LG=24px, XL=32px, XXL=48px
  - Alinear con sistema de 8pt grid (múltiplos de 8)
  - Documentar en comentarios XML

- [ ] **P9.3.2** Auditar y corregir paddings en todos los componentes
  - MudCard → Class="pa-4" (16px) consistente
  - MudPaper → Class="pa-6" (24px) para contenedores grandes
  - MudDialog → DialogContent padding 24px
  - Forms → Field spacing 16px vertical
  - Revisar prototipo para cada componente

- [ ] **P9.3.3** Auditar y corregir margins entre secciones
  - Dashboard → Spacing entre StatsCards: 16px
  - Profile → Spacing entre secciones: 24px
  - History → Spacing tabla-filtros: 16px
  - Trends → Spacing entre charts: 24px
  - Admin → Spacing stats-tabla: 32px

- [ ] **P9.3.4** Configurar gaps en layouts flexbox/grid
  - MainLayout → MudLayout Spacing consistente
  - NavMenu → Item spacing vertical: 8px
  - Dashboard grid → Gap: 16px (entre cards)
  - Forms → Stack spacing: 16px

- [ ] **P9.3.5** Responsive spacing adjustments
  - Desktop: Usar spacings completos (MD, LG, XL)
  - Tablet: Reducir 25% (MD→SM, LG→MD)
  - Mobile: Reducir 50% (MD→XS, LG→SM)
  - Media queries en CSS custom si necesario

### 9.4 - Color System (4 tareas)

- [ ] **P9.4.1** Extraer paleta exacta del prototipo
  - Primary: #1E88E5 (blue) → Verificar con eyedropper
  - Secondary: #FFC107 (amber) → Verificar
  - Success: #4CAF50 → Verificar
  - Error: #F44336 → Verificar
  - Warning: #FF9800 → Verificar
  - Info: #2196F3 → Verificar
  - Dark: #121212 (background oscuro) → Verificar
  - Surface: #1E1E1E → Verificar
  - Crear `docs/COLOR_PALETTE.md` con valores RGB/HEX

- [ ] **P9.4.2** Actualizar `ControlPesoTheme.cs` con paleta exacta
  - `Palette.Primary` → Color exacto prototipo
  - `Palette.Secondary` → Color exacto prototipo
  - `Palette.Background` → Fondo oscuro exacto
  - `Palette.Surface` → Cards fondo exacto
  - `Palette.TextPrimary` → Texto blanco opacidad 87%
  - `Palette.TextSecondary` → Texto blanco opacidad 60%
  - `Palette.Divider` → Líneas separadoras opacidad 12%

- [ ] **P9.4.3** Revisar contraste WCAG AA en todos los componentes
  - Ratio mínimo: 4.5:1 para texto normal
  - Ratio mínimo: 3:1 para texto grande (18px+)
  - Herramienta: WebAIM Contrast Checker
  - Ajustar colores si no cumplen

- [ ] **P9.4.4** Aplicar estados hover/active/disabled consistentes
  - Buttons → Hover: lighten 10%, Active: darken 10%
  - Links → Hover: underline + lighten 20%
  - Cards → Hover elevation +2dp (si clickeable)
  - Inputs → Focus: border primary color + glow

### 9.5 - Component Refinement (8 tareas)

- [ ] **P9.5.1** Refinar `MainLayout.razor`
  - MudAppBar height: 64px (verificar prototipo)
  - MudDrawer width: 280px (verificar prototipo)
  - App bar shadow: Elevation 4dp
  - Drawer shadow: Elevation 8dp
  - Logo size y posición exacta
  - User avatar size: 40x40px

- [ ] **P9.5.2** Refinar `Dashboard.razor`
  - Grid layout: 4 columns desktop, 2 tablet, 1 mobile
  - StatsCard dimensiones: 100% width, min-height 120px
  - WeightChart aspect ratio: 16:9
  - Spacing entre elementos: 16px
  - FAB position: bottom-right, offset 24px
  - FAB size: Large (56x56px)

- [ ] **P9.5.3** Refinar `StatsCard.razor`
  - Padding: 16px
  - Icon size: 48x48px
  - Icon color: Primary
  - Value font: H4 (24px/500)
  - Label font: Body2 (14px/400)
  - Trend indicator: Arrow + percentage + color

- [ ] **P9.5.4** Refinar `WeightChart.razor`
  - MudChart configurar todos los parámetros:
    - ChartOptions: InterpolationOption = Smooth
    - XAxisLabels font size: 12px
    - YAxisLabels font size: 12px
    - Legend position: Bottom
    - Grid lines: color rgba(255,255,255,0.1)
    - Line width: 3px
    - Point radius: 4px (hover: 6px)

- [ ] **P9.5.5** Refinar `AddWeightDialog.razor`
  - Dialog MaxWidth: 600px
  - Title bar: MudText Typo.H6
  - Content padding: 24px
  - Form field spacing: 16px
  - Date picker format: dd/MM/yyyy
  - Time picker format: HH:mm
  - Action buttons: spacing 8px, height 40px

- [ ] **P9.5.6** Refinar `History.razor` (MudDataGrid)
  - Row height: 56px
  - Header height: 64px
  - Cell padding: 16px horizontal, 12px vertical
  - Stripe rows: alternating background
  - Hover row: elevation +1dp + background lighten 5%
  - Pagination: bottom, items per page [10, 25, 50, 100]

- [ ] **P9.5.7** Refinar `Profile.razor`
  - Avatar upload: circular, 128x128px, centered
  - Form layout: 2 columns desktop, 1 mobile
  - Field width: 100% max-width 400px
  - Submit button: Full width mobile, auto desktop
  - Section dividers: 1px solid Divider color

- [ ] **P9.5.8** Refinar `Admin.razor`
  - Stats grid: 4 columns desktop, 2 tablet, 1 mobile
  - Stats card: min-height 100px, icon left
  - User table: similar a History pero con acciones
  - Dialog actions: ChangeRole, ChangeStatus, Delete
  - Export button: top-right, MudButton Variant.Outlined

### 9.6 - Iconography (3 tareas)

- [ ] **P9.6.1** Auditar todos los iconos usados actualmente
  - Crear lista: Componente | Icon | MudBlazor.Icons.Material.*
  - Verificar consistencia: Filled vs Outlined vs Rounded
  - Verificar tamaños: Small (20px), Medium (24px), Large (32px)

- [ ] **P9.6.2** Alinear iconografía con prototipo
  - Dashboard → Icons.Material.Filled.Dashboard
  - Profile → Icons.Material.Filled.Person
  - History → Icons.Material.Filled.History
  - Trends → Icons.Material.Filled.TrendingUp
  - Admin → Icons.Material.Filled.AdminPanelSettings
  - Add Weight → Icons.Material.Filled.Add (FAB)
  - Logout → Icons.Material.Filled.Logout

- [ ] **P9.6.3** Configurar icon buttons y FABs
  - Icon button size: Medium default, Small en toolbars
  - FAB size: Large (56x56px) con Icon size 24px
  - FAB shadow: Elevation 6dp, hover 8dp
  - Icon color: inherit de button color

### 9.7 - Animations & Transitions (4 tareas)

- [ ] **P9.7.1** Configurar transiciones MudBlazor
  - MudCard: Hover transition 300ms ease-in-out
  - MudButton: Ripple effect habilitado
  - MudDialog: Slide-up animation 250ms
  - MudDrawer: Slide transition 225ms
  - MudSnackbar: Slide-in bottom 300ms

- [ ] **P9.7.2** Configurar page transitions
  - Route change: Fade transition 150ms
  - Component mount: Fade-in 200ms
  - Component unmount: Fade-out 150ms
  - Evitar flash of unstyled content (FOUC)

- [ ] **P9.7.3** Configurar skeleton loaders
  - Dashboard → MudSkeleton mientras carga stats
  - WeightChart → MudSkeleton rectangular mientras carga
  - History table → MudSkeleton rows mientras carga
  - Profile → MudSkeleton avatar y forms mientras carga

- [ ] **P9.7.4** Configurar loading indicators
  - Global: MudOverlay con MudProgressCircular
  - Local: MudProgressLinear en top de contenedor
  - Buttons: MudButton Loading=true disable + spinner
  - Forms: Disable inputs durante submit

### 9.8 - Responsive Design (4 tareas)

- [ ] **P9.8.1** Definir breakpoints en `ControlPesoTheme.cs`
  - XS: 0-599px (mobile)
  - SM: 600-959px (tablet portrait)
  - MD: 960-1279px (tablet landscape)
  - LG: 1280-1919px (desktop)
  - XL: 1920px+ (large desktop)

- [ ] **P9.8.2** Optimizar Desktop (LG, XL)
  - MainLayout: Drawer permanent visible
  - Dashboard: Grid 4 columns
  - Forms: 2 columns layout
  - Tables: Mostrar todas las columnas
  - Charts: Full width

- [ ] **P9.8.3** Optimizar Tablet (SM, MD)
  - MainLayout: Drawer colapsable
  - Dashboard: Grid 2 columns
  - Forms: 1 column layout
  - Tables: Ocultar columnas secundarias
  - Charts: 100% width container

- [ ] **P9.8.4** Optimizar Mobile (XS)
  - MainLayout: Drawer overlay (no permanent)
  - Dashboard: Grid 1 column, cards full width
  - Forms: 1 column, full width inputs
  - Tables: Responsive mode (cards en vez de tabla)
  - Charts: Aspect ratio 1:1, height 300px

### 9.9 - Performance Optimization (4 tareas)

- [ ] **P9.9.1** Optimizar render performance
  - Agregar `@key` en loops de componentes
  - Usar `ShouldRender()` override donde aplique
  - Evitar `StateHasChanged()` innecesarios
  - Virtualizar listas largas con `MudVirtualize`

- [ ] **P9.9.2** Optimizar asset loading
  - Lazy load components con `@lazy`
  - Preconnect a fonts.googleapis.com
  - Preload critical CSS
  - Defer non-critical JavaScript

- [ ] **P9.9.3** Run Lighthouse audit
  - Performance: Target 90+
  - Accessibility: Target 100
  - Best Practices: Target 100
  - SEO: Target 100
  - Documentar resultados en `docs/LIGHTHOUSE_REPORT.md`

- [ ] **P9.9.4** Optimizar bundle size
  - Analizar bundle con BundleAnalyzer
  - Tree-shaking de MudBlazor (si posible)
  - Comprimir assets (Brotli, Gzip)
  - Documentar en `docs/PERFORMANCE.md`

---

## Criterios de Aceptación Global (Fase 9)

- [ ] **Visual**: App es indistinguible del prototipo en viewport 1920x1080
- [ ] **Responsive**: Funciona perfecto en mobile (375px), tablet (768px), desktop (1920px)
- [ ] **Typography**: Sistema consistente aplicado en todos los componentes
- [ ] **Spacing**: 8pt grid respetado, spacing consistente
- [ ] **Colors**: Paleta exacta del prototipo, contraste WCAG AA cumplido
- [ ] **Icons**: Iconografía consistente (estilo único: Filled o Outlined)
- [ ] **Animations**: Transiciones suaves (200-300ms), skeleton loaders en carga
- [ ] **Performance**: Lighthouse Performance 90+, sin flash of unstyled content
- [ ] **Accessibility**: Keyboard navigation fluida, screen reader compatible
- [ ] **Cross-browser**: Funciona en Chrome, Edge, Firefox, Safari (últimas 2 versiones)

---

## Estimación

- **Tiempo estimado**: 30-40 horas
- **Complejidad**: Media-Alta
- **Prioridad**: Alta (bloqueante para release v1.0 final)

---

## Notas

- Esta fase es **puramente cosmética/visual** — no agrega funcionalidad nueva
- El backend (lógica, tests, seguridad) NO cambia
- Requiere atención extrema al detalle ("pixel peeping")
- Usar herramientas: Color Picker, Rulers, Screenshot Diff Tools
- Testing manual exhaustivo en todos los viewports

---

**Ready to start Fase 9?** 🎨
