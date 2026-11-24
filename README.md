# VDC Usage plugin for Veeam Service Provider Console

A Veeam Service Provider Console (VSPC) plugin that shows Veeam Data Cloud (VDC) monthly usage inside VSPC, so service-provider engineers do not need to log in to Pulse.
It reads `GET /api/v3/pulse/monthlyUsages` from the VSPC REST API.

## Features

- **Usage report**: filter by month range, tenant, product family, product (SKU), Vault cloud, Vault Core/Non-Core and tenant mapping. Export to CSV and PDF.
- **Dashboard**: points per month, split by product, by tenant, and tenant trends.
- **Tenant mapping**: tie each Pulse tenant to a VSPC company (set in Pulse, picked in the plugin, or an exact name match you confirm).
- **PDF export** follows the layout of the VSPC License Usage report.

## Setup in VSPC

Full walkthrough: [docs/HOWTO.md](docs/HOWTO.md).

1. In VSPC, create a REST API key that can read Pulse usage and organizations (Configuration, API keys).
2. Install the plugin package (Configuration, Catalog).
3. Open the plugin from its tile, go to **Settings**, paste the key and choose **Verify and save**.
   The key is stored encrypted on the VSPC server (Windows DPAPI) and is never shown again.
4. Only VSPC Portal Administrators can open the plugin.

## Building

Requirements: .NET 10 SDK, Node.js 20+ and npm.

The Veeam UI kit packages are **not** in this repository (Veeam's licence does not allow republishing them).
Get the VSPC plugin SDK template from Veeam and copy its `npm_packages` folder next to `build.bat`, then:

```
create_sign_certificate.ps1   # once: makes your own signing certificate (sign.p12)
build.bat
```

The signed package is written to `build\`.

## Security notes

- Never commit `sign.p12`, API keys or passwords. The `.gitignore` already excludes certificates and key files.
- The plugin only calls the VSPC REST API it is installed on, with the key you give it. Use a read-only key.
- VSPC can auto-generate an unused API key when a plugin is installed. This plugin asks VSPC not to (since 1.0.10). On older builds, delete the entry named "Auto-generated key for ... plugin" in REST API Keys.
- Exports neutralise spreadsheet formulas in tenant and product names.

## Licence

The plugin uses Veeam's proprietary UI kit. See `LICENSE.txt` (Veeam's UI kit licence) for the terms that apply to it.
