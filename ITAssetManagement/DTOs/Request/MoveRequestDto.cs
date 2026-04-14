namespace ITAssetManagement.DTOs.Request
{
    public class MoveRequestDto : BaseRequestDto
    {
        public int MoveToDepartmentId { get; set; }
        public int PersonInChargeId { get; set; }

        public int FirstApproverId { get; set; }
        public int ? SecondApproverId { get; set; }
    }
}