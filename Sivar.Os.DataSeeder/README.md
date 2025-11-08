# 🌱 Sivar.Os Data Seeder

This console application seeds realistic test data into the Sivar.Os database using the Keycloak user IDs provided in `users.txt`.

## 📋 What Gets Seeded

### 👥 Users & Profiles
- **4 Users** with real Keycloak IDs from `users.txt`
- **Mixed Profile Types**: Personal and Business profiles
- **Realistic Profile Data**: Names, bios, locations across Central America
- **Geographic Diversity**: El Salvador, Guatemala, Nicaragua, Costa Rica

### 📝 Content
- **3-5 Posts per user** with diverse content types
- **Location-based posts** (30% of posts include geographic data)
- **Business posts** for business profiles (services, meetups)
- **Realistic tags** and content

### 🤝 Social Network
- **Follow relationships** between users (1-2 follows per user)
- **Post reactions** (likes, loves, etc.)
- **Organic interactions** that feel natural

## 🚀 How to Run

### Prerequisites
1. **Database Running**: Ensure PostgreSQL is running with the Sivar.Os database
2. **Migrations Applied**: Run database migrations first
3. **ProfileTypes Seeded**: Ensure ProfileTypes are seeded (happens automatically in migrations)

### Running the Seeder

#### Option 1: Visual Studio
1. Set `Sivar.Os.DataSeeder` as startup project
2. Press F5 or run

#### Option 2: Command Line
```bash
cd Sivar.Os.DataSeeder
dotnet run
```

#### Option 3: From Solution Root
```bash
dotnet run --project Sivar.Os.DataSeeder
```

## 📊 Seeded Data Overview

| User | Keycloak ID | Profile Type | Location | Specialty |
|------|------------|--------------|----------|-----------|
| **Gustavo Martinez** | `20b52564-...` | Personal | San Salvador, El Salvador | Software Architecture |
| **Jaime Rodriguez** | `b65fd3b2-...` | Business | Guatemala City, Guatemala | Business Consulting |
| **Jose Ojeda** | `28b46a88-...` | Personal | Managua, Nicaragua | Full-stack Development |
| **Oscar Fernandez** | `ea06c2da-...` | Business | San Jose, Costa Rica | Digital Marketing |

## ⚙️ Configuration

### Database Connection
Edit `appsettings.json` to match your database configuration:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=sivaros;Username=postgres;Password=postgres"
  }
}
```

### User Data
Modify `users.txt` to add more users:
```
user , id
newuser, NEW-KEYCLOAK-ID-HERE
```

## 🔄 Idempotent Operation

The seeder is designed to be **safe to run multiple times**:
- ✅ **Checks existing data** before creating
- ✅ **Skips existing users** and profiles
- ✅ **Won't duplicate data** if run again
- ✅ **Logs all operations** for transparency

## 🧹 Data Cleanup

If you need to reset the data:

### Option 1: Clear Specific Tables (SQL)
```sql
-- Clear in dependency order
DELETE FROM "Sivar_Reactions";
DELETE FROM "Sivar_Comments";
DELETE FROM "Sivar_ProfileFollowers";
DELETE FROM "Sivar_Posts";
DELETE FROM "Sivar_Profiles";
DELETE FROM "Sivar_Users" WHERE "KeycloakId" IN (
  '20b52564-e505-404a-bd7a-be5916c8e0a4',
  'b65fd3b2-e181-4830-8678-fff5f96492b9',
  '28b46a88-d191-4c63-8812-1bb8f3332228',
  'ea06c2da-07f3-4606-aa65-46a67cb0a471'
);
```

### Option 2: Reset Entire Database
```bash
# Drop and recreate database
dotnet ef database drop --project Sivar.Os
dotnet ef database update --project Sivar.Os
```

## 📈 Expected Results

After running the seeder, you should have:
- ✅ **4 Users** with realistic profiles
- ✅ **12-20 Posts** with diverse content
- ✅ **4-8 Follow relationships**
- ✅ **10-30 Reactions** on posts
- ✅ **Geographic data** for location-based features
- ✅ **Mix of profile types** to showcase platform capabilities

## 🐛 Troubleshooting

### Common Issues

#### Database Connection Error
```
❌ Failed to connect to database
```
**Solution**: Verify PostgreSQL is running and connection string is correct

#### ProfileTypes Missing
```
❌ Required ProfileTypes not found
```
**Solution**: Run database migrations first:
```bash
dotnet ef database update --project Sivar.Os
```

#### Duplicate Key Errors
```
❌ User already exists
```
**Solution**: This is expected behavior - the seeder will skip existing users

### Verbose Logging
The seeder uses structured logging. All operations are logged with:
- ✅ **User creation** progress
- ✅ **Profile creation** details  
- ✅ **Post creation** counts
- ✅ **Social interaction** results
- ❌ **Error details** if something fails

## 🎯 Next Steps

After seeding:
1. **Start the main application**: `dotnet run --project Sivar.Os`
2. **Login with Keycloak** using the seeded users
3. **Explore the features** with realistic data
4. **Test location services** with the geographic data
5. **Try profile switching** between Personal/Business profiles

The seeded data provides a realistic foundation for testing and development! 🚀