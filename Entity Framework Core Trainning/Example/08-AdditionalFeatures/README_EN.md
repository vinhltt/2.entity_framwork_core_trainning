# Entity Framework Core Additional Features Demo

This demo illustrates additional features and important considerations when working with Entity Framework Core.

## Project Structure

```
Models/
  ├── IAuditableEntity.cs
  ├── Product.cs
  └── Category.cs
Data/
  └── ApplicationDbContext.cs
Program.cs
```

## Demo Features

1. **Soft Delete**
   - Using query filters to automatically filter deleted records
   - Using IgnoreQueryFilters to query all records

2. **Temporal Tables**
   - Configuring temporal tables
   - Querying change history
   - Viewing data at a specific point in time

3. **Concurrency Handling**
   - Using RowVersion to detect conflicts
   - Handling DbUpdateConcurrencyException
   - Conflict resolution strategies

4. **Transactions**
   - Using explicit transactions
   - Committing and rolling back transactions
   - Combining multiple operations in a single transaction

5. **Data Validation**
   - Using Data Annotations
   - Validation rules
   - Handling validation errors

## Running the Demo

1. Ensure SQL Server LocalDB is installed
2. Open terminal and navigate to the project directory
3. Run the following commands:

```bash
dotnet restore
dotnet run
```

## Notes

- The demo uses SQL Server LocalDB as the database
- Sample data is automatically created when running the application
- Using async/await for all database operations
- Configured retry policy for database connection

## Best Practices

1. **Soft Delete**
   - Always use query filters to avoid querying deleted data
   - Consider using IgnoreQueryFilters when you need to query all data

2. **Temporal Tables**
   - Use for tables that need to track change history
   - Clearly configure history table names and time columns

3. **Concurrency**
   - Prefer using RowVersion over ConcurrencyCheck
   - Have a clear conflict resolution strategy

4. **Transactions**
   - Use using statement to ensure proper transaction disposal
   - Handle rollback in case of errors

5. **Validation**
   - Combine Data Annotations with Fluent API
   - Handle validation errors appropriately 