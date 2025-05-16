using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TeacherPlatform.Migrations
{
    /// <inheritdoc />
    public partial class g : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddForeignKey(
                name: "FK_lessons_study_plans_StudyPlanId",
                table: "lessons",
                column: "StudyPlanId",
                principalTable: "study_plans",
                principalColumn: "study_plan_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_lessons_study_plans_StudyPlanId",
                table: "lessons");

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
    }
}
