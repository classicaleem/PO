namespace HRPackage.Models.ViewModels
{
    public class PendingQuantityReportViewModel
    {
        public List<PendingQuantityPoSummary> PoSummaries { get; set; } = new();
    }

    public class PendingQuantityPoSummary
    {
        public int PoId { get; set; }
        public string InternalPoCode { get; set; } = string.Empty;
        public string? PoNumber { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? PoDate { get; set; }
        public int TotalOrderedQuantity { get; set; }
        public int TotalInvoicedQuantity { get; set; }
        public int TotalPendingQuantity => TotalOrderedQuantity - TotalInvoicedQuantity;
        public string Status => TotalPendingQuantity == 0 ? "Completed" : "Pending";
        public bool IsCompleted => TotalPendingQuantity == 0;

        public List<PendingQuantityItemDetail> ItemDetails { get; set; } = new();
    }

    public class PendingQuantityItemDetail
    {
        public int PoItemId { get; set; }
        public int LineNumber { get; set; }
        public string ItemDescription { get; set; } = string.Empty;
        public int OrderedQuantity { get; set; }
        public int InvoicedQuantity { get; set; }
        public int PendingQuantity => OrderedQuantity - InvoicedQuantity;
    }
}
