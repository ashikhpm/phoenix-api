using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace phoenix_sangam_api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserRoleNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update role names from "Admin" to "Secretary" and "User" to "Member"
            migrationBuilder.Sql("UPDATE \"UserRoles\" SET \"Name\" = 'Secretary', \"Description\" = 'Secretary with full access' WHERE \"Name\" = 'Admin'");
            migrationBuilder.Sql("UPDATE \"UserRoles\" SET \"Name\" = 'Member', \"Description\" = 'Regular member with limited access' WHERE \"Name\" = 'User'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert role names back to original
            migrationBuilder.Sql("UPDATE \"UserRoles\" SET \"Name\" = 'Admin', \"Description\" = 'Administrator with full access' WHERE \"Name\" = 'Secretary'");
            migrationBuilder.Sql("UPDATE \"UserRoles\" SET \"Name\" = 'User', \"Description\" = 'Regular user with limited access' WHERE \"Name\" = 'Member'");
        }
    }
}
