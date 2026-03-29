# LingoLearn – Tester Setup Guide

## Prerequisites

Before starting, make sure you have the following installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or Developer edition is fine) **or** SQL Server LocalDB (included with Visual Studio)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) (recommended) **or** the .NET CLI

---

## 1. Extract the Project

Unzip the downloaded archive to a folder of your choice, then open the solution file `LanguageLearningPlatform.sln` in Visual Studio.

---

## 2. Configure the Connection String

Open `LanguageLearningPlatform/appsettings.json` and verify the `DefaultConnection` string points to your SQL Server instance. The default looks like:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=LanguageLearningPlatformDb;Trusted_Connection=True;"
}
```

Adjust the server name if you are using a named SQL Server instance instead of LocalDB.

---

## 3. Apply the Database Migration

The application will automatically run migrations and seed the database on first startup — you do **not** need to run `Update-Database` manually. Simply proceed to step 4.

If for any reason the auto-migration fails, you can apply it manually:

**Package Manager Console** (Visual Studio → Tools → NuGet Package Manager):
```
PM> Update-Database
```

**Or via the .NET CLI** (run from the solution root):
```bash
dotnet ef database update --project LanguageLearningPlatform.Data --startup-project LanguageLearningPlatform
```

---

## 4. Run the Application

Press **F5** in Visual Studio, or run from the CLI:

```bash
dotnet run --project LanguageLearningPlatform
```

The app will open in your browser, typically at `https://localhost:7xxx` or `http://localhost:5xxx`.

---

## 5. Seeded Accounts

### Regular (Student) Users

Three student accounts are seeded automatically:

| Name | Email | Password |
|---|---|---|
| John Doe | john.doe@example.com | `Password123!` |
| Jane Smith | jane.smith@example.com | `Password123!` |
| Demo User | demo@example.com | `Password123!` |

### Admin Account

An admin account is seeded automatically on first startup:

| Email | Password |
|---|---|
| admin@lingolearn.com | `Admin123!` |

Log in with these credentials and navigate to `/Admin/Dashboard` to access the Admin Panel.

### Teacher Account

> ⚠️ **No teacher account is seeded automatically.** Once you have admin access (see above), you can assign the Teacher role to any existing user directly from the Admin Panel — no SQL required.

**Steps:**

1. Log in as the admin account.
2. Go to **Admin Panel → Users**.
3. Find the user you want to make a teacher (e.g. `john.doe@example.com`).
4. Click **View** (eye icon) to open their details.
5. Click **Make Teacher**.
6. Optionally click **Assign Courses** to give the teacher access to specific courses.

That user can then log in and access teacher-specific features.

---

## 6. Quick Reference

| What | Where |
|---|---|
| Main site | `/` |
| Login | `/Identity/Account/Login` |
| Register new account | `/Identity/Account/Register` |
| Admin Panel | `/Admin/Dashboard` *(Admin role required)* |
| User profile / settings | `/Profile/Settings` |

---

## 7. Recommended Course for Testing

For the most complete testing experience, enroll in the **Beginner Spanish** course. It contains the widest variety of exercise types (Translation, Multiple Choice, Fill in the Blank, Matching, and Speaking), making it the best course to test all exercise-related functionality.

---

## 8. Notes

- All three seeded student accounts share the same password: `Password123!`
- Courses, lessons, exercises, and achievements are all seeded automatically on first run — no extra steps needed.
- If you want a clean slate, drop the database and restart the application; everything will be re-seeded.