namespace HRPackage.Models
{
    public class InvoiceItem
    {
        public int InvoiceItemId { get; set; }
        public int InvoiceId { get; set; }
        public int PoItemId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineAmount { get; set; }

        // Navigation/display properties from PO Item
        public string? ItemDescription { get; set; }
        public int OrderedQuantity { get; set; }
        public int PreviouslyInvoiced { get; set; }
        public int PendingQuantity { get; set; }
    }
}
