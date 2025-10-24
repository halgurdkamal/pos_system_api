using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace pos_system_api.src.Infrastructure.Data.Migrations
{
    public partial class AddShopReceiptAndHardwareConfig : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "AcceptedInsuranceProviders",
                table: "Shops",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowsControlledSubstances",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "BrandColorPrimary",
                table: "Shops",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BrandColorSecondary",
                table: "Shops",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Hardware_AutoSubmitOnScan",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Hardware_BarcodeLabelSize",
                table: "Shops",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_BarcodePrinterConnection",
                table: "Shops",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_BarcodePrinterIp",
                table: "Shops",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_BarcodePrinterName",
                table: "Shops",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_BarcodeScannerConnection",
                table: "Shops",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_BarcodeScannerModel",
                table: "Shops",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Hardware_CashDrawerEnabled",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_CashDrawerModel",
                table: "Shops",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_CashDrawerOpenCommand",
                table: "Shops",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Hardware_CustomerDisplayEnabled",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_CustomerDisplayType",
                table: "Shops",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Hardware_IntegratedPayments",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_PaymentTerminalConnection",
                table: "Shops",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_PaymentTerminalIp",
                table: "Shops",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_PaymentTerminalModel",
                table: "Shops",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_PosTerminalId",
                table: "Shops",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_PosTerminalName",
                table: "Shops",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_ReceiptPrinterConnection",
                table: "Shops",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_ReceiptPrinterIp",
                table: "Shops",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hardware_ReceiptPrinterName",
                table: "Shops",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Hardware_ReceiptPrinterPort",
                table: "Shops",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Shops",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PharmacyRegistrationNumber",
                table: "Shops",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Receipt_ControlledSubstanceWarning",
                table: "Shops",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Receipt_FooterText",
                table: "Shops",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Receipt_HeaderText",
                table: "Shops",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Receipt_Language",
                table: "Shops",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Receipt_PharmacistName",
                table: "Shops",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Receipt_PharmacyWarning",
                table: "Shops",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Receipt_PrintDuplicate",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Receipt_ReturnPolicy",
                table: "Shops",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Receipt_ShopName",
                table: "Shops",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "Receipt_ShowBarcode",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Receipt_ShowLogo",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Receipt_ShowPharmacyLicense",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Receipt_ShowQrCode",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Receipt_ShowTaxBreakdown",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Receipt_ShowVatNumber",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Receipt_Width",
                table: "Shops",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresPrescriptionVerification",
                table: "Shops",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "ShopImageUrls",
                table: "Shops",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "VatRegistrationNumber",
                table: "Shops",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptedInsuranceProviders",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "AllowsControlledSubstances",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "BrandColorPrimary",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "BrandColorSecondary",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_AutoSubmitOnScan",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_BarcodeLabelSize",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_BarcodePrinterConnection",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_BarcodePrinterIp",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_BarcodePrinterName",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_BarcodeScannerConnection",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_BarcodeScannerModel",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_CashDrawerEnabled",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_CashDrawerModel",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_CashDrawerOpenCommand",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_CustomerDisplayEnabled",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_CustomerDisplayType",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_IntegratedPayments",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_PaymentTerminalConnection",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_PaymentTerminalIp",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_PaymentTerminalModel",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_PosTerminalId",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_PosTerminalName",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_ReceiptPrinterConnection",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_ReceiptPrinterIp",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_ReceiptPrinterName",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Hardware_ReceiptPrinterPort",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "PharmacyRegistrationNumber",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_ControlledSubstanceWarning",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_FooterText",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_HeaderText",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_Language",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_PharmacistName",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_PharmacyWarning",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_PrintDuplicate",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_ReturnPolicy",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_ShopName",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_ShowBarcode",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_ShowLogo",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_ShowPharmacyLicense",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_ShowQrCode",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_ShowTaxBreakdown",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_ShowVatNumber",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "Receipt_Width",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "RequiresPrescriptionVerification",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "ShopImageUrls",
                table: "Shops");

            migrationBuilder.DropColumn(
                name: "VatRegistrationNumber",
                table: "Shops");
        }
    }
}
