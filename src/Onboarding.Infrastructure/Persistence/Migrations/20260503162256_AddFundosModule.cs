using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Onboarding.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFundosModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cedentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    documento = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    endereco = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cnpj_cedente = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    cpf = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: true),
                    documento_tipo = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cedentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cedentes_companies_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "consultoria_fundos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    razao_social = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nome_fantasia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consultoria_fundos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_consultoria_fundos_companies_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "custodiantes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    razao_social = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    codigo_interno = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    telefone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_custodiantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_custodiantes_companies_cliente_id",
                        column: x => x.cliente_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tipos_ativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    categoria = table.Column<int>(type: "integer", nullable: false),
                    subcategoria = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    ordem_exibicao = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tipos_ativo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "fundos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cnpj = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: false),
                    nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    consultoria_fundo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    custodiante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_fundo = table.Column<int>(type: "integer", nullable: false),
                    classe_anbima = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    segmento = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    data_constituicao = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fundos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fundos_consultoria_fundos_consultoria_fundo_id",
                        column: x => x.consultoria_fundo_id,
                        principalTable: "consultoria_fundos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fundos_custodiantes_custodiante_id",
                        column: x => x.custodiante_id,
                        principalTable: "custodiantes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cedente_tipos_ativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    cedente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_ativo_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cedente_tipos_ativo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cedente_tipos_ativo_cedentes_cedente_id",
                        column: x => x.cedente_id,
                        principalTable: "cedentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cedente_tipos_ativo_tipos_ativo_tipo_ativo_id",
                        column: x => x.tipo_ativo_id,
                        principalTable: "tipos_ativo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "fundo_cedentes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    fundo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cedente_id = table.Column<Guid>(type: "uuid", nullable: false),
                    limite_exposicao_percentual = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    limite_exposicao_valor = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    data_inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    data_fim = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fundo_cedentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fundo_cedentes_cedentes_cedente_id",
                        column: x => x.cedente_id,
                        principalTable: "cedentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_fundo_cedentes_fundos_fundo_id",
                        column: x => x.fundo_id,
                        principalTable: "fundos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fundo_tipos_ativo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    fundo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo_ativo_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_fundo_tipos_ativo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_fundo_tipos_ativo_fundos_fundo_id",
                        column: x => x.fundo_id,
                        principalTable: "fundos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_fundo_tipos_ativo_tipos_ativo_tipo_ativo_id",
                        column: x => x.tipo_ativo_id,
                        principalTable: "tipos_ativo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cedente_tipos_ativo_cedente_id_tipo_ativo_id",
                table: "cedente_tipos_ativo",
                columns: new[] { "cedente_id", "tipo_ativo_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cedente_tipos_ativo_tipo_ativo_id",
                table: "cedente_tipos_ativo",
                column: "tipo_ativo_id");

            migrationBuilder.CreateIndex(
                name: "IX_cedentes_cliente_id",
                table: "cedentes",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_cedentes_cliente_id_cnpj_cedente",
                table: "cedentes",
                columns: new[] { "cliente_id", "cnpj_cedente" },
                unique: true,
                filter: "documento_tipo = 'PJ' AND cnpj_cedente IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_cedentes_cliente_id_cpf",
                table: "cedentes",
                columns: new[] { "cliente_id", "cpf" },
                unique: true,
                filter: "documento_tipo = 'PF' AND cpf IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_consultoria_fundos_cliente_id",
                table: "consultoria_fundos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_consultoria_fundos_cnpj",
                table: "consultoria_fundos",
                column: "cnpj",
                unique: true,
                filter: "cnpj IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_custodiantes_cliente_id",
                table: "custodiantes",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_custodiantes_cnpj",
                table: "custodiantes",
                column: "cnpj",
                unique: true,
                filter: "cnpj IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_fundo_cedentes_cedente_id",
                table: "fundo_cedentes",
                column: "cedente_id");

            migrationBuilder.CreateIndex(
                name: "IX_fundo_cedentes_fundo_id_cedente_id_active",
                table: "fundo_cedentes",
                columns: new[] { "fundo_id", "cedente_id" },
                unique: true,
                filter: "status = 1");

            migrationBuilder.CreateIndex(
                name: "IX_fundo_tipos_ativo_fundo_id_tipo_ativo_id",
                table: "fundo_tipos_ativo",
                columns: new[] { "fundo_id", "tipo_ativo_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_fundo_tipos_ativo_tipo_ativo_id",
                table: "fundo_tipos_ativo",
                column: "tipo_ativo_id");

            migrationBuilder.CreateIndex(
                name: "IX_fundos_cliente_id",
                table: "fundos",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_fundos_cnpj",
                table: "fundos",
                column: "cnpj",
                unique: true,
                filter: "cnpj IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_fundos_consultoria_fundo_id",
                table: "fundos",
                column: "consultoria_fundo_id");

            migrationBuilder.CreateIndex(
                name: "IX_fundos_custodiante_id",
                table: "fundos",
                column: "custodiante_id");

            migrationBuilder.CreateIndex(
                name: "IX_tipos_ativo_codigo",
                table: "tipos_ativo",
                column: "codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cedente_tipos_ativo");

            migrationBuilder.DropTable(
                name: "fundo_cedentes");

            migrationBuilder.DropTable(
                name: "fundo_tipos_ativo");

            migrationBuilder.DropTable(
                name: "cedentes");

            migrationBuilder.DropTable(
                name: "fundos");

            migrationBuilder.DropTable(
                name: "tipos_ativo");

            migrationBuilder.DropTable(
                name: "consultoria_fundos");

            migrationBuilder.DropTable(
                name: "custodiantes");
        }
    }
}
