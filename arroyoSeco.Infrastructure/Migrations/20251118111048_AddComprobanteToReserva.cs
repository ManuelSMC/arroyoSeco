using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace arroyoSeco.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComprobanteToReserva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ComprobanteUrl",
                table: "Reservas",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ComprobanteUrl",
                table: "Reservas");
        }
    }
}
