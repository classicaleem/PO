namespace HRPackage.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string State { get; set; } = string.Empty;
        public string? StateCode { get; set; }
        public string? Pincode { get; set; }
        public string? ContactNumber { get; set; }
        public string? EmailId { get; set; }
        public string? GstNumber { get; set; }
        public decimal DefaultCgstPercent { get; set; }
        public decimal DefaultSgstPercent { get; set; }
        public decimal DefaultIgstPercent { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; }
        public int? CreatedByUserId { get; set; }

        // Computed property for full address
        public string FullAddress
        {
            get
            {
                var parts = new List<string>();
                if (!string.IsNullOrEmpty(AddressLine1)) parts.Add(AddressLine1);
                if (!string.IsNullOrEmpty(AddressLine2)) parts.Add(AddressLine2);
                if (!string.IsNullOrEmpty(City)) parts.Add(City);
                if (!string.IsNullOrEmpty(State)) parts.Add(State);
                if (!string.IsNullOrEmpty(Pincode)) parts.Add(Pincode);
                return string.Join(", ", parts);
            }
        }
    }
}
