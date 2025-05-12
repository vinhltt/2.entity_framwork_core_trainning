using Microsoft.EntityFrameworkCore.Migrations;

namespace RawSQL.Migrations
{
    public partial class AddProductSummaryView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE VIEW ProductSummaryView AS
                SELECT p.Id, p.Name, p.Price, c.Name AS CategoryName
                FROM Products p
                INNER JOIN Categories c ON p.CategoryId = c.Id
                WHERE p.IsAvailable = 1;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP VIEW ProductSummaryView;
            ");
        }
    }
} 