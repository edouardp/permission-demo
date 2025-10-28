-- Initial schema for Permissions API
-- Creates core tables with proper many-to-many relationships

-- Users table
CREATE TABLE users (
    email VARCHAR(255) PRIMARY KEY,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Groups table  
CREATE TABLE `groups` (
    name VARCHAR(100) PRIMARY KEY,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Permissions table
CREATE TABLE permissions (
    name VARCHAR(100) PRIMARY KEY,
    description TEXT NOT NULL,
    is_default BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Junction table for user-group memberships
CREATE TABLE user_group_memberships (
    user_email VARCHAR(255) NOT NULL,
    group_name VARCHAR(100) NOT NULL,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    assigned_by VARCHAR(255),
    PRIMARY KEY (user_email, group_name),
    FOREIGN KEY (user_email) REFERENCES users(email) ON DELETE CASCADE,
    FOREIGN KEY (group_name) REFERENCES `groups`(name) ON DELETE CASCADE
);

-- Group permissions (group-level ALLOW/DENY rules)
CREATE TABLE group_permissions (
    group_name VARCHAR(100) NOT NULL,
    permission_name VARCHAR(100) NOT NULL,
    access_type ENUM('ALLOW', 'DENY') NOT NULL,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    assigned_by VARCHAR(255),
    PRIMARY KEY (group_name, permission_name),
    FOREIGN KEY (group_name) REFERENCES `groups`(name) ON DELETE CASCADE,
    FOREIGN KEY (permission_name) REFERENCES permissions(name) ON DELETE CASCADE
);

-- User permissions (user-level overrides)
CREATE TABLE user_permissions (
    user_email VARCHAR(255) NOT NULL,
    permission_name VARCHAR(100) NOT NULL,
    access_type ENUM('ALLOW', 'DENY') NOT NULL,
    assigned_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    assigned_by VARCHAR(255),
    PRIMARY KEY (user_email, permission_name),
    FOREIGN KEY (user_email) REFERENCES users(email) ON DELETE CASCADE,
    FOREIGN KEY (permission_name) REFERENCES permissions(name) ON DELETE CASCADE
);

-- History/audit table for all changes
CREATE TABLE history (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    change_type ENUM('CREATE', 'UPDATE', 'DELETE') NOT NULL,
    entity_type VARCHAR(50) NOT NULL,
    entity_id VARCHAR(255) NOT NULL,
    entity_after_change JSON,
    changed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    changed_by VARCHAR(255),
    reason TEXT,
    INDEX idx_entity (entity_type, entity_id),
    INDEX idx_changed_at (changed_at)
);

-- Indexes for performance
CREATE INDEX idx_user_groups ON user_group_memberships(user_email);
CREATE INDEX idx_group_users ON user_group_memberships(group_name);
CREATE INDEX idx_group_permissions ON group_permissions(group_name);
CREATE INDEX idx_user_permissions ON user_permissions(user_email);
CREATE INDEX idx_permission_defaults ON permissions(is_default);
