using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using HRPackage.Models;

namespace HRPackage.Services
{
    public class QuotationDocument : IDocument
    {
        public Quotation QuotationModel { get; }
        public CompanySettings Company { get; }
        public string LogoPath { get; }

        public QuotationDocument(Quotation quotation, CompanySettings company, string logoPath)
        {
            QuotationModel = quotation;
            Company = company;
            LogoPath = logoPath;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Margin(20);
                    // page.ContentFromRightToLeft(); // No
                    
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                });
        }

        void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                // 1. Header Section (Boxed)
                column.Item().Border(1).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(140); // Logo Section
                        columns.RelativeColumn();    // Title
                        columns.ConstantColumn(140); // Details
                    });

                    // Cell 1: Logo
                    table.Cell().BorderRight(1).Padding(5).Height(50).AlignMiddle().Element(c =>
                    {
                        if (!string.IsNullOrEmpty(LogoPath) && File.Exists(LogoPath))
                            c.Image(LogoPath).FitArea();
                    });

                    // Cell 2: Title
                    table.Cell().BorderRight(1).AlignCenter().AlignMiddle().Text("Quotation").FontSize(20).Bold();

                    // Cell 3: Quote Details
                    table.Cell().Padding(5).Column(c => 
                    {
                         c.Item().Row(row => 
                         { 
                             row.AutoItem().Text("Quotation No :").Bold().FontSize(8); 
                             row.RelativeItem().Text(QuotationModel.QuotationNo).Bold().FontSize(8);
                         });
                         
                         c.Item().Row(row => 
                         { 
                             row.AutoItem().Text("Quotation Date :").Bold().FontSize(8); 
                             row.RelativeItem().Text(QuotationModel.Date.ToString("dd/MM/yyyy")).Bold().FontSize(8);
                         });
                    });
                });

                // 2. Info Grid (From / To)
                column.Item().Border(1).BorderTop(0).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    // From
                    table.Cell().BorderRight(1).Padding(5).Column(c =>
                    {
                        c.Item().Text($"From : {Company.Name ?? "SAI INDUSTRY & MARKETING"}").Bold().FontSize(9);
                        c.Item().Text($"{Company.AddressLine1}, {Company.AddressLine2}").FontSize(9);
                        c.Item().Text($"Email : {Company.Email}").FontSize(9);
                        // Phone/GST hardcoded in image? Or use Company.
                        c.Item().Text($"Ph: {Company.Phone} GST NO : {Company.GstNumber}").FontSize(9);
                        // Vendor Code? 
                        c.Item().Text("Vendor Code : 208404").FontSize(9); // Hardcoded from image or generic? Keeping for similarity.
                    });

                    // To
                    table.Cell().Padding(5).Column(c =>
                    {
                        var custName = QuotationModel.Customer?.CustomerName ?? "Unknown";
                        c.Item().Text($"To : {custName}").Bold().FontSize(9);
                        c.Item().Text(QuotationModel.Customer?.FullAddress ?? "").FontSize(9);
                        c.Item().Text($"GST NO : {QuotationModel.Customer?.GstNumber}").FontSize(9);
                    });
                });

                // 3. Kind Attn Row
                column.Item().Border(1).BorderTop(0).Padding(5).Row(row =>
                {
                    row.RelativeItem().Text("Kind Attn.: ").Bold().FontSize(9);
                    
                    var contactNo = QuotationModel.Customer?.ContactNumber;
                    if (!string.IsNullOrEmpty(contactNo))
                    {
                        row.RelativeItem().AlignRight().Text(t => 
                        {
                            t.Span("Cell: ").Bold().FontSize(9);
                            t.Span(contactNo).FontSize(9);
                        });
                    }
                });

                // 4. Items Table (with integrated Total Row)
                column.Item().Border(1).BorderTop(0).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);  // Sno
                        columns.RelativeColumn();    // Description
                        columns.ConstantColumn(70);  // Unit Price
                        columns.ConstantColumn(40);  // Qty
                        columns.ConstantColumn(80);  // Total Amt
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().BorderRight(1).BorderBottom(1).Padding(2).AlignCenter().Text("SNo").Bold().FontSize(9);
                        header.Cell().BorderRight(1).BorderBottom(1).Padding(2).AlignCenter().Text("Description").Bold().FontSize(9);
                        header.Cell().BorderRight(1).BorderBottom(1).Padding(2).AlignCenter().Text("Unit price").Bold().FontSize(9);
                        header.Cell().BorderRight(1).BorderBottom(1).Padding(2).AlignCenter().Text("Qty").Bold().FontSize(9);
                        header.Cell().BorderBottom(1).Padding(2).AlignCenter().Text("Total Amt").Bold().FontSize(9);
                    });

                    // Items
                    int i = 1;
                    foreach (var item in QuotationModel.Items)
                    {
                        table.Cell().BorderRight(1).Padding(2).AlignCenter().Text(i++.ToString()).FontSize(9);
                        table.Cell().BorderRight(1).Padding(2).Text(item.Description).FontSize(9);
                        table.Cell().BorderRight(1).Padding(2).AlignCenter().Text(item.UnitPrice.ToString("0.##")).FontSize(9);
                        table.Cell().BorderRight(1).Padding(2).AlignCenter().Text(item.Quantity.ToString()).FontSize(9);
                        table.Cell().Padding(2).AlignCenter().Text(item.TotalAmount.ToString("0.##")).FontSize(9);
                    }

                    // Filler Row to mimic height and extend vertical lines
                    // We generate 5 empty cells with borders to ensure vertical lines go down
                    table.Cell().BorderRight(1).Height(300); // Sno
                    table.Cell().BorderRight(1).Height(300); // Desc
                    table.Cell().BorderRight(1).Height(300); // Unit Price
                    table.Cell().BorderRight(1).Height(300); // Qty
                    table.Cell().Height(300);                // Total Amt (No right border for last col)

                    // TOTAL ROW (Integrated)
                    // Spanning first 3 columns for "Total" label
                    table.Cell().ColumnSpan(3).BorderRight(1).BorderTop(1).Padding(2).Text("Total").Bold().FontSize(9);
                    
                    // Total Qty
                    table.Cell().BorderRight(1).BorderTop(1).Padding(2).AlignCenter().Text(QuotationModel.Items.Sum(x => x.Quantity).ToString()).Bold().FontSize(9);
                    
                    // Total Amt
                    table.Cell().BorderTop(1).Padding(2).AlignCenter().Text(QuotationModel.Items.Sum(x => x.TotalAmount).ToString("0.##")).Bold().FontSize(9);
                });


                // 6. Commercial Terms & Conditions (Attached to bottom)
                // "Outer line merge" -> Border(1) and BorderTop(0) so it shares the border with the table above.
                // Removed Padding(5) from container to ensure inner table touches the borders.
                column.Item().Border(1).BorderTop(0).Column(c => 
                {
                    c.Item().Padding(5).BorderBottom(1).Text("Commercial Terms & Conditions").Bold().FontSize(9).Underline();
                    
                    c.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30); // Sl
                            columns.ConstantColumn(120); // Term Name
                            columns.RelativeColumn(); // Description
                        });

                        void AddTerm(string sl, string name, string desc)
                        {
                            // Last row doesn't strictly need BorderBottom if container has it, but consistent grid is fine.
                            // To prevent double bottom border on the very last item, we *could* handle it, but BorderBottom(1) usually overlaps fine with Container Border.
                            // Actually, if we want "perfect" merge, we shouldn't draw BorderBottom on the very last row? 
                            // Or just let it overlap. Standard overlap is usually 1pt so it looks like one line.
                            table.Cell().BorderBottom(1).BorderRight(1).Padding(2).AlignCenter().Text(sl).FontSize(8);
                            table.Cell().BorderBottom(1).BorderRight(1).Padding(2).Text(name).FontSize(8);
                            table.Cell().BorderBottom(1).Padding(2).Text(desc).FontSize(8).Bold();
                        }

                        AddTerm("1", "GST", "18% Extra");
                        AddTerm("2", "Validity", "This quotation is valid for 30 days from the date of issue.");
                        AddTerm("3", "Packing & Transport", "Included in the above pricing.");
                        AddTerm("4", "Insurance", "Extra at actual");
                        AddTerm("5", "Delivery", "Design, manufacture, supply, installation & commissioning will be completed within 3 - 4 weeks from receipt of a techno-commercially clear purchase order.");
                        AddTerm("6", "Payment", "For supply : 100% against Proforma invoice");
                        AddTerm("7", "Unloading & Handling Charges", "By client");
                    });
                });
            });
        }

        void ComposeFooter(IContainer container)
        {
             // Removed per user request
        }
    }
}
