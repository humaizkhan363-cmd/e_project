using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NexusServiceMarketingSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "char(3)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                    table.CheckConstraint("CK_Cities_Code_Format", "[Code] COLLATE Latin1_General_100_BIN2 LIKE '[0-9][0-9][0-9]'");
                });

            migrationBuilder.CreateTable(
                name: "DiscountSchemes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MinConnections = table.Column<int>(type: "int", nullable: false),
                    MaxConnections = table.Column<int>(type: "int", nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    AppliesToAdvance = table.Column<bool>(type: "bit", nullable: false),
                    AppliesToSecurityDeposit = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountSchemes", x => x.Id);
                    table.CheckConstraint("CK_DiscountSchemes_Percent", "[DiscountPercent] >= 0 AND [DiscountPercent] <= 100");
                    table.CheckConstraint("CK_DiscountSchemes_Range", "[MinConnections] >= 1 AND ([MaxConnections] IS NULL OR [MaxConnections] >= [MinConnections])");
                });

            migrationBuilder.CreateTable(
                name: "IdentifierCounters",
                columns: table => new
                {
                    Scope = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: false),
                    LastValue = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentifierCounters", x => x.Scope);
                    table.CheckConstraint("CK_IdentifierCounters_LastValue", "[LastValue] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ConnectionType = table.Column<string>(type: "char(1)", nullable: false),
                    Kind = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    IncludedHours = table.Column<int>(type: "int", nullable: true),
                    SpeedKbps = table.Column<int>(type: "int", nullable: true),
                    ValidityMonths = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SecurityDeposit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LocalCallRatePerMinute = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    StdCallRatePerMinute = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    MobileMessagingRatePerMinute = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                    table.CheckConstraint("CK_Plans_Amounts", "[ValidityMonths] > 0 AND [Price] >= 0 AND [SecurityDeposit] >= 0");
                    table.CheckConstraint("CK_Plans_ConnectionType_Letter", "[ConnectionType] COLLATE Latin1_General_100_BIN2 LIKE '[DBT]'");
                    table.CheckConstraint("CK_Plans_Hourly_Hours", "[Kind] <> 'Hourly' OR ([IncludedHours] IS NOT NULL AND [IncludedHours] > 0)");
                    table.CheckConstraint("CK_Plans_Kind_Matches_Type", "([Kind] IN ('LocalRental', 'StdRental') AND [ConnectionType] = 'T') OR ([Kind] IN ('Hourly', 'Unlimited') AND [ConnectionType] IN ('D', 'B'))");
                    table.CheckConstraint("CK_Plans_Kind_Values", "[Kind] IN ('Hourly', 'Unlimited', 'LocalRental', 'StdRental')");
                    table.CheckConstraint("CK_Plans_Unlimited_Speed", "[Kind] <> 'Unlimited' OR ([SpeedKbps] IS NOT NULL AND [SpeedKbps] > 0)");
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ContactPerson = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CustomerType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.CheckConstraint("CK_Customers_Corporate_Company", "[CustomerType] <> 'Corporate' OR [CompanyName] IS NOT NULL");
                    table.CheckConstraint("CK_Customers_CustomerType_Values", "[CustomerType] IN ('Individual', 'Corporate')");
                    table.ForeignKey(
                        name: "FK_Customers_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RetailShops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    AddressLine = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetailShops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetailShops_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Sku = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VendorId = table.Column<int>(type: "int", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReplacementCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    ReorderLevel = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_Category_Values", "[Category] IN ('Modem', 'Router', 'Other')");
                    table.CheckConstraint("CK_Products_Prices_NonNegative", "[PurchasePrice] >= 0 AND [ReplacementCharge] >= 0");
                    table.CheckConstraint("CK_Products_Stock_NonNegative", "[StockQuantity] >= 0 AND [ReorderLevel] >= 0");
                    table.ForeignKey(
                        name: "FK_Products_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Role = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    RetailShopId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.CheckConstraint("CK_Employees_Role_Shop", "([Role] = 'RetailStaff' AND [RetailShopId] IS NOT NULL) OR ([Role] <> 'RetailStaff' AND [RetailShopId] IS NULL)");
                    table.CheckConstraint("CK_Employees_Role_Values", "[Role] IN ('Admin', 'Accounts', 'Technical', 'RetailStaff')");
                    table.ForeignKey(
                        name: "FK_Employees_RetailShops_RetailShopId",
                        column: x => x.RetailShopId,
                        principalTable: "RetailShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderNumber = table.Column<string>(type: "char(11)", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    ConnectionType = table.Column<string>(type: "char(1)", nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    InstallationAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    RetailShopId = table.Column<int>(type: "int", nullable: false),
                    PlacedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    DiscountSchemeId = table.Column<int>(type: "int", nullable: true),
                    DiscountPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    PlacedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    StatusChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.CheckConstraint("CK_Orders_ConnectionType_Letter", "[ConnectionType] COLLATE Latin1_General_100_BIN2 LIKE '[DBT]'");
                    table.CheckConstraint("CK_Orders_DiscountPercent", "[DiscountPercent] >= 0 AND [DiscountPercent] <= 100");
                    table.CheckConstraint("CK_Orders_OrderNumber_Format", "[OrderNumber] COLLATE Latin1_General_100_BIN2 LIKE '[DBT][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'");
                    table.CheckConstraint("CK_Orders_OrderNumber_Type", "LEFT([OrderNumber], 1) = [ConnectionType]");
                    table.CheckConstraint("CK_Orders_Quantity", "[Quantity] >= 1");
                    table.CheckConstraint("CK_Orders_Status_Values", "[Status] IN ('Placed', 'UnderFeasibilityCheck', 'Feasible', 'NotFeasible', 'Connected', 'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Orders_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_DiscountSchemes_DiscountSchemeId",
                        column: x => x.DiscountSchemeId,
                        principalTable: "DiscountSchemes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Employees_PlacedByEmployeeId",
                        column: x => x.PlacedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_RetailShops_RetailShopId",
                        column: x => x.RetailShopId,
                        principalTable: "RetailShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    LastLoginAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    CustomerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.CheckConstraint("CK_Users_OneOwner", "([EmployeeId] IS NOT NULL AND [CustomerId] IS NULL) OR ([EmployeeId] IS NULL AND [CustomerId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Users_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Users_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Connections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccountId = table.Column<string>(type: "char(16)", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    PlanId = table.Column<int>(type: "int", nullable: false),
                    ConnectionType = table.Column<string>(type: "char(1)", nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: false),
                    InstallationAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    LandlineConnectionId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    SecurityDepositAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    StatusChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                    table.CheckConstraint("CK_Connections_AccountId_Format", "[AccountId] COLLATE Latin1_General_100_BIN2 LIKE '[DBT][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'");
                    table.CheckConstraint("CK_Connections_AccountId_Type", "LEFT([AccountId], 1) = [ConnectionType]");
                    table.CheckConstraint("CK_Connections_ConnectionType_Letter", "[ConnectionType] COLLATE Latin1_General_100_BIN2 LIKE '[DBT]'");
                    table.CheckConstraint("CK_Connections_Deposit", "[SecurityDepositAmount] >= 0");
                    table.CheckConstraint("CK_Connections_Status_Values", "[Status] IN ('Active', 'TemporarilyInactive', 'PermanentlyInactive')");
                    table.ForeignKey(
                        name: "FK_Connections_Cities_CityId",
                        column: x => x.CityId,
                        principalTable: "Cities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Connections_Connections_LandlineConnectionId",
                        column: x => x.LandlineConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Connections_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Connections_Employees_CreatedByEmployeeId",
                        column: x => x.CreatedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Connections_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Connections_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FeasibilityChecks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    CheckType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: true),
                    ServerAvailable = table.Column<bool>(type: "bit", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CheckedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    CheckedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeasibilityChecks", x => x.Id);
                    table.CheckConstraint("CK_FeasibilityChecks_CheckType_Values", "[CheckType] IN ('Landline', 'Internet')");
                    table.CheckConstraint("CK_FeasibilityChecks_Distance", "[DistanceKm] IS NULL OR [DistanceKm] >= 0");
                    table.CheckConstraint("CK_FeasibilityChecks_Status_Values", "[Status] IN ('Pending', 'Feasible', 'NotFeasible')");
                    table.ForeignKey(
                        name: "FK_FeasibilityChecks_Employees_CheckedByEmployeeId",
                        column: x => x.CheckedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FeasibilityChecks_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Bills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectionId = table.Column<int>(type: "int", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PlanCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UsageCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SecurityDepositCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReplacementCharge = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ServiceTaxRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ServiceTaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    GeneratedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bills", x => x.Id);
                    table.CheckConstraint("CK_Bills_Amounts_NonNegative", "[PlanCharge] >= 0 AND [UsageCharge] >= 0 AND [SecurityDepositCharge] >= 0 AND [ReplacementCharge] >= 0 AND [DiscountAmount] >= 0 AND [SubTotal] >= 0 AND [ServiceTaxAmount] >= 0 AND [TotalAmount] >= 0");
                    table.CheckConstraint("CK_Bills_DueDate", "[DueDate] >= [IssueDate]");
                    table.CheckConstraint("CK_Bills_Period", "[PeriodEnd] >= [PeriodStart]");
                    table.CheckConstraint("CK_Bills_Status_Values", "[Status] IN ('Issued', 'PartiallyPaid', 'Paid', 'Cancelled')");
                    table.CheckConstraint("CK_Bills_TaxRate", "[ServiceTaxRate] >= 0 AND [ServiceTaxRate] <= 100");
                    table.ForeignKey(
                        name: "FK_Bills_Connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bills_Employees_GeneratedByEmployeeId",
                        column: x => x.GeneratedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConnectionId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SerialNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    IsReplacement = table.Column<bool>(type: "bit", nullable: false),
                    IssuedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReturnedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionProducts", x => x.Id);
                    table.CheckConstraint("CK_ConnectionProducts_Quantity", "[Quantity] >= 1");
                    table.CheckConstraint("CK_ConnectionProducts_Returned", "[ReturnedAtUtc] IS NULL OR [ReturnedAtUtc] >= [IssuedAtUtc]");
                    table.ForeignKey(
                        name: "FK_ConnectionProducts_Connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConnectionProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Feedbacks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    ConnectionId = table.Column<int>(type: "int", nullable: true),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedbacks", x => x.Id);
                    table.CheckConstraint("CK_Feedbacks_Rating", "[Rating] >= 1 AND [Rating] <= 5");
                    table.ForeignKey(
                        name: "FK_Feedbacks_Connections_ConnectionId",
                        column: x => x.ConnectionId,
                        principalTable: "Connections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Feedbacks_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Method = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PaidAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    ReceivedByEmployeeId = table.Column<int>(type: "int", nullable: false),
                    RetailShopId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amount", "[Amount] > 0");
                    table.CheckConstraint("CK_Payments_Method_Values", "[Method] IN ('Cash', 'Card', 'BankTransfer', 'Cheque', 'Online')");
                    table.ForeignKey(
                        name: "FK_Payments_Bills_BillId",
                        column: x => x.BillId,
                        principalTable: "Bills",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_Employees_ReceivedByEmployeeId",
                        column: x => x.ReceivedByEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Payments_RetailShops_RetailShopId",
                        column: x => x.RetailShopId,
                        principalTable: "RetailShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "DiscountSchemes",
                columns: new[] { "Id", "AppliesToAdvance", "AppliesToSecurityDeposit", "CreatedAtUtc", "DiscountPercent", "IsActive", "MaxConnections", "MinConnections", "Name" },
                values: new object[,]
                {
                    { 1, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 25m, true, 14, 10, "Bulk 10 to 14 connections" },
                    { 2, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 50m, true, 24, 15, "Bulk 15 to 24 connections" },
                    { 3, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 75m, true, 49, 25, "Bulk 25 to 49 connections" },
                    { 4, true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 100m, true, null, 50, "Bulk 50 or more connections" }
                });

            migrationBuilder.InsertData(
                table: "Plans",
                columns: new[] { "Id", "ConnectionType", "CreatedAtUtc", "Description", "IncludedHours", "IsActive", "Kind", "LocalCallRatePerMinute", "MobileMessagingRatePerMinute", "Name", "Price", "SecurityDeposit", "SpeedKbps", "StdCallRatePerMinute", "ValidityMonths" },
                values: new object[,]
                {
                    { 1, "D", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 10, true, "Hourly", null, null, "Dial-Up Hourly 10 Hrs", 50m, 325m, null, null, 1 },
                    { 2, "D", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 30, true, "Hourly", null, null, "Dial-Up Hourly 30 Hrs", 130m, 325m, null, null, 3 },
                    { 3, "D", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 60, true, "Hourly", null, null, "Dial-Up Hourly 60 Hrs", 260m, 325m, null, null, 6 },
                    { 4, "D", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Unlimited", null, null, "Dial-Up Unlimited 28 Kbps Monthly", 75m, 325m, 28, null, 1 },
                    { 5, "D", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Unlimited", null, null, "Dial-Up Unlimited 28 Kbps Quarterly", 150m, 325m, 28, null, 3 },
                    { 6, "D", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Unlimited", null, null, "Dial-Up Unlimited 56 Kbps Monthly", 100m, 325m, 56, null, 1 },
                    { 7, "D", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Unlimited", null, null, "Dial-Up Unlimited 56 Kbps Quarterly", 180m, 325m, 56, null, 3 },
                    { 8, "B", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 30, true, "Hourly", null, null, "Broadband Hourly 30 Hrs", 175m, 500m, null, null, 1 },
                    { 9, "B", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 60, true, "Hourly", null, null, "Broadband Hourly 60 Hrs", 315m, 500m, null, null, 6 },
                    { 10, "B", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Unlimited", null, null, "Broadband Unlimited 64 Kbps Monthly", 225m, 500m, 64, null, 1 },
                    { 11, "B", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Unlimited", null, null, "Broadband Unlimited 64 Kbps Quarterly", 400m, 500m, 64, null, 3 },
                    { 12, "B", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Unlimited", null, null, "Broadband Unlimited 128 Kbps Monthly", 350m, 500m, 128, null, 1 },
                    { 13, "B", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "Unlimited", null, null, "Broadband Unlimited 128 Kbps Quarterly", 445m, 500m, 128, null, 3 },
                    { 14, "T", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "LocalRental", 0.55m, null, "Landline Local Unlimited (Yearly rental)", 75m, 250m, null, null, 12 },
                    { 15, "T", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "LocalRental", 0.75m, null, "Landline Local Monthly", 35m, 250m, null, null, 1 },
                    { 16, "T", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "StdRental", 0.70m, 1.00m, "Landline STD Monthly", 125m, 250m, null, 2.25m, 1 },
                    { 17, "T", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, "StdRental", 0.60m, 1.15m, "Landline STD Half-Yearly", 420m, 250m, null, 2.00m, 6 },
                    { 18, "T", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "PRICE NOT GIVEN IN THE PROJECT SPECIFICATION. Admin must set the yearly rental before activating this plan.", null, false, "StdRental", 0.60m, 1.25m, "Landline STD Yearly", 0m, 250m, null, 1.75m, 12 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bills_DueDate",
                table: "Bills",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_GeneratedByEmployeeId",
                table: "Bills",
                column: "GeneratedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_Status",
                table: "Bills",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_Bills_Connection_Period",
                table: "Bills",
                columns: new[] { "ConnectionId", "PeriodStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Cities_Code",
                table: "Cities",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Cities_Name",
                table: "Cities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionProducts_ConnectionId",
                table: "ConnectionProducts",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "UX_ConnectionProducts_Product_Serial",
                table: "ConnectionProducts",
                columns: new[] { "ProductId", "SerialNumber" },
                unique: true,
                filter: "[SerialNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_ActivatedAtUtc",
                table: "Connections",
                column: "ActivatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_CityId",
                table: "Connections",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_ConnectionType",
                table: "Connections",
                column: "ConnectionType");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_CreatedByEmployeeId",
                table: "Connections",
                column: "CreatedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_CustomerId",
                table: "Connections",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_LandlineConnectionId",
                table: "Connections",
                column: "LandlineConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_OrderId",
                table: "Connections",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_PlanId",
                table: "Connections",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_Status",
                table: "Connections",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_Connections_AccountId",
                table: "Connections",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CityId",
                table: "Customers",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_FullName",
                table: "Customers",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Phone",
                table: "Customers",
                column: "Phone");

            migrationBuilder.CreateIndex(
                name: "UX_Customers_Email",
                table: "Customers",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_DiscountSchemes_Min",
                table: "DiscountSchemes",
                column: "MinConnections",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_DiscountSchemes_Name",
                table: "DiscountSchemes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_RetailShopId",
                table: "Employees",
                column: "RetailShopId");

            migrationBuilder.CreateIndex(
                name: "UX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeasibilityChecks_CheckedByEmployeeId",
                table: "FeasibilityChecks",
                column: "CheckedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FeasibilityChecks_Status",
                table: "FeasibilityChecks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_FeasibilityChecks_Order_Type",
                table: "FeasibilityChecks",
                columns: new[] { "OrderId", "CheckType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_ConnectionId",
                table: "Feedbacks",
                column: "ConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_CustomerId",
                table: "Feedbacks",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_OrderId",
                table: "Feedbacks",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_SubmittedAtUtc",
                table: "Feedbacks",
                column: "SubmittedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CityId",
                table: "Orders",
                column: "CityId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ConnectionType",
                table: "Orders",
                column: "ConnectionType");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DiscountSchemeId",
                table: "Orders",
                column: "DiscountSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PlacedAtUtc",
                table: "Orders",
                column: "PlacedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PlacedByEmployeeId",
                table: "Orders",
                column: "PlacedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PlanId",
                table: "Orders",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RetailShopId",
                table: "Orders",
                column: "RetailShopId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_BillId",
                table: "Payments",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PaidAtUtc",
                table: "Payments",
                column: "PaidAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceivedByEmployeeId",
                table: "Payments",
                column: "ReceivedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_RetailShopId",
                table: "Payments",
                column: "RetailShopId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_ConnectionType",
                table: "Plans",
                column: "ConnectionType");

            migrationBuilder.CreateIndex(
                name: "UX_Plans_Name",
                table: "Plans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_VendorId",
                table: "Products",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "UX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_RetailShops_City_Name",
                table: "RetailShops",
                columns: new[] { "CityId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Users_CustomerId",
                table: "Users",
                column: "CustomerId",
                unique: true,
                filter: "[CustomerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Users_EmployeeId",
                table: "Users",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Vendors_Name",
                table: "Vendors",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionProducts");

            migrationBuilder.DropTable(
                name: "FeasibilityChecks");

            migrationBuilder.DropTable(
                name: "Feedbacks");

            migrationBuilder.DropTable(
                name: "IdentifierCounters");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Bills");

            migrationBuilder.DropTable(
                name: "Vendors");

            migrationBuilder.DropTable(
                name: "Connections");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "DiscountSchemes");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropTable(
                name: "RetailShops");

            migrationBuilder.DropTable(
                name: "Cities");
        }
    }
}
