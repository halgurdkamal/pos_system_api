CREATE TABLE "Categories" (
    "Id" text NOT NULL,
    "CategoryId" character varying(50) NOT NULL,
    "Name" character varying(100) NOT NULL,
    "LogoUrl" character varying(500) NULL,
    "Description" character varying(500) NULL,
    "ColorCode" character varying(7) NULL,
    "DisplayOrder" integer NOT NULL DEFAULT 0,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" character varying(100) NULL,
    CONSTRAINT "PK_Categories" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_Categories_CategoryId" UNIQUE ("CategoryId")
);


CREATE TABLE "InventoryAlerts" (
    "Id" text NOT NULL,
    "ShopId" character varying(50) NOT NULL,
    "DrugId" character varying(50) NOT NULL,
    "BatchNumber" character varying(50) NULL,
    "AlertType" text NOT NULL,
    "Severity" text NOT NULL,
    "Status" text NOT NULL,
    "Message" character varying(500) NOT NULL,
    "CurrentQuantity" integer NULL,
    "ThresholdQuantity" integer NULL,
    "ExpiryDate" timestamp with time zone NULL,
    "GeneratedAt" timestamp with time zone NOT NULL,
    "AcknowledgedAt" timestamp with time zone NULL,
    "AcknowledgedBy" character varying(100) NULL,
    "ResolvedAt" timestamp with time zone NULL,
    "ResolvedBy" character varying(100) NULL,
    "ResolutionNotes" character varying(1000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    CONSTRAINT "PK_InventoryAlerts" PRIMARY KEY ("Id")
);


CREATE TABLE "PurchaseOrders" (
    "Id" text NOT NULL,
    "OrderNumber" character varying(50) NOT NULL,
    "ShopId" character varying(100) NOT NULL,
    "SupplierId" character varying(100) NOT NULL,
    "Status" character varying(50) NOT NULL,
    "Priority" character varying(20) NOT NULL,
    "SubTotal" numeric(18,2) NOT NULL,
    "TaxAmount" numeric(18,2) NOT NULL,
    "ShippingCost" numeric(18,2) NOT NULL,
    "DiscountAmount" numeric(18,2) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "PaymentTerms" character varying(20) NOT NULL,
    "CustomPaymentTerms" character varying(200) NULL,
    "PaymentDueDate" timestamp with time zone NULL,
    "IsPaid" boolean NOT NULL,
    "PaidAt" timestamp with time zone NULL,
    "OrderDate" timestamp with time zone NOT NULL,
    "ExpectedDeliveryDate" timestamp with time zone NULL,
    "SubmittedAt" timestamp with time zone NULL,
    "ConfirmedAt" timestamp with time zone NULL,
    "CompletedAt" timestamp with time zone NULL,
    "CancelledAt" timestamp with time zone NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "SubmittedBy" character varying(100) NULL,
    "ConfirmedBy" character varying(100) NULL,
    "CancelledBy" character varying(100) NULL,
    "CancellationReason" character varying(500) NULL,
    "Notes" character varying(2000) NULL,
    "DeliveryAddress" character varying(500) NULL,
    "ReferenceNumber" character varying(100) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    CONSTRAINT "PK_PurchaseOrders" PRIMARY KEY ("Id")
);


CREATE TABLE "SalesOrders" (
    "Id" text NOT NULL,
    "OrderNumber" character varying(50) NOT NULL,
    "ShopId" character varying(100) NOT NULL,
    "CustomerId" character varying(100) NULL,
    "CustomerName" character varying(200) NULL,
    "CustomerPhone" character varying(50) NULL,
    "Status" character varying(50) NOT NULL,
    "SubTotal" numeric(18,2) NOT NULL,
    "TaxAmount" numeric(18,2) NOT NULL,
    "DiscountAmount" numeric(18,2) NOT NULL,
    "TotalAmount" numeric(18,2) NOT NULL,
    "AmountPaid" numeric(18,2) NOT NULL,
    "ChangeGiven" numeric(18,2) NOT NULL,
    "PaymentMethod" character varying(50) NULL,
    "PaymentReference" character varying(200) NULL,
    "PaidAt" timestamp with time zone NULL,
    "OrderDate" timestamp with time zone NOT NULL,
    "CompletedAt" timestamp with time zone NULL,
    "CancelledAt" timestamp with time zone NULL,
    "CashierId" character varying(100) NOT NULL,
    "CancelledBy" character varying(100) NULL,
    "CancellationReason" character varying(500) NULL,
    "Notes" character varying(2000) NULL,
    "IsPrescriptionRequired" boolean NOT NULL,
    "PrescriptionNumber" character varying(100) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    CONSTRAINT "PK_SalesOrders" PRIMARY KEY ("Id")
);


CREATE TABLE "Shops" (
    "Id" character varying(50) NOT NULL,
    "ShopName" character varying(200) NOT NULL,
    "LegalName" character varying(300) NOT NULL,
    "LicenseNumber" character varying(100) NOT NULL,
    "TaxId" character varying(100) NULL,
    "VatRegistrationNumber" character varying(100) NULL,
    "PharmacyRegistrationNumber" character varying(100) NULL,
    "Address_Street" character varying(300) NOT NULL,
    "Address_City" character varying(100) NOT NULL,
    "Address_State" character varying(100) NOT NULL,
    "Address_ZipCode" character varying(20) NOT NULL,
    "Address_Country" character varying(100) NOT NULL,
    "Contact_Phone" character varying(50) NOT NULL,
    "Contact_Email" character varying(200) NOT NULL,
    "Contact_Website" character varying(300) NULL,
    "LogoUrl" character varying(500) NULL,
    "ShopImageUrls" jsonb NOT NULL,
    "BrandColorPrimary" character varying(10) NULL,
    "BrandColorSecondary" character varying(10) NULL,
    "Receipt_ShopName" character varying(200) NOT NULL,
    "Receipt_HeaderText" character varying(500) NULL,
    "Receipt_FooterText" character varying(500) NULL,
    "Receipt_ReturnPolicy" character varying(1000) NULL,
    "Receipt_PharmacistName" character varying(200) NULL,
    "Receipt_ShowLogo" boolean NOT NULL,
    "Receipt_ShowTaxBreakdown" boolean NOT NULL,
    "Receipt_ShowBarcode" boolean NOT NULL,
    "Receipt_ShowQrCode" boolean NOT NULL,
    "Receipt_ShowPharmacyLicense" boolean NOT NULL,
    "Receipt_ShowVatNumber" boolean NOT NULL,
    "Receipt_Width" integer NOT NULL,
    "Receipt_Language" character varying(10) NOT NULL,
    "Receipt_PrintDuplicate" boolean NOT NULL,
    "Receipt_PharmacyWarning" character varying(500) NULL,
    "Receipt_ControlledSubstanceWarning" character varying(500) NULL,
    "Hardware_ReceiptPrinterName" character varying(200) NULL,
    "Hardware_ReceiptPrinterConnection" character varying(50) NULL,
    "Hardware_ReceiptPrinterIp" character varying(50) NULL,
    "Hardware_ReceiptPrinterPort" integer NULL,
    "Hardware_BarcodePrinterName" character varying(200) NULL,
    "Hardware_BarcodePrinterConnection" character varying(50) NULL,
    "Hardware_BarcodePrinterIp" character varying(50) NULL,
    "Hardware_BarcodeLabelSize" integer NOT NULL,
    "Hardware_BarcodeScannerModel" character varying(200) NULL,
    "Hardware_BarcodeScannerConnection" character varying(50) NULL,
    "Hardware_AutoSubmitOnScan" boolean NOT NULL,
    "Hardware_CashDrawerModel" character varying(200) NULL,
    "Hardware_CashDrawerEnabled" boolean NOT NULL,
    "Hardware_CashDrawerOpenCommand" character varying(100) NULL,
    "Hardware_PaymentTerminalModel" character varying(200) NULL,
    "Hardware_PaymentTerminalConnection" character varying(50) NULL,
    "Hardware_PaymentTerminalIp" character varying(50) NULL,
    "Hardware_IntegratedPayments" boolean NOT NULL,
    "Hardware_PosTerminalId" character varying(50) NULL,
    "Hardware_PosTerminalName" character varying(200) NULL,
    "Hardware_CustomerDisplayEnabled" boolean NOT NULL,
    "Hardware_CustomerDisplayType" character varying(100) NULL,
    "Currency" character varying(10) NOT NULL,
    "DefaultTaxRate" numeric(5,2) NOT NULL,
    "AutoReorderEnabled" boolean NOT NULL,
    "LowStockAlertThreshold" integer NOT NULL,
    "OperatingHours" jsonb NOT NULL,
    "RequiresPrescriptionVerification" boolean NOT NULL,
    "AllowsControlledSubstances" boolean NOT NULL,
    "AcceptedInsuranceProviders" jsonb NOT NULL,
    "Status" integer NOT NULL,
    "RegistrationDate" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" character varying(100) NULL,
    CONSTRAINT "PK_Shops" PRIMARY KEY ("Id")
);


CREATE TABLE "StockAdjustments" (
    "Id" character varying(50) NOT NULL,
    "ShopId" character varying(50) NOT NULL,
    "DrugId" character varying(50) NOT NULL,
    "BatchNumber" character varying(100) NULL,
    "AdjustmentType" integer NOT NULL,
    "QuantityChanged" integer NOT NULL,
    "QuantityBefore" integer NOT NULL,
    "QuantityAfter" integer NOT NULL,
    "Reason" character varying(500) NOT NULL,
    "Notes" character varying(1000) NULL,
    "AdjustedBy" character varying(50) NOT NULL,
    "AdjustedAt" timestamp with time zone NOT NULL,
    "ReferenceId" character varying(50) NULL,
    "ReferenceType" character varying(50) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    CONSTRAINT "PK_StockAdjustments" PRIMARY KEY ("Id")
);


CREATE TABLE "StockCounts" (
    "Id" character varying(50) NOT NULL,
    "ShopId" character varying(50) NOT NULL,
    "DrugId" character varying(50) NOT NULL,
    "Status" integer NOT NULL,
    "SystemQuantity" integer NOT NULL,
    "PhysicalQuantity" integer NULL,
    "VarianceQuantity" integer NULL,
    "VarianceReason" character varying(500) NULL,
    "CountedBy" character varying(50) NOT NULL,
    "ScheduledAt" timestamp with time zone NOT NULL,
    "CountedAt" timestamp with time zone NULL,
    "CompletedAt" timestamp with time zone NULL,
    "Notes" character varying(1000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    CONSTRAINT "PK_StockCounts" PRIMARY KEY ("Id")
);


CREATE TABLE "StockTransfers" (
    "Id" character varying(50) NOT NULL,
    "FromShopId" character varying(50) NOT NULL,
    "ToShopId" character varying(50) NOT NULL,
    "DrugId" character varying(50) NOT NULL,
    "BatchNumber" character varying(100) NULL,
    "Quantity" integer NOT NULL,
    "Status" integer NOT NULL,
    "InitiatedBy" character varying(50) NOT NULL,
    "InitiatedAt" timestamp with time zone NOT NULL,
    "ApprovedBy" character varying(50) NULL,
    "ApprovedAt" timestamp with time zone NULL,
    "ReceivedBy" character varying(50) NULL,
    "ReceivedAt" timestamp with time zone NULL,
    "CancelledBy" character varying(50) NULL,
    "CancelledAt" timestamp with time zone NULL,
    "CancellationReason" character varying(500) NULL,
    "Notes" character varying(1000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    CONSTRAINT "PK_StockTransfers" PRIMARY KEY ("Id")
);


CREATE TABLE "Suppliers" (
    "Id" character varying(50) NOT NULL,
    "SupplierName" character varying(200) NOT NULL,
    "SupplierType" integer NOT NULL,
    "ContactNumber" character varying(50) NOT NULL,
    "Email" character varying(200) NOT NULL,
    "Address_Street" character varying(300) NOT NULL,
    "Address_City" character varying(100) NOT NULL,
    "Address_State" character varying(100) NOT NULL,
    "Address_ZipCode" character varying(20) NOT NULL,
    "Address_Country" character varying(100) NOT NULL,
    "PaymentTerms" character varying(100) NOT NULL,
    "DeliveryLeadTime" integer NOT NULL,
    "MinimumOrderValue" numeric(18,2) NOT NULL,
    "IsActive" boolean NOT NULL,
    "Website" character varying(300) NULL,
    "TaxId" character varying(100) NULL,
    "LicenseNumber" character varying(100) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" character varying(100) NULL,
    CONSTRAINT "PK_Suppliers" PRIMARY KEY ("Id")
);


CREATE TABLE "Users" (
    "Id" character varying(50) NOT NULL,
    "Username" character varying(100) NOT NULL,
    "Email" character varying(255) NOT NULL,
    "PasswordHash" character varying(500) NOT NULL,
    "FullName" character varying(200) NOT NULL,
    "SystemRole" integer NOT NULL DEFAULT 1,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "IsEmailVerified" boolean NOT NULL DEFAULT FALSE,
    "LastLoginAt" timestamp with time zone NULL,
    "FailedLoginAttempts" integer NOT NULL DEFAULT 0,
    "LockedUntil" timestamp with time zone NULL,
    "RefreshToken" character varying(500) NULL,
    "RefreshTokenExpiryTime" timestamp with time zone NULL,
    "Phone" character varying(20) NULL,
    "ProfileImageUrl" character varying(500) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);


CREATE TABLE "Drugs" (
    "Id" text NOT NULL,
    "DrugId" character varying(50) NOT NULL,
    "Barcode" character varying(100) NOT NULL,
    "BarcodeType" character varying(50) NOT NULL,
    "BrandName" character varying(200) NOT NULL,
    "GenericName" character varying(200) NOT NULL,
    "Manufacturer" character varying(200) NOT NULL,
    "OriginCountry" character varying(100) NOT NULL,
    "Category" character varying(100) NOT NULL,
    "CategoryId" character varying(50) NOT NULL,
    "ImageUrls" jsonb NOT NULL,
    "Description" character varying(1000) NOT NULL,
    "SideEffects" jsonb NOT NULL,
    "InteractionNotes" jsonb NOT NULL,
    "Tags" jsonb NOT NULL,
    "RelatedDrugs" jsonb NOT NULL,
    "FormulationForm" character varying(50) NOT NULL,
    "FormulationStrength" character varying(50) NOT NULL,
    "RouteOfAdministration" character varying(50) NOT NULL,
    "BasePricing_SuggestedRetailPrice" numeric(18,2) NOT NULL,
    "BasePricing_Currency" character varying(10) NOT NULL,
    "BasePricing_SuggestedTaxRate" numeric(5,2) NOT NULL,
    "BasePricing_LastPriceUpdate" timestamp with time zone NULL,
    "IsPrescriptionRequired" boolean NOT NULL,
    "IsHighRisk" boolean NOT NULL,
    "DrugAuthorityNumber" character varying(100) NOT NULL,
    "ApprovalDate" timestamp with time zone NOT NULL,
    "ControlSchedule" character varying(50) NOT NULL,
    "PackagingInfo_UnitType" text NOT NULL,
    "PackagingInfo_BaseUnit" character varying(50) NOT NULL,
    "PackagingInfo_BaseUnitDisplayName" character varying(100) NOT NULL,
    "PackagingInfo_IsSubdivisible" boolean NOT NULL,
    "PackagingInfo_PackagingLevels" jsonb NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" character varying(100) NULL,
    CONSTRAINT "PK_Drugs" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_Drugs_DrugId" UNIQUE ("DrugId"),
    CONSTRAINT "FK_Drugs_Categories_CategoryId" FOREIGN KEY ("CategoryId") REFERENCES "Categories" ("CategoryId") ON DELETE RESTRICT
);


CREATE TABLE "PurchaseOrderItems" (
    "Id" text NOT NULL,
    "PurchaseOrderId" character varying(100) NOT NULL,
    "DrugId" character varying(100) NOT NULL,
    "OrderedQuantity" integer NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "DiscountPercentage" numeric(5,2) NOT NULL,
    "DiscountAmount" numeric(18,2) NOT NULL,
    "TotalPrice" numeric(18,2) NOT NULL,
    "ReceivedQuantity" integer NOT NULL,
    CONSTRAINT "PK_PurchaseOrderItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId" FOREIGN KEY ("PurchaseOrderId") REFERENCES "PurchaseOrders" ("Id") ON DELETE CASCADE
);


CREATE TABLE "SalesOrderItems" (
    "Id" text NOT NULL,
    "SalesOrderId" character varying(100) NOT NULL,
    "DrugId" character varying(100) NOT NULL,
    "Quantity" integer NOT NULL,
    "UnitPrice" numeric(18,2) NOT NULL,
    "DiscountPercentage" numeric(5,2) NOT NULL,
    "DiscountAmount" numeric(18,2) NOT NULL,
    "TotalPrice" numeric(18,2) NOT NULL,
    "PackagingLevelSold" character varying(50) NULL,
    "BaseUnitsConsumed" numeric(18,2) NOT NULL,
    "BatchNumber" character varying(100) NULL,
    CONSTRAINT "PK_SalesOrderItems" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_SalesOrderItems_SalesOrders_SalesOrderId" FOREIGN KEY ("SalesOrderId") REFERENCES "SalesOrders" ("Id") ON DELETE CASCADE
);


CREATE TABLE "ShopUsers" (
    "Id" character varying(50) NOT NULL,
    "UserId" character varying(50) NOT NULL,
    "ShopId" character varying(50) NOT NULL,
    "Role" integer NOT NULL DEFAULT 99,
    "Permissions" jsonb NOT NULL,
    "JoinedDate" timestamp with time zone NOT NULL DEFAULT (NOW()),
    "InvitedBy" character varying(50) NULL,
    "IsOwner" boolean NOT NULL DEFAULT FALSE,
    "IsActive" boolean NOT NULL DEFAULT TRUE,
    "LastAccessDate" timestamp with time zone NULL,
    "Notes" character varying(1000) NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" text NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" text NULL,
    CONSTRAINT "PK_ShopUsers" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ShopUsers_Shops_ShopId" FOREIGN KEY ("ShopId") REFERENCES "Shops" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ShopUsers_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE
);


CREATE TABLE "ShopInventory" (
    "Id" character varying(50) NOT NULL,
    "ShopId" character varying(50) NOT NULL,
    "DrugId" character varying(50) NOT NULL,
    "TotalStock" integer NOT NULL,
    "ReorderPoint" integer NOT NULL,
    "StorageLocation" character varying(300) NOT NULL,
    "ShopSpecificSellUnit" character varying(50) NULL,
    "MinimumSaleQuantity" numeric(18,2) NULL,
    "Batches" jsonb NOT NULL,
    "Pricing_CostPrice" numeric(18,2) NOT NULL,
    "Pricing_SellingPrice" numeric(18,2) NOT NULL,
    "Pricing_Discount" numeric(5,2) NOT NULL,
    "Pricing_Currency" character varying(10) NOT NULL,
    "Pricing_TaxRate" numeric(5,2) NOT NULL,
    "Pricing_LastPriceUpdate" timestamp with time zone NOT NULL,
    "Pricing_PackagingLevelPrices" jsonb NOT NULL,
    "IsAvailable" boolean NOT NULL,
    "LastRestockDate" timestamp with time zone NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" character varying(100) NULL,
    CONSTRAINT "PK_ShopInventory" PRIMARY KEY ("Id"),
    CONSTRAINT "AK_ShopInventory_ShopId_DrugId" UNIQUE ("ShopId", "DrugId"),
    CONSTRAINT "FK_ShopInventory_Drugs_DrugId" FOREIGN KEY ("DrugId") REFERENCES "Drugs" ("DrugId") ON DELETE RESTRICT,
    CONSTRAINT "FK_ShopInventory_Shops_ShopId" FOREIGN KEY ("ShopId") REFERENCES "Shops" ("Id") ON DELETE CASCADE
);


CREATE TABLE "ReceiptRecords" (
    "Id" text NOT NULL,
    "OrderItemId" character varying(100) NOT NULL,
    "Quantity" integer NOT NULL,
    "BatchNumber" character varying(100) NOT NULL,
    "ExpiryDate" timestamp with time zone NOT NULL,
    "ReceivedBy" character varying(100) NOT NULL,
    "ReceivedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "PK_ReceiptRecords" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ReceiptRecords_PurchaseOrderItems_OrderItemId" FOREIGN KEY ("OrderItemId") REFERENCES "PurchaseOrderItems" ("Id") ON DELETE CASCADE
);


CREATE TABLE "ShopPackagingOverrides" (
    "Id" character varying(50) NOT NULL,
    "ShopId" character varying(50) NOT NULL,
    "DrugId" character varying(50) NOT NULL,
    "PackagingLevelId" character varying(50) NULL,
    "ParentPackagingLevelId" character varying(50) NULL,
    "ParentOverrideId" character varying(50) NULL,
    "CustomUnitName" character varying(100) NULL,
    "OverrideQuantityPerParent" numeric(18,2) NULL,
    "SellingPrice" numeric(18,2) NULL,
    "IsSellable" boolean NULL,
    "IsDefaultSellUnit" boolean NULL,
    "MinimumSaleQuantity" numeric(18,2) NULL,
    "CustomLevelOrder" integer NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "CreatedBy" character varying(100) NOT NULL,
    "LastUpdated" timestamp with time zone NULL,
    "UpdatedBy" character varying(100) NULL,
    CONSTRAINT "PK_ShopPackagingOverrides" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_ShopPackagingOverrides_ShopInventory_ShopId_DrugId" FOREIGN KEY ("ShopId", "DrugId") REFERENCES "ShopInventory" ("ShopId", "DrugId") ON DELETE CASCADE
);


CREATE UNIQUE INDEX "IX_Categories_CategoryId" ON "Categories" ("CategoryId");


CREATE UNIQUE INDEX "IX_Categories_Name" ON "Categories" ("Name");


CREATE UNIQUE INDEX "IX_Drugs_Barcode" ON "Drugs" ("Barcode");


CREATE INDEX "IX_Drugs_CategoryId" ON "Drugs" ("CategoryId");


CREATE UNIQUE INDEX "IX_Drugs_DrugId" ON "Drugs" ("DrugId");


CREATE INDEX "IX_InventoryAlerts_AlertType" ON "InventoryAlerts" ("AlertType");


CREATE INDEX "IX_InventoryAlerts_DrugId" ON "InventoryAlerts" ("DrugId");


CREATE INDEX "IX_InventoryAlerts_GeneratedAt" ON "InventoryAlerts" ("GeneratedAt");


CREATE INDEX "IX_InventoryAlerts_Severity" ON "InventoryAlerts" ("Severity");


CREATE INDEX "IX_InventoryAlerts_ShopId" ON "InventoryAlerts" ("ShopId");


CREATE INDEX "IX_InventoryAlerts_ShopId_AlertType_Status" ON "InventoryAlerts" ("ShopId", "AlertType", "Status");


CREATE INDEX "IX_InventoryAlerts_ShopId_Status" ON "InventoryAlerts" ("ShopId", "Status");


CREATE INDEX "IX_InventoryAlerts_Status" ON "InventoryAlerts" ("Status");


CREATE INDEX "IX_PurchaseOrderItems_DrugId" ON "PurchaseOrderItems" ("DrugId");


CREATE INDEX "IX_PurchaseOrderItems_PurchaseOrderId" ON "PurchaseOrderItems" ("PurchaseOrderId");


CREATE INDEX "IX_PurchaseOrders_OrderDate" ON "PurchaseOrders" ("OrderDate");


CREATE UNIQUE INDEX "IX_PurchaseOrders_OrderNumber" ON "PurchaseOrders" ("OrderNumber");


CREATE INDEX "IX_PurchaseOrders_ShopId" ON "PurchaseOrders" ("ShopId");


CREATE INDEX "IX_PurchaseOrders_ShopId_OrderDate" ON "PurchaseOrders" ("ShopId", "OrderDate");


CREATE INDEX "IX_PurchaseOrders_ShopId_Status" ON "PurchaseOrders" ("ShopId", "Status");


CREATE INDEX "IX_PurchaseOrders_Status" ON "PurchaseOrders" ("Status");


CREATE INDEX "IX_PurchaseOrders_SupplierId" ON "PurchaseOrders" ("SupplierId");


CREATE INDEX "IX_ReceiptRecords_BatchNumber" ON "ReceiptRecords" ("BatchNumber");


CREATE INDEX "IX_ReceiptRecords_OrderItemId" ON "ReceiptRecords" ("OrderItemId");


CREATE INDEX "IX_ReceiptRecords_ReceivedAt" ON "ReceiptRecords" ("ReceivedAt");


CREATE INDEX "IX_SalesOrderItems_DrugId" ON "SalesOrderItems" ("DrugId");


CREATE INDEX "IX_SalesOrderItems_SalesOrderId" ON "SalesOrderItems" ("SalesOrderId");


CREATE INDEX "IX_SalesOrderItems_SalesOrderId_DrugId" ON "SalesOrderItems" ("SalesOrderId", "DrugId");


CREATE INDEX "IX_SalesOrders_CashierId" ON "SalesOrders" ("CashierId");


CREATE INDEX "IX_SalesOrders_CustomerId" ON "SalesOrders" ("CustomerId");


CREATE INDEX "IX_SalesOrders_OrderDate" ON "SalesOrders" ("OrderDate");


CREATE UNIQUE INDEX "IX_SalesOrders_OrderNumber" ON "SalesOrders" ("OrderNumber");


CREATE INDEX "IX_SalesOrders_ShopId" ON "SalesOrders" ("ShopId");


CREATE INDEX "IX_SalesOrders_ShopId_CashierId_OrderDate" ON "SalesOrders" ("ShopId", "CashierId", "OrderDate");


CREATE INDEX "IX_SalesOrders_ShopId_OrderDate" ON "SalesOrders" ("ShopId", "OrderDate");


CREATE INDEX "IX_SalesOrders_ShopId_Status" ON "SalesOrders" ("ShopId", "Status");


CREATE INDEX "IX_SalesOrders_Status" ON "SalesOrders" ("Status");


CREATE INDEX "IX_ShopInventory_DrugId" ON "ShopInventory" ("DrugId");


CREATE INDEX "IX_ShopInventory_IsAvailable" ON "ShopInventory" ("IsAvailable");


CREATE INDEX "IX_ShopInventory_LastRestockDate" ON "ShopInventory" ("LastRestockDate");


CREATE INDEX "IX_ShopInventory_ShopId" ON "ShopInventory" ("ShopId");


CREATE UNIQUE INDEX "IX_ShopInventory_ShopId_DrugId" ON "ShopInventory" ("ShopId", "DrugId");


CREATE INDEX "IX_ShopInventory_TotalStock" ON "ShopInventory" ("TotalStock");


CREATE INDEX "IX_ShopPackagingOverride_DefaultSellUnit" ON "ShopPackagingOverrides" ("ShopId", "DrugId", "IsDefaultSellUnit");


CREATE UNIQUE INDEX "IX_ShopPackagingOverride_ShopDrugLevel" ON "ShopPackagingOverrides" ("ShopId", "DrugId", "PackagingLevelId") WHERE "PackagingLevelId" IS NOT NULL;


CREATE INDEX "IX_ShopPackagingOverrides_ParentOverrideId" ON "ShopPackagingOverrides" ("ParentOverrideId");


CREATE INDEX "IX_ShopPackagingOverrides_ShopId_DrugId" ON "ShopPackagingOverrides" ("ShopId", "DrugId");


CREATE UNIQUE INDEX "IX_Shops_LicenseNumber" ON "Shops" ("LicenseNumber");


CREATE INDEX "IX_Shops_RegistrationDate" ON "Shops" ("RegistrationDate");


CREATE INDEX "IX_Shops_ShopName" ON "Shops" ("ShopName");


CREATE INDEX "IX_Shops_Status" ON "Shops" ("Status");


CREATE INDEX "IX_ShopUsers_IsActive" ON "ShopUsers" ("IsActive");


CREATE INDEX "IX_ShopUsers_IsOwner" ON "ShopUsers" ("IsOwner");


CREATE INDEX "IX_ShopUsers_JoinedDate" ON "ShopUsers" ("JoinedDate");


CREATE INDEX "IX_ShopUsers_Role" ON "ShopUsers" ("Role");


CREATE INDEX "IX_ShopUsers_ShopId" ON "ShopUsers" ("ShopId");


CREATE INDEX "IX_ShopUsers_UserId" ON "ShopUsers" ("UserId");


CREATE UNIQUE INDEX "IX_ShopUsers_UserId_ShopId" ON "ShopUsers" ("UserId", "ShopId");


CREATE INDEX "IX_StockAdjustments_AdjustedAt" ON "StockAdjustments" ("AdjustedAt");


CREATE INDEX "IX_StockAdjustments_AdjustmentType" ON "StockAdjustments" ("AdjustmentType");


CREATE INDEX "IX_StockAdjustments_DrugId" ON "StockAdjustments" ("DrugId");


CREATE INDEX "IX_StockAdjustments_ShopId" ON "StockAdjustments" ("ShopId");


CREATE INDEX "IX_StockAdjustments_ShopId_AdjustedAt" ON "StockAdjustments" ("ShopId", "AdjustedAt");


CREATE INDEX "IX_StockCounts_ShopId" ON "StockCounts" ("ShopId");


CREATE INDEX "IX_StockCounts_ShopId_Status" ON "StockCounts" ("ShopId", "Status");


CREATE INDEX "IX_StockCounts_Status" ON "StockCounts" ("Status");


CREATE INDEX "IX_StockTransfers_FromShopId" ON "StockTransfers" ("FromShopId");


CREATE INDEX "IX_StockTransfers_FromShopId_Status" ON "StockTransfers" ("FromShopId", "Status");


CREATE INDEX "IX_StockTransfers_InitiatedAt" ON "StockTransfers" ("InitiatedAt");


CREATE INDEX "IX_StockTransfers_Status" ON "StockTransfers" ("Status");


CREATE INDEX "IX_StockTransfers_ToShopId" ON "StockTransfers" ("ToShopId");


CREATE INDEX "IX_StockTransfers_ToShopId_Status" ON "StockTransfers" ("ToShopId", "Status");


CREATE INDEX "IX_Suppliers_Email" ON "Suppliers" ("Email");


CREATE INDEX "IX_Suppliers_IsActive" ON "Suppliers" ("IsActive");


CREATE INDEX "IX_Suppliers_SupplierName" ON "Suppliers" ("SupplierName");


CREATE INDEX "IX_Suppliers_SupplierType" ON "Suppliers" ("SupplierType");


CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");


CREATE INDEX "IX_Users_IsActive" ON "Users" ("IsActive");


CREATE INDEX "IX_Users_LastLoginAt" ON "Users" ("LastLoginAt");


CREATE INDEX "IX_Users_SystemRole" ON "Users" ("SystemRole");


CREATE UNIQUE INDEX "IX_Users_Username" ON "Users" ("Username");


