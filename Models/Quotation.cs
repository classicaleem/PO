using System.ComponentModel.DataAnnotations;

namespace HRPackage.Models
{
    public class Quotation
    {
        public int QuotationId { get; set; }
        public string QuotationNo { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public DateTime? ValidUntil { get; set; }
        public int? CustomerId { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsDeleted { get; set; }

        public Customer? Customer { get; set; }
        public List<QuotationItem> Items { get; set; } = new List<QuotationItem>();

         // Calculated properties
        public decimal TotalAmount => Items.Where(i => !i.IsDeleted).Sum(i => i.TotalAmount);
    }

    public class QuotationItem
    {
        public int QuotationItemId { get; set; }
        public int QuotationId { get; set; }
        public int SlNo { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsDeleted { get; set; }
    }
}
