namespace HRPackage.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalPOs { get; set; }
        public int CompletedPOs { get; set; }
        public int UnpaidInvoices { get; set; }
        public decimal TotalPoAmount { get; set; }
        public List<PurchaseOrder> RecentPOs { get; set; } = new();
    }
}
