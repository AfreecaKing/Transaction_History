using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApplication1.Models;

#nullable disable

namespace WebApplication1.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260825000000_AddStockPrices")]
public partial class AddStockPrices : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "stockprices",
            columns: table => new
            {
                StockPriceId = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                StockCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                Ticker = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                PriceDate = table.Column<DateOnly>(type: "date", nullable: false),
                Close = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_stockprices", x => x.StockPriceId));

        migrationBuilder.CreateIndex(
            name: "IX_stockprices_StockCode_PriceDate",
            table: "stockprices",
            columns: new[] { "StockCode", "PriceDate" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable(name: "stockprices");
}
