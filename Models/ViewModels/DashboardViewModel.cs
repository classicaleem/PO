namespace SmartPO.Models.ViewModels
{
    public class DashboardViewModel
    {
        // PO Stats
        public int TotalPOs { get; set; }
        public int CompletedPOs { get; set; }
        public int PendingPOs { get; set; }
        public decimal TotalPoAmount { get; set; }

        // Invoice Stats
        public int TotalInvoices { get; set; }
        public int PaidInvoices { get; set; }
        public int UnpaidInvoices { get; set; }
        public decimal TotalInvoiceAmount { get; set; }
        public decimal UnpaidInvoiceAmount { get; set; }

        // This Month Stats
        public int ThisMonthPOs { get; set; }
        public int ThisMonthInvoices { get; set; }
        public decimal ThisMonthPoAmount { get; set; }
        public decimal ThisMonthInvoiceAmount { get; set; }

        // Recent Activity
        public List<PurchaseOrder> RecentPOs { get; set; } = new();
        public List<Invoice> RecentInvoices { get; set; } = new();

        // Top Customers
        public List<TopCustomerDto> TopCustomers { get; set; } = new();
    }

    public class TopCustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int PoCount { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
