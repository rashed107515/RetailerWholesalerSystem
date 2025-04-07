using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailerWholesalerSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddNewTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Date",
                table: "Transactions",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_Date",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_Products_Name",
                table: "Products");
        }
    }
}
