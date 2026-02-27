using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DogDoor.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenPrefixAndHashInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TokenPrefix column to PasswordResetTokens for O(1) lookup
            migrationBuilder.AddColumn<string>(
                name: "TokenPrefix",
                table: "PasswordResetTokens",
                type: "character varying(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TokenPrefix",
                table: "PasswordResetTokens",
                column: "TokenPrefix");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PasswordResetTokens_TokenPrefix",
                table: "PasswordResetTokens");

            migrationBuilder.DropColumn(
                name: "TokenPrefix",
                table: "PasswordResetTokens");
        }
    }
}
