using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace project26.Migrations
{
    /// <inheritdoc />
    public partial class usertable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    uid = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    uname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    uemail = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    uphone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    upassword = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Image = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    urole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    verifycode = table.Column<int>(type: "int", nullable: true),
                    verifycodetime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    verifystatus = table.Column<bool>(type: "bit", nullable: true),
                    forgotpasscode = table.Column<int>(type: "int", nullable: true),
                    forgotpasstime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.uid);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
