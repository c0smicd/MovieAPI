using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieAPI.Migrations
{
    /// <inheritdoc />
    public partial class SeatingPlanManyAuditoriums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeatingPlans_Auditoriums_AuditoriumId",
                table: "SeatingPlans");

            migrationBuilder.DropIndex(
                name: "IX_SeatingPlans_AuditoriumId",
                table: "SeatingPlans");

            migrationBuilder.DropColumn(
                name: "AuditoriumId",
                table: "SeatingPlans");

            migrationBuilder.AddColumn<int>(
                name: "SeatingPlanId",
                table: "Auditoriums",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Auditoriums_SeatingPlanId",
                table: "Auditoriums",
                column: "SeatingPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Auditoriums_SeatingPlans_SeatingPlanId",
                table: "Auditoriums",
                column: "SeatingPlanId",
                principalTable: "SeatingPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Auditoriums_SeatingPlans_SeatingPlanId",
                table: "Auditoriums");

            migrationBuilder.DropIndex(
                name: "IX_Auditoriums_SeatingPlanId",
                table: "Auditoriums");

            migrationBuilder.DropColumn(
                name: "SeatingPlanId",
                table: "Auditoriums");

            migrationBuilder.AddColumn<int>(
                name: "AuditoriumId",
                table: "SeatingPlans",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SeatingPlans_AuditoriumId",
                table: "SeatingPlans",
                column: "AuditoriumId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SeatingPlans_Auditoriums_AuditoriumId",
                table: "SeatingPlans",
                column: "AuditoriumId",
                principalTable: "Auditoriums",
                principalColumn: "Id");
        }
    }
}
