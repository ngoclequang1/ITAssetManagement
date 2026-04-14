namespace ITAssetManagement.DTOs.Request
{
    public class BulkFailureRequestDto
    {
        public List<int> AssetIds { get; set; }
        public int UserCreatedId { get; set; }

        public string ApplicationClassification { get; set; }

        public DateTime PickupDate { get; set; }
        public DateTime? ReceiptDate { get; set; }

        public string BreakdownReason { get; set; }
        public string Description { get; set; }

        public int ApproverId { get; set; }
    }
}
