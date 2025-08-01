using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace phoenix_sangam_api.Migrations
{
    /// <inheritdoc />
    public partial class AddLoanTermColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LoanTerm",
                table: "Loans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LoanTerm",
                table: "LoanRequests",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LoanTerm",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "LoanTerm",
                table: "LoanRequests");
        }
    }
}
