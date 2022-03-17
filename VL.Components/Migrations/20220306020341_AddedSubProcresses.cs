using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VL.Components.Migrations
{
    public partial class AddedSubProcresses : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Cmd = table.Column<string>(type: "TEXT", nullable: false),
                    Args = table.Column<string>(type: "TEXT", nullable: false),
                    Start = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Finish = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProcessID = table.Column<int>(type: "INTEGER", nullable: false),
                    ExitCode = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputCount = table.Column<int>(type: "INTEGER", nullable: false),
                    ErrorCount = table.Column<int>(type: "INTEGER", nullable: false),
                    IsFinished = table.Column<bool>(type: "INTEGER", nullable: false),
                    NumSubProcesses = table.Column<long>(type: "INTEGER", nullable: false),
                    NetworkSend = table.Column<long>(type: "INTEGER", nullable: false),
                    NetworkReceive = table.Column<long>(type: "INTEGER", nullable: false),
                    FileRead = table.Column<long>(type: "INTEGER", nullable: false),
                    FileWrite = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Channel = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    JobId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    JobEntryId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Logs_Jobs_JobEntryId",
                        column: x => x.JobEntryId,
                        principalTable: "Jobs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProcessEntry",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Cmd = table.Column<string>(type: "TEXT", nullable: false),
                    Args = table.Column<string>(type: "TEXT", nullable: false),
                    Start = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Finish = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ParentUniqueId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ProcessID = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentProcessID = table.Column<int>(type: "INTEGER", nullable: false),
                    JobEntryId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProcessEntry_Jobs_JobEntryId",
                        column: x => x.JobEntryId,
                        principalTable: "Jobs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FileEntry",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Filename = table.Column<string>(type: "TEXT", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    Cmd = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcessId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    JobEntryId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FileEntry_Jobs_JobEntryId",
                        column: x => x.JobEntryId,
                        principalTable: "Jobs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FileEntry_ProcessEntry_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "ProcessEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NetworkEntry",
                columns: table => new
                {
                    Id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    Size = table.Column<long>(type: "INTEGER", nullable: false),
                    Cmd = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcessId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    JobEntryId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NetworkEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NetworkEntry_Jobs_JobEntryId",
                        column: x => x.JobEntryId,
                        principalTable: "Jobs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_NetworkEntry_ProcessEntry_ProcessId",
                        column: x => x.ProcessId,
                        principalTable: "ProcessEntry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileEntry_JobEntryId",
                table: "FileEntry",
                column: "JobEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FileEntry_ProcessId",
                table: "FileEntry",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_JobEntryId",
                table: "Logs",
                column: "JobEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkEntry_JobEntryId",
                table: "NetworkEntry",
                column: "JobEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_NetworkEntry_ProcessId",
                table: "NetworkEntry",
                column: "ProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessEntry_JobEntryId",
                table: "ProcessEntry",
                column: "JobEntryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileEntry");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "NetworkEntry");

            migrationBuilder.DropTable(
                name: "ProcessEntry");

            migrationBuilder.DropTable(
                name: "Jobs");
        }
    }
}
