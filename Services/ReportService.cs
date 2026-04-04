using Microsoft.Reporting.NETCore;
using SmartPO.Models;
using SmartPO.Helpers;

namespace SmartPO.Services
{
    public interface IReportService
    {
        byte[] GeneratePurchaseOrderPdf(PurchaseOrder po, CompanySettings company);
        byte[] GenerateInvoicePdf(Invoice invoice, CompanySettings company, string copyType = "Original for Recipient");
        byte[] GenerateQuotationPdf(Quotation quotation, CompanySettings company);
        byte[] GenerateDeliveryChallanPdf(DeliveryChallan dc, CompanySettings company);
    }

    public class ReportService : IReportService
    {
        private readonly IWebHostEnvironment _env;

        public ReportService(IWebHostEnvironment env)
        {
            _env = env;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  INVOICE
        // ─────────────────────────────────────────────────────────────────────
        public byte[] GenerateInvoicePdf(Invoice invoice, CompanySettings company, string copyType = "Original for Recipient")
        {
            // Read logo bytes (wwwroot/images/logo.png)
            byte[] logoBytes = Array.Empty<byte>();
            string logoPath = Path.Combine(_env.WebRootPath, "images", "logo.png");
            if (File.Exists(logoPath))
                logoBytes = File.ReadAllBytes(logoPath);

            var header = new InvoiceHeaderDto
            {
                LogoBytes       = logoBytes,
                InvoiceNumber   = invoice.InvoiceNumber,
                InvoiceDate     = invoice.InvoiceDate.ToString("dd-MMM-yyyy"),
                PoNumber        = invoice.PoNumber ?? "",
                PoDate          = invoice.PurchaseOrder?.PoDate?.ToString("dd-MMM-yyyy") ?? "",
                ContactName     = invoice.ContactName ?? "",
                ContactNo       = invoice.ContactNo ?? "",
                VehicleNo       = invoice.VehicleNo ?? "",
                SimDcNo         = invoice.SimDcNo ?? "",
                YourDcNo        = invoice.YourDcNo ?? "",
                Remarks         = invoice.Remarks ?? "",
                FromName        = company.Name,
                FromAddress     = $"{company.AddressLine1}, {company.AddressLine2}",
                FromEmail       = company.Email,
                FromPhone       = company.Phone,
                FromGst         = company.GstNumber,
                CustomerName    = invoice.Customer?.CustomerName ?? invoice.CustomerName ?? "",
                BillingAddress  = invoice.Customer?.AddressLine1 ?? "",   // address only, no state/pincode
                CustomerState   = invoice.Customer?.State ?? "",
                CustomerPincode = invoice.Customer?.Pincode ?? "",
                ShippingAddress = invoice.ShippingAddress ?? "",
                CustomerGst     = invoice.Customer?.GstNumber ?? "",
                CustomerPhone   = invoice.Customer?.ContactNumber ?? "",
                CustomerDcDate  = invoice.CustomerDcDate?.ToString("dd-MMM-yyyy") ?? "",
                SubTotal        = invoice.TotalAmount,
                CgstPercent     = invoice.CgstPercent,
                SgstPercent     = invoice.SgstPercent,
                IgstPercent     = invoice.IgstPercent,
                CgstAmount      = invoice.CgstAmount,
                SgstAmount      = invoice.SgstAmount,
                IgstAmount      = invoice.IgstAmount,
                FreightAmount   = invoice.FreightAmount,
                RoundOff        = invoice.RoundOff,
                GrandTotal      = invoice.GrandTotal,
                TotalQty        = invoice.TotalQuantity,
                AmountInWords   = NumberToWords.ConvertAmount((double)invoice.GrandTotal)
            };

            var items = invoice.Items.Select((item, idx) => new InvoiceItemDto
            {
                SNo         = idx + 1,
                Description = item.ItemDescription ?? "",
                HsnCode     = item.HsnCode ?? "",
                UnitPrice   = item.UnitPrice,
                Quantity    = item.Quantity,
                LineAmount  = item.LineAmount
            }).ToList();

            return Render("Invoice.rdl",
                new Dictionary<string, string> { { "CopyType", copyType } },
                ("InvoiceHeader", new[] { header }.ToList<object>()),
                ("InvoiceItems",  items.Cast<object>().ToList()));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  PURCHASE ORDER
        // ─────────────────────────────────────────────────────────────────────
        public byte[] GeneratePurchaseOrderPdf(PurchaseOrder po, CompanySettings company)
        {
            var header = new PoHeaderDto
            {
                PoNumber        = po.PoNumber,
                PoDate          = po.PoDate?.ToString("dd-MMM-yyyy") ?? "",
                FromName        = company.Name,
                FromAddress     = $"{company.AddressLine1}, {company.AddressLine2}",
                FromEmail       = company.Email,
                FromPhone       = company.Phone,
                FromGst         = company.GstNumber,
                CustomerName    = po.Customer?.CustomerName ?? po.SupplierName,
                CustomerAddress = po.Customer?.FullAddress ?? "",
                CustomerGst     = po.Customer?.GstNumber ?? "",
                CgstPercent     = po.CgstPercent,
                SgstPercent     = po.SgstPercent,
                IgstPercent     = po.IgstPercent,
                TaxAmount       = po.TaxAmount,
                RoundOff        = po.RoundOff,
                GrandTotal      = po.GrandTotal,
                AmountInWords   = NumberToWords.ConvertAmount((double)po.GrandTotal)
            };

            var items = po.Items
                .Where(i => !i.IsDeleted)
                .Select((item, idx) => new PoItemDto
                {
                    SNo         = idx + 1,
                    Description = item.ItemDescription,
                    HsnCode     = item.HsnCode ?? "",
                    UnitPrice   = item.UnitPrice,
                    Quantity    = item.Quantity,
                    LineTotal   = item.LineTotal
                }).ToList();

            return Render("PurchaseOrder.rdl",
                ("PoHeader", new[] { header }.ToList<object>()),
                ("PoItems",  items.Cast<object>().ToList()));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  QUOTATION
        // ─────────────────────────────────────────────────────────────────────
        public byte[] GenerateQuotationPdf(Quotation quotation, CompanySettings company)
        {
            var header = new QuotationHeaderDto
            {
                QuotationNo     = quotation.QuotationNo,
                QuotationDate   = quotation.Date.ToString("dd-MMM-yyyy"),
                ValidUntil      = quotation.ValidUntil?.ToString("dd-MMM-yyyy") ?? "",
                FromName        = company.Name,
                FromAddress     = $"{company.AddressLine1}, {company.AddressLine2}",
                FromEmail       = company.Email,
                FromPhone       = company.Phone,
                FromGst         = company.GstNumber,
                CustomerName    = quotation.Customer?.CustomerName ?? "",
                CustomerAddress = quotation.Customer?.FullAddress ?? "",
                CustomerGst     = quotation.Customer?.GstNumber ?? "",
                CustomerPhone   = quotation.Customer?.ContactNumber ?? "",
                TotalAmount     = quotation.TotalAmount
            };

            var items = quotation.Items
                .Where(i => !i.IsDeleted)
                .Select((item, idx) => new QuotationItemDto
                {
                    SNo         = idx + 1,
                    Description = item.Description,
                    UnitPrice   = item.UnitPrice,
                    Quantity    = item.Quantity,
                    TotalAmount = item.TotalAmount
                }).ToList();

            return Render("Quotation.rdl",
                ("QuotationHeader", new[] { header }.ToList<object>()),
                ("QuotationItems",  items.Cast<object>().ToList()));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  DELIVERY CHALLAN
        // ─────────────────────────────────────────────────────────────────────
        public byte[] GenerateDeliveryChallanPdf(DeliveryChallan dc, CompanySettings company)
        {
            var header = new DcHeaderDto
            {
                DcNumber        = dc.DcNumber,
                DcDate          = dc.DcDate.ToString("dd-MMM-yyyy"),
                VehicleNo       = dc.VehicleNo,
                FromName        = company.Name,
                FromAddress     = $"{company.AddressLine1}, {company.AddressLine2}",
                FromEmail       = company.Email,
                FromPhone       = company.Phone,
                FromGst         = company.GstNumber,
                CustomerName    = dc.Customer?.CustomerName ?? "",
                CustomerAddress = dc.Customer?.FullAddress ?? "",
                CustomerGst     = dc.Customer?.GstNumber ?? "",
                TargetCompany   = dc.TargetCompany
            };

            var items = dc.Items
                .Where(i => !i.IsDeleted)
                .Select((item, idx) => new DcItemDto
                {
                    SNo         = idx + 1,
                    Description = item.Description,
                    Quantity    = item.Quantity,
                    Unit        = item.Unit,
                    Remarks     = item.Remarks
                }).ToList();

            return Render("DeliveryChallan.rdl",
                ("DcHeader", new[] { header }.ToList<object>()),
                ("DcItems",  items.Cast<object>().ToList()));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helper
        // ─────────────────────────────────────────────────────────────────────
        private byte[] Render(string rdlFileName, Dictionary<string, string>? parameters, params (string name, List<object> data)[] sources)
        {
            string reportPath = Path.Combine(_env.ContentRootPath, "Reports", rdlFileName);
            var report = new LocalReport();
            report.ReportPath = reportPath;
            if (parameters != null)
            {
                foreach (var kv in parameters)
                    report.SetParameters(new ReportParameter(kv.Key, kv.Value));
            }
            foreach (var (name, data) in sources)
            {
                report.DataSources.Add(new ReportDataSource(name, data));
            }
            return report.Render("PDF");
        }

        // Overload for callers with no parameters
        private byte[] Render(string rdlFileName, params (string name, List<object> data)[] sources)
            => Render(rdlFileName, null, sources);
    }
}
