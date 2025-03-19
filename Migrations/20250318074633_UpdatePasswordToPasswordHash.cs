using Microsoft.EntityFrameworkCore.Migrations;

namespace AgroMarket.Backend.Migrations
{
    public partial class UpdatePasswordToPasswordHash : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Удаляем переименование Password -> PasswordHash, так как колонка уже называется PasswordHash
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Ничего не делаем в откате
        }
    }
}