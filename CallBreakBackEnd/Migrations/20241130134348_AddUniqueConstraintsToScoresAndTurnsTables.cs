using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallBreakBackEnd.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintsToScoresAndTurnsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Turns_RoundId",
                table: "Turns");

            migrationBuilder.DropIndex(
                name: "IX_Scores_GameId",
                table: "Scores");

            migrationBuilder.CreateIndex(
                name: "IX_Turns_RoundId_PlayerId",
                table: "Turns",
                columns: new[] { "RoundId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Scores_GameId_PlayerId",
                table: "Scores",
                columns: new[] { "GameId", "PlayerId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Turns_RoundId_PlayerId",
                table: "Turns");

            migrationBuilder.DropIndex(
                name: "IX_Scores_GameId_PlayerId",
                table: "Scores");

            migrationBuilder.CreateIndex(
                name: "IX_Turns_RoundId",
                table: "Turns",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_GameId",
                table: "Scores",
                column: "GameId");
        }
    }
}
