using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRPackage.Models.ViewModels
{
    public class DeliveryChallanViewModel
    {
        public int DcId { get; set; }

        [Required]
        [Display(Name = "DC Number")]
        public string DcNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "DC Date")]
        public DateTime DcDate { get; set; } = DateTime.Today;

        [Display(Name = "Customer")]
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }

        [Display(Name = "Target Company (If no customer)")]
        public string? TargetCompany { get; set; }

        [Display(Name = "Vehicle No")]
        public string? VehicleNo { get; set; }

        // Items
        public List<DeliveryChallanItemViewModel> Items { get; set; } = new List<DeliveryChallanItemViewModel> { new DeliveryChallanItemViewModel() };

        // Dropdowns
        public List<SelectListItem> Customers { get; set; } = new List<SelectListItem>();
    }

    public class DeliveryChallanItemViewModel
    {
        public int SlNo { get; set; }
        
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Qty must be > 0")]
        public int Quantity { get; set; }

        public string Unit { get; set; } = "NO";
        public string? Remarks { get; set; }
    }
}
