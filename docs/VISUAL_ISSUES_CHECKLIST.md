# Checklist de Problemas Visuales - Control Peso Thiscloud

**Fecha**: 2026-02-20  
**Revisión**: Usuario final vs Prototipo (docs/screenshots)

## ⚠️ CONTEXTO IMPORTANTE

**Prototipo**: Tailwind CSS + HTML custom (docs/screenshots/*.html)  
**App real**: Blazor Server + MudBlazor 8.0.0 (Material Design)

**Limitaciones técnicas**:
- Typography: Prototipo usa "Inter", MudBlazor usa "Roboto" (Material Design default)
- Componentes: Prototipo HTML custom, MudBlazor tiene su propio sistema de componentes
- Grid system: Prototipo Tailwind (flexbox custom), MudBlazor usa Material Grid (12 columns)

---

## 🔴 PROBLEMAS CRÍTICOS (Bloqueantes)

### Textos rotos
- [ ] **Ubicación**: _________________________________
- [ ] **Descripción**: _________________________________
- [ ] **Screenshot**: _________________________________

### Errores de carga
- [ ] **Tipo error**: Console / Network / Visual
- [ ] **Mensaje**: _________________________________
- [ ] **Screenshot**: _________________________________

### Layout roto
- [ ] **Ubicación**: _________________________________
- [ ] **Descripción**: _________________________________
- [ ] **Screenshot**: _________________________________

---

## 🟡 DIFERENCIAS VISUALES (Alta prioridad)

### Dashboard
- [ ] **AppBar height**: Prototipo: ___px vs Actual: ___px
- [ ] **Drawer width**: Prototipo: ___px vs Actual: ___px
- [ ] **StatsCard spacing**: Prototipo: ___px vs Actual: ___px
- [ ] **StatsCard padding**: Prototipo: ___px vs Actual: ___px
- [ ] **Typography scale**: ¿Títulos con mismo tamaño que prototipo?
- [ ] **Colors**: ¿Background #0f172a vs #121212?
- [ ] **Iconos**: ¿Material Symbols vs Material Icons?
- [ ] **FAB**: ¿Posición bottom-right correcta?

### Profile
- [ ] **Avatar size**: Prototipo: ___px vs Actual: ___px
- [ ] **Form layout**: ¿2 columnas desktop correcto?
- [ ] **Field spacing**: Prototipo: ___px vs Actual: ___px

### History
- [ ] **Table row height**: Prototipo: ___px vs Actual: ___px
- [ ] **Stripe rows**: ¿Alternancia visible?
- [ ] **Pagination**: ¿Position y styling correctos?

---

## 🟢 VERIFICACIONES FUNCIONALES

### Responsive
- [ ] Desktop (1920x1080): ¿Layout correcto?
- [ ] Tablet (768x1024): ¿2 columnas correcto?
- [ ] Mobile (375x667): ¿1 columna + drawer overlay?

### Performance
- [ ] ¿Carga inicial < 3s?
- [ ] ¿Transiciones suaves sin stuttering?
- [ ] ¿Scroll fluido?

### Accesibilidad
- [ ] ¿Tab navigation funcional?
- [ ] ¿Contraste texto/fondo correcto?

---

## 📝 NOTAS ADICIONALES

_Agregar aquí observaciones específicas del usuario:_

---

**ACCIÓN REQUERIDA**: Por favor completa este checklist con los problemas específicos que observas en http://localhost:8080
