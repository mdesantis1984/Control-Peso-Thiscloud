# Open Graph image

**File**: `src/ControlPeso.Web/wwwroot/images/og-image.png` (served as `/images/og-image.png`)

## Current state

- PNG, 700 x 660 px, about 700 KB.

## Where it is used

- `og:image` and `twitter:image` meta tags in `Components/App.razor` (default for every page) and in the `<HeadContent>` of `Dashboard`, `History`, `Profile` and `Admin`, with the absolute URL `https://controlpeso.thiscloud.com.ar/images/og-image.png`.
- Small logo (48 px) in the header of the legal pages (`/privacy`, `/terms`, `/licenses`, `/changelog`) and in the Dashboard and Admin headers.

Do not rename or delete the file without updating those references (a missing file produces 404 errors and broken link previews).

## Recommended specs when it is replaced

| Property | Value |
|----------|-------|
| Dimensions | 1200 x 630 px (the ratio Facebook and X expect for large previews) |
| Format | PNG or JPEG |
| Size | Under 300 KB |
| Content | Logo, short tagline, dark background consistent with the app theme |

After replacing it, check the previews with the Facebook Sharing Debugger and a Twitter/X card validator. Social networks cache the previous image, so use the debugger tools to refresh it.
