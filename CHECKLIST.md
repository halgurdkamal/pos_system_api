# ✅ DEPLOYMENT CHECKLIST

## What to Do Right Now:

### 1️⃣ Open PowerShell in Project Folder

```powershell
cd "C:\Users\pc\Documents\My Projects\pos_system_api"
```

### 2️⃣ Run the Publish Command

**OPTION A - Recommended (Self-Contained)**:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

_This creates ~80-120 MB of files_

**OPTION B - If hosting has .NET 6.0**:
First, edit `pos_system_api.csproj` and change `net8.0` to `net6.0`, then:

```powershell
dotnet publish -c Release -o ./publish
```

_This creates ~5-10 MB of files_

### 3️⃣ Verify Files

Open the `publish` folder and check:

- [ ] `pos_system_api.exe` exists (for Option A)
- [ ] `pos_system_api.dll` exists (for Option B)
- [ ] `web.config` exists
- [ ] `appsettings.json` exists
- [ ] Many `.dll` files present

### 4️⃣ Upload to MonsterASP.NET

1. Login to MonsterASP.NET control panel
2. Open File Manager or FTP client
3. Navigate to your web root folder (wwwroot / httpdocs / public_html)
4. **DELETE** old files first
5. Upload **ALL** files from the `publish` folder
6. Wait for upload to complete

### 5️⃣ Test Your Website

Visit these URLs:

- [ ] http://pos-pharamcy-system.runasp.net/ → Should show welcome message
- [ ] http://pos-pharamcy-system.runasp.net/drugs → Should show drug list
- [ ] http://pos-pharamcy-system.runasp.net/swagger → Should show API docs

---

## ⚠️ Common Mistakes to Avoid:

- ❌ Don't upload only some files - upload ALL files
- ❌ Don't forget to delete old files before uploading new ones
- ❌ Don't upload to wrong folder - upload to web root
- ❌ Don't skip the publish step - don't upload project files directly

## 🎉 Success Indicators:

- ✅ Website loads without errors
- ✅ `/drugs` endpoint returns JSON data
- ✅ Swagger UI is accessible

## 🆘 If Still Not Working:

1. Check `logs` folder on server for error details
2. Verify all files were uploaded (check file count)
3. Contact MonsterASP.NET support with:
   - "I need .NET 8.0 Runtime support" OR
   - "I'm using self-contained deployment"
   - Show them the error message

---

## 📚 Documentation Files Available:

- `README_FIX.md` - Visual guide with emojis
- `FIX_500_ERROR.md` - Detailed technical guide
- `DEPLOYMENT_GUIDE.md` - Original deployment guide
- `publish-self-contained.ps1` - Automated publish script
- `publish-framework-dependent.ps1` - Alternative publish script

---

## 🔄 Current Configuration:

- Project: .NET 8.0
- Deployment Type: Self-Contained (default)
- Target: Windows x64
- Web Server: IIS (via web.config)
- Logging: Enabled (check logs folder on server)

---

**Last Updated**: Configuration ready for self-contained deployment
**Next Step**: Run the publish command above and upload!
