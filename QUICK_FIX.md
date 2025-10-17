# 🚨 QUICK FIX - 500.0 Error

## The Problem
Your app is crashing when IIS tries to run it in-process mode.

## The Solution
Changed to out-of-process mode + added error handling.

---

## DO THIS NOW:

### 1️⃣ Re-Publish
```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

### 2️⃣ Check web.config
Open `publish/web.config` and verify this line says **outofprocess**:
```xml
hostingModel="outofprocess"
```

### 3️⃣ Create logs folder
```powershell
mkdir publish\logs
```

### 4️⃣ Upload Everything
- Delete old files on server
- Upload ALL files from `publish` folder
- Make sure `logs` folder exists on server

### 5️⃣ Set Permissions
In MonsterASP.NET:
- Give `logs` folder WRITE permissions

### 6️⃣ Test
- http://pos-pharamcy-system.runasp.net/

---

## If Still Broken:

1. **Check logs folder** on server for error files
2. **Restart application** in hosting control panel
3. **Contact support** - tell them you need ASP.NET Core Module v2

---

## Key Changes:
✅ Changed `inprocess` → `outofprocess` (more stable)
✅ Added error logging (check logs folder)
✅ Added error handling (won't crash on errors)
✅ Enabled detailed errors (can see what's wrong)

---

## Test Commands (run locally first):
```powershell
cd publish
.\pos_system_api.exe
```

Then visit http://localhost:5000

If it works locally but not on server, it's a hosting configuration issue.

---

**Read `FIX_500_0_ERROR.md` for detailed troubleshooting!**
