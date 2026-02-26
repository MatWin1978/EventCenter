using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventCenter.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase03RegistrationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CancellationDateUtc",
                table: "Registrations",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCancelled",
                table: "Registrations",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Events",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RegistrationAgendaItems",
                columns: table => new
                {
                    RegistrationId = table.Column<int>(type: "int", nullable: false),
                    AgendaItemId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationAgendaItems", x => new { x.RegistrationId, x.AgendaItemId });
                    table.ForeignKey(
                        name: "FK_RegistrationAgendaItems_EventAgendaItems_AgendaItemId",
                        column: x => x.AgendaItemId,
                        principalTable: "EventAgendaItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RegistrationAgendaItems_Registrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "Registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationAgendaItems_AgendaItemId",
                table: "RegistrationAgendaItems",
                column: "AgendaItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrationAgendaItems");

            migrationBuilder.DropColumn(
                name: "CancellationDateUtc",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "IsCancelled",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Events");
        }
    }
}
