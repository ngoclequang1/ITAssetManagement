namespace ITAssetManagement.DTOs.Request
{
    public class ChangeRequestDto : BaseRequestDto
    {
        public List<ChangeItemDto> Changes { get; set; } = new();
    }

    public class ChangeItemDto
    {
        public string FieldName { get; set; }   // asset_name, user_used_id...
        public string? OldValue { get; set; }
        public string? NewValue { get; set; }
    }
}