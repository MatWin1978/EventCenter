using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventCenter.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RegistrationDeadlineUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    MaxCompanions = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.CheckConstraint("CK_Event_RegistrationDeadlineBeforeStart", "[RegistrationDeadlineUtc] <= [StartDateUtc]");
                });

            migrationBuilder.CreateTable(
                name: "EventAgendaItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    StartDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDateTimeUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CostForMakler = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostForGuest = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAgendaItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventAgendaItems_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventCompanies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PricePerPerson = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MaxParticipants = table.Column<int>(type: "int", nullable: true),
                    InvitationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    InvitationSentUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCompanies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventCompanies_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxQuantity = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventOptions_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Registrations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    EventCompanyId = table.Column<int>(type: "int", nullable: true),
                    RegistrationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Company = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RegistrationDateUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    NumberOfCompanions = table.Column<int>(type: "int", nullable: false),
                    SpecialRequirements = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Registrations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Registrations_EventCompanies_EventCompanyId",
                        column: x => x.EventCompanyId,
                        principalTable: "EventCompanies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Registrations_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationEventOption",
                columns: table => new
                {
                    EventOptionId = table.Column<int>(type: "int", nullable: false),
                    RegistrationId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationEventOption", x => new { x.EventOptionId, x.RegistrationId });
                    table.ForeignKey(
                        name: "FK_RegistrationEventOption_EventOptions_EventOptionId",
                        column: x => x.EventOptionId,
                        principalTable: "EventOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrationEventOption_Registrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "Registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventAgendaItems_EventId",
                table: "EventAgendaItems",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventCompanies_EventId",
                table: "EventCompanies",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventCompanies_InvitationCode",
                table: "EventCompanies",
                column: "InvitationCode");

            migrationBuilder.CreateIndex(
                name: "IX_EventOptions_EventId",
                table: "EventOptions",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_IsPublished",
                table: "Events",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationEventOption_RegistrationId",
                table: "RegistrationEventOption",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventCompanyId",
                table: "Registrations",
                column: "EventCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId",
                table: "Registrations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_EventId_Email",
                table: "Registrations",
                columns: new[] { "EventId", "Email" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventAgendaItems");

            migrationBuilder.DropTable(
                name: "RegistrationEventOption");

            migrationBuilder.DropTable(
                name: "EventOptions");

            migrationBuilder.DropTable(
                name: "Registrations");

            migrationBuilder.DropTable(
                name: "EventCompanies");

            migrationBuilder.DropTable(
                name: "Events");
        }
    }
}
