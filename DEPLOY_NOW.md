# ✅ DEPLOYMENT CHECKLIST - .NET 6.0 Build

## 🎯 CURRENT STATUS: READY TO DEPLOY!

Your application has been successfully published with .NET 6.0!

---

## 📋 PRE-UPLOAD CHECKLIST

✅ Application published to `publish` folder
✅ Using .NET 6.0 (compatible with MonsterASP.NET)
✅ Self-contained deployment (includes runtime)
✅ OutOfProcess hosting model
✅ Error logging enabled
✅ All files ready

---

## 🚀 UPLOAD STEPS

### 1. Login to MonsterASP.NET

- Go to control panel
- Open File Manager or FTP

### 2. Clean Old Files

- Navigate to your web root folder
- **DELETE ALL old files** (important!)
- Start fresh

### 3. Upload New Files

- Upload **ALL files** from `publish` folder
- This includes:
  - pos_system_api.exe
  - web.config
  - appsettings.json
  - All .dll files
  - logs folder (create if not uploaded)

### 4. Set Permissions

- Find `logs` folder on server
- Right-click → Properties/Permissions
- Enable **Write** permission
- Save changes

### 5. Restart (if available)

- Look for "Restart Application" option
- Or "Recycle App Pool"
- Click it to restart your app

---

## 🧪 TEST YOUR API

### URL 1: Root

http://pos-pharamcy-system.runasp.net/

**Expected**: Welcome message

### URL 2: Drugs List

http://pos-pharamcy-system.runasp.net/drugs

**Expected**: JSON with drug data

### URL 3: Swagger UI

http://pos-pharamcy-system.runasp.net/swagger

**Expected**: API documentation page

---

## ❌ TROUBLESHOOTING

### If 502.5 Error Again:

1. Check if ALL files were uploaded
2. Verify `pos_system_api.exe` exists on server
3. Check `logs` folder for error files
4. Ensure `logs` folder is writable

### If 500 Error:

1. Check `logs` folder on server
2. Download and read `stdout_*.log` files
3. Look for error messages

### If 404 Error:

1. Files in wrong directory
2. Upload to web root (wwwroot/httpdocs/public_html)

### If Blank Page:

1. Wait 30 seconds (first startup is slow)
2. Refresh the page
3. Check if application started

---

## 📊 FILE COUNT

Your publish folder should have approximately:

- **~120-150 files** (self-contained includes .NET runtime)
- **Size**: ~80-100 MB total

If you have significantly fewer files, the publish didn't complete!

---

## ⚡ QUICK COMMANDS

### Test Locally (Optional):

```powershell
cd publish
.\pos_system_api.exe
```

Visit: http://localhost:5000

### Re-publish if needed:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

---

## 🎉 SUCCESS INDICATORS

You'll know it's working when:

- ✅ No error codes (403, 500, 502)
- ✅ Welcome message appears at root URL
- ✅ `/drugs` returns JSON data
- ✅ Swagger UI loads and works
- ✅ Can interact with API endpoints

---

## 📁 FILES IN PUBLISH FOLDER

Check these are present before uploading:

**Critical Files**:

- [x] pos_system_api.exe (main app)
- [x] web.config (IIS configuration)
- [x] appsettings.json (settings)
- [x] logs (folder for errors)

**Runtime Files** (many DLLs):

- [x] System.\*.dll files
- [x] Microsoft.\*.dll files
- [x] All dependency DLLs

---

## 🔧 FINAL CONFIGURATION

| Setting         | Value          |
| --------------- | -------------- |
| .NET Version    | 6.0            |
| Deployment Type | Self-Contained |
| Platform        | Windows x64    |
| Hosting Model   | OutOfProcess   |
| Error Logging   | Enabled        |
| Swagger         | Enabled        |
| CORS            | Enabled        |

---

## 💪 YOU'VE GOT THIS!

1. Upload all files from `publish` folder
2. Set `logs` folder permissions
3. Test the URLs
4. Done! 🎉

**If you encounter any issues, check the `logs` folder on the server first!**

---

**Build Date**: October 17, 2025
**Status**: ✅ READY FOR PRODUCTION
**Action**: Upload and test!
