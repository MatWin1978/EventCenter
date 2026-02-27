using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventCenter.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase08WebinarFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Event_RegistrationDeadlineBeforeStart",
                table: "Events");

            migrationBuilder.AddColumn<string>(
                name: "EventType",
                table: "Events",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "InPerson");

            migrationBuilder.AddColumn<string>(
                name: "ExternalRegistrationUrl",
                table: "Events",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EventType",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ExternalRegistrationUrl",
                table: "Events");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Event_RegistrationDeadlineBeforeStart",
                table: "Events",
                sql: "[RegistrationDeadlineUtc] <= [StartDateUtc]");
        }
    }
}
