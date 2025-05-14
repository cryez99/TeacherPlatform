using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherPlatform.Migrations
{
    /// <inheritdoc />
    public partial class t : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lessons_users_UserId",
                table: "lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_study_plans_users_tutor_id",
                table: "study_plans");

            migrationBuilder.DropIndex(
                name: "IX_study_plans_tutor_id",
                table: "study_plans");

            migrationBuilder.DropIndex(
                name: "IX_lessons_UserId",
                table: "lessons");

            migrationBuilder.DropColumn(
                name: "tutor_id",
                table: "study_plans");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "lessons");

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "students",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "students",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "additional_info",
                table: "students",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "tutor_id",
                table: "study_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "phone",
                table: "students",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "email",
                table: "students",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "additional_info",
                table: "students",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "lessons",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_study_plans_tutor_id",
                table: "study_plans",
                column: "tutor_id");

            migrationBuilder.CreateIndex(
                name: "IX_lessons_UserId",
                table: "lessons",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_lessons_users_UserId",
                table: "lessons",
                column: "UserId",
                principalTable: "users",
                principalColumn: "user_id");

            migrationBuilder.AddForeignKey(
                name: "FK_study_plans_users_tutor_id",
                table: "study_plans",
                column: "tutor_id",
                principalTable: "users",
                principalColumn: "user_id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
