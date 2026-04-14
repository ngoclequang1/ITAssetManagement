namespace ITAssetManagement.DTOs
{
    public class HardwareSearchDto
    {
        public string? AssetControlNumber { get; set; }
        public string? AssetName { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}