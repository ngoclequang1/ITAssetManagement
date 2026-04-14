namespace ITAssetManagement.DTOs
{
    public class DepartmentDto
    {
        public int DepartmentId { get; set; }

        public string DepartmentCode { get; set; }

        public string DepartmentName { get; set; }

        public string ParentDepartmentCode { get; set; }

        public string TopDepartmentName { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}