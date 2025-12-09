namespace HRPackage.Models.ViewModels
{
    public class PoReportViewModel
    {
        // PO Stats
        public decimal TotalPoAmount { get; set; }
        public int TotalPoCount { get; set; }
        public int CompletedPoCount { get; set; }
        public int PendingPoCount { get; set; }

        // Invoice Stats
        public decimal TotalInvoicedAmount { get; set; }
        public int TotalInvoiceCount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal UnpaidAmount { get; set; }
    }
}
