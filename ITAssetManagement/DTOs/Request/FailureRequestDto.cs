namespace ITAssetManagement.DTOs.Request
{
    public class FailureRequestDto
    {
        public int AssetId { get; set; }
        public int UserCreatedId { get; set; }

        public string ApplicationClassification { get; set; } // Exchange / Repair / Return

        public DateTime PickupDate { get; set; }
        public DateTime? ReceiptDate { get; set; }

        public string BreakdownReason { get; set; }
        public string Description { get; set; }

        public int ApproverId { get; set; }
    }
}
