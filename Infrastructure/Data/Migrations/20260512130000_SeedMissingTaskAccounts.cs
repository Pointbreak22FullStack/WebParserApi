using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedMissingTaskAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // TaskAccountStatus: InQueue=1, Active=2, InWork=3, Completed=4, Error=5
            migrationBuilder.Sql(@"
INSERT INTO ""TaskAccounts"" (""Id"",""UserTaskId"",""AccountId"",""Status"",""CompletedAt"",""DurationSeconds"")
VALUES
  -- Like, Pending → InQueue
  ('b0000004-0000-0000-0000-000000000001','d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f80',9001,1,NULL,NULL),

  -- Report, Canceled → InQueue (не успел выполниться)
  ('b0000006-0000-0000-0000-000000000001','f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f809102',9002,1,NULL,NULL),

  -- AutoReply, Completed → Completed
  ('b0000007-0000-0000-0000-000000000001','a7b8c9d0-e1f2-4a3b-4c5d-6e7f80910213',9001,4,TIMESTAMPTZ '2026-05-07T16:35:00Z',5580),

  -- Monitoring, Pending → InQueue
  ('b0000008-0000-0000-0000-000000000001','b8c9d0e1-f2a3-4b4c-5d6e-7f8091021324',9003,1,NULL,NULL),

  -- Like, Completed → Completed
  ('b0000009-0000-0000-0000-000000000001','c9d0e1f2-a3b4-4c5d-6e7f-809102132435',9002,4,TIMESTAMPTZ '2026-05-09T13:25:00Z',11520),

  -- Comment, Pending → InQueue
  ('b0000010-0000-0000-0000-000000000001','d0e1f2a3-b4c5-4d6e-7f80-910213243546',9004,1,NULL,NULL),

  -- Dislike, Completed → Completed
  ('b0000011-0000-0000-0000-000000000001','e1f2a3b4-c5d6-4e7f-8091-021324354657',9003,4,TIMESTAMPTZ '2026-05-11T09:32:00Z',1800)

ON CONFLICT (""Id"") DO NOTHING;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM ""TaskAccounts"" WHERE ""Id"" IN (
  'b0000004-0000-0000-0000-000000000001',
  'b0000006-0000-0000-0000-000000000001',
  'b0000007-0000-0000-0000-000000000001',
  'b0000008-0000-0000-0000-000000000001',
  'b0000009-0000-0000-0000-000000000001',
  'b0000010-0000-0000-0000-000000000001',
  'b0000011-0000-0000-0000-000000000001'
);");
        }
    }
}
