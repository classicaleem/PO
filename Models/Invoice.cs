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

        // New properties for Report
        public decimal FreightAmount { get; set; }
        public string? ContactName { get; set; }
        public string? ContactNo { get; set; }
        public string? VehicleNo { get; set; }
        public string? SimDcNo { get; set; }
        public string? YourDcNo { get; set; }
        public string? Remarks { get; set; }

        // Calculated Tax Amounts (Read-only for convenience)
        public decimal CgstAmount => (TotalAmount * CgstPercent) / 100;
        public decimal SgstAmount => (TotalAmount * SgstPercent) / 100;
        public decimal IgstAmount => (TotalAmount * IgstPercent) / 100;
    }
}
