using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRPackage.Models.ViewModels
{
    public class PurchaseOrderViewModel
    {
        public int PoId { get; set; }

        [Display(Name = "Internal PO Code")]
        public string InternalPoCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "PO Number is required")]
        [Display(Name = "PO Number")]
        [StringLength(50)]
        public string PoNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        // For backward compat - will be auto-filled from customer
        [Display(Name = "Supplier Name")]
        [StringLength(200)]
        public string SupplierName { get; set; } = string.Empty;

        [Display(Name = "PO Amount")]
        [DataType(DataType.Currency)]
        public decimal PoAmount { get; set; }

        [Required(ErrorMessage = "PO Date is required")]
        [Display(Name = "PO Date")]
        [DataType(DataType.Date)]
        public DateTime? PoDate { get; set; } = DateTime.Today;

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime? EndDate { get; set; }

        [Display(Name = "Completed")]
        public bool IsCompleted { get; set; }

        // Line items (max 10)
        public List<PurchaseOrderItemViewModel> Items { get; set; } = new();

        // For dropdowns
        public List<SelectListItem> Customers { get; set; } = new();

        // Customer info display (readonly)
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerGstNumber { get; set; }
    }
}
