using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TeacherPlatform.Migrations
{
    /// <inheritdoc />
    public partial class LastChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "study_plans",
                columns: table => new
                {
                    study_plan_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    lessons_per_week = table.Column<int>(type: "integer", nullable: false),
                    lesson_duration_minutes = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_study_plans", x => x.study_plan_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "topics",
                columns: table => new
                {
                    topic_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "varchar(100)", nullable: false),
                    study_plan_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_topics", x => x.topic_id);
                    table.ForeignKey(
                        name: "FK_topics_study_plans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalTable: "study_plans",
                        principalColumn: "study_plan_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "students",
                columns: table => new
                {
                    student_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    full_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true),
                    @class = table.Column<string>(name: "class", type: "varchar(20)", maxLength: 20, nullable: false),
                    additional_info = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    tutor_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_students", x => x.student_id);
                    table.ForeignKey(
                        name: "FK_students_users_tutor_id",
                        column: x => x.tutor_id,
                        principalTable: "users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sub_topics",
                columns: table => new
                {
                    sub_topic_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "text", nullable: false),
                    topic_id = table.Column<int>(type: "integer", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sub_topics", x => x.sub_topic_id);
                    table.ForeignKey(
                        name: "FK_sub_topics_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "topics",
                        principalColumn: "topic_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lessons",
                columns: table => new
                {
                    lesson_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    student_id = table.Column<int>(type: "integer", nullable: false),
                    study_plan_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lessons", x => x.lesson_id);
                    table.ForeignKey(
                        name: "FK_lessons_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_lessons_study_plans_study_plan_id",
                        column: x => x.study_plan_id,
                        principalTable: "study_plans",
                        principalColumn: "study_plan_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "progress",
                columns: table => new
                {
                    progress_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    student_id = table.Column<int>(type: "integer", nullable: true),
                    topic_id = table.Column<int>(type: "integer", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    completion_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    score = table.Column<int>(type: "integer", nullable: true),
                    notes = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_progress", x => x.progress_id);
                    table.ForeignKey(
                        name: "FK_progress_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "student_id");
                    table.ForeignKey(
                        name: "FK_progress_topics_topic_id",
                        column: x => x.topic_id,
                        principalTable: "topics",
                        principalColumn: "topic_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_lessons_start_time",
                table: "lessons",
                column: "start_time");

            migrationBuilder.CreateIndex(
                name: "IX_lessons_student_id",
                table: "lessons",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_lessons_study_plan_id",
                table: "lessons",
                column: "study_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_progress_student_id",
                table: "progress",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_progress_topic_id",
                table: "progress",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "IX_students_tutor_id",
                table: "students",
                column: "tutor_id");

            migrationBuilder.CreateIndex(
                name: "IX_sub_topics_topic_id",
                table: "sub_topics",
                column: "topic_id");

            migrationBuilder.CreateIndex(
                name: "IX_topics_study_plan_id",
                table: "topics",
                column: "study_plan_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "lessons");

            migrationBuilder.DropTable(
                name: "progress");

            migrationBuilder.DropTable(
                name: "sub_topics");

            migrationBuilder.DropTable(
                name: "students");

            migrationBuilder.DropTable(
                name: "topics");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "study_plans");
        }
    }
}
