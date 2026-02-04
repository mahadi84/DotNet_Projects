CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role ENUM('HoSuperAdmin', 'BrAdmin','Auditor', 'Maker', 'Checker') NOT NULL,
    branch_id INT NOT NULL,
    failed_attempts INT DEFAULT 0,
    lock_until TIMESTAMP NULL,
    is_locked BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    last_login TIMESTAMP NULL,
    created_by INT NOT NULL,
    updated_by INT DEFAULT NULL, 
    approved_by INT DEFAULT NULL,
    row_version INT NOT NULL DEFAULT 1, 
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP, 
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (branch_id) REFERENCES branches(id)
);