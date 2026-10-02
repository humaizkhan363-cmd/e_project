IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Cities] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Code] char(3) NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_Cities] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Cities_Code_Format] CHECK ([Code] COLLATE Latin1_General_100_BIN2 LIKE '[0-9][0-9][0-9]')
);
GO

CREATE TABLE [DiscountSchemes] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [MinConnections] int NOT NULL,
    [MaxConnections] int NULL,
    [DiscountPercent] decimal(5,2) NOT NULL,
    [AppliesToAdvance] bit NOT NULL,
    [AppliesToSecurityDeposit] bit NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_DiscountSchemes] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_DiscountSchemes_Percent] CHECK ([DiscountPercent] >= 0 AND [DiscountPercent] <= 100),
    CONSTRAINT [CK_DiscountSchemes_Range] CHECK ([MinConnections] >= 1 AND ([MaxConnections] IS NULL OR [MaxConnections] >= [MinConnections]))
);
GO

CREATE TABLE [IdentifierCounters] (
    [Scope] varchar(40) NOT NULL,
    [LastValue] bigint NOT NULL,
    CONSTRAINT [PK_IdentifierCounters] PRIMARY KEY ([Scope]),
    CONSTRAINT [CK_IdentifierCounters_LastValue] CHECK ([LastValue] >= 0)
);
GO

CREATE TABLE [Plans] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [Description] nvarchar(500) NULL,
    [ConnectionType] char(1) NOT NULL,
    [Kind] varchar(30) NOT NULL,
    [IncludedHours] int NULL,
    [SpeedKbps] int NULL,
    [ValidityMonths] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [SecurityDeposit] decimal(18,2) NOT NULL,
    [LocalCallRatePerMinute] decimal(10,4) NULL,
    [StdCallRatePerMinute] decimal(10,4) NULL,
    [MobileMessagingRatePerMinute] decimal(10,4) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Plans] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Plans_Amounts] CHECK ([ValidityMonths] > 0 AND [Price] >= 0 AND [SecurityDeposit] >= 0),
    CONSTRAINT [CK_Plans_ConnectionType_Letter] CHECK ([ConnectionType] COLLATE Latin1_General_100_BIN2 LIKE '[DBT]'),
    CONSTRAINT [CK_Plans_Hourly_Hours] CHECK ([Kind] <> 'Hourly' OR ([IncludedHours] IS NOT NULL AND [IncludedHours] > 0)),
    CONSTRAINT [CK_Plans_Kind_Matches_Type] CHECK (([Kind] IN ('LocalRental', 'StdRental') AND [ConnectionType] = 'T') OR ([Kind] IN ('Hourly', 'Unlimited') AND [ConnectionType] IN ('D', 'B'))),
    CONSTRAINT [CK_Plans_Kind_Values] CHECK ([Kind] IN ('Hourly', 'Unlimited', 'LocalRental', 'StdRental')),
    CONSTRAINT [CK_Plans_Unlimited_Speed] CHECK ([Kind] <> 'Unlimited' OR ([SpeedKbps] IS NOT NULL AND [SpeedKbps] > 0))
);
GO

CREATE TABLE [Vendors] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [ContactPerson] nvarchar(100) NULL,
    [Email] nvarchar(256) NULL,
    [Phone] nvarchar(20) NOT NULL,
    [AddressLine] nvarchar(300) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Vendors] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Customers] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [CustomerType] varchar(30) NOT NULL,
    [CompanyName] nvarchar(150) NULL,
    [Email] nvarchar(256) NULL,
    [Phone] nvarchar(20) NOT NULL,
    [AddressLine] nvarchar(300) NOT NULL,
    [PostalCode] nvarchar(20) NULL,
    [CityId] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Customers] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Customers_Corporate_Company] CHECK ([CustomerType] <> 'Corporate' OR [CompanyName] IS NOT NULL),
    CONSTRAINT [CK_Customers_CustomerType_Values] CHECK ([CustomerType] IN ('Individual', 'Corporate')),
    CONSTRAINT [FK_Customers_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [RetailShops] (
    [Id] int NOT NULL IDENTITY,
    [Name] nvarchar(150) NOT NULL,
    [AddressLine] nvarchar(300) NOT NULL,
    [Phone] nvarchar(20) NULL,
    [CityId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_RetailShops] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RetailShops_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Products] (
    [Id] int NOT NULL IDENTITY,
    [Sku] nvarchar(40) NOT NULL,
    [Name] nvarchar(150) NOT NULL,
    [Category] varchar(30) NOT NULL,
    [Description] nvarchar(500) NULL,
    [VendorId] int NOT NULL,
    [PurchasePrice] decimal(18,2) NOT NULL,
    [ReplacementCharge] decimal(18,2) NOT NULL,
    [StockQuantity] int NOT NULL,
    [ReorderLevel] int NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Products_Category_Values] CHECK ([Category] IN ('Modem', 'Router', 'Other')),
    CONSTRAINT [CK_Products_Prices_NonNegative] CHECK ([PurchasePrice] >= 0 AND [ReplacementCharge] >= 0),
    CONSTRAINT [CK_Products_Stock_NonNegative] CHECK ([StockQuantity] >= 0 AND [ReorderLevel] >= 0),
    CONSTRAINT [FK_Products_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Employees] (
    [Id] int NOT NULL IDENTITY,
    [FullName] nvarchar(150) NOT NULL,
    [Email] nvarchar(256) NOT NULL,
    [Phone] nvarchar(20) NOT NULL,
    [Role] varchar(30) NOT NULL,
    [RetailShopId] int NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Employees_Role_Shop] CHECK (([Role] = 'RetailStaff' AND [RetailShopId] IS NOT NULL) OR ([Role] <> 'RetailStaff' AND [RetailShopId] IS NULL)),
    CONSTRAINT [CK_Employees_Role_Values] CHECK ([Role] IN ('Admin', 'Accounts', 'Technical', 'RetailStaff')),
    CONSTRAINT [FK_Employees_RetailShops_RetailShopId] FOREIGN KEY ([RetailShopId]) REFERENCES [RetailShops] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Orders] (
    [Id] int NOT NULL IDENTITY,
    [OrderNumber] char(11) NOT NULL,
    [CustomerId] int NOT NULL,
    [ConnectionType] char(1) NOT NULL,
    [PlanId] int NOT NULL,
    [CityId] int NOT NULL,
    [InstallationAddress] nvarchar(300) NOT NULL,
    [Quantity] int NOT NULL,
    [RetailShopId] int NOT NULL,
    [PlacedByEmployeeId] int NOT NULL,
    [DiscountSchemeId] int NULL,
    [DiscountPercent] decimal(5,2) NOT NULL,
    [Status] varchar(30) NOT NULL,
    [PlacedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [StatusChangedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [Remarks] nvarchar(500) NULL,
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Orders_ConnectionType_Letter] CHECK ([ConnectionType] COLLATE Latin1_General_100_BIN2 LIKE '[DBT]'),
    CONSTRAINT [CK_Orders_DiscountPercent] CHECK ([DiscountPercent] >= 0 AND [DiscountPercent] <= 100),
    CONSTRAINT [CK_Orders_OrderNumber_Format] CHECK ([OrderNumber] COLLATE Latin1_General_100_BIN2 LIKE '[DBT][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [CK_Orders_OrderNumber_Type] CHECK (LEFT([OrderNumber], 1) = [ConnectionType]),
    CONSTRAINT [CK_Orders_Quantity] CHECK ([Quantity] >= 1),
    CONSTRAINT [CK_Orders_Status_Values] CHECK ([Status] IN ('Placed', 'UnderFeasibilityCheck', 'Feasible', 'NotFeasible', 'Connected', 'Cancelled')),
    CONSTRAINT [FK_Orders_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Orders_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Orders_DiscountSchemes_DiscountSchemeId] FOREIGN KEY ([DiscountSchemeId]) REFERENCES [DiscountSchemes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Orders_Employees_PlacedByEmployeeId] FOREIGN KEY ([PlacedByEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Orders_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [Plans] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Orders_RetailShops_RetailShopId] FOREIGN KEY ([RetailShopId]) REFERENCES [RetailShops] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Users] (
    [Id] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(500) NOT NULL,
    [IsActive] bit NOT NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [LastLoginAtUtc] datetime2 NULL,
    [EmployeeId] int NULL,
    [CustomerId] int NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Users_OneOwner] CHECK (([EmployeeId] IS NOT NULL AND [CustomerId] IS NULL) OR ([EmployeeId] IS NULL AND [CustomerId] IS NOT NULL)),
    CONSTRAINT [FK_Users_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Users_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Connections] (
    [Id] int NOT NULL IDENTITY,
    [AccountId] char(16) NOT NULL,
    [OrderId] int NOT NULL,
    [CustomerId] int NOT NULL,
    [PlanId] int NOT NULL,
    [ConnectionType] char(1) NOT NULL,
    [CityId] int NOT NULL,
    [InstallationAddress] nvarchar(300) NOT NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [LandlineConnectionId] int NULL,
    [Status] varchar(30) NOT NULL,
    [SecurityDepositAmount] decimal(18,2) NOT NULL,
    [CreatedByEmployeeId] int NOT NULL,
    [ActivatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [StatusChangedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Connections] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Connections_AccountId_Format] CHECK ([AccountId] COLLATE Latin1_General_100_BIN2 LIKE '[DBT][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9][0-9]'),
    CONSTRAINT [CK_Connections_AccountId_Type] CHECK (LEFT([AccountId], 1) = [ConnectionType]),
    CONSTRAINT [CK_Connections_ConnectionType_Letter] CHECK ([ConnectionType] COLLATE Latin1_General_100_BIN2 LIKE '[DBT]'),
    CONSTRAINT [CK_Connections_Deposit] CHECK ([SecurityDepositAmount] >= 0),
    CONSTRAINT [CK_Connections_Status_Values] CHECK ([Status] IN ('Active', 'TemporarilyInactive', 'PermanentlyInactive')),
    CONSTRAINT [FK_Connections_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Connections_Connections_LandlineConnectionId] FOREIGN KEY ([LandlineConnectionId]) REFERENCES [Connections] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Connections_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Connections_Employees_CreatedByEmployeeId] FOREIGN KEY ([CreatedByEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Connections_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Connections_Plans_PlanId] FOREIGN KEY ([PlanId]) REFERENCES [Plans] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [FeasibilityChecks] (
    [Id] int NOT NULL IDENTITY,
    [OrderId] int NOT NULL,
    [CheckType] varchar(30) NOT NULL,
    [Status] varchar(30) NOT NULL,
    [DistanceKm] decimal(8,2) NULL,
    [ServerAvailable] bit NULL,
    [Remarks] nvarchar(500) NULL,
    [CheckedByEmployeeId] int NULL,
    [CheckedAtUtc] datetime2 NULL,
    [CreatedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_FeasibilityChecks] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_FeasibilityChecks_CheckType_Values] CHECK ([CheckType] IN ('Landline', 'Internet')),
    CONSTRAINT [CK_FeasibilityChecks_Distance] CHECK ([DistanceKm] IS NULL OR [DistanceKm] >= 0),
    CONSTRAINT [CK_FeasibilityChecks_Status_Values] CHECK ([Status] IN ('Pending', 'Feasible', 'NotFeasible')),
    CONSTRAINT [FK_FeasibilityChecks_Employees_CheckedByEmployeeId] FOREIGN KEY ([CheckedByEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_FeasibilityChecks_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Bills] (
    [Id] int NOT NULL IDENTITY,
    [ConnectionId] int NOT NULL,
    [PeriodStart] date NOT NULL,
    [PeriodEnd] date NOT NULL,
    [IssueDate] date NOT NULL,
    [DueDate] date NOT NULL,
    [PlanCharge] decimal(18,2) NOT NULL,
    [UsageCharge] decimal(18,2) NOT NULL,
    [SecurityDepositCharge] decimal(18,2) NOT NULL,
    [ReplacementCharge] decimal(18,2) NOT NULL,
    [DiscountAmount] decimal(18,2) NOT NULL,
    [SubTotal] decimal(18,2) NOT NULL,
    [ServiceTaxRate] decimal(5,2) NOT NULL,
    [ServiceTaxAmount] decimal(18,2) NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Status] varchar(30) NOT NULL,
    [GeneratedByEmployeeId] int NOT NULL,
    [GeneratedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_Bills] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Bills_Amounts_NonNegative] CHECK ([PlanCharge] >= 0 AND [UsageCharge] >= 0 AND [SecurityDepositCharge] >= 0 AND [ReplacementCharge] >= 0 AND [DiscountAmount] >= 0 AND [SubTotal] >= 0 AND [ServiceTaxAmount] >= 0 AND [TotalAmount] >= 0),
    CONSTRAINT [CK_Bills_DueDate] CHECK ([DueDate] >= [IssueDate]),
    CONSTRAINT [CK_Bills_Period] CHECK ([PeriodEnd] >= [PeriodStart]),
    CONSTRAINT [CK_Bills_Status_Values] CHECK ([Status] IN ('Issued', 'PartiallyPaid', 'Paid', 'Cancelled')),
    CONSTRAINT [CK_Bills_TaxRate] CHECK ([ServiceTaxRate] >= 0 AND [ServiceTaxRate] <= 100),
    CONSTRAINT [FK_Bills_Connections_ConnectionId] FOREIGN KEY ([ConnectionId]) REFERENCES [Connections] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Bills_Employees_GeneratedByEmployeeId] FOREIGN KEY ([GeneratedByEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [ConnectionProducts] (
    [Id] int NOT NULL IDENTITY,
    [ConnectionId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    [SerialNumber] nvarchar(60) NULL,
    [IsReplacement] bit NOT NULL,
    [IssuedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [ReturnedAtUtc] datetime2 NULL,
    [Notes] nvarchar(500) NULL,
    CONSTRAINT [PK_ConnectionProducts] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ConnectionProducts_Quantity] CHECK ([Quantity] >= 1),
    CONSTRAINT [CK_ConnectionProducts_Returned] CHECK ([ReturnedAtUtc] IS NULL OR [ReturnedAtUtc] >= [IssuedAtUtc]),
    CONSTRAINT [FK_ConnectionProducts_Connections_ConnectionId] FOREIGN KEY ([ConnectionId]) REFERENCES [Connections] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ConnectionProducts_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Feedbacks] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] int NOT NULL,
    [OrderId] int NULL,
    [ConnectionId] int NULL,
    [Rating] int NOT NULL,
    [Comments] nvarchar(2000) NOT NULL,
    [SubmittedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_Feedbacks] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Feedbacks_Rating] CHECK ([Rating] >= 1 AND [Rating] <= 5),
    CONSTRAINT [FK_Feedbacks_Connections_ConnectionId] FOREIGN KEY ([ConnectionId]) REFERENCES [Connections] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Feedbacks_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Feedbacks_Orders_OrderId] FOREIGN KEY ([OrderId]) REFERENCES [Orders] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Payments] (
    [Id] int NOT NULL IDENTITY,
    [BillId] int NOT NULL,
    [Amount] decimal(18,2) NOT NULL,
    [Method] varchar(30) NOT NULL,
    [Reference] nvarchar(100) NULL,
    [PaidAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    [ReceivedByEmployeeId] int NOT NULL,
    [RetailShopId] int NULL,
    CONSTRAINT [PK_Payments] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Payments_Amount] CHECK ([Amount] > 0),
    CONSTRAINT [CK_Payments_Method_Values] CHECK ([Method] IN ('Cash', 'Card', 'BankTransfer', 'Cheque', 'Online')),
    CONSTRAINT [FK_Payments_Bills_BillId] FOREIGN KEY ([BillId]) REFERENCES [Bills] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Payments_Employees_ReceivedByEmployeeId] FOREIGN KEY ([ReceivedByEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Payments_RetailShops_RetailShopId] FOREIGN KEY ([RetailShopId]) REFERENCES [RetailShops] ([Id]) ON DELETE NO ACTION
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AppliesToAdvance', N'AppliesToSecurityDeposit', N'CreatedAtUtc', N'DiscountPercent', N'IsActive', N'MaxConnections', N'MinConnections', N'Name') AND [object_id] = OBJECT_ID(N'[DiscountSchemes]'))
    SET IDENTITY_INSERT [DiscountSchemes] ON;
INSERT INTO [DiscountSchemes] ([Id], [AppliesToAdvance], [AppliesToSecurityDeposit], [CreatedAtUtc], [DiscountPercent], [IsActive], [MaxConnections], [MinConnections], [Name])
VALUES (1, CAST(1 AS bit), CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', 25.0, CAST(1 AS bit), 14, 10, N'Bulk 10 to 14 connections'),
(2, CAST(1 AS bit), CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', 50.0, CAST(1 AS bit), 24, 15, N'Bulk 15 to 24 connections'),
(3, CAST(1 AS bit), CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', 75.0, CAST(1 AS bit), 49, 25, N'Bulk 25 to 49 connections'),
(4, CAST(1 AS bit), CAST(1 AS bit), '2026-01-01T00:00:00.0000000Z', 100.0, CAST(1 AS bit), NULL, 50, N'Bulk 50 or more connections');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'AppliesToAdvance', N'AppliesToSecurityDeposit', N'CreatedAtUtc', N'DiscountPercent', N'IsActive', N'MaxConnections', N'MinConnections', N'Name') AND [object_id] = OBJECT_ID(N'[DiscountSchemes]'))
    SET IDENTITY_INSERT [DiscountSchemes] OFF;
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConnectionType', N'CreatedAtUtc', N'Description', N'IncludedHours', N'IsActive', N'Kind', N'LocalCallRatePerMinute', N'MobileMessagingRatePerMinute', N'Name', N'Price', N'SecurityDeposit', N'SpeedKbps', N'StdCallRatePerMinute', N'ValidityMonths') AND [object_id] = OBJECT_ID(N'[Plans]'))
    SET IDENTITY_INSERT [Plans] ON;
INSERT INTO [Plans] ([Id], [ConnectionType], [CreatedAtUtc], [Description], [IncludedHours], [IsActive], [Kind], [LocalCallRatePerMinute], [MobileMessagingRatePerMinute], [Name], [Price], [SecurityDeposit], [SpeedKbps], [StdCallRatePerMinute], [ValidityMonths])
VALUES (1, 'D', '2026-01-01T00:00:00.0000000Z', NULL, 10, CAST(1 AS bit), 'Hourly', NULL, NULL, N'Dial-Up Hourly 10 Hrs', 50.0, 325.0, NULL, NULL, 1),
(2, 'D', '2026-01-01T00:00:00.0000000Z', NULL, 30, CAST(1 AS bit), 'Hourly', NULL, NULL, N'Dial-Up Hourly 30 Hrs', 130.0, 325.0, NULL, NULL, 3),
(3, 'D', '2026-01-01T00:00:00.0000000Z', NULL, 60, CAST(1 AS bit), 'Hourly', NULL, NULL, N'Dial-Up Hourly 60 Hrs', 260.0, 325.0, NULL, NULL, 6),
(4, 'D', '2026-01-01T00:00:00.0000000Z', NULL, NULL, CAST(1 AS bit), 'Unlimited', NULL, NULL, N'Dial-Up Unlimited 28 Kbps Monthly', 75.0, 325.0, 28, NULL, 1),
(5, 'D', '2026-01-01T00:00:00.0000000Z', NULL, NULL, CAST(1 AS bit), 'Unlimited', NULL, NULL, N'Dial-Up Unlimited 28 Kbps Quarterly', 150.0, 325.0, 28, NULL, 3),
(6, 'D', '2026-01-01T00:00:00.0000000Z', NULL, NULL, CAST(1 AS bit), 'Unlimited', NULL, NULL, N'Dial-Up Unlimited 56 Kbps Monthly', 100.0, 325.0, 56, NULL, 1),
(7, 'D', '2026-01-01T00:00:00.0000000Z', NULL, NULL, CAST(1 AS bit), 'Unlimited', NULL, NULL, N'Dial-Up Unlimited 56 Kbps Quarterly', 180.0, 325.0, 56, NULL, 3),
(8, 'B', '2026-01-01T00:00:00.0000000Z', NULL, 30, CAST(1 AS bit), 'Hourly', NULL, NULL, N'Broadband Hourly 30 Hrs', 175.0, 500.0, NULL, NULL, 1),
(9, 'B', '2026-01-01T00:00:00.0000000Z', NULL, 60, CAST(1 AS bit), 'Hourly', NULL, NULL, N'Broadband Hourly 60 Hrs', 315.0, 500.0, NULL, NULL, 6),
(10, 'B', '2026-01-01T00:00:00.0000000Z', NULL, NULL, CAST(1 AS bit), 'Unlimited', NULL, NULL, N'Broadband Unlimited 64 Kbps Monthly', 225.0, 500.0, 64, NULL, 1),
(11, 'B', '2026-01-01T00:00:00.0000000Z', NULL, NULL, CAST(1 AS bit), 'Unlimited', NULL, NULL, N'Broadband Unlimited 64 Kbps Quarterly', 400.0, 500.0, 64, NULL, 3),
(12, 'B', '2026-01-01T00:00:00.0000000Z', NULL, NULL, CAST(1 AS bit), 'Unlimited', NULL, NULL, N'Broadband Unlimited 128 Kbps Monthly', 350.0, 500.0, 128, NULL, 1),
(13, 'B', '2026-01-01T00:00:00.0000000Z', NULL, NULL, CAST(1 AS bit), 'Unlimited', NULL, NULL, N'Broadband Unlimited 128 Kbps Quarterly', 445.0, 500.0, 128, NULL, 3),
(14, 'T', '2026-01-01T00:00:00.0000000Z', NULL, NULL, CAST(1 AS bit), 'LocalRental', 0.55, NULL, N'Landline Local Unlimited (Yearly rental)', 75.0, 250.0, NULL, NULL, 12),
(15, 'T', '2026-01-01T00:00:00.0000000Z', NULL, NULL, CAST(1 AS bit), 'LocalRental', 0.75, NULL, N'Landline Local Monthly', 35.0, 250.0, NULL, NULL, 1),
(16, 'T', '2026-01-01T00:00:00.0000000Z', NULL, NULL, CAST(1 AS bit), 'StdRental', 0.7, 1.0, N'Landline STD Monthly', 125.0, 250.0, NULL, 2.25, 1),
(17, 'T', '2026-01-01T00:00:00.0000000Z', NULL, NULL, CAST(1 AS bit), 'StdRental', 0.6, 1.15, N'Landline STD Half-Yearly', 420.0, 250.0, NULL, 2.0, 6),
(18, 'T', '2026-01-01T00:00:00.0000000Z', N'PRICE NOT GIVEN IN THE PROJECT SPECIFICATION. Admin must set the yearly rental before activating this plan.', NULL, CAST(0 AS bit), 'StdRental', 0.6, 1.25, N'Landline STD Yearly', 0.0, 250.0, NULL, 1.75, 12);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'ConnectionType', N'CreatedAtUtc', N'Description', N'IncludedHours', N'IsActive', N'Kind', N'LocalCallRatePerMinute', N'MobileMessagingRatePerMinute', N'Name', N'Price', N'SecurityDeposit', N'SpeedKbps', N'StdCallRatePerMinute', N'ValidityMonths') AND [object_id] = OBJECT_ID(N'[Plans]'))
    SET IDENTITY_INSERT [Plans] OFF;
GO

CREATE INDEX [IX_Bills_DueDate] ON [Bills] ([DueDate]);
GO

CREATE INDEX [IX_Bills_GeneratedByEmployeeId] ON [Bills] ([GeneratedByEmployeeId]);
GO

CREATE INDEX [IX_Bills_Status] ON [Bills] ([Status]);
GO

CREATE UNIQUE INDEX [UX_Bills_Connection_Period] ON [Bills] ([ConnectionId], [PeriodStart]);
GO

CREATE UNIQUE INDEX [UX_Cities_Code] ON [Cities] ([Code]);
GO

CREATE UNIQUE INDEX [UX_Cities_Name] ON [Cities] ([Name]);
GO

CREATE INDEX [IX_ConnectionProducts_ConnectionId] ON [ConnectionProducts] ([ConnectionId]);
GO

CREATE UNIQUE INDEX [UX_ConnectionProducts_Product_Serial] ON [ConnectionProducts] ([ProductId], [SerialNumber]) WHERE [SerialNumber] IS NOT NULL;
GO

CREATE INDEX [IX_Connections_ActivatedAtUtc] ON [Connections] ([ActivatedAtUtc]);
GO

CREATE INDEX [IX_Connections_CityId] ON [Connections] ([CityId]);
GO

CREATE INDEX [IX_Connections_ConnectionType] ON [Connections] ([ConnectionType]);
GO

CREATE INDEX [IX_Connections_CreatedByEmployeeId] ON [Connections] ([CreatedByEmployeeId]);
GO

CREATE INDEX [IX_Connections_CustomerId] ON [Connections] ([CustomerId]);
GO

CREATE INDEX [IX_Connections_LandlineConnectionId] ON [Connections] ([LandlineConnectionId]);
GO

CREATE INDEX [IX_Connections_OrderId] ON [Connections] ([OrderId]);
GO

CREATE INDEX [IX_Connections_PlanId] ON [Connections] ([PlanId]);
GO

CREATE INDEX [IX_Connections_Status] ON [Connections] ([Status]);
GO

CREATE UNIQUE INDEX [UX_Connections_AccountId] ON [Connections] ([AccountId]);
GO

CREATE INDEX [IX_Customers_CityId] ON [Customers] ([CityId]);
GO

CREATE INDEX [IX_Customers_FullName] ON [Customers] ([FullName]);
GO

CREATE INDEX [IX_Customers_Phone] ON [Customers] ([Phone]);
GO

CREATE UNIQUE INDEX [UX_Customers_Email] ON [Customers] ([Email]) WHERE [Email] IS NOT NULL;
GO

CREATE UNIQUE INDEX [UX_DiscountSchemes_Min] ON [DiscountSchemes] ([MinConnections]);
GO

CREATE UNIQUE INDEX [UX_DiscountSchemes_Name] ON [DiscountSchemes] ([Name]);
GO

CREATE INDEX [IX_Employees_RetailShopId] ON [Employees] ([RetailShopId]);
GO

CREATE UNIQUE INDEX [UX_Employees_Email] ON [Employees] ([Email]);
GO

CREATE INDEX [IX_FeasibilityChecks_CheckedByEmployeeId] ON [FeasibilityChecks] ([CheckedByEmployeeId]);
GO

CREATE INDEX [IX_FeasibilityChecks_Status] ON [FeasibilityChecks] ([Status]);
GO

CREATE UNIQUE INDEX [UX_FeasibilityChecks_Order_Type] ON [FeasibilityChecks] ([OrderId], [CheckType]);
GO

CREATE INDEX [IX_Feedbacks_ConnectionId] ON [Feedbacks] ([ConnectionId]);
GO

CREATE INDEX [IX_Feedbacks_CustomerId] ON [Feedbacks] ([CustomerId]);
GO

CREATE INDEX [IX_Feedbacks_OrderId] ON [Feedbacks] ([OrderId]);
GO

CREATE INDEX [IX_Feedbacks_SubmittedAtUtc] ON [Feedbacks] ([SubmittedAtUtc]);
GO

CREATE INDEX [IX_Orders_CityId] ON [Orders] ([CityId]);
GO

CREATE INDEX [IX_Orders_ConnectionType] ON [Orders] ([ConnectionType]);
GO

CREATE INDEX [IX_Orders_CustomerId] ON [Orders] ([CustomerId]);
GO

CREATE INDEX [IX_Orders_DiscountSchemeId] ON [Orders] ([DiscountSchemeId]);
GO

CREATE INDEX [IX_Orders_PlacedAtUtc] ON [Orders] ([PlacedAtUtc]);
GO

CREATE INDEX [IX_Orders_PlacedByEmployeeId] ON [Orders] ([PlacedByEmployeeId]);
GO

CREATE INDEX [IX_Orders_PlanId] ON [Orders] ([PlanId]);
GO

CREATE INDEX [IX_Orders_RetailShopId] ON [Orders] ([RetailShopId]);
GO

CREATE INDEX [IX_Orders_Status] ON [Orders] ([Status]);
GO

CREATE UNIQUE INDEX [UX_Orders_OrderNumber] ON [Orders] ([OrderNumber]);
GO

CREATE INDEX [IX_Payments_BillId] ON [Payments] ([BillId]);
GO

CREATE INDEX [IX_Payments_PaidAtUtc] ON [Payments] ([PaidAtUtc]);
GO

CREATE INDEX [IX_Payments_ReceivedByEmployeeId] ON [Payments] ([ReceivedByEmployeeId]);
GO

CREATE INDEX [IX_Payments_RetailShopId] ON [Payments] ([RetailShopId]);
GO

CREATE INDEX [IX_Plans_ConnectionType] ON [Plans] ([ConnectionType]);
GO

CREATE UNIQUE INDEX [UX_Plans_Name] ON [Plans] ([Name]);
GO

CREATE INDEX [IX_Products_Name] ON [Products] ([Name]);
GO

CREATE INDEX [IX_Products_VendorId] ON [Products] ([VendorId]);
GO

CREATE UNIQUE INDEX [UX_Products_Sku] ON [Products] ([Sku]);
GO

CREATE UNIQUE INDEX [UX_RetailShops_City_Name] ON [RetailShops] ([CityId], [Name]);
GO

CREATE UNIQUE INDEX [UX_Users_CustomerId] ON [Users] ([CustomerId]) WHERE [CustomerId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [UX_Users_EmployeeId] ON [Users] ([EmployeeId]) WHERE [EmployeeId] IS NOT NULL;
GO

CREATE UNIQUE INDEX [UX_Users_Username] ON [Users] ([Username]);
GO

CREATE UNIQUE INDEX [UX_Vendors_Name] ON [Vendors] ([Name]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260929172739_InitialCreate', N'8.0.31');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'RetailShopId');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Orders] ALTER COLUMN [RetailShopId] int NULL;
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Orders]') AND [c].[name] = N'PlacedByEmployeeId');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Orders] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Orders] ALTER COLUMN [PlacedByEmployeeId] int NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260930094256_CustomerSelfServiceOrders', N'8.0.31');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [ProductPurchases] (
    [Id] int NOT NULL IDENTITY,
    [VendorId] int NOT NULL,
    [ProductId] int NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [AmountPaid] decimal(18,2) NOT NULL,
    [PurchaseDate] date NOT NULL,
    [SupplierReference] nvarchar(100) NULL,
    [Notes] nvarchar(500) NULL,
    [RecordedByEmployeeId] int NOT NULL,
    [RecordedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_ProductPurchases] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_ProductPurchases_Positive] CHECK ([Quantity] > 0 AND [UnitPrice] >= 0 AND [AmountPaid] >= 0 AND [AmountPaid] <= [Quantity] * [UnitPrice]),
    CONSTRAINT [FK_ProductPurchases_Employees_RecordedByEmployeeId] FOREIGN KEY ([RecordedByEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ProductPurchases_Products_ProductId] FOREIGN KEY ([ProductId]) REFERENCES [Products] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_ProductPurchases_Vendors_VendorId] FOREIGN KEY ([VendorId]) REFERENCES [Vendors] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_ProductPurchases_ProductId] ON [ProductPurchases] ([ProductId]);
GO

CREATE INDEX [IX_ProductPurchases_PurchaseDate] ON [ProductPurchases] ([PurchaseDate]);
GO

CREATE INDEX [IX_ProductPurchases_RecordedByEmployeeId] ON [ProductPurchases] ([RecordedByEmployeeId]);
GO

CREATE INDEX [IX_ProductPurchases_VendorId] ON [ProductPurchases] ([VendorId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260930134834_VendorProductPurchases', N'8.0.31');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [CustomerDocuments] (
    [Id] int NOT NULL IDENTITY,
    [CustomerId] int NOT NULL,
    [CityId] int NOT NULL,
    [DocumentType] varchar(30) NOT NULL,
    [DocumentYear] int NOT NULL,
    [OriginalFileName] nvarchar(255) NOT NULL,
    [StorageName] nvarchar(80) NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [SizeBytes] bigint NOT NULL,
    [Notes] nvarchar(500) NULL,
    [UploadedByUserId] int NOT NULL,
    [UploadedAtUtc] datetime2 NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT [PK_CustomerDocuments] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_CustomerDocuments_Year] CHECK ([DocumentYear] BETWEEN 2000 AND 2100),
    CONSTRAINT [FK_CustomerDocuments_Cities_CityId] FOREIGN KEY ([CityId]) REFERENCES [Cities] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CustomerDocuments_Customers_CustomerId] FOREIGN KEY ([CustomerId]) REFERENCES [Customers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_CustomerDocuments_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_CustomerDocuments_CityId] ON [CustomerDocuments] ([CityId]);
GO

CREATE INDEX [IX_CustomerDocuments_CustomerId_DocumentYear_CityId] ON [CustomerDocuments] ([CustomerId], [DocumentYear], [CityId]);
GO

CREATE INDEX [IX_CustomerDocuments_UploadedByUserId] ON [CustomerDocuments] ([UploadedByUserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260930135529_CustomerDocumentFiling', N'8.0.31');
GO

COMMIT;
GO

