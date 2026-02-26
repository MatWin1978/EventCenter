using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EventCenter.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPhase02Fields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Events",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "Events",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Events",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocumentPaths",
                table: "Events",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "GuestsCanParticipate",
                table: "EventAgendaItems",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "MaklerCanParticipate",
                table: "EventAgendaItems",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "DocumentPaths",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "GuestsCanParticipate",
                table: "EventAgendaItems");

            migrationBuilder.DropColumn(
                name: "MaklerCanParticipate",
                table: "EventAgendaItems");
        }
    }
}
