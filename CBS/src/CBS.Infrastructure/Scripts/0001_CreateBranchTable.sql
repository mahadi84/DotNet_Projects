
CREATE TABLE branches (
    id INT AUTO_INCREMENT PRIMARY KEY,   
    branch_code VARCHAR(20) NOT NULL, 
    branch_name VARCHAR(150) NOT NULL,
    vault_balance DECIMAL(18, 2) NOT NULL DEFAULT 0.00, 
    row_version INT NOT NULL DEFAULT 1, 
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_by INT NOT NULL,
    updated_by INT DEFAULT NULL, 
    approved_by INT DEFAULT NULL, 
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP, 
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    
    -- Indexing
    CONSTRAINT uq_branch_code UNIQUE (branch_code),
    INDEX idx_branch_active (is_active)
);

