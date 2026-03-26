using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AnonymousEnrollmentFormBuilder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "StudentId",
                table: "Enrollments");

            migrationBuilder.AddColumn<string>(
                name: "StudentEmail",
                table: "Enrollments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StudentName",
                table: "Enrollments",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "EnrollmentForms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    LessonSeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnrollmentForms_LessonSeries_LessonSeriesId",
                        column: x => x.LessonSeriesId,
                        principalTable: "LessonSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Options = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormFields_EnrollmentForms_EnrollmentFormId",
                        column: x => x.EnrollmentFormId,
                        principalTable: "EnrollmentForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FormResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FormFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormResponses_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormResponses_FormFields_FormFieldId",
                        column: x => x.FormFieldId,
                        principalTable: "FormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentEmail",
                table: "Enrollments",
                column: "StudentEmail");

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentForms_LessonSeriesId",
                table: "EnrollmentForms",
                column: "LessonSeriesId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EnrollmentForms_OrganizationId",
                table: "EnrollmentForms",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_FormFields_EnrollmentFormId",
                table: "FormFields",
                column: "EnrollmentFormId");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponses_EnrollmentId",
                table: "FormResponses",
                column: "EnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FormResponses_FormFieldId",
                table: "FormResponses",
                column: "FormFieldId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FormResponses");

            migrationBuilder.DropTable(
                name: "FormFields");

            migrationBuilder.DropTable(
                name: "EnrollmentForms");

            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentEmail",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "StudentEmail",
                table: "Enrollments");

            migrationBuilder.DropColumn(
                name: "StudentName",
                table: "Enrollments");

            migrationBuilder.AddColumn<Guid>(
                name: "StudentId",
                table: "Enrollments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId",
                table: "Enrollments",
                column: "StudentId");
        }
    }
}
