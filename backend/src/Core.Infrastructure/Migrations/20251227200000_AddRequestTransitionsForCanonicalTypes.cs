using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

using Microsoft.EntityFrameworkCore.Infrastructure;
using MyIS.Core.Infrastructure.Data;

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20251227200000_AddRequestTransitionsForCanonicalTypes")]
    public partial class AddRequestTransitionsForCanonicalTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-- Seed workflow transitions for remaining canonical request types (idempotent)

-- CustomerDevelopment
INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '20000000-0000-0000-0000-000000000001', rt.id, 'Draft', 'Submitted', 'Submit', 'Requests.Submit'
FROM requests.request_types rt
WHERE rt.id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '20000000-0000-0000-0000-000000000002', rt.id, 'Submitted', 'InReview', 'StartReview', 'Requests.StartReview'
FROM requests.request_types rt
WHERE rt.id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '20000000-0000-0000-0000-000000000003', rt.id, 'InReview', 'Approved', 'Approve', 'Requests.Approve'
FROM requests.request_types rt
WHERE rt.id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '20000000-0000-0000-0000-000000000004', rt.id, 'InReview', 'Rejected', 'Reject', 'Requests.Reject'
FROM requests.request_types rt
WHERE rt.id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '20000000-0000-0000-0000-000000000005', rt.id, 'Approved', 'InWork', 'StartWork', 'Requests.StartWork'
FROM requests.request_types rt
WHERE rt.id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '20000000-0000-0000-0000-000000000006', rt.id, 'InWork', 'Done', 'Complete', 'Requests.Complete'
FROM requests.request_types rt
WHERE rt.id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '20000000-0000-0000-0000-000000000007', rt.id, 'Done', 'Closed', 'Close', 'Requests.Close'
FROM requests.request_types rt
WHERE rt.id = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

-- InternalProductionRequest
INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '21000000-0000-0000-0000-000000000001', rt.id, 'Draft', 'Submitted', 'Submit', 'Requests.Submit'
FROM requests.request_types rt
WHERE rt.id = 'cccccccc-cccc-cccc-cccc-cccccccccccc'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '21000000-0000-0000-0000-000000000002', rt.id, 'Submitted', 'InReview', 'StartReview', 'Requests.StartReview'
FROM requests.request_types rt
WHERE rt.id = 'cccccccc-cccc-cccc-cccc-cccccccccccc'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '21000000-0000-0000-0000-000000000003', rt.id, 'InReview', 'Approved', 'Approve', 'Requests.Approve'
FROM requests.request_types rt
WHERE rt.id = 'cccccccc-cccc-cccc-cccc-cccccccccccc'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '21000000-0000-0000-0000-000000000004', rt.id, 'InReview', 'Rejected', 'Reject', 'Requests.Reject'
FROM requests.request_types rt
WHERE rt.id = 'cccccccc-cccc-cccc-cccc-cccccccccccc'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '21000000-0000-0000-0000-000000000005', rt.id, 'Approved', 'InWork', 'StartWork', 'Requests.StartWork'
FROM requests.request_types rt
WHERE rt.id = 'cccccccc-cccc-cccc-cccc-cccccccccccc'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '21000000-0000-0000-0000-000000000006', rt.id, 'InWork', 'Done', 'Complete', 'Requests.Complete'
FROM requests.request_types rt
WHERE rt.id = 'cccccccc-cccc-cccc-cccc-cccccccccccc'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '21000000-0000-0000-0000-000000000007', rt.id, 'Done', 'Closed', 'Close', 'Requests.Close'
FROM requests.request_types rt
WHERE rt.id = 'cccccccc-cccc-cccc-cccc-cccccccccccc'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

-- ChangeRequest
INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '22000000-0000-0000-0000-000000000001', rt.id, 'Draft', 'Submitted', 'Submit', 'Requests.Submit'
FROM requests.request_types rt
WHERE rt.id = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '22000000-0000-0000-0000-000000000002', rt.id, 'Submitted', 'InReview', 'StartReview', 'Requests.StartReview'
FROM requests.request_types rt
WHERE rt.id = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '22000000-0000-0000-0000-000000000003', rt.id, 'InReview', 'Approved', 'Approve', 'Requests.Approve'
FROM requests.request_types rt
WHERE rt.id = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '22000000-0000-0000-0000-000000000004', rt.id, 'InReview', 'Rejected', 'Reject', 'Requests.Reject'
FROM requests.request_types rt
WHERE rt.id = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '22000000-0000-0000-0000-000000000005', rt.id, 'Approved', 'InWork', 'StartWork', 'Requests.StartWork'
FROM requests.request_types rt
WHERE rt.id = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '22000000-0000-0000-0000-000000000006', rt.id, 'InWork', 'Done', 'Complete', 'Requests.Complete'
FROM requests.request_types rt
WHERE rt.id = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '22000000-0000-0000-0000-000000000007', rt.id, 'Done', 'Closed', 'Close', 'Requests.Close'
FROM requests.request_types rt
WHERE rt.id = 'dddddddd-dddd-dddd-dddd-dddddddddddd'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

-- ExternalTechStageRequest
INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '23000000-0000-0000-0000-000000000001', rt.id, 'Draft', 'Submitted', 'Submit', 'Requests.Submit'
FROM requests.request_types rt
WHERE rt.id = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '23000000-0000-0000-0000-000000000002', rt.id, 'Submitted', 'InReview', 'StartReview', 'Requests.StartReview'
FROM requests.request_types rt
WHERE rt.id = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '23000000-0000-0000-0000-000000000003', rt.id, 'InReview', 'Approved', 'Approve', 'Requests.Approve'
FROM requests.request_types rt
WHERE rt.id = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '23000000-0000-0000-0000-000000000004', rt.id, 'InReview', 'Rejected', 'Reject', 'Requests.Reject'
FROM requests.request_types rt
WHERE rt.id = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '23000000-0000-0000-0000-000000000005', rt.id, 'Approved', 'InWork', 'StartWork', 'Requests.StartWork'
FROM requests.request_types rt
WHERE rt.id = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '23000000-0000-0000-0000-000000000006', rt.id, 'InWork', 'Done', 'Complete', 'Requests.Complete'
FROM requests.request_types rt
WHERE rt.id = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '23000000-0000-0000-0000-000000000007', rt.id, 'Done', 'Closed', 'Close', 'Requests.Close'
FROM requests.request_types rt
WHERE rt.id = 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM requests.request_transitions rt
USING requests.request_types t
WHERE rt.request_type_id = t.id
  AND t.id IN (
    'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
    'cccccccc-cccc-cccc-cccc-cccccccccccc',
    'dddddddd-dddd-dddd-dddd-dddddddddddd',
    'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee'
  )
  AND rt.action_code IN ('Submit', 'StartReview', 'Approve', 'Reject', 'StartWork', 'Complete', 'Close');
");
        }
    }
}
