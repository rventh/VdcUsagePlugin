# How to install and set up the VDC Usage plugin

This guide takes about ten minutes. You need a VSPC account with the **Portal Administrator** role.

## 1. Install the plugin

1. Download `VdcUsagePlugin.<version>.nupkg` from the repository's Releases page (or build it yourself, see the README).
2. In VSPC, go to **Configuration**, then **Catalog**, and upload the package.
3. Install it. After install, the **VDC Usage** tile appears in the Catalog. Click the tile to open the plugin.

## 2. Create a VSPC API key

> **About the key VSPC creates on install.** Older builds of this plugin let VSPC auto-generate a REST API key
> (a REST API Keys entry with the description "Auto-generated key for '...' plugin"). The plugin never used it.
> From 1.0.10 the plugin asks VSPC not to create one. If you installed an earlier build, you can safely delete that
> entry in **Configuration > REST API Keys**. The plugin only uses the read-only key you create below.


The plugin reads Veeam Data Cloud usage through the VSPC REST API, so it needs an API key. Create a dedicated key for it:

1. In VSPC, go to **Configuration**, then **REST API Keys**.
2. Click **New**, then **Simple Key (Recommended)**.
3. Enter a description such as `VDC Usage plugin`.
4. Turn **Read-only access** on. The plugin only reads data, so it does not need more.
5. Click **Create**, then **Copy key to clipboard**.
   VSPC shows the private key only when you create it. Paste it somewhere safe until step 3.

## 3. Give the key to the plugin

1. Open the plugin and go to **Settings**.
2. Paste the key under **New API key** and choose **Verify and save**.
3. The plugin tests the key before saving it. When it works, the status shows **Connected**.

The key is stored encrypted on the VSPC server (Windows DPAPI). The plugin never shows it again, and it is not sent to the browser.
To change it, paste a new key. To remove it, choose **Remove saved key**.

## 4. Map tenants to VSPC companies (optional)

Still on **Settings**, the **Tenant mapping** panel lists every Veeam Data Cloud tenant.

- Pick the matching VSPC company from the drop-down. The choice is saved by ID.
- If a tenant's name exactly matches one company, it is matched for you and marked **Name match**. Choose **Confirm** to keep it.
- Tenants already mapped in Pulse show as **Set in Pulse** and cannot be changed here.

## 5. Use it

- **Usage report**: filter by month range, tenant, product family, product, Vault cloud and Core/Non-Core. Export to CSV or PDF.
- **Dashboard**: points per month, by tenant and by product.

## Troubleshooting

| What you see | What to do |
| --- | --- |
| "No API key is saved yet" | Complete step 3. |
| Key is rejected when saving | Check you copied the whole key, and that it was created by a Portal Administrator. |
| No usage lines | Check that Pulse has data for the months you picked, then use **Reload from VSPC**. |
| Tenant mapping dropdown is empty | The key could not read VSPC companies. Create the key with access to organizations. |
| Plugin tile shows a 404 page | Reinstall the plugin and open it from the Catalog tile again. |

Reinstalling the plugin clears the saved key, so enter it again afterwards.
For deeper problems, the plugin writes logs to `C:\ProgramData\VdcUsage\Logs` on the VSPC server.

## Security notes

- Use a dedicated, read-only key and give it a clear description so it is easy to find and revoke.
- Revoke the key in **REST API Keys** if the plugin is removed or the key is ever shared by mistake.
- The plugin uses only the key you save in Settings. It does not use any other key from VSPC.
- Only Portal Administrators can open the plugin.
