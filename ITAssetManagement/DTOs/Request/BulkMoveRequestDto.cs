namespace ITAssetManagement.DTOs.Request
{
    public class BulkMoveRequestDto : BulkRequestDto
    {
        public int ApproverId { get; set; }
        public int NewDepartmentId { get; set; }
        public int NewLocationId { get; set; }
    }
}
