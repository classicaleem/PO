namespace HRPackage.Models.ViewModels
{
    public class SalesReportViewModel
    {
        public int SlNo { get; set; }
        public string InvoiceNo { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string CustomerGstNo { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty; // From PO? Or Internal Code? Using InternalPoCode for now.
        public string PoNo { get; set; } = string.Empty;
        public string HsnCode { get; set; } = string.Empty; // Placeholder
        public int Qty { get; set; }
        public int PendingQty { get; set; }
        public decimal Amount { get; set; }
        public decimal Cgst { get; set; }
        public decimal Sgst { get; set; }
        public decimal Igst { get; set; }
        public decimal RoundOff { get; set; }
        public decimal TotalAmount { get; set; }
        public string GstStatus { get; set; } = "Approve";
        public string PaymentStatus { get; set; } = "Pending";
        public bool IsPaid { get; set; }
    }
}
