using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DogDoor.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCollarDevicesAndGeofences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollarDevices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    AnimalId = table.Column<int>(type: "integer", nullable: true),
                    CollarId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SharedSecret = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FirmwareVersion = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    BatteryPercent = table.Column<float>(type: "real", nullable: true),
                    BatteryVoltage = table.Column<float>(type: "real", nullable: true),
                    LastSeenAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastLatitude = table.Column<double>(type: "double precision", nullable: true),
                    LastLongitude = table.Column<double>(type: "double precision", nullable: true),
                    LastAccuracy = table.Column<float>(type: "real", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollarDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollarDevices_Animals_AnimalId",
                        column: x => x.AnimalId,
                        principalTable: "Animals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CollarDevices_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Geofences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FenceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Rule = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    BoundaryJson = table.Column<string>(type: "text", nullable: false),
                    BuzzerPattern = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Geofences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Geofences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LocationPoints",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CollarDeviceId = table.Column<int>(type: "integer", nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Altitude = table.Column<float>(type: "real", nullable: true),
                    Accuracy = table.Column<float>(type: "real", nullable: true),
                    Speed = table.Column<float>(type: "real", nullable: true),
                    Heading = table.Column<float>(type: "real", nullable: true),
                    Satellites = table.Column<int>(type: "integer", nullable: true),
                    BatteryVoltage = table.Column<float>(type: "real", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocationPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LocationPoints_CollarDevices_CollarDeviceId",
                        column: x => x.CollarDeviceId,
                        principalTable: "CollarDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeofenceEvents",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GeofenceId = table.Column<int>(type: "integer", nullable: false),
                    CollarDeviceId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeofenceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeofenceEvents_CollarDevices_CollarDeviceId",
                        column: x => x.CollarDeviceId,
                        principalTable: "CollarDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GeofenceEvents_Geofences_GeofenceId",
                        column: x => x.GeofenceId,
                        principalTable: "Geofences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollarDevices_AnimalId",
                table: "CollarDevices",
                column: "AnimalId");

            migrationBuilder.CreateIndex(
                name: "IX_CollarDevices_CollarId",
                table: "CollarDevices",
                column: "CollarId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollarDevices_UserId",
                table: "CollarDevices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_CollarDeviceId",
                table: "GeofenceEvents",
                column: "CollarDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_GeofenceId",
                table: "GeofenceEvents",
                column: "GeofenceId");

            migrationBuilder.CreateIndex(
                name: "IX_GeofenceEvents_Timestamp",
                table: "GeofenceEvents",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Geofences_UserId",
                table: "Geofences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationPoints_CollarDeviceId",
                table: "LocationPoints",
                column: "CollarDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_LocationPoints_CollarDeviceId_Timestamp",
                table: "LocationPoints",
                columns: new[] { "CollarDeviceId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_LocationPoints_Timestamp",
                table: "LocationPoints",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeofenceEvents");

            migrationBuilder.DropTable(
                name: "LocationPoints");

            migrationBuilder.DropTable(
                name: "Geofences");

            migrationBuilder.DropTable(
                name: "CollarDevices");
        }
    }
}
