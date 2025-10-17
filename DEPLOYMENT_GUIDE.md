# Deployment Guide for MonsterASP.NET Hosting

## Changes Made to Fix 403 Error

### 1. Added `web.config` file

- Required for IIS hosting (MonsterASP.NET uses IIS)
- Configures ASP.NET Core module to run your application

### 2. Modified `Program.cs`

- **Disabled HTTPS Redirection**: Commented out `app.UseHttpsRedirection()` to avoid SSL issues
- **Enabled Swagger in Production**: Removed the Development-only check so you can test the API
- **Added CORS Support**: Allows cross-origin requests

## Deployment Steps

### Option 1: Publish via Visual Studio

1. Right-click on the project in Solution Explorer
2. Select **Publish**
3. Choose **Folder** as publish target
4. Set output path (e.g., `bin\Release\net8.0\publish`)
5. Click **Publish**
6. Upload the contents of the publish folder to MonsterASP.NET

### Option 2: Publish via Command Line

```powershell
dotnet publish -c Release -o ./publish
```

Then upload the contents of the `publish` folder to your hosting service.

### Option 3: Direct Deployment to MonsterASP.NET

1. Log in to your MonsterASP.NET control panel
2. Go to File Manager or FTP
3. Upload ALL files from your publish folder to the root directory (wwwroot)
4. Make sure the following files are present:
   - `pos_system_api.dll`
   - `web.config`
   - `appsettings.json`
   - All other DLL files

## Testing Your API

After deployment, test these URLs:

1. **Root endpoint**: http://pos-pharamcy-system.runasp.net/

   - Should return: "Welcome to the POS System API! Use /drugs to get sample drug data."

2. **Drugs list**: http://pos-pharamcy-system.runasp.net/drugs

   - Should return list of drugs

3. **Single drug**: http://pos-pharamcy-system.runasp.net/drug/1

   - Should return drug data

4. **Swagger UI**: http://pos-pharamcy-system.runasp.net/swagger
   - Interactive API documentation

## Troubleshooting

### If you still get 403 error:

1. **Check .NET Runtime**: Ensure MonsterASP.NET has .NET 8.0 runtime installed
2. **Check File Permissions**: Make sure the uploaded files have proper permissions
3. **Check Application Pool**: Ensure it's set to "No Managed Code" mode
4. **Check web.config**: Ensure it's uploaded to the root directory

### If you get 500 error:

1. Enable detailed errors by adding this to `appsettings.json`:
   ```json
   {
     "DetailedErrors": true
   }
   ```
2. Check the logs in MonsterASP.NET control panel

### Important Notes:

- The `web.config` file is CRITICAL for IIS hosting
- Make sure you publish in **Release** mode, not Debug
- Ensure all dependencies are included in the publish folder
- The hosting service must support .NET 8.0

## Environment Variables

If needed, you can set these in MonsterASP.NET control panel:

- `ASPNETCORE_ENVIRONMENT`: Set to "Production"
- `ASPNETCORE_URLS`: Usually handled automatically by IIS

## Contact MonsterASP.NET Support

If issues persist, contact their support with:

- Your project is using .NET 8.0
- It's an ASP.NET Core Web API
- You need the .NET 8.0 runtime enabled
