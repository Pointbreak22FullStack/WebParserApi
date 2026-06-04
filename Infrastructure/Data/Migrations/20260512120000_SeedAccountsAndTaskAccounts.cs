using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedAccountsAndTaskAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Используем ON CONFLICT DO NOTHING — безопасно при повторном запуске
            // AccountStatus: Active=0, Cooldown=1  |  LoginMethod: Mail=0, VK=2
            migrationBuilder.Sql(@"
INSERT INTO ""Accounts"" (""Id"",""Login"",""Password"",""Description"",""FirstName"",""LastName"",""SiteNick"",""Status"",""CompletedTasksCount"",""ProxyId"",""LastUsedAtUtc"",""CreatedAt"",""CurrentHandlerUserId"",""LockedUntilUtc"",""LoginMethod"",""PhoneNumber"",""SessionInfo"",""RiskScore"")
VALUES
  (9001,'vk_bot_account_1','pass_hash_1','Основной бот',  'Алексей','Морозов', 'alex_mzv', 0,47,NULL,TIMESTAMPTZ '2026-05-11T09:32:00Z',TIMESTAMPTZ '2026-04-01T00:00:00Z',NULL,NULL,2,'+79001234501',NULL,12),
  (9002,'vk_bot_account_2','pass_hash_2','Запасной бот',  'Дмитрий','Соколов', 'dm_sokol', 0,35,NULL,TIMESTAMPTZ '2026-05-11T09:32:00Z',TIMESTAMPTZ '2026-04-02T00:00:00Z',NULL,NULL,2,'+79001234502',NULL, 8),
  (9003,'vk_bot_account_3','pass_hash_3','Комментатор',   'Мария',  'Петрова', 'masha_p',  0,20,NULL,TIMESTAMPTZ '2026-05-09T13:25:00Z',TIMESTAMPTZ '2026-04-03T00:00:00Z',NULL,NULL,2,'+79001234503',NULL, 5),
  (9004,'vk_bot_account_4','pass_hash_4','Новый аккаунт', 'Игорь',  'Волков',  'igor_vlk', 0, 0,NULL,NULL,                              TIMESTAMPTZ '2026-05-01T00:00:00Z',NULL,NULL,0,'+79001234504',NULL, 0),
  (9005,'vk_bot_account_5','pass_hash_5','На кулдауне',   'Светлана','Ковалёва','sveta_kv', 1, 8,NULL,TIMESTAMPTZ '2026-05-03T11:18:00Z',TIMESTAMPTZ '2026-04-05T00:00:00Z',NULL,TIMESTAMPTZ '2026-05-12T14:00:00Z',2,'+79001234505',NULL,45)
ON CONFLICT (""Id"") DO NOTHING;

SELECT setval(pg_get_serial_sequence('""Accounts""', 'Id'), GREATEST(MAX(""Id""), 9005)) FROM ""Accounts"";
");

            // ── TaskAccounts ──────────────────────────────────────────────────────
            // TaskAccountStatus: InQueue=1, Active=2, InWork=3, Completed=4, Error=5

            // Task a1b2c3d4 — Like, Completed  → result: completed=3, inProgress=0, error=0
            migrationBuilder.Sql(@"
INSERT INTO ""TaskAccounts"" (""Id"",""UserTaskId"",""AccountId"",""Status"",""CompletedAt"",""DurationSeconds"")
VALUES
  ('a0000001-0000-0000-0000-000000000001','a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d',9001,4,TIMESTAMPTZ '2026-05-01T08:20:00Z', 900),
  ('a0000001-0000-0000-0000-000000000002','a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d',9002,4,TIMESTAMPTZ '2026-05-01T08:35:00Z',1800),
  ('a0000001-0000-0000-0000-000000000003','a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d',9003,4,TIMESTAMPTZ '2026-05-01T08:47:00Z',2520)
ON CONFLICT (""Id"") DO NOTHING;");

            // Task b2c3d4e5 — Comment, Completed  → result: completed=3, inProgress=0, error=0
            migrationBuilder.Sql(@"
INSERT INTO ""TaskAccounts"" (""Id"",""UserTaskId"",""AccountId"",""Status"",""CompletedAt"",""DurationSeconds"")
VALUES
  ('a0000002-0000-0000-0000-000000000001','b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e',9001,4,TIMESTAMPTZ '2026-05-02T09:30:00Z',1620),
  ('a0000002-0000-0000-0000-000000000002','b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e',9002,4,TIMESTAMPTZ '2026-05-02T09:45:00Z',2520),
  ('a0000002-0000-0000-0000-000000000003','b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e',9003,4,TIMESTAMPTZ '2026-05-02T09:55:00Z',3120)
ON CONFLICT (""Id"") DO NOTHING;");

            // Task c3d4e5f6 — Dislike, Failed  → result: completed=0, inProgress=1, error=2
            migrationBuilder.Sql(@"
INSERT INTO ""TaskAccounts"" (""Id"",""UserTaskId"",""AccountId"",""Status"",""CompletedAt"",""DurationSeconds"")
VALUES
  ('a0000003-0000-0000-0000-000000000001','c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f',9004,5,NULL,NULL),
  ('a0000003-0000-0000-0000-000000000002','c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f',9005,5,NULL,NULL),
  ('a0000003-0000-0000-0000-000000000003','c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f',9003,1,NULL,NULL)
ON CONFLICT (""Id"") DO NOTHING;");

            // Task e5f6a7b8 — Comment, Processing  → result: completed=1, inProgress=3, error=0
            migrationBuilder.Sql(@"
INSERT INTO ""TaskAccounts"" (""Id"",""UserTaskId"",""AccountId"",""Status"",""CompletedAt"",""DurationSeconds"")
VALUES
  ('a0000005-0000-0000-0000-000000000001','e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8091',9001,4,TIMESTAMPTZ '2026-05-05T14:20:00Z',900),
  ('a0000005-0000-0000-0000-000000000002','e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8091',9002,3,NULL,NULL),
  ('a0000005-0000-0000-0000-000000000003','e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8091',9003,1,NULL,NULL),
  ('a0000005-0000-0000-0000-000000000004','e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8091',9004,1,NULL,NULL)
ON CONFLICT (""Id"") DO NOTHING;");

            // Task f2a3b4c5 — AutoReply, Processing  → result: completed=2, inProgress=2, error=0
            migrationBuilder.Sql(@"
INSERT INTO ""TaskAccounts"" (""Id"",""UserTaskId"",""AccountId"",""Status"",""CompletedAt"",""DurationSeconds"")
VALUES
  ('a0000012-0000-0000-0000-000000000001','f2a3b4c5-d6e7-4f80-9102-132435465768',9001,4,TIMESTAMPTZ '2026-05-12T08:45:00Z',600),
  ('a0000012-0000-0000-0000-000000000002','f2a3b4c5-d6e7-4f80-9102-132435465768',9002,4,TIMESTAMPTZ '2026-05-12T08:50:00Z',900),
  ('a0000012-0000-0000-0000-000000000003','f2a3b4c5-d6e7-4f80-9102-132435465768',9003,2,NULL,NULL),
  ('a0000012-0000-0000-0000-000000000004','f2a3b4c5-d6e7-4f80-9102-132435465768',9004,1,NULL,NULL)
ON CONFLICT (""Id"") DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM ""TaskAccounts"" WHERE ""Id"" IN (
  'a0000001-0000-0000-0000-000000000001','a0000001-0000-0000-0000-000000000002','a0000001-0000-0000-0000-000000000003',
  'a0000002-0000-0000-0000-000000000001','a0000002-0000-0000-0000-000000000002','a0000002-0000-0000-0000-000000000003',
  'a0000003-0000-0000-0000-000000000001','a0000003-0000-0000-0000-000000000002','a0000003-0000-0000-0000-000000000003',
  'a0000005-0000-0000-0000-000000000001','a0000005-0000-0000-0000-000000000002','a0000005-0000-0000-0000-000000000003','a0000005-0000-0000-0000-000000000004',
  'a0000012-0000-0000-0000-000000000001','a0000012-0000-0000-0000-000000000002','a0000012-0000-0000-0000-000000000003','a0000012-0000-0000-0000-000000000004'
);
DELETE FROM ""Accounts"" WHERE ""Id"" IN (9001,9002,9003,9004,9005);
");
        }
    }
}
