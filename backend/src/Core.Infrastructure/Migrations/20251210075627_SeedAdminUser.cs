using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyIS.Core.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminUser : Migration
    {
                /// <inheritdoc />
                protected override void Up(MigrationBuilder migrationBuilder)
                {
                    migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS \"uuid-ossp\";");

                    migrationBuilder.EnsureSchema(
                        name: "core");
        
                    migrationBuilder.CreateTable(
                        name: "roles",
                        schema: "core",
                        columns: table => new
                        {
                            id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                            code = table.Column<string>(type: "text", nullable: false),
                            name = table.Column<string>(type: "text", nullable: false),
                            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                        },
                        constraints: table =>
                        {
                            table.PrimaryKey("PK_roles", x => x.id);
                        });
        
                    migrationBuilder.CreateTable(
                        name: "users",
                        schema: "core",
                        columns: table => new
                        {
                            id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                            login = table.Column<string>(type: "text", nullable: false),
                            password_hash = table.Column<string>(type: "text", nullable: false),
                            full_name = table.Column<string>(type: "text", nullable: true),
                            is_active = table.Column<bool>(type: "boolean", nullable: false),
                            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                            updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                        },
                        constraints: table =>
                        {
                            table.PrimaryKey("PK_users", x => x.id);
                        });
        
                    migrationBuilder.CreateTable(
                        name: "user_roles",
                        schema: "core",
                        columns: table => new
                        {
                            user_id = table.Column<Guid>(type: "uuid", nullable: false),
                            role_id = table.Column<Guid>(type: "uuid", nullable: false),
                            assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                            created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                        },
                        constraints: table =>
                        {
                            table.PrimaryKey("PK_user_roles", x => new { x.user_id, x.role_id });
                            table.ForeignKey(
                                name: "FK_user_roles_roles_role_id",
                                column: x => x.role_id,
                                principalSchema: "core",
                                principalTable: "roles",
                                principalColumn: "id",
                                onDelete: ReferentialAction.Cascade);
                            table.ForeignKey(
                                name: "FK_user_roles_users_user_id",
                                column: x => x.user_id,
                                principalSchema: "core",
                                principalTable: "users",
                                principalColumn: "id",
                                onDelete: ReferentialAction.Cascade);
                        });
        
                    migrationBuilder.CreateIndex(
                        name: "IX_roles_code",
                        schema: "core",
                        table: "roles",
                        column: "code",
                        unique: true);
        
                    migrationBuilder.CreateIndex(
                        name: "IX_user_roles_role_id",
                        schema: "core",
                        table: "user_roles",
                        column: "role_id");
        
                    migrationBuilder.CreateIndex(
                        name: "IX_users_login",
                        schema: "core",
                        table: "users",
                        column: "login",
                        unique: true);
        
                    // Seed ADMIN role and Admin user with bcrypt password hash for "admin"
                    const string adminPasswordHash = "$2a$11$IetpaomqTX7/ijhg8gyhbe89KV7CYzgpfsUKumcQZMbVYyuOg22c.";
        
                    // Role ADMIN
                    migrationBuilder.Sql(@"
                        INSERT INTO core.roles (id, code, name, created_at)
                        SELECT '11111111-1111-1111-1111-111111111111'::uuid, 'ADMIN', 'Administrator', NOW()
                        WHERE NOT EXISTS (
                            SELECT 1 FROM core.roles WHERE code = 'ADMIN'
                        );
                    ");
        
                    // User Admin
                    migrationBuilder.Sql(@"
                        INSERT INTO core.users (id, login, full_name, is_active, password_hash, created_at, updated_at)
                        SELECT '22222222-2222-2222-2222-222222222222'::uuid, 'Admin', 'Administrator', TRUE, '" + adminPasswordHash + @"', NOW(), NOW()
                        WHERE NOT EXISTS (
                            SELECT 1 FROM core.users WHERE login = 'Admin'
                        );
                    ");
        
                    // User-Role link
                    migrationBuilder.Sql(@"
                        INSERT INTO core.user_roles (user_id, role_id, assigned_at, created_at)
                        SELECT '22222222-2222-2222-2222-222222222222'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, NOW(), NOW()
                        WHERE NOT EXISTS (
                            SELECT 1 FROM core.user_roles WHERE user_id = '22222222-2222-2222-2222-222222222222'::uuid AND role_id = '11111111-1111-1111-1111-111111111111'::uuid
                        );
                    ");
                }

                /// <inheritdoc />
                protected override void Down(MigrationBuilder migrationBuilder)
                {
                    migrationBuilder.Sql(@"
                        DELETE FROM core.user_roles
                        WHERE user_id = '22222222-2222-2222-2222-222222222222'::uuid
                          AND role_id = '11111111-1111-1111-1111-111111111111'::uuid;
                    ");
        
                    migrationBuilder.Sql(@"
                        DELETE FROM core.users
                        WHERE id = '22222222-2222-2222-2222-222222222222'::uuid
                          AND login = 'Admin';
                    ");
        
                    migrationBuilder.Sql(@"
                        DELETE FROM core.roles
                        WHERE id = '11111111-1111-1111-1111-111111111111'::uuid
                          AND code = 'ADMIN';
                    ");
        
                    migrationBuilder.DropTable(
                        name: "user_roles",
                        schema: "core");
        
                    migrationBuilder.DropTable(
                        name: "roles",
                        schema: "core");
        
                    migrationBuilder.DropTable(
                        name: "users",
                        schema: "core");
                }
    }
}
