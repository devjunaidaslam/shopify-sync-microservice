using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PartFinderMicroServices_DataAccessLayer.Migrations
{
    /// <inheritdoc />
    public partial class TransactionHistoryadded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            

            migrationBuilder.CreateTable(
                name: "ShopifyTransactionHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VariantId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CollectionId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InventoryItemId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LocationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TransactionType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EventType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Microservice = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: true),
                    Response = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ReceivedBy = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentToShopifyAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResponseReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    WebhookId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: true),
                    HttpMethod = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Endpoint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopifyTransactionHistory", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopifyTransactionHistory");

        
        }
    }
}
