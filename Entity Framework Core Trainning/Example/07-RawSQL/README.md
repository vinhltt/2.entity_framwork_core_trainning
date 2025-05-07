# Entity Framework Core Raw SQL, Views and Stored Procedures Demo

This demo illustrates how to work with raw SQL, Views, and Stored Procedures in Entity Framework Core.

## Project Structure

```
Models/
  ├── Product.cs
  ├── Category.cs
  └── ProductSummary.cs
Data/
  └── ApplicationDbContext.cs
Migrations/
  └── 20240315000000_AddProductSummaryView.cs
Program.cs
```

## Demo Features

1. **Raw SQL Query**
   - Using FromSqlInterpolated to execute raw SQL
   - Query products by category and price

2. **Composing LINQ with Raw SQL**
   - Combining raw SQL with LINQ
   - Filtering and sorting results

3. **Querying View**
   - Using Keyless Entity to query View
   - Filtering and sorting data from View

4. **Executing Non-Query SQL**
   - Executing UPDATE commands
   - Getting affected record count

5. **Querying Scalar Value**
   - Using ADO.NET to get single values
   - Executing COUNT queries

6. **Using UDF in LINQ**
   - Mapping User-Defined Function
   - Using UDF in LINQ queries

## How to Run the Demo

1. Ensure SQL Server LocalDB is installed
2. Open terminal and navigate to the project directory
3. Run the following commands:

```bash
dotnet restore
dotnet run
```

## Notes

- Demo uses SQL Server LocalDB as the database
- Sample data is automatically generated when running the application
- View is created through migration
- Uses async/await for all database operations

## Best Practices

1. **Raw SQL**
   - Always use parameterization to prevent SQL Injection
   - Prefer FromSqlInterpolated over FromSqlRaw
   - Check generated SQL to ensure performance

2. **Views**
   - Use Keyless Entity to map Views
   - Use Views only for read operations
   - Update Views through migration

3. **Stored Procedures and UDFs**
   - Map UDF to C# methods
   - Use parameterization when calling SPs
   - Check compatibility between database providers 