using HRPackage.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using HRPackage.Documents; // Will contain our Document classes

namespace HRPackage.Services
{
    public class PdfService : IPdfService
    {
        public byte[] GenerateInvoicePdf(Invoice invoice, CompanySettings companySettings)
        {
            var document = new InvoiceDocument(invoice, companySettings);
            return document.GeneratePdf();
        }

        public byte[] GeneratePurchaseOrderPdf(PurchaseOrder po, CompanySettings companySettings)
        {
            var document = new PurchaseOrderDocument(po, companySettings);
            return document.GeneratePdf();
        }

        public byte[] GenerateDeliveryChallanPdf(DeliveryChallan dc, CompanySettings companySettings)
        {
            var document = new DeliveryChallanDocument(dc, companySettings);
            return document.GeneratePdf();
        }

        public byte[] GenerateQuotationPdf(Quotation quotation, CompanySettings companySettings)
        {
            var document = new QuotationDocument(quotation, companySettings);
            return document.GeneratePdf();
        }
    }
}
