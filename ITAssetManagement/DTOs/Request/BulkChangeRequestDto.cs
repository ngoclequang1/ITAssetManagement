namespace ITAssetManagement.DTOs.Request
{
    public class BulkChangeRequestDto : BulkRequestDto
    {
        public int ApproverId { get; set; }
        public string FieldName { get; set; }
        public string NewValue { get; set; }
    }
}
