using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Mercato.Infrastructure.Data;

public static class DatabaseInitializer
{
    private const int MaxAttempts = 5;

    public static async Task InitializeAsync(MercatoDbContext context, CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var migrations = context.Database.GetMigrations();
                if (migrations.Any()) context.Database.Migrate(); else context.Database.EnsureCreated();

                context.Database.ExecuteSqlRaw("""
                    CREATE TABLE IF NOT EXISTS "UserBranchAssignments" (
                        "UserId" uuid NOT NULL,
                        "BranchId" uuid NOT NULL,
                        CONSTRAINT "PK_UserBranchAssignments" PRIMARY KEY ("UserId", "BranchId"),
                        CONSTRAINT "FK_UserBranchAssignments_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE CASCADE,
                        CONSTRAINT "FK_UserBranchAssignments_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS "IX_UserBranchAssignments_BranchId" ON "UserBranchAssignments" ("BranchId");

                    ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "Username" character varying(120) NULL;
                    ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "MobileNumber" character varying(40) NULL;
                    ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "CanAccessBackOffice" boolean NOT NULL DEFAULT FALSE;
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Username" ON "Users" ("Username") WHERE "Username" IS NOT NULL;
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_MobileNumber" ON "Users" ("MobileNumber") WHERE "MobileNumber" IS NOT NULL;
                    UPDATE "Users" SET "CanAccessBackOffice" = TRUE WHERE "Role" = 'Admin' AND "CanAccessBackOffice" = FALSE;

                    ALTER TABLE "Products" ADD COLUMN IF NOT EXISTS "ImageUrl" text NULL;
                    ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "SubtotalAmount" numeric(18,2) NOT NULL DEFAULT 0;
                    ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "DiscountId" uuid NULL;
                    ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "DiscountName" character varying(100) NULL;
                    ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "DiscountAmount" numeric(18,2) NOT NULL DEFAULT 0;
                    UPDATE "Orders" SET "SubtotalAmount" = "TotalAmount" WHERE "SubtotalAmount" = 0 AND "TotalAmount" <> 0;

                    ALTER TABLE "Invoices" ADD COLUMN IF NOT EXISTS "SubtotalAmount" numeric(18,2) NOT NULL DEFAULT 0;
                    ALTER TABLE "Invoices" ADD COLUMN IF NOT EXISTS "DiscountName" character varying(100) NULL;
                    ALTER TABLE "Invoices" ADD COLUMN IF NOT EXISTS "DiscountAmount" numeric(18,2) NOT NULL DEFAULT 0;
                    UPDATE "Invoices" SET "SubtotalAmount" = "TotalAmount" WHERE "SubtotalAmount" = 0 AND "TotalAmount" <> 0;

                    CREATE TABLE IF NOT EXISTS "InvoiceItems" (
                        "Id" uuid NOT NULL,
                        "InvoiceId" uuid NOT NULL,
                        "ProductId" uuid NOT NULL,
                        "Quantity" numeric(18,4) NOT NULL,
                        "UnitPrice" numeric(18,2) NOT NULL,
                        CONSTRAINT "PK_InvoiceItems" PRIMARY KEY ("Id"),
                        CONSTRAINT "FK_InvoiceItems_Invoices_InvoiceId" FOREIGN KEY ("InvoiceId") REFERENCES "Invoices" ("Id") ON DELETE CASCADE
                    );
                    CREATE INDEX IF NOT EXISTS "IX_InvoiceItems_InvoiceId" ON "InvoiceItems" ("InvoiceId");

                    CREATE TABLE IF NOT EXISTS "PaymentMethods" (
                        "Id" uuid NOT NULL,
                        "Name" character varying(100) NOT NULL,
                        "IsActive" boolean NOT NULL DEFAULT TRUE,
                        "SortOrder" integer NOT NULL DEFAULT 0,
                        CONSTRAINT "PK_PaymentMethods" PRIMARY KEY ("Id")
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_PaymentMethods_Name" ON "PaymentMethods" ("Name");

                    CREATE TABLE IF NOT EXISTS "DiscountDefinitions" (
                        "Id" uuid NOT NULL,
                        "Name" character varying(100) NOT NULL,
                        "Type" character varying(20) NOT NULL,
                        "Value" numeric(18,2) NOT NULL,
                        "IsActive" boolean NOT NULL DEFAULT TRUE,
                        "SortOrder" integer NOT NULL DEFAULT 0,
                        CONSTRAINT "PK_DiscountDefinitions" PRIMARY KEY ("Id")
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS "IX_DiscountDefinitions_Name" ON "DiscountDefinitions" ("Name");

                    CREATE TABLE IF NOT EXISTS "ApplicationSettings" (
                        "Key" character varying(120) NOT NULL,
                        "Value" character varying(2000) NOT NULL,
                        CONSTRAINT "PK_ApplicationSettings" PRIMARY KEY ("Key")
                    );

                    CREATE TABLE IF NOT EXISTS "GoodsReceipts" (
                        "Id" uuid NOT NULL,
                        "ArtistId" uuid NOT NULL,
                        "BranchId" uuid NOT NULL,
                        "Reference" character varying(120) NULL,
                        "CreatedAtUtc" timestamp with time zone NOT NULL,
                        CONSTRAINT "PK_GoodsReceipts" PRIMARY KEY ("Id"),
                        CONSTRAINT "FK_GoodsReceipts_Artists_ArtistId" FOREIGN KEY ("ArtistId") REFERENCES "Artists" ("Id") ON DELETE RESTRICT,
                        CONSTRAINT "FK_GoodsReceipts_Branches_BranchId" FOREIGN KEY ("BranchId") REFERENCES "Branches" ("Id") ON DELETE RESTRICT
                    );
                    CREATE INDEX IF NOT EXISTS "IX_GoodsReceipts_ArtistId" ON "GoodsReceipts" ("ArtistId");
                    CREATE INDEX IF NOT EXISTS "IX_GoodsReceipts_BranchId" ON "GoodsReceipts" ("BranchId");

                    CREATE TABLE IF NOT EXISTS "GoodsReceiptLines" (
                        "Id" uuid NOT NULL,
                        "GoodsReceiptId" uuid NOT NULL,
                        "ProductId" uuid NOT NULL,
                        "Quantity" integer NOT NULL,
                        "PurchaseUnitPrice" numeric(18,2) NOT NULL,
                        CONSTRAINT "PK_GoodsReceiptLines" PRIMARY KEY ("Id"),
                        CONSTRAINT "FK_GoodsReceiptLines_GoodsReceipts_GoodsReceiptId" FOREIGN KEY ("GoodsReceiptId") REFERENCES "GoodsReceipts" ("Id") ON DELETE CASCADE,
                        CONSTRAINT "FK_GoodsReceiptLines_Products_ProductId" FOREIGN KEY ("ProductId") REFERENCES "Products" ("Id") ON DELETE RESTRICT
                    );
                    CREATE INDEX IF NOT EXISTS "IX_GoodsReceiptLines_GoodsReceiptId" ON "GoodsReceiptLines" ("GoodsReceiptId");
                    CREATE INDEX IF NOT EXISTS "IX_GoodsReceiptLines_ProductId" ON "GoodsReceiptLines" ("ProductId");

                    INSERT INTO "PaymentMethods" ("Id","Name","IsActive","SortOrder")
                    SELECT '30000000-0000-0000-0000-000000000001'::uuid, 'Cash', TRUE, 10
                    WHERE NOT EXISTS (SELECT 1 FROM "PaymentMethods");
                    INSERT INTO "PaymentMethods" ("Id","Name","IsActive","SortOrder")
                    SELECT '30000000-0000-0000-0000-000000000002'::uuid, 'Card', TRUE, 20
                    WHERE NOT EXISTS (SELECT 1 FROM "PaymentMethods" WHERE "Name" = 'Card');

                    INSERT INTO "ApplicationSettings" ("Key","Value") VALUES ('System.Language','en') ON CONFLICT ("Key") DO NOTHING;
                    INSERT INTO "ApplicationSettings" ("Key","Value") VALUES ('Pos.ShowProductImages','false') ON CONFLICT ("Key") DO NOTHING;
                    INSERT INTO "ApplicationSettings" ("Key","Value") VALUES ('Auth.UseUsername','false') ON CONFLICT ("Key") DO NOTHING;
                    """);

                return;
            }
            catch (Exception exception) when (attempt < MaxAttempts && IsTransient(exception) && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }
    }

    private static bool IsTransient(Exception exception)
    {
        if (exception is TimeoutException) return true;
        if (exception is NpgsqlException npgsqlException && npgsqlException.IsTransient) return true;
        return exception.InnerException is not null && IsTransient(exception.InnerException);
    }
}
