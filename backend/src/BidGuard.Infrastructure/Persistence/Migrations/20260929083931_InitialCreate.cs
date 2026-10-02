using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BidGuard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    prev_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    legal_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    pan = table.Column<string>(type: "character(10)", fixedLength: true, maxLength: 10, nullable: false),
                    gstin = table.Column<string>(type: "character(15)", fixedLength: true, maxLength: 15, nullable: true),
                    udyam_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    cin = table.Column<string>(type: "character(21)", fixedLength: true, maxLength: 21, nullable: true),
                    epfo_code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_startup = table.Column<bool>(type: "boolean", nullable: false),
                    dpiit_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    doc_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ocr_used = table.Column<bool>(type: "boolean", nullable: false),
                    extraction_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_documents_users_uploaded_by_id",
                        column: x => x.uploaded_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    reference_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "credentials",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    issued_on = table.Column<DateOnly>(type: "date", nullable: true),
                    expires_on = table.Column<DateOnly>(type: "date", nullable: true),
                    document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_expiry_alert_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_credentials", x => x.id);
                    table.ForeignKey(
                        name: "fk_credentials_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_credentials_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tenders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    gem_bid_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    estimated_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    emd_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    bid_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tender_document_id = table.Column<Guid>(type: "uuid", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tenders", x => x.id);
                    table.ForeignKey(
                        name: "fk_tenders_documents_tender_document_id",
                        column: x => x.tender_document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_tenders_users_created_by_id",
                        column: x => x.created_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bids",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_simulation = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    overall_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    suggested_decision = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    decision = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    decision_remarks = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    decided_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bids", x => x.id);
                    table.ForeignKey(
                        name: "fk_bids_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bids_tenders_tender_id",
                        column: x => x.tender_id,
                        principalTable: "tenders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_bids_users_decided_by_id",
                        column: x => x.decided_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "compliance_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    weight = table.Column<decimal>(type: "numeric(9,2)", precision: 9, scale: 2, nullable: false),
                    expression = table.Column<string>(type: "jsonb", nullable: false),
                    source_clause = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    source_page = table.Column<int>(type: "integer", nullable: true),
                    exemption_kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_compliance_rules", x => x.id);
                    table.ForeignKey(
                        name: "fk_compliance_rules_tenders_tender_id",
                        column: x => x.tender_id,
                        principalTable: "tenders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "extracted_fields",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bid_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    confidence = table.Column<decimal>(type: "numeric(4,3)", precision: 4, scale: 3, nullable: false),
                    page = table.Column<int>(type: "integer", nullable: true),
                    bounding_box = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_extracted_fields", x => x.id);
                    table.ForeignKey(
                        name: "fk_extracted_fields_bids_bid_id",
                        column: x => x.bid_id,
                        principalTable: "bids",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_extracted_fields_documents_document_id",
                        column: x => x.document_id,
                        principalTable: "documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "verification_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bid_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    identifier = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    registered_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    details = table.Column<string>(type: "jsonb", nullable: true),
                    checked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_verification_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_verification_results_bids_bid_id",
                        column: x => x.bid_id,
                        principalTable: "bids",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rule_evaluations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    bid_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    explanation = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    evidence_json = table.Column<string>(type: "jsonb", nullable: false),
                    officer_override = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    override_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    reviewed_by_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rule_evaluations", x => x.id);
                    table.ForeignKey(
                        name: "fk_rule_evaluations_bids_bid_id",
                        column: x => x.bid_id,
                        principalTable: "bids",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rule_evaluations_compliance_rules_rule_id",
                        column: x => x.rule_id,
                        principalTable: "compliance_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rule_evaluations_users_reviewed_by_id",
                        column: x => x.reviewed_by_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_hash",
                table: "audit_logs",
                column: "hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_sequence",
                table: "audit_logs",
                column: "sequence",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bids_decided_by_id",
                table: "bids",
                column: "decided_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_bids_organization_id",
                table: "bids",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_bids_tender_id_is_simulation",
                table: "bids",
                columns: new[] { "tender_id", "is_simulation" });

            migrationBuilder.CreateIndex(
                name: "ix_compliance_rules_tender_id_code",
                table: "compliance_rules",
                columns: new[] { "tender_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_credentials_document_id",
                table: "credentials",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_expires_on",
                table: "credentials",
                column: "expires_on");

            migrationBuilder.CreateIndex(
                name: "ix_credentials_organization_id_type",
                table: "credentials",
                columns: new[] { "organization_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_documents_owner_type_owner_id",
                table: "documents",
                columns: new[] { "owner_type", "owner_id" });

            migrationBuilder.CreateIndex(
                name: "ix_documents_storage_key",
                table: "documents",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_documents_uploaded_by_id",
                table: "documents",
                column: "uploaded_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_extracted_fields_bid_id_key",
                table: "extracted_fields",
                columns: new[] { "bid_id", "key" });

            migrationBuilder.CreateIndex(
                name: "ix_extracted_fields_document_id",
                table: "extracted_fields",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "ix_notifications_user_id_is_read",
                table: "notifications",
                columns: new[] { "user_id", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_organizations_pan",
                table: "organizations",
                column: "pan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id_revoked_at",
                table: "refresh_tokens",
                columns: new[] { "user_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_rule_evaluations_bid_id_rule_id",
                table: "rule_evaluations",
                columns: new[] { "bid_id", "rule_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rule_evaluations_reviewed_by_id",
                table: "rule_evaluations",
                column: "reviewed_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_rule_evaluations_rule_id",
                table: "rule_evaluations",
                column: "rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenders_created_by_id",
                table: "tenders",
                column: "created_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenders_gem_bid_number",
                table: "tenders",
                column: "gem_bid_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenders_status",
                table: "tenders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_tenders_tender_document_id",
                table: "tenders",
                column: "tender_document_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_organization_id",
                table: "users",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_verification_results_bid_id_source",
                table: "verification_results",
                columns: new[] { "bid_id", "source" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "credentials");

            migrationBuilder.DropTable(
                name: "extracted_fields");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "rule_evaluations");

            migrationBuilder.DropTable(
                name: "verification_results");

            migrationBuilder.DropTable(
                name: "compliance_rules");

            migrationBuilder.DropTable(
                name: "bids");

            migrationBuilder.DropTable(
                name: "tenders");

            migrationBuilder.DropTable(
                name: "documents");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "organizations");
        }
    }
}
