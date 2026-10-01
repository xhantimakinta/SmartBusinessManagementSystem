using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartBusiness.Infrastructure.Persistence.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Categories",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Categories", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Customers",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                Phone = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Customers", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_Users", x => x.Id));

        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CategoryId = table.Column<int>(type: "int", nullable: false),
                Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                StockQuantity = table.Column<int>(type: "int", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", x => x.Id);
                table.ForeignKey("FK_Products_Categories_CategoryId", x => x.CategoryId, "Categories", "Id", onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_Products_Price_NonNegative", "[Price] >= 0");
                table.CheckConstraint("CK_Products_StockQuantity_NonNegative", "[StockQuantity] >= 0");
            });

        migrationBuilder.CreateTable(
            name: "Orders",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                CustomerId = table.Column<int>(type: "int", nullable: false),
                OrderDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", x => x.Id);
                table.ForeignKey("FK_Orders_Customers_CustomerId", x => x.CustomerId, "Customers", "Id", onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_Orders_TotalAmount_NonNegative", "[TotalAmount] >= 0");
            });

        migrationBuilder.CreateTable(
            name: "OrderItems",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                OrderId = table.Column<int>(type: "int", nullable: false),
                ProductId = table.Column<int>(type: "int", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderItems", x => x.Id);
                table.ForeignKey("FK_OrderItems_Orders_OrderId", x => x.OrderId, "Orders", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_OrderItems_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_OrderItems_Quantity_Positive", "[Quantity] > 0");
                table.CheckConstraint("CK_OrderItems_UnitPrice_NonNegative", "[UnitPrice] >= 0");
                table.CheckConstraint("CK_OrderItems_Subtotal_NonNegative", "[Subtotal] >= 0");
            });

        migrationBuilder.CreateIndex("IX_Categories_Name", "Categories", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_Customers_Email", "Customers", "Email");
        migrationBuilder.CreateIndex("IX_Users_Email", "Users", "Email", unique: true);
        migrationBuilder.CreateIndex("IX_Products_CategoryId", "Products", "CategoryId");
        migrationBuilder.CreateIndex("IX_Products_Name", "Products", "Name");
        migrationBuilder.CreateIndex("IX_Orders_CustomerId_OrderDate", "Orders", new[] { "CustomerId", "OrderDate" });
        migrationBuilder.CreateIndex("IX_OrderItems_OrderId_ProductId", "OrderItems", new[] { "OrderId", "ProductId" }, unique: true);
        migrationBuilder.CreateIndex("IX_OrderItems_ProductId", "OrderItems", "ProductId");

        migrationBuilder.InsertData("Users", new[] { "Id", "Name", "Email", "PasswordHash", "Role", "CreatedAt" },
            new object[] { 1, "Development Admin", "admin@smartbusiness.local", "sha256:7f5c7c7f8e9a7d4a1ce6c9e5d65a70c4b39f6a8f9b8e7d2c6a5e4f3d2c1b0a9", "Admin", new DateTime(2026, 1, 1) });
        migrationBuilder.InsertData("Categories", new[] { "Id", "Name", "Description" },
            new object[,]
            {
                { 1, "Office Supplies", "Everyday supplies for business operations." },
                { 2, "Technology", "Hardware and technology products." }
            });
        migrationBuilder.InsertData("Customers", new[] { "Id", "Name", "Email", "Phone", "Address", "CreatedAt" },
            new object[] { 1, "Development Customer", "customer@smartbusiness.local", "+1 555 0100", "100 Main Street", new DateTime(2026, 1, 1) });
        migrationBuilder.InsertData("Products", new[] { "Id", "CategoryId", "Name", "Description", "Price", "StockQuantity", "CreatedAt", "UpdatedAt" },
            new object[,]
            {
                { 1, 1, "Notebook", "Hardcover business notebook.", 12.50m, 100, new DateTime(2026, 1, 1), new DateTime(2026, 1, 1) },
                { 2, 2, "Wireless Keyboard", "Compact wireless keyboard.", 49.99m, 25, new DateTime(2026, 1, 1), new DateTime(2026, 1, 1) }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("OrderItems");
        migrationBuilder.DropTable("Orders");
        migrationBuilder.DropTable("Products");
        migrationBuilder.DropTable("Categories");
        migrationBuilder.DropTable("Customers");
        migrationBuilder.DropTable("Users");
    }
}