namespace ITAssetManagement.DTOs.Request
{
    public class BulkRequestDto
    {
        public List<int> AssetIds { get; set; }


        public int ApproverId { get; set; }

        public string? Description { get; set; }
    }
}
