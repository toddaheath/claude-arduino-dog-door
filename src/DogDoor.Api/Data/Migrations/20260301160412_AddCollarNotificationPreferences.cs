using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogDoor.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCollarNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CollarBatteryLow",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CollarDisconnected",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "GeofenceBreach",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "GeofenceEnteredExited",
                table: "NotificationPreferences",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollarBatteryLow",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "CollarDisconnected",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "GeofenceBreach",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "GeofenceEnteredExited",
                table: "NotificationPreferences");
        }
    }
}
