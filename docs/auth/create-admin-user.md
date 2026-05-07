# How to Create Admin User

## Overview

Your POS System API has multiple ways to create an admin user with SuperAdmin privileges.

## Method 1: Automatic Seeding (Recommended for Development)

### What Happens Automatically

When you run the API in **Development mode**, it automatically seeds an admin user on startup:

**Default Admin Credentials:**

- **Username**: `admin`
- **Email**: `admin@possystem.com`
- **Password**: `Admin@123`
- **System Role**: SuperAdmin (full access to all shops)
- **Phone**: +1234567890

### How to Use

1. **Start the API**:

```powershell
dotnet run
```

2. **Check Console Output**:
   You should see:

```
Seeding users...
Seeded X users successfully:
  - 1 Admin: admin / Admin@123
  - Shop SHOP-XXX:
      Owner: owner_xxx / Owner@123
      Staff: staff_xxx / Staff@123
```

3. **Login via API**:

```http
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "identifier": "admin",
  "password": "Admin@123"
}
```

**Response**:

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "...",
  "user": {
    "id": "USER-XXXXXXXX",
    "username": "admin",
    "email": "admin@possystem.com",
    "fullName": "System Administrator",
    "systemRole": "SuperAdmin",
    "shops": []
  }
}
```

## Method 2: Manual Seeding via API Endpoint

If users were not auto-seeded or you cleared the database, use the admin endpoint.

### Prerequisites

⚠️ **Note**: This endpoint requires authentication, so you need an existing admin user first. If no admin exists, use Method 3 below.

### Steps

1. **Login as existing admin** (if one exists)

2. **Call the seed endpoint**:

```http
POST http://localhost:5000/api/admin/seed-users
Authorization: Bearer YOUR_ACCESS_TOKEN
```

**Response**:

```json
{
  "message": "Users seeded successfully. Check console output for login credentials."
}
```

## Method 3: Register First Admin User (If No Admin Exists)

If no admin user exists and auto-seeding failed, you can register the first user directly via the database or by temporarily modifying the registration endpoint.

### Option A: Direct Database Insert

Run this SQL in your PostgreSQL database:

```sql
-- First, generate a password hash (Admin@123)
-- The hash below is bcrypt hash of "Admin@123"

INSERT INTO "Users" (
    "Id",
    "Username",
    "Email",
    "PasswordHash",
    "FullName",
    "SystemRole",
    "IsActive",
    "IsEmailVerified",
    "Phone",
    "CreatedAt",
    "CreatedBy"
) VALUES (
    'USER-' || UPPER(SUBSTRING(MD5(RANDOM()::TEXT) FROM 1 FOR 8)),
    'admin',
    'admin@possystem.com',
    '$2a$11$your_bcrypt_hash_here',  -- See "Generate Password Hash" section below
    'System Administrator',
    0,  -- SystemRole.SuperAdmin = 0
    true,
    true,
    '+1234567890',
    NOW(),
    'System'
);
```

### Option B: Generate Password Hash Using C#

Create a simple console app or use the existing PasswordHasher:

```csharp
using pos_system_api.Infrastructure.Auth;

var passwordHasher = new PasswordHasher();
var hash = passwordHasher.HashPassword("Admin@123");
Console.WriteLine($"Password Hash: {hash}");
```

Then use this hash in the SQL statement above.

### Option C: Temporary Registration Endpoint Modification

Temporarily allow SuperAdmin registration:

1. **Open**: `src/Core/Application/Auth/Commands/Register/RegisterCommandHandler.cs`

2. **Find the SystemRole assignment** and temporarily change it:

```csharp
// Temporarily allow first user to be SuperAdmin
var userCount = await _context.Users.CountAsync(cancellationToken);
var systemRole = userCount == 0 ? SystemRole.SuperAdmin : SystemRole.User;

var user = new User(
    command.Username,
    command.Email,
    passwordHash,
    command.FullName,
    systemRole  // First user becomes SuperAdmin
);
```

3. **Register via API**:

```http
POST http://localhost:5000/api/auth/register
Content-Type: application/json

{
  "username": "admin",
  "email": "admin@possystem.com",
  "password": "Admin@123",
  "fullName": "System Administrator",
  "phone": "+1234567890"
}
```

4. **Remove the temporary change** after creating the admin.

## Method 4: Using Entity Framework Migrations

Create a dedicated migration for the admin user:

```powershell
dotnet ef migrations add SeedAdminUser
```

Then edit the migration file to add:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        INSERT INTO ""Users"" (
            ""Id"", ""Username"", ""Email"", ""PasswordHash"", ""FullName"",
            ""SystemRole"", ""IsActive"", ""IsEmailVerified"", ""CreatedAt""
        ) VALUES (
            'USER-ADMIN01',
            'admin',
            'admin@possystem.com',
            '$2a$11$your_hash_here',
            'System Administrator',
            0,
            true,
            true,
            NOW()
        )
        ON CONFLICT (""Username"") DO NOTHING;
    ");
}
```

## Verifying Admin User

### Check Database

```sql
SELECT
    "Id",
    "Username",
    "Email",
    "FullName",
    "SystemRole",
    "IsActive"
FROM "Users"
WHERE "SystemRole" = 0;  -- SuperAdmin
```

### Check via API

```http
GET http://localhost:5000/api/admin/stats
Authorization: Bearer YOUR_ADMIN_TOKEN
```

## SuperAdmin vs Regular User

### SuperAdmin Capabilities

✅ Access to **all shops** without needing ShopUser membership  
✅ Can perform system-wide operations  
✅ Can access admin endpoints (`/api/admin/*`)  
✅ Can manage all users across all shops  
✅ Bypass shop-level permission checks

### Regular User Capabilities

❌ Only access shops where they have ShopUser membership  
✅ Permissions controlled by `ShopRole` (Owner, Manager, Cashier, etc.)  
❌ Cannot access admin endpoints  
❌ Shop-specific access only

## Security Best Practices

### Production Environment

1. **Change Default Password Immediately**:

```http
POST http://localhost:5000/api/auth/change-password
Authorization: Bearer YOUR_ADMIN_TOKEN

{
  "currentPassword": "Admin@123",
  "newPassword": "YourStrongPassword!2024"
}
```

2. **Disable Auto-Seeding in Production**:
   Edit `Program.cs` to only seed in Development:

```csharp
if (app.Environment.IsDevelopment())
{
    // Seeding code here
}
```

3. **Use Strong Passwords**:

- Minimum 8 characters
- Include uppercase, lowercase, numbers, and special characters
- Example: `P@ssw0rd!2024`

4. **Enable Two-Factor Authentication** (if implemented)

5. **Regularly Audit Admin Accounts**:

```sql
SELECT * FROM "Users" WHERE "SystemRole" = 0;
```

6. **Limit Admin Accounts**:

- Only create admin accounts when absolutely necessary
- Use regular users with appropriate ShopRole permissions instead

## Troubleshooting

### Problem: "Users already exist" message but no admin

**Solution**: Clear and re-seed:

```http
DELETE http://localhost:5000/api/admin/clear
POST http://localhost:5000/api/admin/seed-users
```

### Problem: Cannot access admin endpoints

**Solution**: Check if user has `SystemRole = SuperAdmin`:

```sql
UPDATE "Users"
SET "SystemRole" = 0
WHERE "Username" = 'admin';
```

### Problem: Auto-seeding not working

**Solution**:

1. Check `appsettings.Development.json` for correct DB connection
2. Ensure you're running in Development mode
3. Check console logs for seeding errors
4. Manually call seed endpoint

### Problem: Password hash mismatch

**Solution**: Use the PasswordHasher class to generate correct hashes:

```csharp
var hasher = new PasswordHasher();
var hash = hasher.HashPassword("YourPassword");
```

## Quick Start Checklist

- [ ] Start API in Development mode (`dotnet run`)
- [ ] Check console for "Seeding users..." message
- [ ] Note the admin credentials from console output
- [ ] Test login at `/api/auth/login`
- [ ] Verify SuperAdmin access at `/api/admin/stats`
- [ ] Change default password
- [ ] Create your actual users via `/api/auth/register`

## API Endpoints Summary

### Public (No Auth Required)

- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login (get JWT token)
- `POST /api/auth/refresh` - Refresh access token

### Admin Only (SuperAdmin Required)

- `POST /api/admin/seed` - Seed entire database
- `POST /api/admin/seed-users` - Seed users only
- `DELETE /api/admin/clear` - Clear database
- `GET /api/admin/stats` - Get database statistics

### Authenticated

- `GET /api/auth/me` - Get current user info
- `POST /api/auth/logout` - Logout user

---

**Default Admin Account**:

- Username: `admin`
- Password: `Admin@123`
- Email: `admin@possystem.com`
- Role: SuperAdmin

**⚠️ Remember to change the default password in production!**
