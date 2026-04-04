using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SmartPO.Models.ViewModels
{
    public class InvoiceViewModel
    {
        public int InvoiceId { get; set; }

        [Required(ErrorMessage = "Purchase Order is required")]
        [Display(Name = "Purchase Order")]
        public int PoId { get; set; }

        [Required(ErrorMessage = "Invoice Number is required")]
        [Display(Name = "Invoice Number")]
        [StringLength(100)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Invoice Date is required")]
        [Display(Name = "Invoice Date")]
        [DataType(DataType.Date)]
        public DateTime InvoiceDate { get; set; } = DateTime.Today;

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }
        public decimal CgstPercent { get; set; }
        public decimal SgstPercent { get; set; }
        public decimal IgstPercent { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal RoundOff { get; set; }
        public decimal GrandTotal { get; set; }

        [Display(Name = "Paid")]
        public bool IsPaid { get; set; }

        // Shipping address option
        [Display(Name = "Use Different Shipping Address")]
        public bool UseDifferentShippingAddress { get; set; }

        [Display(Name = "Shipping Address")]
        public string? ShippingAddress { get; set; }

        // Invoice line items
        public List<InvoiceItemViewModel> Items { get; set; } = new();

        // For dropdown
        public List<SelectListItem> PurchaseOrders { get; set; } = new();

        // ── Extra details (editable on Create / Edit) ──
        [Display(Name = "Contact Name")]
        [StringLength(200)]
        public string? ContactName { get; set; }

        [Display(Name = "Contact Number")]
        [StringLength(50)]
        public string? ContactNo { get; set; }

        [Display(Name = "Customer DC")]
        [StringLength(100)]
        public string? CustomerDc { get; set; }

        [Display(Name = "Customer DC Date")]
        [DataType(DataType.Date)]
        public DateTime? CustomerDcDate { get; set; }

        [Display(Name = "Remarks")]
        [StringLength(500)]
        public string? Remarks { get; set; }

        // Display properties from PO
        public string? InternalPoCode { get; set; }
        public string? PoNumber { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerGstNumber { get; set; }
    }
}
