using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusServiceMarketingSystem.Migrations
{
    /// <inheritdoc />
    public partial class CustomerSelfServiceOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RetailShopId",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "PlacedByEmployeeId",
                table: "Orders",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Customer self-service orders legitimately have no shop or employee. Refuse to
            // roll back while any such rows exist rather than inventing invalid FK values.
            migrationBuilder.Sql("IF EXISTS (SELECT 1 FROM [Orders] WHERE [RetailShopId] IS NULL OR [PlacedByEmployeeId] IS NULL) THROW 51000, 'Cannot roll back CustomerSelfServiceOrders while customer-placed orders exist.', 1;");
            migrationBuilder.AlterColumn<int>(
                name: "RetailShopId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "PlacedByEmployeeId",
                table: "Orders",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
