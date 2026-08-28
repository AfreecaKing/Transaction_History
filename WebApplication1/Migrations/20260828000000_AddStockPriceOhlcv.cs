using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using WebApplication1.Models;

#nullable disable

namespace WebApplication1.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260828000000_AddStockPriceOhlcv")]
public partial class AddStockPriceOhlcv : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<decimal>(name: "Open", table: "stockprices", type: "decimal(65,30)", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "High", table: "stockprices", type: "decimal(65,30)", nullable: true);
        migrationBuilder.AddColumn<decimal>(name: "Low", table: "stockprices", type: "decimal(65,30)", nullable: true);
        migrationBuilder.AddColumn<long>(name: "Volume", table: "stockprices", type: "bigint", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Open", table: "stockprices");
        migrationBuilder.DropColumn(name: "High", table: "stockprices");
        migrationBuilder.DropColumn(name: "Low", table: "stockprices");
        migrationBuilder.DropColumn(name: "Volume", table: "stockprices");
    }
}
