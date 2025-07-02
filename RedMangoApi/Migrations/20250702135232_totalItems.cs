using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RedMangoApi.Migrations
{
    /// <inheritdoc />
    public partial class totalItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalUtems",
                table: "OrderHeaders",
                newName: "TotalItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TotalItems",
                table: "OrderHeaders",
                newName: "TotalUtems");
        }
    }
}
