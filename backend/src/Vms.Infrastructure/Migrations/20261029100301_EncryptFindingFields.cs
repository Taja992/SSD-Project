using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vms.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EncryptFindingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReporterEmail",
                table: "Findings",
                type: "character varying(1344)",
                maxLength: 1344,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Findings",
                type: "character varying(40064)",
                maxLength: 40064,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10000)",
                oldMaxLength: 10000);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ReporterEmail",
                table: "Findings",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1344)",
                oldMaxLength: 1344,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Findings",
                type: "character varying(10000)",
                maxLength: 10000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(40064)",
                oldMaxLength: 40064);
        }
    }
}
