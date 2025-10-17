# 🔧 FIX for HTTP Error 500.0 - IIS Hosting Failure

## ✅ What I Fixed

### 1. Changed Hosting Model: `inprocess` → `outofprocess`
- **Why**: In-process mode can be unstable on some IIS configurations
- **Benefit**: More stable and compatible with shared hosting

### 2. Added Global Error Handling
- **Why**: Crashes now show error messages instead of generic 500 error
- **Benefit**: You can see what's actually wrong

### 3. Added Detailed Error Logging
- **Why**: Errors will be written to log files
- **Benefit**: Even if the page doesn't show it, you can check logs folder

### 4. Added Try-Catch to All Endpoints
- **Why**: Prevents unhandled exceptions from crashing the app
- **Benefit**: API returns proper error responses

### 5. Created Logs Directory
- **Why**: App needs a place to write error logs
- **Benefit**: You can troubleshoot issues

---

## 🚀 HOW TO FIX - Step by Step

### Step 1: Re-Publish Your Application

Run this command in PowerShell:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

Or use the script:
```powershell
.\publish-self-contained.ps1
```

### Step 2: Verify Files

Check that these files exist in `publish` folder:
- ✅ `pos_system_api.exe`
- ✅ `web.config` (should have `hostingModel="outofprocess"`)
- ✅ `logs` folder (empty folder)
- ✅ `appsettings.json`
- ✅ All DLL files

### Step 3: Upload to MonsterASP.NET

1. **DELETE all old files first** (important!)
2. Upload ALL files from `publish` folder
3. Make sure the `logs` folder is created on server (even if empty)

### Step 4: Set Folder Permissions

In MonsterASP.NET control panel:
1. Find the `logs` folder
2. Set permissions to allow **Write** access
3. This allows the app to write error logs

### Step 5: Test Your API

Visit:
- http://pos-pharamcy-system.runasp.net/
- http://pos-pharamcy-system.runasp.net/drugs
- http://pos-pharamcy-system.runasp.net/swagger

---

## 🔍 If Still Getting 500.0 Error

### Check 1: Look for Log Files
1. In MonsterASP.NET, navigate to the `logs` folder
2. Look for files like `stdout_*.log`
3. Download and read them - they contain the actual error

### Check 2: Verify web.config
Make sure your `web.config` has:
```xml
hostingModel="outofprocess"
```
NOT:
```xml
hostingModel="inprocess"
```

### Check 3: Check Application Pool Settings
In MonsterASP.NET control panel:
1. Find "Application Pool" or "App Pool" settings
2. Make sure it's set to **"No Managed Code"** or **".NET CLR Version: No Managed Code"**
3. Make sure it's set to **"Integrated"** pipeline mode

### Check 4: Restart Application
In MonsterASP.NET control panel:
1. Find "Restart Application" or "Recycle App Pool"
2. Click it to restart your app
3. Try accessing the URL again

---

## 🆘 Common Issues and Solutions

### Issue: "logs" folder not found
**Solution**: Create the folder manually in MonsterASP.NET File Manager

### Issue: Still getting 500.0 after following all steps
**Solution**: Contact MonsterASP.NET support and ask them to:
1. Check Application Pool settings
2. Verify ASP.NET Core Module v2 is installed
3. Check server Event Viewer for detailed errors

### Issue: App works locally but not on server
**Solution**: 
1. Test locally with Release build: `dotnet run -c Release`
2. Make sure all required files are uploaded
3. Check file paths are correct (case-sensitive on some systems)

---

## 📋 Files Changed

| File | What Changed | Why |
|------|-------------|-----|
| `web.config` | `hostingModel="outofprocess"` | More stable hosting |
| `web.config` | Added `ASPNETCORE_DETAILEDERRORS` | Shows detailed errors |
| `Program.cs` | Added global error handler | Catches all exceptions |
| `Program.cs` | Added try-catch to endpoints | Prevents crashes |
| `Program.cs` | Added Kestrel configuration | Better IIS compatibility |
| `appsettings.json` | Added `DetailedErrors: true` | Enable error details |
| `publish-self-contained.ps1` | Creates logs folder | For error logging |

---

## 🎯 What Each Change Does

### OutOfProcess Hosting
```
Before: IIS → ASP.NET Core (in same process) ❌ Can crash IIS
After:  IIS → Separate Process → ASP.NET Core ✅ More stable
```

### Error Handling
```
Before: Exception → 500.0 generic error ❌ No details
After:  Exception → Logged + JSON error ✅ Can debug
```

---

## 📞 Contact Support With This Info

If you need to contact MonsterASP.NET support, tell them:

> "I'm deploying a .NET 8.0 ASP.NET Core application with self-contained deployment.
> 
> I'm getting HTTP Error 500.0 - IIS hosting failure (in-process).
> 
> I've configured:
> - hostingModel="outofprocess" in web.config
> - Self-contained deployment (includes .NET runtime)
> - Created logs folder for error logging
> 
> Can you please:
> 1. Verify ASP.NET Core Module v2 is installed on the server
> 2. Check if Application Pool is set to 'No Managed Code'
> 3. Check server Event Viewer for specific error messages
> 4. Confirm if there are any path or permission issues"

---

## ✨ Next Steps

1. ✅ Re-publish using the command above
2. ✅ Delete old files on server
3. ✅ Upload new files
4. ✅ Create `logs` folder (if not automatically created)
5. ✅ Set write permissions on `logs` folder
6. ✅ Test the URL
7. ✅ Check logs folder if still getting errors

---

**Remember**: The `outofprocess` hosting model is more compatible with shared hosting environments!
