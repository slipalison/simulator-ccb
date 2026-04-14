using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onboarding.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    admin_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    admin_user_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    action_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    target_user_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_audit_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_action_type",
                table: "admin_audit_logs",
                column: "action_type");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_admin_user_id",
                table: "admin_audit_logs",
                column: "admin_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_admin_audit_logs_timestamp",
                table: "admin_audit_logs",
                column: "timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_audit_logs");
        }
    }
}
