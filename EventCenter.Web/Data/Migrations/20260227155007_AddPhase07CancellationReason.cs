using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventCenter.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase07CancellationReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Registrations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentRegistrationId",
                table: "Registrations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RelationshipType",
                table: "Registrations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Salutation",
                table: "Registrations",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "BookingDateUtc",
                table: "EventCompanies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancellationComment",
                table: "EventCompanies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsNonParticipation",
                table: "EventCompanies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_ParentRegistrationId",
                table: "Registrations",
                column: "ParentRegistrationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_Registrations_ParentRegistrationId",
                table: "Registrations",
                column: "ParentRegistrationId",
                principalTable: "Registrations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_Registrations_ParentRegistrationId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_ParentRegistrationId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "ParentRegistrationId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "RelationshipType",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "Salutation",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "BookingDateUtc",
                table: "EventCompanies");

            migrationBuilder.DropColumn(
                name: "CancellationComment",
                table: "EventCompanies");

            migrationBuilder.DropColumn(
                name: "IsNonParticipation",
                table: "EventCompanies");
        }
    }
}
