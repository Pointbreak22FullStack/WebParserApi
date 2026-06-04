using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedTaskLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Like — Completed (a1b2c3d4...)
            migrationBuilder.InsertData(
                table: "TaskLogs",
                columns: new[] { "Id", "ParserTaskId", "Timestamp", "Message", "AccountName", "Level", "StatusText", "QuestionNumber" },
                values: new object[,]
                {
                    { new Guid("10000001-0000-0000-0000-000000000001"), new Guid("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"), new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),  "Задача создана",                        null,       0, null, null },
                    { new Guid("10000001-0000-0000-0000-000000000002"), new Guid("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"), new DateTime(2026, 5, 1, 8, 5, 0, DateTimeKind.Utc),  "Задача запущена",                       null,       0, null, null },
                    { new Guid("10000001-0000-0000-0000-000000000003"), new Guid("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"), new DateTime(2026, 5, 1, 8, 47, 0, DateTimeKind.Utc), "Задача завершена. Поставлено лайков: 50", null,       3, null, null },
                });

            // Comment — Completed (b2c3d4e5...)
            migrationBuilder.InsertData(
                table: "TaskLogs",
                columns: new[] { "Id", "ParserTaskId", "Timestamp", "Message", "AccountName", "Level", "StatusText", "QuestionNumber" },
                values: new object[,]
                {
                    { new Guid("10000002-0000-0000-0000-000000000001"), new Guid("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e"), new DateTime(2026, 5, 2, 9, 0, 0, DateTimeKind.Utc),  "Задача создана",                                null,        0, null, null },
                    { new Guid("10000002-0000-0000-0000-000000000002"), new Guid("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e"), new DateTime(2026, 5, 2, 9, 3, 0, DateTimeKind.Utc),  "Начало публикации комментариев",                null,        0, null, null },
                    { new Guid("10000002-0000-0000-0000-000000000003"), new Guid("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e"), new DateTime(2026, 5, 2, 9, 30, 0, DateTimeKind.Utc), "Опубликовано 10 из 20 комментариев",            "user_nick_3", 0, null, null },
                    { new Guid("10000002-0000-0000-0000-000000000004"), new Guid("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e"), new DateTime(2026, 5, 2, 9, 55, 0, DateTimeKind.Utc), "Задача завершена. Опубликовано комментариев: 20", null,        3, null, null },
                });

            // Dislike — Failed (c3d4e5f6...)
            migrationBuilder.InsertData(
                table: "TaskLogs",
                columns: new[] { "Id", "ParserTaskId", "Timestamp", "Message", "AccountName", "Level", "StatusText", "QuestionNumber" },
                values: new object[,]
                {
                    { new Guid("10000003-0000-0000-0000-000000000001"), new Guid("c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f"), new DateTime(2026, 5, 3, 11, 0, 0, DateTimeKind.Utc),  "Задача создана",                              null,        0, null, null },
                    { new Guid("10000003-0000-0000-0000-000000000002"), new Guid("c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f"), new DateTime(2026, 5, 3, 11, 2, 0, DateTimeKind.Utc),  "Задача запущена",                             null,        0, null, null },
                    { new Guid("10000003-0000-0000-0000-000000000003"), new Guid("c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f"), new DateTime(2026, 5, 3, 11, 10, 0, DateTimeKind.Utc), "Аккаунт user_5 потребовал капчу",             "user_5",    1, null, null },
                    { new Guid("10000003-0000-0000-0000-000000000004"), new Guid("c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f"), new DateTime(2026, 5, 3, 11, 18, 0, DateTimeKind.Utc), "Ошибка выполнения: превышен лимит запросов",  null,        2, null, null },
                });

            // Comment — Processing (e5f6a7b8...)
            migrationBuilder.InsertData(
                table: "TaskLogs",
                columns: new[] { "Id", "ParserTaskId", "Timestamp", "Message", "AccountName", "Level", "StatusText", "QuestionNumber" },
                values: new object[,]
                {
                    { new Guid("10000005-0000-0000-0000-000000000001"), new Guid("e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8091"), new DateTime(2026, 5, 5, 14, 0, 0, DateTimeKind.Utc),  "Задача создана",                    null,  0, null, null },
                    { new Guid("10000005-0000-0000-0000-000000000002"), new Guid("e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8091"), new DateTime(2026, 5, 5, 14, 5, 0, DateTimeKind.Utc),  "Задача запущена",                   null,  0, null, null },
                    { new Guid("10000005-0000-0000-0000-000000000003"), new Guid("e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8091"), new DateTime(2026, 5, 5, 14, 15, 0, DateTimeKind.Utc), "Опубликовано 12 из 40 комментариев", null,  0, null, null },
                });

            // AutoReply — Processing (f2a3b4c5...)
            migrationBuilder.InsertData(
                table: "TaskLogs",
                columns: new[] { "Id", "ParserTaskId", "Timestamp", "Message", "AccountName", "Level", "StatusText", "QuestionNumber" },
                values: new object[,]
                {
                    { new Guid("10000012-0000-0000-0000-000000000001"), new Guid("f2a3b4c5-d6e7-4f80-9102-132435465768"), new DateTime(2026, 5, 12, 8, 30, 0, DateTimeKind.Utc), "Задача создана",                          null,        0, null, null },
                    { new Guid("10000012-0000-0000-0000-000000000002"), new Guid("f2a3b4c5-d6e7-4f80-9102-132435465768"), new DateTime(2026, 5, 12, 8, 35, 0, DateTimeKind.Utc), "Задача запущена",                         null,        0, null, null },
                    { new Guid("10000012-0000-0000-0000-000000000003"), new Guid("f2a3b4c5-d6e7-4f80-9102-132435465768"), new DateTime(2026, 5, 12, 8, 45, 0, DateTimeKind.Utc), "Обработано 10 из 35 ответов",             null,        0, null, null },
                    { new Guid("10000012-0000-0000-0000-000000000004"), new Guid("f2a3b4c5-d6e7-4f80-9102-132435465768"), new DateTime(2026, 5, 12, 8, 55, 0, DateTimeKind.Utc), "Аккаунт user_2 временно заблокирован",    "user_2",    1, null, null },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(table: "TaskLogs", keyColumn: "Id", keyValues: new object[]
            {
                new Guid("10000001-0000-0000-0000-000000000001"), new Guid("10000001-0000-0000-0000-000000000002"), new Guid("10000001-0000-0000-0000-000000000003"),
                new Guid("10000002-0000-0000-0000-000000000001"), new Guid("10000002-0000-0000-0000-000000000002"), new Guid("10000002-0000-0000-0000-000000000003"), new Guid("10000002-0000-0000-0000-000000000004"),
                new Guid("10000003-0000-0000-0000-000000000001"), new Guid("10000003-0000-0000-0000-000000000002"), new Guid("10000003-0000-0000-0000-000000000003"), new Guid("10000003-0000-0000-0000-000000000004"),
                new Guid("10000005-0000-0000-0000-000000000001"), new Guid("10000005-0000-0000-0000-000000000002"), new Guid("10000005-0000-0000-0000-000000000003"),
                new Guid("10000012-0000-0000-0000-000000000001"), new Guid("10000012-0000-0000-0000-000000000002"), new Guid("10000012-0000-0000-0000-000000000003"), new Guid("10000012-0000-0000-0000-000000000004"),
            });
        }
    }
}
