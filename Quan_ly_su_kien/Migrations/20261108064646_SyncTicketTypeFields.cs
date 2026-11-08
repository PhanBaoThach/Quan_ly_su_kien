using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Quan_ly_su_kien.Migrations
{
    /// <inheritdoc />
    public partial class SyncTicketTypeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SoldQuantity",
                table: "TicketTypes",
                newName: "QuantityTotal");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "TicketTypes",
                newName: "QuantityAvailable");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TicketTypes",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "TicketTypes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "TicketTypes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "TicketTypes");

            migrationBuilder.RenameColumn(
                name: "QuantityTotal",
                table: "TicketTypes",
                newName: "SoldQuantity");

            migrationBuilder.RenameColumn(
                name: "QuantityAvailable",
                table: "TicketTypes",
                newName: "Quantity");
        }
    }
}
