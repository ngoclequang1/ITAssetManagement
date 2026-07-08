using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ITAssetManagement.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ASSET_CATEGORY",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    category_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASSET_CATEGORY", x => x.category_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ASSET_LOCATION",
                columns: table => new
                {
                    location_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    location_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASSET_LOCATION", x => x.location_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ASSET_STATUS",
                columns: table => new
                {
                    status_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    status_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ASSET_STATUS", x => x.status_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DEPARTMENT",
                columns: table => new
                {
                    department_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    department_code = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    department_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    parent_department_id = table.Column<int>(type: "int", nullable: true),
                    deployment_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    top_deployment_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    overall_deployment = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_kitting_department = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEPARTMENT", x => x.department_id);
                    table.ForeignKey(
                        name: "FK_DEPARTMENT_DEPARTMENT_parent_department_id",
                        column: x => x.parent_department_id,
                        principalTable: "DEPARTMENT",
                        principalColumn: "department_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PERMISSION_ROLE",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    role_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PERMISSION_ROLE", x => x.role_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "request_history",
                columns: table => new
                {
                    history_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    request_id = table.Column<int>(type: "int", nullable: false),
                    status_id = table.Column<int>(type: "int", nullable: true),
                    user_created_id = table.Column<int>(type: "int", nullable: true),
                    action = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    action_by = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    action_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    remarks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_history", x => x.history_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "REQUEST_STATUS",
                columns: table => new
                {
                    status_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    status_name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REQUEST_STATUS", x => x.status_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "REQUEST_TYPE",
                columns: table => new
                {
                    type_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    type_name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REQUEST_TYPE", x => x.type_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "USERS",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_code = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    username = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    username_kana = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    username_alphabet = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    console_login_id = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    system_login_id = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    password_hash = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    primary_department_id = table.Column<int>(type: "int", nullable: true),
                    role_id = table.Column<int>(type: "int", nullable: true),
                    auditor_flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    last_login = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    reset_otp = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reset_otp_expiry = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERS", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_USERS_DEPARTMENT_primary_department_id",
                        column: x => x.primary_department_id,
                        principalTable: "DEPARTMENT",
                        principalColumn: "department_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USERS_PERMISSION_ROLE_role_id",
                        column: x => x.role_id,
                        principalTable: "PERMISSION_ROLE",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DEPARTMENT_REPRESENTATIVE",
                columns: table => new
                {
                    rep_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    department_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    is_primary_admin = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEPARTMENT_REPRESENTATIVE", x => x.rep_id);
                    table.ForeignKey(
                        name: "FK_DEPARTMENT_REPRESENTATIVE_DEPARTMENT_department_id",
                        column: x => x.department_id,
                        principalTable: "DEPARTMENT",
                        principalColumn: "department_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DEPARTMENT_REPRESENTATIVE_PERMISSION_ROLE_role_id",
                        column: x => x.role_id,
                        principalTable: "PERMISSION_ROLE",
                        principalColumn: "role_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DEPARTMENT_REPRESENTATIVE_USERS_user_id",
                        column: x => x.user_id,
                        principalTable: "USERS",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "INVENTORY_HISTORY",
                columns: table => new
                {
                    inventory_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    inventory_department_id = table.Column<int>(type: "int", nullable: false),
                    inventory_implementer = table.Column<int>(type: "int", nullable: false),
                    inventory_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    inventory_status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    remarks = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INVENTORY_HISTORY", x => x.inventory_id);
                    table.ForeignKey(
                        name: "FK_INVENTORY_HISTORY_DEPARTMENT_inventory_department_id",
                        column: x => x.inventory_department_id,
                        principalTable: "DEPARTMENT",
                        principalColumn: "department_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_INVENTORY_HISTORY_USERS_inventory_implementer",
                        column: x => x.inventory_implementer,
                        principalTable: "USERS",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LICENSE",
                columns: table => new
                {
                    license_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    license_management_number = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    license_key = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    license_count = table.Column<int>(type: "int", nullable: true),
                    expiry_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    installation_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    publisher_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    software_type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    license_type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    license_format = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    counting_method = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    academic_flag = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    number_of_licenses = table.Column<int>(type: "int", nullable: false),
                    number_available = table.Column<int>(type: "int", nullable: false),
                    management_department_id = table.Column<int>(type: "int", nullable: true),
                    manager_user_id = table.Column<int>(type: "int", nullable: true),
                    license_status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    disposal_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    parent_license_id = table.Column<int>(type: "int", nullable: true),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_by = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LICENSE", x => x.license_id);
                    table.ForeignKey(
                        name: "FK_LICENSE_DEPARTMENT_management_department_id",
                        column: x => x.management_department_id,
                        principalTable: "DEPARTMENT",
                        principalColumn: "department_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LICENSE_LICENSE_parent_license_id",
                        column: x => x.parent_license_id,
                        principalTable: "LICENSE",
                        principalColumn: "license_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LICENSE_USERS_manager_user_id",
                        column: x => x.manager_user_id,
                        principalTable: "USERS",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "USER_ITEMS",
                columns: table => new
                {
                    user_item_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    item_key = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    item_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_ITEMS", x => x.user_item_id);
                    table.ForeignKey(
                        name: "FK_USER_ITEMS_USERS_user_id",
                        column: x => x.user_id,
                        principalTable: "USERS",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "USER_PERMISSION",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false),
                    can_view = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    can_request = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    can_approve = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    can_admin = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USER_PERMISSION", x => x.user_id);
                    table.ForeignKey(
                        name: "FK_USER_PERMISSION_USERS_user_id",
                        column: x => x.user_id,
                        principalTable: "USERS",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "IT_ASSET",
                columns: table => new
                {
                    asset_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    asset_control_number = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    asset_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category_id = table.Column<int>(type: "int", nullable: true),
                    manufacturer = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    model = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    serial_number = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    purchase_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    warranty_expiry = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    department_id = table.Column<int>(type: "int", nullable: true),
                    asset_manager_id = table.Column<int>(type: "int", nullable: true),
                    user_created_id = table.Column<int>(type: "int", nullable: true),
                    user_used_id = table.Column<int>(type: "int", nullable: true),
                    status_id = table.Column<int>(type: "int", nullable: true),
                    location_id = table.Column<int>(type: "int", nullable: true),
                    inventory_id = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_by = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IT_ASSET", x => x.asset_id);
                    table.ForeignKey(
                        name: "FK_IT_ASSET_ASSET_CATEGORY_category_id",
                        column: x => x.category_id,
                        principalTable: "ASSET_CATEGORY",
                        principalColumn: "category_id");
                    table.ForeignKey(
                        name: "FK_IT_ASSET_ASSET_LOCATION_location_id",
                        column: x => x.location_id,
                        principalTable: "ASSET_LOCATION",
                        principalColumn: "location_id");
                    table.ForeignKey(
                        name: "FK_IT_ASSET_ASSET_STATUS_status_id",
                        column: x => x.status_id,
                        principalTable: "ASSET_STATUS",
                        principalColumn: "status_id");
                    table.ForeignKey(
                        name: "FK_IT_ASSET_DEPARTMENT_department_id",
                        column: x => x.department_id,
                        principalTable: "DEPARTMENT",
                        principalColumn: "department_id");
                    table.ForeignKey(
                        name: "FK_IT_ASSET_INVENTORY_HISTORY_inventory_id",
                        column: x => x.inventory_id,
                        principalTable: "INVENTORY_HISTORY",
                        principalColumn: "inventory_id");
                    table.ForeignKey(
                        name: "FK_IT_ASSET_USERS_asset_manager_id",
                        column: x => x.asset_manager_id,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_IT_ASSET_USERS_user_created_id",
                        column: x => x.user_created_id,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_IT_ASSET_USERS_user_used_id",
                        column: x => x.user_used_id,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LICENSE_INVENTORY_HISTORY",
                columns: table => new
                {
                    inventory_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    license_id = table.Column<int>(type: "int", nullable: false),
                    inventory_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    inventory_taker_id = table.Column<int>(type: "int", nullable: true),
                    inventory_status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    remarks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_by = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LICENSE_INVENTORY_HISTORY", x => x.inventory_id);
                    table.ForeignKey(
                        name: "FK_LICENSE_INVENTORY_HISTORY_LICENSE_license_id",
                        column: x => x.license_id,
                        principalTable: "LICENSE",
                        principalColumn: "license_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LICENSE_INVENTORY_HISTORY_USERS_inventory_taker_id",
                        column: x => x.inventory_taker_id,
                        principalTable: "USERS",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "REQUEST",
                columns: table => new
                {
                    request_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    request_type_id = table.Column<int>(type: "int", nullable: false),
                    user_created_id = table.Column<int>(type: "int", nullable: false),
                    asset_id = table.Column<int>(type: "int", nullable: true),
                    target_div = table.Column<int>(type: "int", nullable: true),
                    status_id = table.Column<int>(type: "int", nullable: false),
                    request_description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    request_data = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    first_approver_id = table.Column<int>(type: "int", nullable: true),
                    second_approver_id = table.Column<int>(type: "int", nullable: true),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    rejected_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    reject_reason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_by = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REQUEST", x => x.request_id);
                    table.ForeignKey(
                        name: "FK_REQUEST_IT_ASSET_asset_id",
                        column: x => x.asset_id,
                        principalTable: "IT_ASSET",
                        principalColumn: "asset_id");
                    table.ForeignKey(
                        name: "FK_REQUEST_REQUEST_STATUS_status_id",
                        column: x => x.status_id,
                        principalTable: "REQUEST_STATUS",
                        principalColumn: "status_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_REQUEST_REQUEST_TYPE_request_type_id",
                        column: x => x.request_type_id,
                        principalTable: "REQUEST_TYPE",
                        principalColumn: "type_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_REQUEST_USERS_user_created_id",
                        column: x => x.user_created_id,
                        principalTable: "USERS",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SOFTWARE",
                columns: table => new
                {
                    software_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    software_name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    software_version = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    vendor_id = table.Column<int>(type: "int", nullable: true),
                    license_id = table.Column<int>(type: "int", nullable: true),
                    license_type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    group_id = table.Column<int>(type: "int", nullable: true),
                    asset_id = table.Column<int>(type: "int", nullable: true),
                    asset_control_number = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    installed_by = table.Column<int>(type: "int", nullable: true),
                    installed_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    software_type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SOFTWARE", x => x.software_id);
                    table.ForeignKey(
                        name: "FK_SOFTWARE_IT_ASSET_asset_id",
                        column: x => x.asset_id,
                        principalTable: "IT_ASSET",
                        principalColumn: "asset_id");
                    table.ForeignKey(
                        name: "FK_SOFTWARE_USERS_installed_by",
                        column: x => x.installed_by,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "request_approval",
                columns: table => new
                {
                    approval_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    request_id = table.Column<int>(type: "int", nullable: false),
                    approver_id = table.Column<int>(type: "int", nullable: false),
                    approval_level = table.Column<int>(type: "int", nullable: false),
                    status_id = table.Column<int>(type: "int", nullable: false),
                    approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Remarks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_approval", x => x.approval_id);
                    table.ForeignKey(
                        name: "FK_request_approval_REQUEST_request_id",
                        column: x => x.request_id,
                        principalTable: "REQUEST",
                        principalColumn: "request_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "REQUEST_ASSET",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    request_id = table.Column<int>(type: "int", nullable: false),
                    asset_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REQUEST_ASSET", x => x.id);
                    table.ForeignKey(
                        name: "FK_REQUEST_ASSET_IT_ASSET_asset_id",
                        column: x => x.asset_id,
                        principalTable: "IT_ASSET",
                        principalColumn: "asset_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_REQUEST_ASSET_REQUEST_request_id",
                        column: x => x.request_id,
                        principalTable: "REQUEST",
                        principalColumn: "request_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "REQUEST_DETAIL",
                columns: table => new
                {
                    detail_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    request_id = table.Column<int>(type: "int", nullable: false),
                    field_name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    old_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_value = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REQUEST_DETAIL", x => x.detail_id);
                    table.ForeignKey(
                        name: "FK_REQUEST_DETAIL_REQUEST_request_id",
                        column: x => x.request_id,
                        principalTable: "REQUEST",
                        principalColumn: "request_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_parent_department_id",
                table: "DEPARTMENT",
                column: "parent_department_id");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_REPRESENTATIVE_department_id",
                table: "DEPARTMENT_REPRESENTATIVE",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_REPRESENTATIVE_role_id",
                table: "DEPARTMENT_REPRESENTATIVE",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_REPRESENTATIVE_user_id",
                table: "DEPARTMENT_REPRESENTATIVE",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_HISTORY_inventory_department_id",
                table: "INVENTORY_HISTORY",
                column: "inventory_department_id");

            migrationBuilder.CreateIndex(
                name: "IX_INVENTORY_HISTORY_inventory_implementer",
                table: "INVENTORY_HISTORY",
                column: "inventory_implementer");

            migrationBuilder.CreateIndex(
                name: "IX_IT_ASSET_asset_manager_id",
                table: "IT_ASSET",
                column: "asset_manager_id");

            migrationBuilder.CreateIndex(
                name: "IX_IT_ASSET_category_id",
                table: "IT_ASSET",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_IT_ASSET_department_id",
                table: "IT_ASSET",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_IT_ASSET_inventory_id",
                table: "IT_ASSET",
                column: "inventory_id");

            migrationBuilder.CreateIndex(
                name: "IX_IT_ASSET_location_id",
                table: "IT_ASSET",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_IT_ASSET_status_id",
                table: "IT_ASSET",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_IT_ASSET_user_created_id",
                table: "IT_ASSET",
                column: "user_created_id");

            migrationBuilder.CreateIndex(
                name: "IX_IT_ASSET_user_used_id",
                table: "IT_ASSET",
                column: "user_used_id");

            migrationBuilder.CreateIndex(
                name: "IX_LICENSE_management_department_id",
                table: "LICENSE",
                column: "management_department_id");

            migrationBuilder.CreateIndex(
                name: "IX_LICENSE_manager_user_id",
                table: "LICENSE",
                column: "manager_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_LICENSE_parent_license_id",
                table: "LICENSE",
                column: "parent_license_id");

            migrationBuilder.CreateIndex(
                name: "IX_LICENSE_INVENTORY_HISTORY_inventory_taker_id",
                table: "LICENSE_INVENTORY_HISTORY",
                column: "inventory_taker_id");

            migrationBuilder.CreateIndex(
                name: "IX_LICENSE_INVENTORY_HISTORY_license_id",
                table: "LICENSE_INVENTORY_HISTORY",
                column: "license_id");

            migrationBuilder.CreateIndex(
                name: "IX_REQUEST_asset_id",
                table: "REQUEST",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_REQUEST_request_type_id",
                table: "REQUEST",
                column: "request_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_REQUEST_status_id",
                table: "REQUEST",
                column: "status_id");

            migrationBuilder.CreateIndex(
                name: "IX_REQUEST_user_created_id",
                table: "REQUEST",
                column: "user_created_id");

            migrationBuilder.CreateIndex(
                name: "IX_request_approval_request_id",
                table: "request_approval",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_REQUEST_ASSET_asset_id",
                table: "REQUEST_ASSET",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_REQUEST_ASSET_request_id",
                table: "REQUEST_ASSET",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_REQUEST_DETAIL_request_id",
                table: "REQUEST_DETAIL",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_SOFTWARE_asset_id",
                table: "SOFTWARE",
                column: "asset_id");

            migrationBuilder.CreateIndex(
                name: "IX_SOFTWARE_installed_by",
                table: "SOFTWARE",
                column: "installed_by");

            migrationBuilder.CreateIndex(
                name: "IX_USER_ITEMS_user_id",
                table: "USER_ITEMS",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_USERS_primary_department_id",
                table: "USERS",
                column: "primary_department_id");

            migrationBuilder.CreateIndex(
                name: "IX_USERS_role_id",
                table: "USERS",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DEPARTMENT_REPRESENTATIVE");

            migrationBuilder.DropTable(
                name: "LICENSE_INVENTORY_HISTORY");

            migrationBuilder.DropTable(
                name: "request_approval");

            migrationBuilder.DropTable(
                name: "REQUEST_ASSET");

            migrationBuilder.DropTable(
                name: "REQUEST_DETAIL");

            migrationBuilder.DropTable(
                name: "request_history");

            migrationBuilder.DropTable(
                name: "SOFTWARE");

            migrationBuilder.DropTable(
                name: "USER_ITEMS");

            migrationBuilder.DropTable(
                name: "USER_PERMISSION");

            migrationBuilder.DropTable(
                name: "LICENSE");

            migrationBuilder.DropTable(
                name: "REQUEST");

            migrationBuilder.DropTable(
                name: "IT_ASSET");

            migrationBuilder.DropTable(
                name: "REQUEST_STATUS");

            migrationBuilder.DropTable(
                name: "REQUEST_TYPE");

            migrationBuilder.DropTable(
                name: "ASSET_CATEGORY");

            migrationBuilder.DropTable(
                name: "ASSET_LOCATION");

            migrationBuilder.DropTable(
                name: "ASSET_STATUS");

            migrationBuilder.DropTable(
                name: "INVENTORY_HISTORY");

            migrationBuilder.DropTable(
                name: "USERS");

            migrationBuilder.DropTable(
                name: "DEPARTMENT");

            migrationBuilder.DropTable(
                name: "PERMISSION_ROLE");
        }
    }
}
