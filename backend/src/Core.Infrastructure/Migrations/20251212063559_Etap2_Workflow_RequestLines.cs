using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Etap2_Workflow_RequestLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "request_lines",
                schema: "requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line_no = table.Column<int>(type: "integer", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_item_code = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    unit_of_measure_id = table.Column<Guid>(type: "uuid", nullable: true),
                    need_by_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    supplier_name = table.Column<string>(type: "text", nullable: true),
                    supplier_contact = table.Column<string>(type: "text", nullable: true),
                    external_row_reference_id = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_lines", x => x.id);
                    table.ForeignKey(
                        name: "FK_request_lines_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "requests",
                        principalTable: "requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "request_transitions",
                schema: "requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status_code = table.Column<string>(type: "text", nullable: false),
                    to_status_code = table.Column<string>(type: "text", nullable: false),
                    action_code = table.Column<string>(type: "text", nullable: false),
                    required_permission = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_transitions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_request_lines_request_id_line_no",
                schema: "requests",
                table: "request_lines",
                columns: new[] { "request_id", "line_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_transitions_request_type_id_from_status_code_action~",
                schema: "requests",
                table: "request_transitions",
                columns: new[] { "request_type_id", "from_status_code", "action_code" },
                unique: true);

            // Seed base statuses and SupplyRequest type + workflow transitions (idempotent via ON CONFLICT)
            migrationBuilder.Sql(@"
INSERT INTO requests.request_statuses (id, code, name, is_final, description)
VALUES ('11111111-1111-1111-1111-111111111111', 'Draft', 'Draft', FALSE, NULL)
ON CONFLICT (code) DO NOTHING;

INSERT INTO requests.request_statuses (id, code, name, is_final, description)
VALUES ('22222222-2222-2222-2222-222222222222', 'Submitted', 'Submitted', FALSE, NULL)
ON CONFLICT (code) DO NOTHING;

INSERT INTO requests.request_statuses (id, code, name, is_final, description)
VALUES ('33333333-3333-3333-3333-333333333333', 'InReview', 'In review', FALSE, NULL)
ON CONFLICT (code) DO NOTHING;

INSERT INTO requests.request_statuses (id, code, name, is_final, description)
VALUES ('44444444-4444-4444-4444-444444444444', 'Approved', 'Approved', FALSE, NULL)
ON CONFLICT (code) DO NOTHING;

INSERT INTO requests.request_statuses (id, code, name, is_final, description)
VALUES ('55555555-5555-5555-5555-555555555555', 'Rejected', 'Rejected', TRUE, NULL)
ON CONFLICT (code) DO NOTHING;

INSERT INTO requests.request_statuses (id, code, name, is_final, description)
VALUES ('66666666-6666-6666-6666-666666666666', 'InWork', 'In work', FALSE, NULL)
ON CONFLICT (code) DO NOTHING;

INSERT INTO requests.request_statuses (id, code, name, is_final, description)
VALUES ('77777777-7777-7777-7777-777777777777', 'Done', 'Done', FALSE, NULL)
ON CONFLICT (code) DO NOTHING;

INSERT INTO requests.request_statuses (id, code, name, is_final, description)
VALUES ('88888888-8888-8888-8888-888888888888', 'Closed', 'Closed', TRUE, NULL)
ON CONFLICT (code) DO NOTHING;

INSERT INTO requests.request_types (id, code, name, description)
VALUES ('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'SupplyRequest', 'Supply request', NULL)
ON CONFLICT (code) DO NOTHING;

-- Transitions for SupplyRequest
INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '10000000-0000-0000-0000-000000000001', rt.id, 'Draft', 'Submitted', 'Submit', 'Requests.Submit'
FROM requests.request_types rt
WHERE rt.code = 'SupplyRequest'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '10000000-0000-0000-0000-000000000002', rt.id, 'Submitted', 'InReview', 'StartReview', 'Requests.StartReview'
FROM requests.request_types rt
WHERE rt.code = 'SupplyRequest'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '10000000-0000-0000-0000-000000000003', rt.id, 'InReview', 'Approved', 'Approve', 'Requests.Approve'
FROM requests.request_types rt
WHERE rt.code = 'SupplyRequest'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '10000000-0000-0000-0000-000000000004', rt.id, 'InReview', 'Rejected', 'Reject', 'Requests.Reject'
FROM requests.request_types rt
WHERE rt.code = 'SupplyRequest'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '10000000-0000-0000-0000-000000000005', rt.id, 'Approved', 'InWork', 'StartWork', 'Requests.StartWork'
FROM requests.request_types rt
WHERE rt.code = 'SupplyRequest'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '10000000-0000-0000-0000-000000000006', rt.id, 'InWork', 'Done', 'Complete', 'Requests.Complete'
FROM requests.request_types rt
WHERE rt.code = 'SupplyRequest'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;

INSERT INTO requests.request_transitions (id, request_type_id, from_status_code, to_status_code, action_code, required_permission)
SELECT '10000000-0000-0000-0000-000000000007', rt.id, 'Done', 'Closed', 'Close', 'Requests.Close'
FROM requests.request_types rt
WHERE rt.code = 'SupplyRequest'
ON CONFLICT (request_type_id, from_status_code, action_code) DO NOTHING;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "request_lines",
                schema: "requests");

            migrationBuilder.DropTable(
                name: "request_transitions",
                schema: "requests");
        }
    }
}
