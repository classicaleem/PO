using System.ComponentModel.DataAnnotations;

namespace HRPackage.Models
{
    public class DeliveryChallan
    {
        public int DcId { get; set; }
        public string DcNumber { get; set; } = string.Empty;
        public DateTime DcDate { get; set; }
        public int? CustomerId { get; set; }
        public string TargetCompany { get; set; } = string.Empty;
        public string VehicleNo { get; set; } = string.Empty;
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation
        public Customer? Customer { get; set; }
        public List<DeliveryChallanItem> Items { get; set; } = new List<DeliveryChallanItem>();
    }

    public class DeliveryChallanItem
    {
        public int DcItemId { get; set; }
        public int DcId { get; set; }
        public int SlNo { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = "NO";
        public string Remarks { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }
}
