namespace HRPackage.Models
{
    public class Invoice
    {
        public int InvoiceId { get; set; }
        public int PoId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal CgstPercent { get; set; }
        public decimal SgstPercent { get; set; }
        public decimal IgstPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal RoundOff { get; set; }
        public decimal GrandTotal { get; set; }
        public string? ShippingAddress { get; set; }
        public bool IsPaid { get; set; }
        public bool IsDeleted { get; set; }
        public int TotalQuantity { get; set; }

        // Navigation/Display properties
        public string? PoNumber { get; set; }
        public string? InternalPoCode { get; set; }
        public string? CustomerName { get; set; }
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }
        public List<InvoiceItem> Items { get; set; } = new();
    }
}
