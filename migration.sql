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
CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [BusinessName] nvarchar(max) NOT NULL,
    [Address] nvarchar(max) NOT NULL,
    [ContactInfo] nvarchar(max) NOT NULL,
    [UserType] int NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);

CREATE TABLE [Products] (
    [ProductID] int NOT NULL IDENTITY,
    [Name] nvarchar(100) NOT NULL,
    [Description] nvarchar(500) NOT NULL,
    [Category] nvarchar(max) NOT NULL,
    [DefaultPrice] decimal(18,2) NOT NULL,
    [ImageURL] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Products] PRIMARY KEY ([ProductID])
);

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Transactions] (
    [TransactionID] int NOT NULL IDENTITY,
    [RetailerID] nvarchar(450) NOT NULL,
    [WholesalerID] nvarchar(450) NOT NULL,
    [Date] datetime2 NOT NULL,
    [TotalAmount] decimal(18,2) NOT NULL,
    [Status] int NOT NULL,
    [PaymentMethod] nvarchar(max) NOT NULL,
    [Notes] nvarchar(max) NOT NULL,
    CONSTRAINT [PK_Transactions] PRIMARY KEY ([TransactionID]),
    CONSTRAINT [FK_Transactions_AspNetUsers_RetailerID] FOREIGN KEY ([RetailerID]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Transactions_AspNetUsers_WholesalerID] FOREIGN KEY ([WholesalerID]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [WholesalerProducts] (
    [WholesalerProductID] int NOT NULL IDENTITY,
    [ProductID] int NOT NULL,
    [WholesalerID] nvarchar(450) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [AvailableQuantity] int NOT NULL,
    [MinimumOrderQuantity] int NOT NULL,
    CONSTRAINT [PK_WholesalerProducts] PRIMARY KEY ([WholesalerProductID]),
    CONSTRAINT [FK_WholesalerProducts_AspNetUsers_WholesalerID] FOREIGN KEY ([WholesalerID]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_WholesalerProducts_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([ProductID]) ON DELETE CASCADE
);

CREATE TABLE [TransactionDetails] (
    [TransactionDetailID] int NOT NULL IDENTITY,
    [TransactionID] int NOT NULL,
    [ProductID] int NOT NULL,
    [Quantity] int NOT NULL,
    [UnitPrice] decimal(18,2) NOT NULL,
    [Subtotal] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_TransactionDetails] PRIMARY KEY ([TransactionDetailID]),
    CONSTRAINT [FK_TransactionDetails_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([ProductID]) ON DELETE CASCADE,
    CONSTRAINT [FK_TransactionDetails_Transactions_TransactionID] FOREIGN KEY ([TransactionID]) REFERENCES [Transactions] ([TransactionID]) ON DELETE CASCADE
);

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;

CREATE INDEX [IX_TransactionDetails_ProductID] ON [TransactionDetails] ([ProductID]);

CREATE INDEX [IX_TransactionDetails_TransactionID] ON [TransactionDetails] ([TransactionID]);

CREATE INDEX [IX_Transactions_RetailerID] ON [Transactions] ([RetailerID]);

CREATE INDEX [IX_Transactions_WholesalerID] ON [Transactions] ([WholesalerID]);

CREATE INDEX [IX_WholesalerProducts_ProductID] ON [WholesalerProducts] ([ProductID]);

CREATE INDEX [IX_WholesalerProducts_WholesalerID] ON [WholesalerProducts] ([WholesalerID]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250303234031_InitialCreate', N'9.0.2');

CREATE TABLE [RetailerProducts] (
    [RetailerProductID] int NOT NULL IDENTITY,
    [ProductID] int NOT NULL,
    [RetailerID] nvarchar(450) NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    [StockQuantity] int NOT NULL,
    CONSTRAINT [PK_RetailerProducts] PRIMARY KEY ([RetailerProductID]),
    CONSTRAINT [FK_RetailerProducts_AspNetUsers_RetailerID] FOREIGN KEY ([RetailerID]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_RetailerProducts_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([ProductID]) ON DELETE CASCADE
);

CREATE INDEX [IX_RetailerProducts_ProductID] ON [RetailerProducts] ([ProductID]);

CREATE INDEX [IX_RetailerProducts_RetailerID] ON [RetailerProducts] ([RetailerID]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250309150309_AddRetailerProductsTable', N'9.0.2');

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Products]') AND [c].[name] = N'Category');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Products] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Products] DROP COLUMN [Category];

ALTER TABLE [Products] ADD [CategoryID] int NOT NULL DEFAULT 0;

CREATE TABLE [Categories] (
    [CategoryID] int NOT NULL IDENTITY,
    [Name] nvarchar(max) NOT NULL,
    [Description] nvarchar(max) NOT NULL,
    [CreatedByUserID] nvarchar(max) NOT NULL,
    [IsGlobal] bit NOT NULL,
    CONSTRAINT [PK_Categories] PRIMARY KEY ([CategoryID])
);

CREATE INDEX [IX_Products_CategoryID] ON [Products] ([CategoryID]);

ALTER TABLE [Products] ADD CONSTRAINT [FK_Products_Categories_CategoryID] FOREIGN KEY ([CategoryID]) REFERENCES [Categories] ([CategoryID]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250310131220_category', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250310133102_category2', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250311034916_image', N'9.0.2');

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Categories]') AND [c].[name] = N'CreatedByUserID');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Categories] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Categories] ALTER COLUMN [CreatedByUserID] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250312001618_creat', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250312005119_stock', N'9.0.2');

CREATE INDEX [IX_Transactions_Date] ON [Transactions] ([Date]);

CREATE INDEX [IX_Products_Name] ON [Products] ([Name]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250407020101_AddNewTables', N'9.0.2');

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250408124159_order', N'9.0.2');

CREATE TABLE [Orders] (
    [OrderID] int NOT NULL IDENTITY,
    [RetailerID] nvarchar(450) NOT NULL,
    [WholesalerID] nvarchar(450) NOT NULL,
    [OrderDate] datetime2 NOT NULL,
    [Status] int NOT NULL,
    CONSTRAINT [PK_Orders] PRIMARY KEY ([OrderID]),
    CONSTRAINT [FK_Orders_AspNetUsers_RetailerID] FOREIGN KEY ([RetailerID]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Orders_AspNetUsers_WholesalerID] FOREIGN KEY ([WholesalerID]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [OrderItem] (
    [OrderItemID] int NOT NULL IDENTITY,
    [OrderID] int NOT NULL,
    [ProductID] int NOT NULL,
    [WholesalerProductID] int NOT NULL,
    [Quantity] int NOT NULL,
    [Price] decimal(18,2) NOT NULL,
    CONSTRAINT [PK_OrderItem] PRIMARY KEY ([OrderItemID]),
    CONSTRAINT [FK_OrderItem_Orders_OrderID] FOREIGN KEY ([OrderID]) REFERENCES [Orders] ([OrderID]) ON DELETE CASCADE,
    CONSTRAINT [FK_OrderItem_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([ProductID]) ON DELETE CASCADE
);

CREATE INDEX [IX_OrderItem_OrderID] ON [OrderItem] ([OrderID]);

CREATE INDEX [IX_OrderItem_ProductID] ON [OrderItem] ([ProductID]);

CREATE INDEX [IX_Orders_RetailerID] ON [Orders] ([RetailerID]);

CREATE INDEX [IX_Orders_WholesalerID] ON [Orders] ([WholesalerID]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250408124840_orderupdates', N'9.0.2');

ALTER TABLE [OrderItem] DROP CONSTRAINT [FK_OrderItem_Orders_OrderID];

ALTER TABLE [OrderItem] DROP CONSTRAINT [FK_OrderItem_Products_ProductID];

ALTER TABLE [OrderItem] DROP CONSTRAINT [PK_OrderItem];

EXEC sp_rename N'[OrderItem]', N'OrderItems', 'OBJECT';

EXEC sp_rename N'[OrderItems].[IX_OrderItem_ProductID]', N'IX_OrderItems_ProductID', 'INDEX';

EXEC sp_rename N'[OrderItems].[IX_OrderItem_OrderID]', N'IX_OrderItems_OrderID', 'INDEX';

ALTER TABLE [Orders] ADD [DeliveredDate] datetime2 NULL;

ALTER TABLE [Orders] ADD [ShippedDate] datetime2 NULL;

ALTER TABLE [Orders] ADD [TrackingNumber] nvarchar(max) NOT NULL DEFAULT N'';

ALTER TABLE [OrderItems] ADD CONSTRAINT [PK_OrderItems] PRIMARY KEY ([OrderItemID]);

CREATE TABLE [CartItems] (
    [CartItemID] int NOT NULL IDENTITY,
    [RetailerID] nvarchar(450) NOT NULL,
    [WholesalerProductID] int NOT NULL,
    [Quantity] int NOT NULL,
    [DateAdded] datetime2 NOT NULL,
    CONSTRAINT [PK_CartItems] PRIMARY KEY ([CartItemID]),
    CONSTRAINT [FK_CartItems_AspNetUsers_RetailerID] FOREIGN KEY ([RetailerID]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_CartItems_WholesalerProducts_WholesalerProductID] FOREIGN KEY ([WholesalerProductID]) REFERENCES [WholesalerProducts] ([WholesalerProductID]) ON DELETE CASCADE
);

CREATE INDEX [IX_OrderItems_WholesalerProductID] ON [OrderItems] ([WholesalerProductID]);

CREATE INDEX [IX_CartItems_RetailerID] ON [CartItems] ([RetailerID]);

CREATE INDEX [IX_CartItems_WholesalerProductID] ON [CartItems] ([WholesalerProductID]);

ALTER TABLE [OrderItems] ADD CONSTRAINT [FK_OrderItems_Orders_OrderID] FOREIGN KEY ([OrderID]) REFERENCES [Orders] ([OrderID]) ON DELETE CASCADE;

ALTER TABLE [OrderItems] ADD CONSTRAINT [FK_OrderItems_Products_ProductID] FOREIGN KEY ([ProductID]) REFERENCES [Products] ([ProductID]) ON DELETE CASCADE;

ALTER TABLE [OrderItems] ADD CONSTRAINT [FK_OrderItems_WholesalerProducts_WholesalerProductID] FOREIGN KEY ([WholesalerProductID]) REFERENCES [WholesalerProducts] ([WholesalerProductID]) ON DELETE CASCADE;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250408133447_cart', N'9.0.2');

ALTER TABLE [Orders] DROP CONSTRAINT [FK_Orders_AspNetUsers_RetailerID];

ALTER TABLE [Orders] DROP CONSTRAINT [FK_Orders_AspNetUsers_WholesalerID];

ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_AspNetUsers_RetailerID] FOREIGN KEY ([RetailerID]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;

ALTER TABLE [Orders] ADD CONSTRAINT [FK_Orders_AspNetUsers_WholesalerID] FOREIGN KEY ([WholesalerID]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20250408140247_FixOrderRelationships', N'9.0.2');

COMMIT;
GO

