using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedMissingTaskLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Level: Info=0, Warning=1, Error=2, Success=3
            migrationBuilder.Sql(@"
INSERT INTO ""TaskLogs"" (""Id"",""ParserTaskId"",""Timestamp"",""Message"",""AccountName"",""Level"",""StatusText"",""QuestionNumber"")
VALUES

  -- d4e5f6a7 — Like, Pending
  ('20000004-0000-0000-0000-000000000001','d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f80',TIMESTAMPTZ '2026-05-04T14:00:00Z','Задача создана',NULL,0,NULL,NULL),

  -- f6a7b8c9 — Report, Canceled
  ('20000006-0000-0000-0000-000000000001','f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f809102',TIMESTAMPTZ '2026-05-06T08:00:00Z','Задача создана',NULL,0,NULL,NULL),
  ('20000006-0000-0000-0000-000000000002','f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f809102',TIMESTAMPTZ '2026-05-06T08:01:00Z','Задача запущена',NULL,0,NULL,NULL),
  ('20000006-0000-0000-0000-000000000003','f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f809102',TIMESTAMPTZ '2026-05-06T08:06:00Z','Задача отменена пользователем',NULL,1,NULL,NULL),

  -- a7b8c9d0 — AutoReply, Completed
  ('20000007-0000-0000-0000-000000000001','a7b8c9d0-e1f2-4a3b-4c5d-6e7f80910213',TIMESTAMPTZ '2026-05-07T15:00:00Z','Задача создана',NULL,0,NULL,NULL),
  ('20000007-0000-0000-0000-000000000002','a7b8c9d0-e1f2-4a3b-4c5d-6e7f80910213',TIMESTAMPTZ '2026-05-07T15:02:00Z','Задача запущена',NULL,0,NULL,NULL),
  ('20000007-0000-0000-0000-000000000003','a7b8c9d0-e1f2-4a3b-4c5d-6e7f80910213',TIMESTAMPTZ '2026-05-07T16:35:00Z','Задача завершена. Отправлено ответов: 60','alex_mzv',3,NULL,NULL),

  -- b8c9d0e1 — Monitoring, Pending
  ('20000008-0000-0000-0000-000000000001','b8c9d0e1-f2a3-4b4c-5d6e-7f8091021324',TIMESTAMPTZ '2026-05-08T10:00:00Z','Задача создана',NULL,0,NULL,NULL),

  -- c9d0e1f2 — Like, Completed (200 лайков)
  ('20000009-0000-0000-0000-000000000001','c9d0e1f2-a3b4-4c5d-6e7f-809102132435',TIMESTAMPTZ '2026-05-09T10:00:00Z','Задача создана',NULL,0,NULL,NULL),
  ('20000009-0000-0000-0000-000000000002','c9d0e1f2-a3b4-4c5d-6e7f-809102132435',TIMESTAMPTZ '2026-05-09T10:03:00Z','Задача запущена',NULL,0,NULL,NULL),
  ('20000009-0000-0000-0000-000000000003','c9d0e1f2-a3b4-4c5d-6e7f-809102132435',TIMESTAMPTZ '2026-05-09T13:25:00Z','Задача завершена. Поставлено лайков: 200','dm_sokol',3,NULL,NULL),

  -- d0e1f2a3 — Comment, Pending
  ('20000010-0000-0000-0000-000000000001','d0e1f2a3-b4c5-4d6e-7f80-910213243546',TIMESTAMPTZ '2026-05-10T12:00:00Z','Задача создана',NULL,0,NULL,NULL),

  -- e1f2a3b4 — Dislike, Completed
  ('20000011-0000-0000-0000-000000000001','e1f2a3b4-c5d6-4e7f-8091-021324354657',TIMESTAMPTZ '2026-05-11T09:00:00Z','Задача создана',NULL,0,NULL,NULL),
  ('20000011-0000-0000-0000-000000000002','e1f2a3b4-c5d6-4e7f-8091-021324354657',TIMESTAMPTZ '2026-05-11T09:02:00Z','Задача запущена',NULL,0,NULL,NULL),
  ('20000011-0000-0000-0000-000000000003','e1f2a3b4-c5d6-4e7f-8091-021324354657',TIMESTAMPTZ '2026-05-11T09:32:00Z','Задача завершена. Поставлено дизлайков: 50','masha_p',3,NULL,NULL)

ON CONFLICT (""Id"") DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM ""TaskLogs"" WHERE ""Id"" IN (
  '20000004-0000-0000-0000-000000000001',
  '20000006-0000-0000-0000-000000000001','20000006-0000-0000-0000-000000000002','20000006-0000-0000-0000-000000000003',
  '20000007-0000-0000-0000-000000000001','20000007-0000-0000-0000-000000000002','20000007-0000-0000-0000-000000000003',
  '20000008-0000-0000-0000-000000000001',
  '20000009-0000-0000-0000-000000000001','20000009-0000-0000-0000-000000000002','20000009-0000-0000-0000-000000000003',
  '20000010-0000-0000-0000-000000000001',
  '20000011-0000-0000-0000-000000000001','20000011-0000-0000-0000-000000000002','20000011-0000-0000-0000-000000000003'
);");
        }
    }
}
