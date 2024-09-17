using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chefster.Migrations
{
    /// <inheritdoc />
    public partial class CreateJobRecordTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "JobRecords",
                columns: table => new
                {
                    JobId = table.Column<string>(type: "TEXT", nullable: false),
                    FamilyId = table.Column<string>(type: "TEXT", nullable: false),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    JobStatus = table.Column<int>(type: "INTEGER", nullable: false),
                    JobType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobRecords", x => x.JobId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "JobRecords");
        }
    }
}
