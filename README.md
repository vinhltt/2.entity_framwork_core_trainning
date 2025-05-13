# Entity Framework Core Training Overview

This repository provides a comprehensive, hands-on training program for mastering Entity Framework Core (EF Core) in .NET. The training is structured into theory lessons (in both English and Vietnamese) and practical demo projects, covering all essential aspects from getting started to advanced features.

## Training Structure

### 1. Theory Lessons

- **Languages:** English (`EN/Lessons/`) and Vietnamese (`VI/Lessons/`)
- **Topics Covered:**
    - 01.Introduction to ASP.NET Core and EF Core
    - 02.Getting Started with EF Core
    - 03.Querying Databases with EF Core
    - 04.Manipulating Data with EF Core
    - 05.Handling Database Changes and Migrations
    - 06.Working with Related Records (Relationships)
    - 07.Using Raw SQL, Views, and Stored Procedures
    - 08.Additional Features and Best Practices

Each lesson provides detailed explanations, code samples, and best practices for real-world application development.

### 2. Practical Demo Projects

Located in `Example/`, each `LessonDemoXX` project corresponds to a lesson and demonstrates its concepts in code:

- **LessonDemo02:** Getting Started with EF Core
- **LessonDemo03:** Querying Data
- **LessonDemo04:** Data Manipulation
- **LessonDemo05:** Migrations and Schema Changes
- **LessonDemo06:** Relationships (One-to-One, One-to-Many, Many-to-Many)
- **LessonDemo07:** Raw SQL, Views, Stored Procedures, and UDFs
- **LessonDemo08:** Advanced Features (Soft Delete, Temporal Tables, Concurrency, Transactions, Validation)

Each demo includes:
- Clear project structure (`Models/`, `Data/`, `Program.cs`)
- Sample data and database setup
- Step-by-step instructions in the project's README
- Best practices and notes for each topic

## How to Use

1. **Read the theory lessons** in your preferred language to understand the concepts.
2. **Explore the corresponding demo project** to see the concepts in action.
3. **Follow the instructions** in each demo's README to run and experiment with the code.
4. **Apply best practices** and experiment with modifications to deepen your understanding.

## Requirements

- .NET SDK (8.0 or later recommended)
- SQL Server LocalDB (for running demos)
- Basic knowledge of C# and .NET

## Getting Started

Clone the repository, navigate to any `LessonDemoXX` folder, and follow the README instructions to run the demo.

```bash
dotnet restore
dotnet run
```

---

**This training is designed for both beginners and experienced developers who want to master EF Core for modern .NET applications.**