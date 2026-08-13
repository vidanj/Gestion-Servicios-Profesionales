using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaServicios.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestCompositeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_ProfessionalId",
                table: "Requests");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ProfessionalId_RequestDate",
                table: "Requests",
                columns: new[] { "ProfessionalId", "RequestDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Requests_ProfessionalId_RequestDate",
                table: "Requests");

            migrationBuilder.CreateIndex(
                name: "IX_Requests_ProfessionalId",
                table: "Requests",
                column: "ProfessionalId");
        }
    }
}
