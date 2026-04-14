namespace ITAssetManagement.DTOs.Department
{
    public class DisposalRequestDto
    {
        public int AssetId { get; set; }
        public int UserCreatedId { get; set; }
        public string Type { get; set; } // "disposal" hoặc "return"
        public string Description { get; set; }
        public int ApproverId { get; set; }
    }
}
