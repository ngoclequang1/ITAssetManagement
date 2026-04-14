namespace ITAssetManagement.DTOs
{
    public class CreateDepartmentDto
    {
        public string DepartmentCode { get; set; }

        public string DepartmentName { get; set; }

        public int? ParentDepartmentId { get; set; }
    }
}
