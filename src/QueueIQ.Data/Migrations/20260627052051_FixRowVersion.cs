using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueueIQ.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Tickets",
                type: "BLOB",
                nullable: false,
                oldClrType: typeof(byte[]),
                oldType: "BLOB",
                oldRowVersion: true,
                oldDefaultValueSql: "CURRENT_TIMESTAMP");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Tickets",
                type: "BLOB",
                rowVersion: true,
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP",
                oldClrType: typeof(byte[]),
                oldType: "BLOB");
        }
    }
}
