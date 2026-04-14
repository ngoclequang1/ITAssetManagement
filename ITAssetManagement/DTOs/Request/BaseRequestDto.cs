namespace ITAssetManagement.DTOs.Request
{
    public class BaseRequestDto
    {
        public int AssetId { get; set; }
        public int UserCreatedId { get; set; }
        public string? Description { get; set; }
        public int ApproverId { get; set; }
    }
}