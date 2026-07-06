using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PartFinderMicroServices_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class RemoveExtraFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollectionId",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "Endpoint",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "HttpMethod",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "HttpStatusCode",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "InventoryItemId",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "Microservice",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "Response",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "ResponseReceivedAt",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "SentToShopifyAt",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "VariantId",
                table: "ShopifyTransactionHistory");

            migrationBuilder.DropColumn(
                name: "WebhookId",
                table: "ShopifyTransactionHistory");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CollectionId",
                table: "ShopifyTransactionHistory",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "ShopifyTransactionHistory",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Endpoint",
                table: "ShopifyTransactionHistory",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HttpMethod",
                table: "ShopifyTransactionHistory",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HttpStatusCode",
                table: "ShopifyTransactionHistory",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InventoryItemId",
                table: "ShopifyTransactionHistory",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationId",
                table: "ShopifyTransactionHistory",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Microservice",
                table: "ShopifyTransactionHistory",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RequestId",
                table: "ShopifyTransactionHistory",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Response",
                table: "ShopifyTransactionHistory",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResponseReceivedAt",
                table: "ShopifyTransactionHistory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "ShopifyTransactionHistory",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentToShopifyAt",
                table: "ShopifyTransactionHistory",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ShopifyTransactionHistory",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantId",
                table: "ShopifyTransactionHistory",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebhookId",
                table: "ShopifyTransactionHistory",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
