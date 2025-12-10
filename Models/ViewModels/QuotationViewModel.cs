using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRPackage.Models.ViewModels
{
    public class QuotationViewModel
    {
        public int QuotationId { get; set; }

        [Required]
        [Display(Name = "Quotation No")]
        public string QuotationNo { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime Date { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Valid Until")]
        public DateTime? ValidUntil { get; set; }

        [Display(Name = "Customer")]
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }

        public List<QuotationItemViewModel> Items { get; set; } = new List<QuotationItemViewModel> { new QuotationItemViewModel() };
        public List<SelectListItem> Customers { get; set; } = new List<SelectListItem>();
    }

    public class QuotationItemViewModel
    {
        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal UnitPrice { get; set; }
        
        public decimal TotalAmount => Quantity * UnitPrice;
    }
}
