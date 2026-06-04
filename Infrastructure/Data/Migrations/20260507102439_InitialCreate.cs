using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Proxies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    Port = table.Column<int>(type: "integer", nullable: false),
                    Login = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    LatencyMs = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proxies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DelayBetweenActionsMs = table.Column<int>(type: "integer", nullable: false, defaultValue: 800),
                    MaxParallelTasks = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    RetryOnError = table.Column<bool>(type: "boolean", nullable: false),
                    HeadlessMode = table.Column<bool>(type: "boolean", nullable: false),
                    FingerprintEngine = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RotateUserAgent = table.Column<bool>(type: "boolean", nullable: false),
                    SimulateHumanBehavior = table.Column<bool>(type: "boolean", nullable: false),
                    GenerationMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CheckIntervalMinutes = table.Column<int>(type: "integer", nullable: false),
                    AutoLikeEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    ReportCompetitors = table.Column<bool>(type: "boolean", nullable: false),
                    TelegramBotToken = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    TelegramChatId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserTasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TargetUrl = table.Column<string>(type: "text", nullable: false),
                    CommentText = table.Column<string>(type: "text", nullable: true),
                    PromotionAfterPost = table.Column<bool>(type: "boolean", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    Delay = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    NotifyOnComplete = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyOnError = table.Column<bool>(type: "boolean", nullable: false),
                    NotifyTelegram = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Login = table.Column<string>(type: "text", nullable: false),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    SiteNick = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CompletedTasksCount = table.Column<int>(type: "integer", nullable: false),
                    ProxyId = table.Column<int>(type: "integer", nullable: true),
                    LastUsedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentHandlerUserId = table.Column<string>(type: "text", nullable: true),
                    LockedUntilUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LoginMethod = table.Column<int>(type: "integer", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: false),
                    SessionInfo = table.Column<string>(type: "text", nullable: true),
                    RiskScore = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_Proxies_ProxyId",
                        column: x => x.ProxyId,
                        principalTable: "Proxies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ParsingResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ParserTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsSuccess = table.Column<bool>(type: "boolean", nullable: false),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParsingResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParsingResults_UserTasks_ParserTaskId",
                        column: x => x.ParserTaskId,
                        principalTable: "UserTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaskLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParserTaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    AccountName = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    StatusText = table.Column<string>(type: "text", nullable: true),
                    QuestionNumber = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskLogs_UserTasks_ParserTaskId",
                        column: x => x.ParserTaskId,
                        principalTable: "UserTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "SystemSettings",
                columns: new[] { "Id", "AutoLikeEnabled", "CheckIntervalMinutes", "DelayBetweenActionsMs", "FingerprintEngine", "GenerationMode", "HeadlessMode", "MaxParallelTasks", "ReportCompetitors", "RetryOnError", "RotateUserAgent", "SimulateHumanBehavior", "TelegramBotToken", "TelegramChatId", "UpdatedAt" },
                values: new object[] { 1, true, 30, 800, "Собственный", "LLM", true, 10, false, true, true, true, null, null, new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ProxyId",
                table: "Accounts",
                column: "ProxyId");

            migrationBuilder.CreateIndex(
                name: "IX_ParsingResults_ParserTaskId",
                table: "ParsingResults",
                column: "ParserTaskId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskLogs_ParserTaskId",
                table: "TaskLogs",
                column: "ParserTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "ParsingResults");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "TaskLogs");

            migrationBuilder.DropTable(
                name: "Proxies");

            migrationBuilder.DropTable(
                name: "UserTasks");
        }
    }
}
