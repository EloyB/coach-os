using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoachOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCampsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "EnrollmentId",
                table: "Payments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "CampEnrollmentId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Camps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TennisClubId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RegistrationDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Camps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Camps_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Camps_TennisClubs_TennisClubId",
                        column: x => x.TennisClubId,
                        principalTable: "TennisClubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampDays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampDays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampDays_Camps_CampId",
                        column: x => x.CampId,
                        principalTable: "Camps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampEnrollmentForms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampEnrollmentForms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampEnrollmentForms_Camps_CampId",
                        column: x => x.CampId,
                        principalTable: "Camps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampDayTrainers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampDayId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrainerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampDayTrainers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampDayTrainers_CampDays_CampDayId",
                        column: x => x.CampDayId,
                        principalTable: "CampDays",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampFormFields",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampEnrollmentFormId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_CampFormFields", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampFormFields_CampEnrollmentForms_CampEnrollmentFormId",
                        column: x => x.CampEnrollmentFormId,
                        principalTable: "CampEnrollmentForms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampEnrollmentGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LeaderEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampEnrollmentGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampEnrollmentGroups_Camps_CampId",
                        column: x => x.CampId,
                        principalTable: "Camps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParticipantName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParticipantEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParticipantPhone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EnrolledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CampEnrollmentGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampEnrollments_CampEnrollmentGroups_CampEnrollmentGroupId",
                        column: x => x.CampEnrollmentGroupId,
                        principalTable: "CampEnrollmentGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampEnrollments_Camps_CampId",
                        column: x => x.CampId,
                        principalTable: "Camps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampFormResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampEnrollmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampFormFieldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampFormResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampFormResponses_CampEnrollments_CampEnrollmentId",
                        column: x => x.CampEnrollmentId,
                        principalTable: "CampEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampFormResponses_CampFormFields_CampFormFieldId",
                        column: x => x.CampFormFieldId,
                        principalTable: "CampFormFields",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_CampEnrollmentId",
                table: "Payments",
                column: "CampEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CampDays_CampId",
                table: "CampDays",
                column: "CampId");

            migrationBuilder.CreateIndex(
                name: "IX_CampDays_OrganizationId",
                table: "CampDays",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CampDayTrainers_CampDayId",
                table: "CampDayTrainers",
                column: "CampDayId");

            migrationBuilder.CreateIndex(
                name: "IX_CampDayTrainers_TrainerId_OrganizationId",
                table: "CampDayTrainers",
                columns: new[] { "TrainerId", "OrganizationId" });

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollmentForms_CampId",
                table: "CampEnrollmentForms",
                column: "CampId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollmentForms_OrganizationId",
                table: "CampEnrollmentForms",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollmentGroups_CampId",
                table: "CampEnrollmentGroups",
                column: "CampId");

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollmentGroups_LeaderEnrollmentId",
                table: "CampEnrollmentGroups",
                column: "LeaderEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollmentGroups_OrganizationId",
                table: "CampEnrollmentGroups",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollments_CampEnrollmentGroupId",
                table: "CampEnrollments",
                column: "CampEnrollmentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollments_CampId",
                table: "CampEnrollments",
                column: "CampId");

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollments_CampId_ParticipantEmail",
                table: "CampEnrollments",
                columns: new[] { "CampId", "ParticipantEmail" },
                unique: true,
                filter: "\"Status\" IN (1, 2, 5)");

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollments_OrganizationId",
                table: "CampEnrollments",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_CampEnrollments_ParticipantEmail",
                table: "CampEnrollments",
                column: "ParticipantEmail");

            migrationBuilder.CreateIndex(
                name: "IX_CampFormFields_CampEnrollmentFormId",
                table: "CampFormFields",
                column: "CampEnrollmentFormId");

            migrationBuilder.CreateIndex(
                name: "IX_CampFormResponses_CampEnrollmentId",
                table: "CampFormResponses",
                column: "CampEnrollmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CampFormResponses_CampFormFieldId",
                table: "CampFormResponses",
                column: "CampFormFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_Camps_OrganizationId",
                table: "Camps",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_Camps_TennisClubId",
                table: "Camps",
                column: "TennisClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_CampEnrollments_CampEnrollmentId",
                table: "Payments",
                column: "CampEnrollmentId",
                principalTable: "CampEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CampEnrollmentGroups_CampEnrollments_LeaderEnrollmentId",
                table: "CampEnrollmentGroups",
                column: "LeaderEnrollmentId",
                principalTable: "CampEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_CampEnrollments_CampEnrollmentId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_CampEnrollmentGroups_Camps_CampId",
                table: "CampEnrollmentGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_CampEnrollments_Camps_CampId",
                table: "CampEnrollments");

            migrationBuilder.DropForeignKey(
                name: "FK_CampEnrollmentGroups_CampEnrollments_LeaderEnrollmentId",
                table: "CampEnrollmentGroups");

            migrationBuilder.DropTable(
                name: "CampDayTrainers");

            migrationBuilder.DropTable(
                name: "CampFormResponses");

            migrationBuilder.DropTable(
                name: "CampDays");

            migrationBuilder.DropTable(
                name: "CampFormFields");

            migrationBuilder.DropTable(
                name: "CampEnrollmentForms");

            migrationBuilder.DropTable(
                name: "Camps");

            migrationBuilder.DropTable(
                name: "CampEnrollments");

            migrationBuilder.DropTable(
                name: "CampEnrollmentGroups");

            migrationBuilder.DropIndex(
                name: "IX_Payments_CampEnrollmentId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CampEnrollmentId",
                table: "Payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "EnrollmentId",
                table: "Payments",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
