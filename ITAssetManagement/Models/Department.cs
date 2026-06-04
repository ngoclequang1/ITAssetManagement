using ITAssetManagement.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

[Table("DEPARTMENT")]
public class Department
{
    [Key]
    [Column("department_id")]
    public int DepartmentId { get; set; }

    [Column("department_code")]
    public string DepartmentCode { get; set; } = null!;

    [Column("department_name")]
    public string? DepartmentName { get; set; }

    [Column("parent_department_id")]
    public int? ParentDepartmentId { get; set; }

    [Column("deployment_name")]
    public string? DeploymentName { get; set; }

    [Column("top_deployment_name")]
    public string? TopDeploymentName { get; set; }

    [Column("overall_deployment")]
    public bool OverallDeployment { get; set; }

    [Column("is_kitting_department")]
    public bool IsKittingDepartment { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    public Department? ParentDepartment { get; set; }

    public ICollection<Department>? ChildDepartments { get; set; }

    public ICollection<User>? Users { get; set; }

    public ICollection<DepartmentRepresentative>? Representatives { get; set; }
}