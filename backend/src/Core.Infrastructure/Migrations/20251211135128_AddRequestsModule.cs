using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "requests");

            migrationBuilder.CreateTable(
                name: "request_statuses",
                schema: "requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_final = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_statuses", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "request_types",
                schema: "requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_types", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "requests",
                schema: "requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    request_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_status_id = table.Column<Guid>(type: "uuid", nullable: false),
                    initiator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    related_entity_type = table.Column<string>(type: "text", nullable: true),
                    related_entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_reference_id = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    due_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_requests_request_statuses_request_status_id",
                        column: x => x.request_status_id,
                        principalSchema: "requests",
                        principalTable: "request_statuses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_requests_request_types_request_type_id",
                        column: x => x.request_type_id,
                        principalSchema: "requests",
                        principalTable: "request_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_requests_users_initiator_id",
                        column: x => x.initiator_id,
                        principalSchema: "core",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "request_attachments",
                schema: "requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "text", nullable: false),
                    file_path = table.Column<string>(type: "text", nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_attachments", x => x.id);
                    table.ForeignKey(
                        name: "FK_request_attachments_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "requests",
                        principalTable: "requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_request_attachments_users_uploaded_by",
                        column: x => x.uploaded_by,
                        principalSchema: "core",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "request_comments",
                schema: "requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_comments", x => x.id);
                    table.ForeignKey(
                        name: "FK_request_comments_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "requests",
                        principalTable: "requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_request_comments_users_author_id",
                        column: x => x.author_id,
                        principalSchema: "core",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "request_history",
                schema: "requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    performed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    old_value = table.Column<string>(type: "text", nullable: false),
                    new_value = table.Column<string>(type: "text", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_history", x => x.id);
                    table.ForeignKey(
                        name: "FK_request_history_requests_request_id",
                        column: x => x.request_id,
                        principalSchema: "requests",
                        principalTable: "requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_request_history_users_performed_by",
                        column: x => x.performed_by,
                        principalSchema: "core",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_request_attachments_request_id",
                schema: "requests",
                table: "request_attachments",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_attachments_uploaded_by",
                schema: "requests",
                table: "request_attachments",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "IX_request_comments_author_id",
                schema: "requests",
                table: "request_comments",
                column: "author_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_comments_request_id",
                schema: "requests",
                table: "request_comments",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_history_performed_by",
                schema: "requests",
                table: "request_history",
                column: "performed_by");

            migrationBuilder.CreateIndex(
                name: "IX_request_history_request_id",
                schema: "requests",
                table: "request_history",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_statuses_code",
                schema: "requests",
                table: "request_statuses",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_request_types_code",
                schema: "requests",
                table: "request_types",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_requests_initiator_id",
                schema: "requests",
                table: "requests",
                column: "initiator_id");

            migrationBuilder.CreateIndex(
                name: "IX_requests_request_status_id",
                schema: "requests",
                table: "requests",
                column: "request_status_id");

            migrationBuilder.CreateIndex(
                name: "IX_requests_request_type_id",
                schema: "requests",
                table: "requests",
                column: "request_type_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "request_attachments",
                schema: "requests");

            migrationBuilder.DropTable(
                name: "request_comments",
                schema: "requests");

            migrationBuilder.DropTable(
                name: "request_history",
                schema: "requests");

            migrationBuilder.DropTable(
                name: "requests",
                schema: "requests");

            migrationBuilder.DropTable(
                name: "request_statuses",
                schema: "requests");

            migrationBuilder.DropTable(
                name: "request_types",
                schema: "requests");
        }
    }
}
