using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onboarding.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRelationshipAggregates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "rel_cedente_tipo_ativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    cedente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_ativo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    limite_percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    limite_valor = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    data_inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data_fim = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rel_cedente_tipo_ativo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rel_cedente_tipo_ativo_cedentes_cedente_id",
                        column: x => x.cedente_id,
                        principalTable: "cedentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rel_cedente_tipo_ativo_tipos_ativo_tipo_ativo_id",
                        column: x => x.tipo_ativo_id,
                        principalTable: "tipos_ativo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rel_fundo_cedente",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    fundo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cedente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    limite_percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    limite_valor = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    data_inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data_fim = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rel_fundo_cedente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rel_fundo_cedente_cedentes_cedente_id",
                        column: x => x.cedente_id,
                        principalTable: "cedentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rel_fundo_cedente_fundos_fundo_id",
                        column: x => x.fundo_id,
                        principalTable: "fundos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rel_fundo_tipo_ativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    fundo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_ativo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    limite_percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    limite_valor = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    data_inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data_fim = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rel_fundo_tipo_ativo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_rel_fundo_tipo_ativo_fundos_fundo_id",
                        column: x => x.fundo_id,
                        principalTable: "fundos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_rel_fundo_tipo_ativo_tipos_ativo_tipo_ativo_id",
                        column: x => x.tipo_ativo_id,
                        principalTable: "tipos_ativo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_rel_cedente_tipo_ativo_active",
                table: "rel_cedente_tipo_ativo",
                columns: new[] { "cedente_id", "tipo_ativo_id" },
                unique: true,
                filter: "status = 'ATIVO'");

            migrationBuilder.CreateIndex(
                name: "IX_rel_cedente_tipo_ativo_cedente_id",
                table: "rel_cedente_tipo_ativo",
                column: "cedente_id");

            migrationBuilder.CreateIndex(
                name: "IX_rel_cedente_tipo_ativo_tipo_ativo_id",
                table: "rel_cedente_tipo_ativo",
                column: "tipo_ativo_id");

            migrationBuilder.CreateIndex(
                name: "IX_rel_fundo_cedente_active",
                table: "rel_fundo_cedente",
                columns: new[] { "fundo_id", "cedente_id" },
                unique: true,
                filter: "status = 'ATIVO'");

            migrationBuilder.CreateIndex(
                name: "IX_rel_fundo_cedente_cedente_id",
                table: "rel_fundo_cedente",
                column: "cedente_id");

            migrationBuilder.CreateIndex(
                name: "IX_rel_fundo_cedente_fundo_id",
                table: "rel_fundo_cedente",
                column: "fundo_id");

            migrationBuilder.CreateIndex(
                name: "IX_rel_fundo_tipo_ativo_active",
                table: "rel_fundo_tipo_ativo",
                columns: new[] { "fundo_id", "tipo_ativo_id" },
                unique: true,
                filter: "status = 'ATIVO'");

            migrationBuilder.CreateIndex(
                name: "IX_rel_fundo_tipo_ativo_fundo_id",
                table: "rel_fundo_tipo_ativo",
                column: "fundo_id");

            migrationBuilder.CreateIndex(
                name: "IX_rel_fundo_tipo_ativo_tipo_ativo_id",
                table: "rel_fundo_tipo_ativo",
                column: "tipo_ativo_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "rel_cedente_tipo_ativo");

            migrationBuilder.DropTable(
                name: "rel_fundo_cedente");

            migrationBuilder.DropTable(
                name: "rel_fundo_tipo_ativo");
        }
    }
}
