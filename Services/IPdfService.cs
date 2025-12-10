using HRPackage.Models;

namespace HRPackage.Services
{
    public interface IPdfService
    {
        byte[] GenerateInvoicePdf(Invoice invoice, CompanySettings companySettings);
        byte[] GeneratePurchaseOrderPdf(PurchaseOrder po, CompanySettings companySettings);
        byte[] GenerateDeliveryChallanPdf(DeliveryChallan dc, CompanySettings companySettings);
        byte[] GenerateQuotationPdf(Quotation quotation, CompanySettings companySettings);
    }
}
