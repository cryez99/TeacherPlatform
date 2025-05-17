using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherPlatform.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lessons_study_plans_StudyPlanId",
                table: "lessons");

            migrationBuilder.DropForeignKey(
                name: "FK_students_study_plans_study_plan_id",
                table: "students");

            migrationBuilder.DropIndex(
                name: "IX_students_study_plan_id",
                table: "students");

            migrationBuilder.DropColumn(
                name: "study_plan_id",
                table: "students");

            migrationBuilder.RenameColumn(
                name: "StudyPlanId",
                table: "lessons",
                newName: "study_plan_id");

            migrationBuilder.RenameIndex(
                name: "IX_lessons_StudyPlanId",
                table: "lessons",
                newName: "IX_lessons_study_plan_id");

            migrationBuilder.AddForeignKey(
                name: "FK_lessons_study_plans_study_plan_id",
                table: "lessons",
                column: "study_plan_id",
                principalTable: "study_plans",
                principalColumn: "study_plan_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lessons_study_plans_study_plan_id",
                table: "lessons");

            migrationBuilder.RenameColumn(
                name: "study_plan_id",
                table: "lessons",
                newName: "StudyPlanId");

            migrationBuilder.RenameIndex(
                name: "IX_lessons_study_plan_id",
                table: "lessons",
                newName: "IX_lessons_StudyPlanId");

            migrationBuilder.AddColumn<int>(
                name: "study_plan_id",
                table: "students",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_students_study_plan_id",
                table: "students",
                column: "study_plan_id");

            migrationBuilder.AddForeignKey(
                name: "FK_lessons_study_plans_StudyPlanId",
                table: "lessons",
                column: "StudyPlanId",
                principalTable: "study_plans",
                principalColumn: "study_plan_id");

            migrationBuilder.AddForeignKey(
                name: "FK_students_study_plans_study_plan_id",
                table: "students",
                column: "study_plan_id",
                principalTable: "study_plans",
                principalColumn: "study_plan_id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
