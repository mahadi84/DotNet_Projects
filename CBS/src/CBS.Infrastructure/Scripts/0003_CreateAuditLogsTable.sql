CREATE TABLE audit_logs (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    branch_code VARCHAR(10) NOT NULL, 
    created_by INT NOT NULL, 
    updated_by INT DEFAULT NULL, 
    approved_by INT DEFAULT NULL, 
    table_name VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL,
    old_value LONGTEXT, -- অথবা JSON
    new_value LONGTEXT, 
    description LONGTEXT, 
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX (branch_code), -- সার্চিং দ্রুত করার জন্য
    INDEX (table_name), -- সার্চিং দ্রুত করার জন্য
    INDEX (created_at)  -- রিপোর্ট দ্রুত জেনারেট করার জন্য
);