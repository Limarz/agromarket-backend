using Microsoft.EntityFrameworkCore.Migrations;

namespace AgroMarket.Backend.Migrations
{
    public partial class AddActionColumnToUserActivities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ничего не делаем, так как Action уже создан в InitialCreate
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ничего не делаем
        }
    }
}