CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `ASSET_CATEGORY` (
    `category_id` int NOT NULL AUTO_INCREMENT,
    `category_name` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_ASSET_CATEGORY` PRIMARY KEY (`category_id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `ASSET_LOCATION` (
    `location_id` int NOT NULL AUTO_INCREMENT,
    `location_name` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_ASSET_LOCATION` PRIMARY KEY (`location_id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `ASSET_STATUS` (
    `status_id` int NOT NULL AUTO_INCREMENT,
    `status_name` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_ASSET_STATUS` PRIMARY KEY (`status_id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `DEPARTMENT` (
    `department_id` int NOT NULL AUTO_INCREMENT,
    `department_code` longtext CHARACTER SET utf8mb4 NOT NULL,
    `department_name` longtext CHARACTER SET utf8mb4 NULL,
    `parent_department_id` int NULL,
    `deployment_name` longtext CHARACTER SET utf8mb4 NULL,
    `top_deployment_name` longtext CHARACTER SET utf8mb4 NULL,
    `overall_deployment` tinyint(1) NOT NULL,
    `is_kitting_department` tinyint(1) NOT NULL,
    `is_active` tinyint(1) NOT NULL,
    `is_deleted` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_DEPARTMENT` PRIMARY KEY (`department_id`),
    CONSTRAINT `FK_DEPARTMENT_DEPARTMENT_parent_department_id` FOREIGN KEY (`parent_department_id`) REFERENCES `DEPARTMENT` (`department_id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `PERMISSION_ROLE` (
    `role_id` int NOT NULL AUTO_INCREMENT,
    `role_name` longtext CHARACTER SET utf8mb4 NULL,
    `description` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_PERMISSION_ROLE` PRIMARY KEY (`role_id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `request_history` (
    `history_id` int NOT NULL AUTO_INCREMENT,
    `request_id` int NOT NULL,
    `status_id` int NULL,
    `user_created_id` int NULL,
    `action` longtext CHARACTER SET utf8mb4 NOT NULL,
    `action_by` longtext CHARACTER SET utf8mb4 NOT NULL,
    `action_at` datetime(6) NOT NULL,
    `note` longtext CHARACTER SET utf8mb4 NULL,
    `remarks` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_request_history` PRIMARY KEY (`history_id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `REQUEST_STATUS` (
    `status_id` int NOT NULL AUTO_INCREMENT,
    `status_name` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_REQUEST_STATUS` PRIMARY KEY (`status_id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `REQUEST_TYPE` (
    `type_id` int NOT NULL AUTO_INCREMENT,
    `type_name` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_REQUEST_TYPE` PRIMARY KEY (`type_id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `USERS` (
    `user_id` int NOT NULL AUTO_INCREMENT,
    `user_code` longtext CHARACTER SET utf8mb4 NULL,
    `username` longtext CHARACTER SET utf8mb4 NULL,
    `username_kana` longtext CHARACTER SET utf8mb4 NULL,
    `username_alphabet` longtext CHARACTER SET utf8mb4 NULL,
    `email` longtext CHARACTER SET utf8mb4 NULL,
    `console_login_id` longtext CHARACTER SET utf8mb4 NULL,
    `system_login_id` longtext CHARACTER SET utf8mb4 NULL,
    `password_hash` longtext CHARACTER SET utf8mb4 NULL,
    `primary_department_id` int NULL,
    `role_id` int NULL,
    `auditor_flag` tinyint(1) NOT NULL,
    `last_login` datetime(6) NULL,
    `created_at` datetime(6) NOT NULL,
    `reset_otp` longtext CHARACTER SET utf8mb4 NULL,
    `reset_otp_expiry` datetime(6) NULL,
    `is_deleted` tinyint(1) NOT NULL,
    CONSTRAINT `PK_USERS` PRIMARY KEY (`user_id`),
    CONSTRAINT `FK_USERS_DEPARTMENT_primary_department_id` FOREIGN KEY (`primary_department_id`) REFERENCES `DEPARTMENT` (`department_id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_USERS_PERMISSION_ROLE_role_id` FOREIGN KEY (`role_id`) REFERENCES `PERMISSION_ROLE` (`role_id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `DEPARTMENT_REPRESENTATIVE` (
    `rep_id` int NOT NULL AUTO_INCREMENT,
    `department_id` int NOT NULL,
    `user_id` int NOT NULL,
    `role_id` int NOT NULL,
    `is_primary_admin` tinyint(1) NOT NULL,
    CONSTRAINT `PK_DEPARTMENT_REPRESENTATIVE` PRIMARY KEY (`rep_id`),
    CONSTRAINT `FK_DEPARTMENT_REPRESENTATIVE_DEPARTMENT_department_id` FOREIGN KEY (`department_id`) REFERENCES `DEPARTMENT` (`department_id`) ON DELETE CASCADE,
    CONSTRAINT `FK_DEPARTMENT_REPRESENTATIVE_PERMISSION_ROLE_role_id` FOREIGN KEY (`role_id`) REFERENCES `PERMISSION_ROLE` (`role_id`) ON DELETE CASCADE,
    CONSTRAINT `FK_DEPARTMENT_REPRESENTATIVE_USERS_user_id` FOREIGN KEY (`user_id`) REFERENCES `USERS` (`user_id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `INVENTORY_HISTORY` (
    `inventory_id` int NOT NULL AUTO_INCREMENT,
    `inventory_department_id` int NOT NULL,
    `inventory_implementer` int NOT NULL,
    `inventory_date` datetime(6) NOT NULL,
    `inventory_status` longtext CHARACTER SET utf8mb4 NOT NULL,
    `remarks` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_INVENTORY_HISTORY` PRIMARY KEY (`inventory_id`),
    CONSTRAINT `FK_INVENTORY_HISTORY_DEPARTMENT_inventory_department_id` FOREIGN KEY (`inventory_department_id`) REFERENCES `DEPARTMENT` (`department_id`) ON DELETE CASCADE,
    CONSTRAINT `FK_INVENTORY_HISTORY_USERS_inventory_implementer` FOREIGN KEY (`inventory_implementer`) REFERENCES `USERS` (`user_id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `LICENSE` (
    `license_id` int NOT NULL AUTO_INCREMENT,
    `license_management_number` longtext CHARACTER SET utf8mb4 NULL,
    `license_key` longtext CHARACTER SET utf8mb4 NULL,
    `license_count` int NULL,
    `expiry_date` datetime(6) NULL,
    `description` longtext CHARACTER SET utf8mb4 NULL,
    `installation_name` longtext CHARACTER SET utf8mb4 NULL,
    `publisher_name` longtext CHARACTER SET utf8mb4 NULL,
    `software_type` longtext CHARACTER SET utf8mb4 NULL,
    `license_type` longtext CHARACTER SET utf8mb4 NULL,
    `license_format` longtext CHARACTER SET utf8mb4 NULL,
    `counting_method` longtext CHARACTER SET utf8mb4 NULL,
    `academic_flag` tinyint(1) NOT NULL,
    `number_of_licenses` int NOT NULL,
    `number_available` int NOT NULL,
    `management_department_id` int NULL,
    `manager_user_id` int NULL,
    `license_status` longtext CHARACTER SET utf8mb4 NOT NULL,
    `disposal_date` datetime(6) NULL,
    `parent_license_id` int NULL,
    `is_deleted` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `created_by` longtext CHARACTER SET utf8mb4 NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    `updated_by` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_LICENSE` PRIMARY KEY (`license_id`),
    CONSTRAINT `FK_LICENSE_DEPARTMENT_management_department_id` FOREIGN KEY (`management_department_id`) REFERENCES `DEPARTMENT` (`department_id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_LICENSE_LICENSE_parent_license_id` FOREIGN KEY (`parent_license_id`) REFERENCES `LICENSE` (`license_id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_LICENSE_USERS_manager_user_id` FOREIGN KEY (`manager_user_id`) REFERENCES `USERS` (`user_id`) ON DELETE RESTRICT
) CHARACTER SET=utf8mb4;

CREATE TABLE `USER_ITEMS` (
    `user_item_id` int NOT NULL AUTO_INCREMENT,
    `user_id` int NOT NULL,
    `item_key` longtext CHARACTER SET utf8mb4 NULL,
    `item_value` longtext CHARACTER SET utf8mb4 NULL,
    `created_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_USER_ITEMS` PRIMARY KEY (`user_item_id`),
    CONSTRAINT `FK_USER_ITEMS_USERS_user_id` FOREIGN KEY (`user_id`) REFERENCES `USERS` (`user_id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `USER_PERMISSION` (
    `user_id` int NOT NULL,
    `can_view` tinyint(1) NOT NULL,
    `can_request` tinyint(1) NOT NULL,
    `can_approve` tinyint(1) NOT NULL,
    `can_admin` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    CONSTRAINT `PK_USER_PERMISSION` PRIMARY KEY (`user_id`),
    CONSTRAINT `FK_USER_PERMISSION_USERS_user_id` FOREIGN KEY (`user_id`) REFERENCES `USERS` (`user_id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `IT_ASSET` (
    `asset_id` int NOT NULL AUTO_INCREMENT,
    `asset_control_number` longtext CHARACTER SET utf8mb4 NULL,
    `asset_name` longtext CHARACTER SET utf8mb4 NULL,
    `category_id` int NULL,
    `manufacturer` longtext CHARACTER SET utf8mb4 NULL,
    `model` longtext CHARACTER SET utf8mb4 NULL,
    `serial_number` longtext CHARACTER SET utf8mb4 NULL,
    `purchase_date` datetime(6) NULL,
    `warranty_expiry` datetime(6) NULL,
    `department_id` int NULL,
    `asset_manager_id` int NULL,
    `user_created_id` int NULL,
    `user_used_id` int NULL,
    `status_id` int NULL,
    `location_id` int NULL,
    `inventory_id` int NULL,
    `created_at` datetime(6) NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    `updated_by` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_IT_ASSET` PRIMARY KEY (`asset_id`),
    CONSTRAINT `FK_IT_ASSET_ASSET_CATEGORY_category_id` FOREIGN KEY (`category_id`) REFERENCES `ASSET_CATEGORY` (`category_id`),
    CONSTRAINT `FK_IT_ASSET_ASSET_LOCATION_location_id` FOREIGN KEY (`location_id`) REFERENCES `ASSET_LOCATION` (`location_id`),
    CONSTRAINT `FK_IT_ASSET_ASSET_STATUS_status_id` FOREIGN KEY (`status_id`) REFERENCES `ASSET_STATUS` (`status_id`),
    CONSTRAINT `FK_IT_ASSET_DEPARTMENT_department_id` FOREIGN KEY (`department_id`) REFERENCES `DEPARTMENT` (`department_id`),
    CONSTRAINT `FK_IT_ASSET_INVENTORY_HISTORY_inventory_id` FOREIGN KEY (`inventory_id`) REFERENCES `INVENTORY_HISTORY` (`inventory_id`),
    CONSTRAINT `FK_IT_ASSET_USERS_asset_manager_id` FOREIGN KEY (`asset_manager_id`) REFERENCES `USERS` (`user_id`),
    CONSTRAINT `FK_IT_ASSET_USERS_user_created_id` FOREIGN KEY (`user_created_id`) REFERENCES `USERS` (`user_id`),
    CONSTRAINT `FK_IT_ASSET_USERS_user_used_id` FOREIGN KEY (`user_used_id`) REFERENCES `USERS` (`user_id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `LICENSE_INVENTORY_HISTORY` (
    `inventory_id` int NOT NULL AUTO_INCREMENT,
    `license_id` int NOT NULL,
    `inventory_date` datetime(6) NOT NULL,
    `inventory_taker_id` int NULL,
    `inventory_status` longtext CHARACTER SET utf8mb4 NOT NULL,
    `remarks` longtext CHARACTER SET utf8mb4 NULL,
    `is_deleted` tinyint(1) NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `created_by` longtext CHARACTER SET utf8mb4 NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    `updated_by` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_LICENSE_INVENTORY_HISTORY` PRIMARY KEY (`inventory_id`),
    CONSTRAINT `FK_LICENSE_INVENTORY_HISTORY_LICENSE_license_id` FOREIGN KEY (`license_id`) REFERENCES `LICENSE` (`license_id`) ON DELETE CASCADE,
    CONSTRAINT `FK_LICENSE_INVENTORY_HISTORY_USERS_inventory_taker_id` FOREIGN KEY (`inventory_taker_id`) REFERENCES `USERS` (`user_id`) ON DELETE SET NULL
) CHARACTER SET=utf8mb4;

CREATE TABLE `REQUEST` (
    `request_id` int NOT NULL AUTO_INCREMENT,
    `request_type_id` int NOT NULL,
    `user_created_id` int NOT NULL,
    `asset_id` int NULL,
    `target_div` int NULL,
    `status_id` int NOT NULL,
    `request_description` longtext CHARACTER SET utf8mb4 NULL,
    `request_data` longtext CHARACTER SET utf8mb4 NULL,
    `first_approver_id` int NULL,
    `second_approver_id` int NULL,
    `approved_at` datetime(6) NULL,
    `rejected_at` datetime(6) NULL,
    `reject_reason` longtext CHARACTER SET utf8mb4 NULL,
    `created_at` datetime(6) NOT NULL,
    `created_by` longtext CHARACTER SET utf8mb4 NOT NULL,
    `updated_at` datetime(6) NOT NULL,
    `updated_by` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_REQUEST` PRIMARY KEY (`request_id`),
    CONSTRAINT `FK_REQUEST_IT_ASSET_asset_id` FOREIGN KEY (`asset_id`) REFERENCES `IT_ASSET` (`asset_id`),
    CONSTRAINT `FK_REQUEST_REQUEST_STATUS_status_id` FOREIGN KEY (`status_id`) REFERENCES `REQUEST_STATUS` (`status_id`) ON DELETE CASCADE,
    CONSTRAINT `FK_REQUEST_REQUEST_TYPE_request_type_id` FOREIGN KEY (`request_type_id`) REFERENCES `REQUEST_TYPE` (`type_id`) ON DELETE CASCADE,
    CONSTRAINT `FK_REQUEST_USERS_user_created_id` FOREIGN KEY (`user_created_id`) REFERENCES `USERS` (`user_id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `SOFTWARE` (
    `software_id` int NOT NULL AUTO_INCREMENT,
    `software_name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `software_version` longtext CHARACTER SET utf8mb4 NOT NULL,
    `vendor_id` int NULL,
    `license_id` int NULL,
    `license_type` longtext CHARACTER SET utf8mb4 NULL,
    `description` longtext CHARACTER SET utf8mb4 NULL,
    `group_id` int NULL,
    `asset_id` int NULL,
    `asset_control_number` longtext CHARACTER SET utf8mb4 NULL,
    `installed_by` int NULL,
    `installed_date` datetime(6) NOT NULL,
    `software_type` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_SOFTWARE` PRIMARY KEY (`software_id`),
    CONSTRAINT `FK_SOFTWARE_IT_ASSET_asset_id` FOREIGN KEY (`asset_id`) REFERENCES `IT_ASSET` (`asset_id`),
    CONSTRAINT `FK_SOFTWARE_USERS_installed_by` FOREIGN KEY (`installed_by`) REFERENCES `USERS` (`user_id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `request_approval` (
    `approval_id` int NOT NULL AUTO_INCREMENT,
    `request_id` int NOT NULL,
    `approver_id` int NOT NULL,
    `approval_level` int NOT NULL,
    `status_id` int NOT NULL,
    `approved_at` datetime(6) NULL,
    `Remarks` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_request_approval` PRIMARY KEY (`approval_id`),
    CONSTRAINT `FK_request_approval_REQUEST_request_id` FOREIGN KEY (`request_id`) REFERENCES `REQUEST` (`request_id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `REQUEST_ASSET` (
    `id` int NOT NULL AUTO_INCREMENT,
    `request_id` int NOT NULL,
    `asset_id` int NOT NULL,
    CONSTRAINT `PK_REQUEST_ASSET` PRIMARY KEY (`id`),
    CONSTRAINT `FK_REQUEST_ASSET_IT_ASSET_asset_id` FOREIGN KEY (`asset_id`) REFERENCES `IT_ASSET` (`asset_id`) ON DELETE CASCADE,
    CONSTRAINT `FK_REQUEST_ASSET_REQUEST_request_id` FOREIGN KEY (`request_id`) REFERENCES `REQUEST` (`request_id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE TABLE `REQUEST_DETAIL` (
    `detail_id` int NOT NULL AUTO_INCREMENT,
    `request_id` int NOT NULL,
    `field_name` longtext CHARACTER SET utf8mb4 NOT NULL,
    `old_value` longtext CHARACTER SET utf8mb4 NULL,
    `new_value` longtext CHARACTER SET utf8mb4 NULL,
    CONSTRAINT `PK_REQUEST_DETAIL` PRIMARY KEY (`detail_id`),
    CONSTRAINT `FK_REQUEST_DETAIL_REQUEST_request_id` FOREIGN KEY (`request_id`) REFERENCES `REQUEST` (`request_id`) ON DELETE CASCADE
) CHARACTER SET=utf8mb4;

CREATE INDEX `IX_DEPARTMENT_parent_department_id` ON `DEPARTMENT` (`parent_department_id`);

CREATE INDEX `IX_DEPARTMENT_REPRESENTATIVE_department_id` ON `DEPARTMENT_REPRESENTATIVE` (`department_id`);

CREATE INDEX `IX_DEPARTMENT_REPRESENTATIVE_role_id` ON `DEPARTMENT_REPRESENTATIVE` (`role_id`);

CREATE INDEX `IX_DEPARTMENT_REPRESENTATIVE_user_id` ON `DEPARTMENT_REPRESENTATIVE` (`user_id`);

CREATE INDEX `IX_INVENTORY_HISTORY_inventory_department_id` ON `INVENTORY_HISTORY` (`inventory_department_id`);

CREATE INDEX `IX_INVENTORY_HISTORY_inventory_implementer` ON `INVENTORY_HISTORY` (`inventory_implementer`);

CREATE INDEX `IX_IT_ASSET_asset_manager_id` ON `IT_ASSET` (`asset_manager_id`);

CREATE INDEX `IX_IT_ASSET_category_id` ON `IT_ASSET` (`category_id`);

CREATE INDEX `IX_IT_ASSET_department_id` ON `IT_ASSET` (`department_id`);

CREATE INDEX `IX_IT_ASSET_inventory_id` ON `IT_ASSET` (`inventory_id`);

CREATE INDEX `IX_IT_ASSET_location_id` ON `IT_ASSET` (`location_id`);

CREATE INDEX `IX_IT_ASSET_status_id` ON `IT_ASSET` (`status_id`);

CREATE INDEX `IX_IT_ASSET_user_created_id` ON `IT_ASSET` (`user_created_id`);

CREATE INDEX `IX_IT_ASSET_user_used_id` ON `IT_ASSET` (`user_used_id`);

CREATE INDEX `IX_LICENSE_management_department_id` ON `LICENSE` (`management_department_id`);

CREATE INDEX `IX_LICENSE_manager_user_id` ON `LICENSE` (`manager_user_id`);

CREATE INDEX `IX_LICENSE_parent_license_id` ON `LICENSE` (`parent_license_id`);

CREATE INDEX `IX_LICENSE_INVENTORY_HISTORY_inventory_taker_id` ON `LICENSE_INVENTORY_HISTORY` (`inventory_taker_id`);

CREATE INDEX `IX_LICENSE_INVENTORY_HISTORY_license_id` ON `LICENSE_INVENTORY_HISTORY` (`license_id`);

CREATE INDEX `IX_REQUEST_asset_id` ON `REQUEST` (`asset_id`);

CREATE INDEX `IX_REQUEST_request_type_id` ON `REQUEST` (`request_type_id`);

CREATE INDEX `IX_REQUEST_status_id` ON `REQUEST` (`status_id`);

CREATE INDEX `IX_REQUEST_user_created_id` ON `REQUEST` (`user_created_id`);

CREATE INDEX `IX_request_approval_request_id` ON `request_approval` (`request_id`);

CREATE INDEX `IX_REQUEST_ASSET_asset_id` ON `REQUEST_ASSET` (`asset_id`);

CREATE INDEX `IX_REQUEST_ASSET_request_id` ON `REQUEST_ASSET` (`request_id`);

CREATE INDEX `IX_REQUEST_DETAIL_request_id` ON `REQUEST_DETAIL` (`request_id`);

CREATE INDEX `IX_SOFTWARE_asset_id` ON `SOFTWARE` (`asset_id`);

CREATE INDEX `IX_SOFTWARE_installed_by` ON `SOFTWARE` (`installed_by`);

CREATE INDEX `IX_USER_ITEMS_user_id` ON `USER_ITEMS` (`user_id`);

CREATE INDEX `IX_USERS_primary_department_id` ON `USERS` (`primary_department_id`);

CREATE INDEX `IX_USERS_role_id` ON `USERS` (`role_id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20260624043019_InitialCreate', '8.0.5');

COMMIT;

