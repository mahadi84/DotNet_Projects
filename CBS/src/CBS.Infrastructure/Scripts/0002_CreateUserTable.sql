CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(50) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role ENUM('HoSuperAdmin', 'BrAdmin','Auditor', 'Maker', 'Checker') NOT NULL,
    branch_code VARCHAR(20) NOT NULL,
    failed_attempts INT DEFAULT 0,
    lock_until TIMESTAMP NULL,
    is_locked BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    last_login TIMESTAMP NULL,
    FOREIGN KEY (branch_code) REFERENCES branches(branch_code)
);