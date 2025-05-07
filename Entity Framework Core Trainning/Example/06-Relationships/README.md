# Entity Framework Core Relationships Demo

This demo illustrates how to work with relationships in Entity Framework Core, including:
- One-to-One (Author - AuthorContact)
- One-to-Many (Author - Books, Publisher - Books)
- Many-to-Many (Books - Categories)

## Project Structure

```
Models/
  ├── Author.cs
  ├── AuthorContact.cs
  ├── Book.cs
  ├── Publisher.cs
  ├── Category.cs
  └── BookCategory.cs
Data/
  └── ApplicationDbContext.cs
Program.cs
```

## Demo Features

1. **Eager Loading**
   - Load all books and related information (author, publisher, categories)
   - Using Include() and ThenInclude()

2. **Explicit Loading**
   - Load author contact information when needed
   - Using Entry().Reference().Load()

3. **Projection**
   - Only retrieve necessary information from relationships
   - Using Select() to create anonymous types

4. **Adding New Relationships**
   - Add new books with relationships
   - Add multiple categories to a book

5. **Updating Relationships**
   - Update book categories
   - Remove and add new categories

6. **Deleting Relationships**
   - Delete categories before deleting a book
   - Delete books and related relationships

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
- Relationships are configured with appropriate constraints (cascade delete, restrict delete)
- Uses async/await for all database operations

## Best Practices

1. **Loading Related Data**
   - Use Eager Loading when all related data is needed
   - Use Explicit Loading when only certain relationships need to be loaded
   - Use Projection to optimize performance

2. **Managing Relationships**
   - Always delete dependent relationships before deleting the main entity
   - Use transactions when data integrity needs to be ensured
   - Configure appropriate DeleteBehavior for each relationship

3. **Performance**
   - Avoid N+1 query problem
   - Use AsNoTracking() when only reading data
   - Optimize queries by selecting only necessary columns 