namespace SmartPO.Models.ViewModels
{
    public class MonthlyTrendItem
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int PoCount { get; set; }
        public int InvoiceCount { get; set; }
        public decimal PoAmount { get; set; }
        public decimal InvoiceAmount { get; set; }
    }

    public class TopCustomerItem
    {
        public string CustomerName { get; set; } = "";
        public decimal TotalAmount { get; set; }
        public int OrderCount { get; set; }
    }

    public class DashboardViewModel
    {
        // KPI Row 1
        public int TotalPOs { get; set; }
        public int PendingPOs { get; set; }
        public int CompletedPOs { get; set; }
        public int TotalInvoices { get; set; }
        public int UnpaidInvoices { get; set; }
        public decimal UnpaidAmount { get; set; }

        // KPI Row 2
        public decimal TotalPoAmount { get; set; }
        public decimal TotalInvoiceAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public int ActiveCustomers { get; set; }
        public int TotalQuotations { get; set; }

        // Chart Data
        public List<MonthlyTrendItem> MonthlyTrend { get; set; } = new();
        public List<TopCustomerItem> TopCustomers { get; set; } = new();

        // Table Data
        public List<PurchaseOrder> RecentPOs { get; set; } = new();
        public List<Invoice> RecentInvoices { get; set; } = new();
        public List<Invoice> UnpaidInvoicesList { get; set; } = new();
        public List<PurchaseOrder> PendingQuantityPOs { get; set; } = new();
    }
}
