namespace ITAssetManagement.Models
{
    public class DepartmentQuery
    {
        public string? Keyword { get; set; }

        public bool ViewObsolete { get; set; } = false;

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }
}