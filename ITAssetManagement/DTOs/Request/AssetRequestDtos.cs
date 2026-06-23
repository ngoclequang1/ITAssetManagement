namespace ITAssetManagement.DTOs.Request
{
    // ─────────────────────────────────────────────────────────────
    // ADD ASSET REQUEST – Manager gửi, Administrator approve
    // ─────────────────────────────────────────────────────────────
    public class AddAssetRequestDto
    {
        // Thông tin tài sản muốn thêm
        public string AssetControlNumber { get; set; } = string.Empty;
        public string AssetName          { get; set; } = string.Empty;
        public string? Manufacturer      { get; set; }
        public string? Model             { get; set; }
        public string? SerialNumber      { get; set; }
        public int     CategoryId        { get; set; }
        public int     StatusId          { get; set; }
        public int     LocationId        { get; set; }
        public int     DepartmentId      { get; set; }

        // Người tạo request
        public int    UserCreatedId  { get; set; }
        public string? Description   { get; set; }

        // Approval
        public int  FirstApproverId  { get; set; }
        public int? SecondApproverId { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    // DELETE ASSET REQUEST – Manager gửi, Administrator approve
    // ─────────────────────────────────────────────────────────────
    public class DeleteAssetRequestDto
    {
        public int    AssetId        { get; set; }
        public int    UserCreatedId  { get; set; }
        public string? Reason        { get; set; }

        // Approval
        public int  FirstApproverId  { get; set; }
        public int? SecondApproverId { get; set; }
    }

    // ─────────────────────────────────────────────────────────────
    // PERMISSION INFO (response DTO – trả về sau khi login)
    // ─────────────────────────────────────────────────────────────
    public class UserPermissionDto
    {
        public int  UserId     { get; set; }
        public bool CanView    { get; set; }
        public bool CanRequest { get; set; }
        public bool CanApprove { get; set; }
        public bool CanAdmin   { get; set; }
    }
}