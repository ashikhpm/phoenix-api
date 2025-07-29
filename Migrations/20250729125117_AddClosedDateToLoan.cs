using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace phoenix_sangam_api.Migrations
{
    /// <inheritdoc />
    public partial class AddClosedDateToLoan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedDate",
                table: "Loans",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClosedDate",
                table: "Loans");
        }
    }
}
