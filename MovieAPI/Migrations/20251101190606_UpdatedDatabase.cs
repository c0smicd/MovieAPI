using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MovieAPI.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeatingPlans_Auditoriums_AuditoriumId",
                table: "SeatingPlans");

            migrationBuilder.AlterColumn<int>(
                name: "AuditoriumId",
                table: "SeatingPlans",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_SeatingPlans_Auditoriums_AuditoriumId",
                table: "SeatingPlans",
                column: "AuditoriumId",
                principalTable: "Auditoriums",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SeatingPlans_Auditoriums_AuditoriumId",
                table: "SeatingPlans");

            migrationBuilder.AlterColumn<int>(
                name: "AuditoriumId",
                table: "SeatingPlans",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SeatingPlans_Auditoriums_AuditoriumId",
                table: "SeatingPlans",
                column: "AuditoriumId",
                principalTable: "Auditoriums",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
