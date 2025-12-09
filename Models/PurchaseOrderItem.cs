namespace HRPackage.Models
{
    public class PurchaseOrderItem
    {
        public int PoItemId { get; set; }
        public int PoId { get; set; }
        public int LineNumber { get; set; }
        public string ItemDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public bool IsDeleted { get; set; }

        // For invoice pending tracking
        public int InvoicedQuantity { get; set; }
        public int PendingQuantity => Quantity - InvoicedQuantity;
    }
}
