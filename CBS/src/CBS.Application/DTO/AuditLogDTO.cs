using System.ComponentModel.DataAnnotations;

namespace CBS.Application.DTO;




public record AuditLogCreateDTO(
    [Required(ErrorMessage = "Branch ID Required")]
    string BranchCode,

    [Required(ErrorMessage = "User ID Required")]
    int? CreatedBy,
    
    int? UpdatedBy,
    int? ApprovedBy,

    [Required(ErrorMessage = "Table Name Required")]
    string TableName,

    [Required(ErrorMessage = "Action Required")]
    string Action,

    string? OldValue,
    string? NewValue,
    string? Description
);





//Show at first load(with pagination)
public record AuditReportViewDTO(
    long Id,
    string? BranchCode,
    int? CreatedBy,
    //int? UpdatedBy,
    //int? ApprovedBy,
    string TableName,
    string Action,
    string? OldValue,
    string? NewValue,
    string? Description,
    DateTime CreatedAt
);




// Search AuditLogs than Show(with pagination)
public record AuditReportFilterDTO(
    string? BranchCode,    // ব্রাঞ্চ কোড দিয়ে সার্চ
    int? CreatedBy,        // নির্দিষ্ট ইউজার দিয়ে সার্চ
    //int? UpdatedBy,        // নির্দিষ্ট ইউজার দিয়ে সার্চ
    //int? ApprovedBy,       // নির্দিষ্ট ইউজার দিয়ে সার্চ
    DateTime? FromDate, // শুরুর তারিখ
    DateTime? ToDate,   // শেষ তারিখ
    int PageNumber = 1, // কততম পেজ
    int PageSize = 10   // প্রতি পেজে কত ডাটা
);



