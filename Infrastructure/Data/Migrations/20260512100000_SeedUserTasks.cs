using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedUserTasks : Migration
    {
        private const string SeedUserId = "a0000000-0000-0000-0000-000000000001";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "UserTasks",
                columns: new[]
                {
                    "Id", "UserId", "Type", "TargetUrl", "CommentText", "OptionalLink",
                    "PromotionAfterPost", "Count", "DelayFromSeconds", "DelayToSeconds",
                    "CreatedAt", "StartedAt", "CompletedAt",
                    "Status", "Progress", "NotifyOnComplete", "NotifyOnError", "NotifyTelegram"
                },
                values: new object[,]
                {
                    // Like — Completed
                    {
                        new Guid("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"), SeedUserId, 2,
                        "https://www.youtube.com/watch?v=dQw4w9WgXcQ", null, null,
                        false, 50, 5, 15,
                        new DateTime(2026, 5, 1, 8, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 1, 8, 5, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 1, 8, 47, 0, DateTimeKind.Utc),
                        3, 50, true, true, false
                    },
                    // Comment — Completed
                    {
                        new Guid("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e"), SeedUserId, 1,
                        "https://www.youtube.com/watch?v=abc123def456", "Отличное видео, спасибо!", null,
                        false, 20, 10, 30,
                        new DateTime(2026, 5, 2, 9, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 2, 9, 3, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 2, 9, 55, 0, DateTimeKind.Utc),
                        3, 20, true, true, false
                    },
                    // Dislike — Failed
                    {
                        new Guid("c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f"), SeedUserId, 3,
                        "https://vk.com/wall-12345678_100", null, null,
                        false, 30, 5, 20,
                        new DateTime(2026, 5, 3, 11, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 3, 11, 2, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 3, 11, 18, 0, DateTimeKind.Utc),
                        4, 7, true, true, false
                    },
                    // Like — Pending
                    {
                        new Guid("d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f80"), SeedUserId, 2,
                        "https://ok.ru/video/1234567890", null, null,
                        false, 100, 3, 10,
                        new DateTime(2026, 5, 4, 14, 0, 0, DateTimeKind.Utc),
                        null, null,
                        1, 0, true, true, false
                    },
                    // Comment — Processing
                    {
                        new Guid("e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8091"), SeedUserId, 1,
                        "https://www.youtube.com/watch?v=xyz789uvw012", "Спасибо за контент, очень помогло!", null,
                        true, 40, 10, 25,
                        new DateTime(2026, 5, 5, 14, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 5, 14, 5, 0, DateTimeKind.Utc),
                        null,
                        2, 12, true, true, true
                    },
                    // Report — Canceled
                    {
                        new Guid("f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f809102"), SeedUserId, 4,
                        "https://vk.com/wall-98765432_200", null, null,
                        false, 10, 15, 45,
                        new DateTime(2026, 5, 6, 8, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 6, 8, 1, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 6, 8, 6, 0, DateTimeKind.Utc),
                        5, 3, false, true, false
                    },
                    // AutoReply — Completed
                    {
                        new Guid("a7b8c9d0-e1f2-4a3b-4c5d-6e7f80910213"), SeedUserId, 5,
                        "https://www.youtube.com/watch?v=lmn456opq789", "Рады ответить на ваш вопрос!", null,
                        false, 60, 5, 15,
                        new DateTime(2026, 5, 7, 15, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 7, 15, 2, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 7, 16, 35, 0, DateTimeKind.Utc),
                        3, 60, true, true, true
                    },
                    // Monitoring — Pending
                    {
                        new Guid("b8c9d0e1-f2a3-4b4c-5d6e-7f8091021324"), SeedUserId, 6,
                        "https://ok.ru/video/9876543210", null, null,
                        false, 1, 60, 120,
                        new DateTime(2026, 5, 8, 10, 0, 0, DateTimeKind.Utc),
                        null, null,
                        1, 0, true, true, false
                    },
                    // Like — Completed (large count)
                    {
                        new Guid("c9d0e1f2-a3b4-4c5d-6e7f-809102132435"), SeedUserId, 2,
                        "https://www.youtube.com/watch?v=rst012uvw345", null, null,
                        false, 200, 3, 8,
                        new DateTime(2026, 5, 9, 10, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 9, 10, 3, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 9, 13, 25, 0, DateTimeKind.Utc),
                        3, 200, true, true, false
                    },
                    // Comment — Pending
                    {
                        new Guid("d0e1f2a3-b4c5-4d6e-7f80-910213243546"), SeedUserId, 1,
                        "https://vk.com/wall-55555555_300", "Очень интересный материал, буду следить!", null,
                        false, 25, 10, 20,
                        new DateTime(2026, 5, 10, 12, 0, 0, DateTimeKind.Utc),
                        null, null,
                        1, 0, true, true, false
                    },
                    // Dislike — Completed
                    {
                        new Guid("e1f2a3b4-c5d6-4e7f-8091-021324354657"), SeedUserId, 3,
                        "https://www.youtube.com/watch?v=hij678klm901", null, null,
                        false, 50, 5, 12,
                        new DateTime(2026, 5, 11, 9, 0, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 11, 9, 2, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 11, 9, 32, 0, DateTimeKind.Utc),
                        3, 50, true, true, false
                    },
                    // AutoReply — Processing
                    {
                        new Guid("f2a3b4c5-d6e7-4f80-9102-132435465768"), SeedUserId, 5,
                        "https://ok.ru/video/1122334455", "Спасибо за ваш комментарий!", "https://example.com/promo",
                        true, 35, 8, 20,
                        new DateTime(2026, 5, 12, 8, 30, 0, DateTimeKind.Utc),
                        new DateTime(2026, 5, 12, 8, 35, 0, DateTimeKind.Utc),
                        null,
                        2, 10, true, true, true
                    },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserTasks",
                keyColumn: "Id",
                keyValues: new object[]
                {
                    new Guid("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"),
                    new Guid("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e"),
                    new Guid("c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f"),
                    new Guid("d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f80"),
                    new Guid("e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8091"),
                    new Guid("f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f809102"),
                    new Guid("a7b8c9d0-e1f2-4a3b-4c5d-6e7f80910213"),
                    new Guid("b8c9d0e1-f2a3-4b4c-5d6e-7f8091021324"),
                    new Guid("c9d0e1f2-a3b4-4c5d-6e7f-809102132435"),
                    new Guid("d0e1f2a3-b4c5-4d6e-7f80-910213243546"),
                    new Guid("e1f2a3b4-c5d6-4e7f-8091-021324354657"),
                    new Guid("f2a3b4c5-d6e7-4f80-9102-132435465768"),
                });
        }
    }
}
