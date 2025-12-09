using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HRPackage.Models.ViewModels
{
    public class CustomerViewModel
    {
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Customer Code is required")]
        [StringLength(50)]
        [Display(Name = "Customer Code")]
        public string CustomerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer Name is required")]
        [StringLength(200)]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Address Line 1")]
        public string? AddressLine1 { get; set; }

        [StringLength(200)]
        [Display(Name = "Address Line 2")]
        public string? AddressLine2 { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [Required(ErrorMessage = "State is required")]
        [StringLength(100)]
        public string State { get; set; } = string.Empty;

        [StringLength(10)]
        [Display(Name = "State Code")]
        public string? StateCode { get; set; }

        [StringLength(20)]
        public string? Pincode { get; set; }

        [StringLength(20)]
        [Display(Name = "Contact Number")]
        public string? ContactNumber { get; set; }

        [StringLength(150)]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Email")]
        public string? EmailId { get; set; }

        [StringLength(50)]
        [Display(Name = "GST Number")]
        public string? GstNumber { get; set; }

        [Range(0, 100)]
        [Display(Name = "CGST %")]
        public decimal DefaultCgstPercent { get; set; }

        [Range(0, 100)]
        [Display(Name = "SGST %")]
        public decimal DefaultSgstPercent { get; set; }

        [Range(0, 100)]
        [Display(Name = "IGST %")]
        public decimal DefaultIgstPercent { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        // For dropdown
        public List<SelectListItem> States { get; set; } = new();
    }
}
