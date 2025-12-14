using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using HRPackage.Models;

namespace HRPackage.Services
{
    public class DeliveryChallanDocument : IDocument
    {
        public DeliveryChallan DcModel { get; }
        public CompanySettings Company { get; }
        public string LogoPath { get; }

        public DeliveryChallanDocument(DeliveryChallan dc, CompanySettings company, string logoPath)
        {
            DcModel = dc;
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
                    page.Content().Element(ComposeContent);
                    // page.Footer().Element(ComposeFooter); // Moved to Content for border merging
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
                        columns.ConstantColumn(160); // Details (Wider for DC details)
                    });

                    // Cell 1: Logo
                    table.Cell().BorderRight(1).Padding(5).Height(65).AlignMiddle().Element(c =>
                    {
                        if (!string.IsNullOrEmpty(LogoPath) && File.Exists(LogoPath))
                            c.Image(LogoPath).FitArea();
                    });

                    // Cell 2: Title
                    table.Cell().BorderRight(1).AlignCenter().AlignMiddle().Text("DELIVERY CHALLAN").FontSize(18).Bold();

                    // Cell 3: DC Details
                    table.Cell().Padding(5).Column(c => 
                    {
                         c.Item().Row(row => { row.AutoItem().Text("DC NO: ").Bold().FontSize(8); row.RelativeItem().Text(DcModel.DcNumber).Bold().FontSize(8); });
                         c.Item().Row(row => { row.AutoItem().Text("DC DATE: ").Bold().FontSize(8); row.RelativeItem().Text(DcModel.DcDate.ToString("dd/MM/yyyy")).Bold().FontSize(8); });
                         
                         // Attn? 
                         // Check if we have contact/attn logic. 
                         // If no explicit field, maybe skip or hardcode label if strictly following image. 
                         // Image shows "Kind Attn: ELANGOVAN". We'll use Customer Contact Person if available or just label.
                         // Using Customer.ContactPerson if exists, else ContactNumber
                         // Customer model doesn't have ContactPerson. Use "Admin" or blank.
                         c.Item().Row(row => { row.AutoItem().Text("Kind Attn: ").Bold().FontSize(8); row.RelativeItem().Text("").Bold().FontSize(8); });

                         c.Item().Row(row => { row.AutoItem().Text("Mob: ").Bold().FontSize(8); row.RelativeItem().Text(DcModel.Customer?.ContactNumber ?? "").Bold().FontSize(8); });
                         
                         c.Item().Row(row => { row.AutoItem().Text("Vehicle no: ").Bold().FontSize(8); row.RelativeItem().Text(DcModel.VehicleNo ?? "").Bold().FontSize(8); });
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
                        c.Item().Text($"From : {Company.Name ?? "SAI INDUSTRY & MARKETING"}").Bold().FontSize(8);
                        c.Item().Text($"{Company.AddressLine1}, {Company.AddressLine2}").FontSize(8);
                        c.Item().Text($"Email : {Company.Email}").FontSize(8);
                        c.Item().Text($"Ph: {Company.Phone}").FontSize(8);
                        c.Item().Text($"GST NO : {Company.GstNumber}").Bold().FontSize(8);
                    });

                    // To
                    table.Cell().Padding(5).Column(c =>
                    {
                        var custName = DcModel.TargetCompany ?? DcModel.Customer?.CustomerName ?? "Unknown";
                        c.Item().Text($"To : {custName}").Bold().FontSize(8);
                        c.Item().Text(DcModel.Customer?.FullAddress ?? "").FontSize(8);
                        c.Item().Text($"GST NO : {DcModel.Customer?.GstNumber}").Bold().FontSize(8);
                        c.Item().Text("VENDOR CODE : 208404").Bold().FontSize(8); // Hardcoded from image
                    });
                });

                // 3. Items Table
                column.Item().Border(1).BorderTop(0).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);  // Sl.No
                        columns.RelativeColumn();    // Description
                        columns.ConstantColumn(80);  // Quantity
                        columns.ConstantColumn(100); // Remark
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().BorderRight(1).BorderBottom(1).Padding(2).AlignCenter().Text("Sl.No").Bold().FontSize(9);
                        header.Cell().BorderRight(1).BorderBottom(1).Padding(2).AlignCenter().Text("Description").Bold().FontSize(9);
                        header.Cell().BorderRight(1).BorderBottom(1).Padding(2).AlignCenter().Text("Quantity").Bold().FontSize(9);
                        header.Cell().BorderBottom(1).Padding(2).AlignCenter().Text("Remark").Bold().FontSize(9);
                    });

                    // Items
                    int i = 1;
                    foreach (var item in DcModel.Items)
                    {
                        table.Cell().BorderRight(1).Padding(2).AlignCenter().Text(i++.ToString()).FontSize(9);
                        table.Cell().BorderRight(1).Padding(2).Text(item.Description).FontSize(9);
                        
                        // Quantity + Unit
                        table.Cell().BorderRight(1).Padding(2).AlignCenter().Text($"{item.Quantity} {item.Unit}").FontSize(9);
                        
                        table.Cell().Padding(2).AlignCenter().Text(item.Remarks).FontSize(9);
                    }

                    // Filler Row to extend borders to bottom
                    table.Cell().BorderRight(1).Height(400); // Sl
                    table.Cell().BorderRight(1).Height(400); // Desc
                    table.Cell().BorderRight(1).Height(400); // Qty
                    table.Cell().Height(400);                // Remark (No right border)
                });

                // 4. Signature Block (Merged)
                column.Item().Border(1).BorderTop(0).Row(row => 
                {
                    // Left: Customer Seal
                    row.RelativeItem().BorderRight(1).Height(80).Padding(5).AlignBottom().AlignCenter().Text("Customer's Seal and Signature").Bold().FontSize(9);
                    
                    // Right: Company Sign
                    row.RelativeItem().Height(80).Padding(5).Column(col => 
                    {
                        col.Item().AlignRight().Text($"For {Company.Name}").Bold().FontSize(9);
                        col.Item().PaddingTop(40).AlignRight().Text("Authorised Signature").Bold().FontSize(9);
                    });
                });
                
                // Bottom Note
                column.Item().Border(1).BorderTop(0).Padding(2).AlignCenter().Text("This is a Computer Generated DC").FontSize(8).FontColor(Colors.Grey.Medium);
            });
        }

        void ComposeFooter(IContainer container)
        {
             // Removed per user request
        }
    }
}
