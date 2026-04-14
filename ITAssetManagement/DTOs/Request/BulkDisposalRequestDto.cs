namespace ITAssetManagement.DTOs.Request
{
    public class BulkDisposalRequestDto : BulkRequestDto
    {
        public int ApproverId { get; set; }
        public bool IsDisposal { get; set; } // true = disposal, false = return
    }
}
