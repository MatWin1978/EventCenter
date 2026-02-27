using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventCenter.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase04CompanyInvitationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventCompanies_InvitationCode",
                table: "EventCompanies");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                table: "EventCompanies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PercentageDiscount",
                table: "EventCompanies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalMessage",
                table: "EventCompanies",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "EventCompanies",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "EventCompanyAgendaItemPrices",
                columns: table => new
                {
                    EventCompanyId = table.Column<int>(type: "int", nullable: false),
                    AgendaItemId = table.Column<int>(type: "int", nullable: false),
                    CustomPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCompanyAgendaItemPrices", x => new { x.EventCompanyId, x.AgendaItemId });
                    table.ForeignKey(
                        name: "FK_EventCompanyAgendaItemPrices_EventAgendaItems_AgendaItemId",
                        column: x => x.AgendaItemId,
                        principalTable: "EventAgendaItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EventCompanyAgendaItemPrices_EventCompanies_EventCompanyId",
                        column: x => x.EventCompanyId,
                        principalTable: "EventCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventCompanies_InvitationCode",
                table: "EventCompanies",
                column: "InvitationCode",
                unique: true,
                filter: "[InvitationCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventCompanyAgendaItemPrices_AgendaItemId",
                table: "EventCompanyAgendaItemPrices",
                column: "AgendaItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventCompanyAgendaItemPrices");

            migrationBuilder.DropIndex(
                name: "IX_EventCompanies_InvitationCode",
                table: "EventCompanies");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "EventCompanies");

            migrationBuilder.DropColumn(
                name: "PercentageDiscount",
                table: "EventCompanies");

            migrationBuilder.DropColumn(
                name: "PersonalMessage",
                table: "EventCompanies");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "EventCompanies");

            migrationBuilder.CreateIndex(
                name: "IX_EventCompanies_InvitationCode",
                table: "EventCompanies",
                column: "InvitationCode");
        }
    }
}
