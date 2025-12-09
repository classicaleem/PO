namespace HRPackage.Models
{
    public class PurchaseOrder
    {
        public int PoId { get; set; }
        public string PoNumber { get; set; } = string.Empty;
        public string InternalPoCode { get; set; } = string.Empty;
        public int? CustomerId { get; set; }
        public string SupplierName { get; set; } = string.Empty; // Kept for backward compat
        public decimal PoAmount { get; set; }
        public DateTime? PoDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public int CreatedByUserId { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsDeleted { get; set; }

        // Navigation/Display properties
        public string? CreatedByUsername { get; set; }
        public Customer? Customer { get; set; }
        public string? CustomerName { get; set; }
        public List<PurchaseOrderItem> Items { get; set; } = new();
        public List<Invoice> Invoices { get; set; } = new();

        // Computed for display
        public int TotalItems => Items?.Count ?? 0;
        public int TotalQuantity => Items?.Sum(i => i.Quantity) ?? 0;
    }
}
