using System.ComponentModel.DataAnnotations;

namespace HRPackage.Models.ViewModels
{
    public class PurchaseOrderItemViewModel
    {
        public int PoItemId { get; set; }
        public int PoId { get; set; }
        
        [Range(1, 10, ErrorMessage = "Line number must be between 1 and 10")]
        [Display(Name = "Line #")]
        public int LineNumber { get; set; }

        [Required(ErrorMessage = "Item description is required")]
        [StringLength(200)]
        [Display(Name = "Item Description")]
        public string ItemDescription { get; set; } = string.Empty;

        [Required]
        [Range(1, 999999, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, 9999999.99, ErrorMessage = "Unit price must be greater than 0")]
        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Line Total")]
        public decimal LineTotal { get; set; }

        public int PendingQuantity { get; set; }
        public bool IsDeleted { get; set; }
    }
}
