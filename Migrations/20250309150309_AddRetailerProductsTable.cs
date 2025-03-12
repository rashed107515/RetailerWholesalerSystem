using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RetailerWholesalerSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddRetailerProductsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RetailerProducts",
                columns: table => new
                {
                    RetailerProductID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductID = table.Column<int>(type: "int", nullable: false),
                    RetailerID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailerProducts", x => x.RetailerProductID);
                    table.ForeignKey(
                        name: "FK_RetailerProducts_AspNetUsers_RetailerID",
                        column: x => x.RetailerID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RetailerProducts_Products_ProductID",
                        column: x => x.ProductID,
                        principalTable: "Products",
                        principalColumn: "ProductID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetailerProducts_ProductID",
                table: "RetailerProducts",
                column: "ProductID");

            migrationBuilder.CreateIndex(
                name: "IX_RetailerProducts_RetailerID",
                table: "RetailerProducts",
                column: "RetailerID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetailerProducts");
        }
    }
}
