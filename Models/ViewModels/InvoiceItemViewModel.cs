using System.ComponentModel.DataAnnotations;

namespace HRPackage.Models.ViewModels
{
    public class InvoiceItemViewModel
    {
        public int InvoiceItemId { get; set; }
        public int InvoiceId { get; set; }
        public int PoItemId { get; set; }

        [Display(Name = "Description")]
        public string ItemDescription { get; set; } = string.Empty;

        [Display(Name = "HSN Code")]
        public string HsnCode { get; set; } = string.Empty;

        [Display(Name = "Ordered")]
        public int OrderedQuantity { get; set; }

        [Display(Name = "Previously Invoiced")]
        public int PreviouslyInvoiced { get; set; }

        [Display(Name = "Pending")]
        public int PendingQuantity { get; set; }

        [Range(0, 999999)]
        [Display(Name = "This Invoice Qty")]
        public int ThisInvoiceQuantity { get; set; }

        [Display(Name = "Unit Price")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Line Amount")]
        public decimal LineAmount { get; set; }
    }
}
