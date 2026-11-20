using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace Khotwa.Migrations;

public partial class LearningFeatures : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("DurationMinutes", "Lessons", type: "int", nullable: true);
        migrationBuilder.CreateTable("Favorites", table => new {
            StudentId = table.Column<int>(type: "int", nullable: false),
            CourseId = table.Column<int>(type: "int", nullable: false),
            CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table => {
            table.PrimaryKey("PK_Favorites", x => new { x.StudentId, x.CourseId });
            table.ForeignKey("FK_Favorites_Users_StudentId", x => x.StudentId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_Favorites_Courses_CourseId", x => x.CourseId, "Courses", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_Favorites_CourseId", "Favorites", "CourseId");
        migrationBuilder.CreateTable("LessonCompletions", table => new {
            EnrollmentId = table.Column<int>(type: "int", nullable: false),
            LessonId = table.Column<int>(type: "int", nullable: false),
            CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table => {
            table.PrimaryKey("PK_LessonCompletions", x => new { x.EnrollmentId, x.LessonId });
            table.ForeignKey("FK_LessonCompletions_Enrollments_EnrollmentId", x => x.EnrollmentId, "Enrollments", "Id", onDelete: ReferentialAction.Cascade);
            table.ForeignKey("FK_LessonCompletions_Lessons_LessonId", x => x.LessonId, "Lessons", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_LessonCompletions_LessonId", "LessonCompletions", "LessonId");
        migrationBuilder.CreateTable("CourseCertificates", table => new {
            EnrollmentId = table.Column<int>(type: "int", nullable: false),
            CertificateNumber = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
            StudentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            CourseTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
            InstructorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
            DurationMinutes = table.Column<int>(type: "int", nullable: true),
            IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
        }, constraints: table => {
            table.PrimaryKey("PK_CourseCertificates", x => x.EnrollmentId);
            table.ForeignKey("FK_CourseCertificates_Enrollments_EnrollmentId", x => x.EnrollmentId, "Enrollments", "Id", onDelete: ReferentialAction.Cascade);
        });
        migrationBuilder.CreateIndex("IX_CourseCertificates_CertificateNumber", "CourseCertificates", "CertificateNumber", unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("CourseCertificates");
        migrationBuilder.DropTable("LessonCompletions");
        migrationBuilder.DropTable("Favorites");
        migrationBuilder.DropColumn("DurationMinutes", "Lessons");
    }
}
