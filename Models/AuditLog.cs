using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models;

public class AuditLog
{
    public long Id { get; set; }

    [StringLength(450)]
    public string? UserId { get; set; }

    [StringLength(256)]
    public string? UserName { get; set; }

    [Required, StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Module { get; set; } = string.Empty;

    [StringLength(100)]
    public string? EntityId { get; set; }

    [StringLength(1000)]
    public string? Details { get; set; }

    [StringLength(4000)]
    public string? OldValues { get; set; }

    [StringLength(4000)]
    public string? NewValues { get; set; }

    [StringLength(50)]
    public string? IpAddress { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [StringLength(20)]
    public string Result { get; set; } = "Success";
}
