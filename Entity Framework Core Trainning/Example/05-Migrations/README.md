# Entity Framework Core Migrations Demo

This demo illustrates how to use Entity Framework Core Migrations to manage database schema changes.

## Project Structure

- `Models/`: Contains entity classes
  - `Product.cs`: Product model
  - `Category.cs`: Category model
- `Data/`: Contains DbContext and configuration
  - `ApplicationDbContext.cs`: Main DbContext
- `Program.cs`: Main file containing demos

## Implementation Steps

1. Create initial migration:
```bash
dotnet ef migrations add InitialCreate
```

2. Create migration for model changes:
```bash
dotnet ef migrations add AddNewField
```

3. Update database:
```bash
dotnet ef database update
```

4. Generate SQL script:
```bash
dotnet ef migrations script -o migration.sql --idempotent
```

5. Rollback migration:
```bash
dotnet ef database update PreviousMigrationName
```

6. Remove last migration:
```bash
dotnet ef migrations remove
```

7. Create EF Bundle:
```bash
dotnet ef migrations bundle -o ./efbundle --force
```

## Demo Features

1. **Apply Migrations at Runtime**: Using `Database.MigrateAsync()`
2. **Check Database Connection**: Verify database connection
3. **Get Pending Migrations**: View pending migrations
4. **Get Applied Migrations**: View applied migrations

## Notes

- Ensure SQL Server LocalDB is installed
- Migration commands must be run in the project root directory
- Always check migration file contents before applying
- Use `--idempotent` when generating SQL scripts for production
- Do not delete migrations that have been applied to the database 