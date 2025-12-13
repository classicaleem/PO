using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using HRPackage.Models;

namespace HRPackage.Services
{
    public class InvoiceDocument : IDocument
    {
        public Invoice InvoiceModel { get; }
        public CompanySettings Company { get; }
        public string LogoPath { get; }

        public InvoiceDocument(Invoice invoice, CompanySettings company, string logoPath)
        {
            InvoiceModel = invoice;
            Company = company;
            LogoPath = logoPath;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Margin(30);
                    //page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
        }

        void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                // Top Header Section (Boxed)
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(150); // Logo Section
                        columns.RelativeColumn();    // Center Title
                        columns.ConstantColumn(150); // Original Box
                    });

                    // Row 1: Logo | Title | Original
                    table.Cell().RowSpan(2).Border(1).Padding(5).Element(cell => 
                    {
                        if(!string.IsNullOrEmpty(LogoPath) && File.Exists(LogoPath))
                             cell.Image(LogoPath).FitArea();
                        else
                             cell.AlignMiddle().AlignCenter().Text("LOGO");
                    });

                    table.Cell().Border(1).AlignCenter().AlignMiddle().Text("INVOICE").FontSize(16).Bold();
                    table.Cell().RowSpan(2).Border(1).AlignCenter().AlignMiddle().Text("Original for Recipient").FontSize(9);

                    // Row 2: Empty/Spacer if needed or fused? 
                    // Actually user layout has Title separate. 
                    // Let's stick to 1 Row for Title/Original if height permits, or 2 rows.
                    // Actually, let's use the Table approach from user request:
                    // Col 1: Logo
                    // Col 2: Invoice Title
                    // Col 3: Original
                    
                    // Wait, the table structure I used in FastReport was:
                    // LOGO is overlay. 
                    // Table is Title | Original.
                    // Here we can do better: Side-by-Side.
                });

                // Let's redefine Header to match SIM exactly.
                // 3 columns:
                // 1. Logo (Width ~45mm)
                // 2. Title "INVOICE" (Center, Bold)
                // 3. "Original for Recipient" (Right)
                // ALL Borders.
                
                column.Item().Border(1).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(140); 
                        columns.RelativeColumn();
                        columns.ConstantColumn(140); 
                    });
                    
                    // Cell 1: Logo
                    table.Cell().BorderRight(1).Padding(5).Height(50).AlignMiddle().Element(c =>
                    {
                        if (!string.IsNullOrEmpty(LogoPath) && File.Exists(LogoPath))
                            c.Image(LogoPath).FitArea();
                    });

                    // Cell 2: Title
                    table.Cell().BorderRight(1).AlignCenter().AlignMiddle().Text("INVOICE").FontSize(20).Bold();

                    // Cell 3: Original
                    table.Cell().AlignCenter().AlignMiddle().Text("Original for Recipient").FontSize(10);
                });

                // INFO GRID (3x2)
                column.Item().Border(1).BorderTop(0).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(); 
                        columns.RelativeColumn();
                    });

                    // ROW 1: From | Invoice Details
                    table.Cell().BorderRight(1).BorderBottom(1).Padding(5).Column(c =>
                    {
                        c.Item().Text($"From : {Company.Name}").Bold().FontSize(9);
                        c.Item().Text($"{Company.AddressLine1}, {Company.AddressLine2}").FontSize(9);
                        c.Item().Text($"Email : {Company.Email}").FontSize(9);
                        c.Item().Text($"GST NO : {Company.GstNumber}").FontSize(9);
                    });

                    table.Cell().BorderBottom(1).Padding(5).Column(c =>
                    {
                        c.Item().Row(row => { row.RelativeItem().Text("Invoice No").Bold().FontSize(9); row.RelativeItem().Text($": {InvoiceModel.InvoiceNumber}").Bold().FontSize(9); });
                        c.Item().Row(row => { row.RelativeItem().Text("Invoice Date").Bold().FontSize(9); row.RelativeItem().Text($": {InvoiceModel.InvoiceDate:dd-MMM-yyyy}").Bold().FontSize(9); });
                    });

                    // ROW 2: To | PO Details
                    table.Cell().BorderRight(1).BorderBottom(1).Padding(5).Column(c =>
                    {
                        c.Item().Text($"To : {InvoiceModel.Customer?.CustomerName ?? InvoiceModel.CustomerName}").Bold().FontSize(9);
                        c.Item().Text(InvoiceModel.Customer?.FullAddress ?? "").FontSize(9);
                        c.Item().Text($"Ph : {InvoiceModel.Customer?.ContactNumber}").FontSize(9);
                        c.Item().Text($"GST NO : {InvoiceModel.Customer?.GstNumber}").FontSize(9);
                    });

                    table.Cell().BorderBottom(1).Padding(5).Column(c =>
                    {
                        c.Item().Row(row => { row.RelativeItem().Text("PO No").Bold().FontSize(9); row.RelativeItem().Text($": {InvoiceModel.PoNumber}").Bold().FontSize(9); });
                        c.Item().Row(row => { row.RelativeItem().Text("PO Date").Bold().FontSize(9); row.RelativeItem().Text($": {InvoiceModel.PurchaseOrder?.PoDate:dd-MMM-yyyy}").Bold().FontSize(9); });
                        c.Item().Row(row => { row.RelativeItem().Text("Contact Name").Bold().FontSize(9); row.RelativeItem().Text($": {InvoiceModel.ContactName}").Bold().FontSize(9); });
                        c.Item().Row(row => { row.RelativeItem().Text("Contact No").Bold().FontSize(9); row.RelativeItem().Text($": {InvoiceModel.ContactNo}").Bold().FontSize(9); });
                        c.Item().Row(row => { row.RelativeItem().Text("Vehicle No").Bold().FontSize(9); row.RelativeItem().Text($": {InvoiceModel.VehicleNo}").Bold().FontSize(9); });
                    });

                    // ROW 3: Ship To | DC Details
                    table.Cell().BorderRight(1).Padding(5).Column(c =>
                    {
                         string shipTo = !string.IsNullOrEmpty(InvoiceModel.ShippingAddress) ? InvoiceModel.ShippingAddress : (InvoiceModel.Customer?.FullAddress ?? "");
                         c.Item().Text($"Ship To : {InvoiceModel.Customer?.CustomerName ?? InvoiceModel.CustomerName}").Bold().FontSize(9);
                         c.Item().Text(shipTo).FontSize(9);
                         c.Item().Text($"GST NO : {InvoiceModel.Customer?.GstNumber}").FontSize(9);
                    });

                    table.Cell().Padding(5).Column(c =>
                    {
                        c.Item().Row(row => { row.RelativeItem().Text("SIM DC No").Bold().FontSize(9); row.RelativeItem().Text($": {InvoiceModel.SimDcNo}").Bold().FontSize(9); });
                        c.Item().Row(row => { row.RelativeItem().Text("Your DC No").Bold().FontSize(9); row.RelativeItem().Text($": {InvoiceModel.YourDcNo}").Bold().FontSize(9); });
                    });
                });

                // ITEMS TABLE
                column.Item().PaddingTop(0).Border(1).BorderTop(0).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);  // SNo
                        columns.RelativeColumn();    // Desc
                        columns.ConstantColumn(60);  // HSN
                        columns.ConstantColumn(70);  // Price
                        columns.ConstantColumn(40);  // Qty
                        columns.ConstantColumn(80);  // Amount
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().BorderRight(1).BorderBottom(1).Padding(2).AlignCenter().Text("S.no").Bold().FontSize(9);
                        header.Cell().BorderRight(1).BorderBottom(1).Padding(2).Text("Description").Bold().FontSize(9);
                        header.Cell().BorderRight(1).BorderBottom(1).Padding(2).AlignCenter().Text("Hsncode").Bold().FontSize(9);
                        header.Cell().BorderRight(1).BorderBottom(1).Padding(2).AlignRight().Text("Unit Price").Bold().FontSize(9);
                        header.Cell().BorderRight(1).BorderBottom(1).Padding(2).AlignCenter().Text("Qty").Bold().FontSize(9);
                        header.Cell().BorderBottom(1).Padding(2).AlignRight().Text("Amount").Bold().FontSize(9);
                    });

                    // Items
                    int i = 1;
                    foreach (var item in InvoiceModel.Items)
                    {
                        table.Cell().BorderRight(1).Padding(2).AlignCenter().Text(i++.ToString()).FontSize(9);
                        table.Cell().BorderRight(1).Padding(2).Text(item.ItemDescription ?? "").FontSize(9);
                        table.Cell().BorderRight(1).Padding(2).AlignCenter().Text(item.HsnCode ?? "").FontSize(9);
                        table.Cell().BorderRight(1).Padding(2).AlignRight().Text(item.UnitPrice.ToString("N2")).FontSize(9);
                        table.Cell().BorderRight(1).Padding(2).AlignCenter().Text(item.Quantity.ToString()).FontSize(9);
                        table.Cell().Padding(2).AlignRight().Text(item.LineAmount.ToString("N2")).FontSize(9);
                    }
                    
                    // Filler Rows if needed to look "Page height" full?
                    // Optional.
                });
                
                // TOTAL Row
                column.Item().Border(1).BorderTop(0).Row(row => 
                {
                    row.RelativeItem().Padding(2).Text("Total").Bold().FontSize(9);
                    // Match visual columns? Hard with Row vs Table. 
                    // Better to append to Table Footer? 
                });

                // Summary Block (Split 65/35)
                column.Item().Border(1).BorderTop(0).Row(row =>
                {
                    // Left: Remarks
                    row.RelativeItem(6.5f).BorderRight(1).Padding(5).Column(c =>
                    {
                        c.Item().Text("Remarks").Bold().FontSize(9);
                        c.Item().Text(InvoiceModel.Remarks ?? "").FontSize(9);
                    });

                    // Right: Taxes
                    row.RelativeItem(3.5f).Padding(5).Column(c =>
                    {
                        void AddTax(string label, decimal val)
                        {
                            if(val > 0)
                                c.Item().Row(r => { 
                                    r.RelativeItem().Text(label).FontSize(9); 
                                    r.RelativeItem().AlignRight().Text(val.ToString("N2")).Bold().FontSize(9); 
                                });
                        }
                        
                        AddTax($"CGST @{InvoiceModel.CgstPercent}%", InvoiceModel.CgstAmount);
                        AddTax($"SGST @{InvoiceModel.SgstPercent}%", InvoiceModel.SgstAmount);
                        AddTax($"IGST @{InvoiceModel.IgstPercent}%", InvoiceModel.IgstAmount);
                        c.Item().Row(r => { r.RelativeItem().Text("Road Freight / Transp").FontSize(9); r.RelativeItem().AlignRight().Text(InvoiceModel.FreightAmount.ToString("N2")).Bold().FontSize(9); });
                        c.Item().Row(r => { r.RelativeItem().Text("Round Off").FontSize(9); r.RelativeItem().AlignRight().Text(InvoiceModel.RoundOff.ToString("N2")).Bold().FontSize(9); });
                    });
                });

                // Words & Grand Total
                column.Item().Border(1).BorderTop(0).Row(row =>
                {
                     string words = HRPackage.Helpers.NumberToWords.ConvertAmount((double)InvoiceModel.GrandTotal);
                     row.RelativeItem(6.5f).BorderRight(1).Padding(5).Column(c => 
                     {
                         c.Item().Text("Grand Total in Words").Bold().FontSize(9);
                         c.Item().Text("Rupees " + words).Bold().Italic().FontSize(9);
                     });

                     row.RelativeItem(3.5f).Padding(5).Row(r => 
                     {
                         r.RelativeItem().Text("Grand Total (₹)").Bold().FontSize(10);
                         r.RelativeItem().AlignRight().Text(InvoiceModel.GrandTotal.ToString("N2")).Bold().FontSize(10);
                     });
                });
            });
        }

        void ComposeFooter(IContainer container)
        {
            container.Column(c =>
            {
                 c.Item().Border(1).Padding(5).Text("Declaration\nWe declare that this invoice shows the actual price of the goods described and that all particulars are true and correct.").FontSize(8);
                 
                 c.Item().Border(1).BorderTop(0).Row(row => 
                 {
                     row.RelativeItem().BorderRight(1).Height(85).Padding(5).AlignBottom().AlignCenter().Text("Customer's Seal and Signature").Bold().FontSize(9);
                     row.RelativeItem().Height(85).Padding(5).Column(col => 
                     {
                         col.Item().AlignRight().Text($"For {Company.Name}").Bold().FontSize(9);
                         col.Item().PaddingTop(30).AlignRight().Text("Authorised Signature").Bold().FontSize(9);
                     });
                 });
                 
                 c.Item().PaddingTop(5).AlignCenter().Text("This is a Computer Generated Invoice").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }
    }
}
