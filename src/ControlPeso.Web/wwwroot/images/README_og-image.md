# Open Graph Image Placeholder

**Location**: `src/ControlPeso.Web/wwwroot/images/og-image.png`

## Specifications

- **Dimensions**: 1200x630 pixels (Facebook/LinkedIn recommended)
- **Format**: PNG
- **File size**: < 1MB (ideally < 300KB)
- **Content**:
  - Logo "Control Peso Thiscloud"
  - Icon (scale/balanza)
  - Tagline: "Seguimiento de Peso Corporal Simple y Efectivo"
  - Background: Dark theme (consistent with app)
  - Brand colors: Primary #1E88E5 (blue), Secondary #FFC107 (amber)

## Usage

This image is referenced in all pages via Open Graph meta tags:
```html
<meta property="og:image" content="https://controlpeso.thiscloud.com.ar/images/og-image.png" />
<meta name="twitter:image" content="https://controlpeso.thiscloud.com.ar/images/og-image.png" />
```

## Creation

Use a design tool (Figma, Canva, Adobe Express) to create the image with:
1. Dark background (#1E1E1E or similar)
2. Centered content with logo + tagline
3. High contrast for visibility
4. Export as PNG 1200x630

## Fallback

Until the real image is created, ensure this file exists to avoid 404 errors.
For now, a simple solid color PNG with text can be used as placeholder.

## TODO

- [ ] Create professional og-image.png with brand guidelines
- [ ] Optimize image size (< 300KB)
- [ ] Test image preview on Facebook Sharing Debugger
- [ ] Test image preview on Twitter Card Validator
- [ ] Test image preview on LinkedIn Post Inspector
