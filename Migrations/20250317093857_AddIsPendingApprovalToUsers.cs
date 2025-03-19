using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace AgroMarket.Backend.Migrations
{
    public partial class AddIsPendingApprovalToUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Удаляем добавление IsPendingApproval, так как колонка уже есть в InitialCreate
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ничего не делаем
        }
    }
}