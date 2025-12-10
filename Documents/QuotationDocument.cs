using HRPackage.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HRPackage.Documents
{
    public class QuotationDocument : IDocument
    {
        private readonly Quotation _quotation;
        private readonly CompanySettings _company;

        public QuotationDocument(Quotation quotation, CompanySettings company)
        {
            _quotation = quotation;
            _company = company;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter);
                    // Watermark Removed
                    // page.Foreground().AlignCenter().AlignMiddle().Rotate(-45).Text("QUOTATION").FontSize(80).FontColor(Colors.Grey.Lighten3).SemiBold();
                });
        }

        void ComposeHeader(IContainer container)
        {
            var logoPath = _company.LogoPath;
            if (string.IsNullOrEmpty(logoPath) || !System.IO.File.Exists(logoPath))
                 logoPath = "wwwroot/images/logo.jpg";

            container.Row(row =>
            {
                // Use MaxHeight and FitArea to avoid size conflicts
                row.ConstantItem(100).MaxHeight(70).Image(logoPath).FitArea();

                row.RelativeItem().PaddingLeft(10).Column(column =>
                {
                    column.Item().Text(_company.Name).FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                    column.Item().Text(_company.AddressLine1);
                    column.Item().Text(_company.AddressLine2);
                    column.Item().Text($"Email: {_company.Email}");
                });

                row.RelativeItem().Column(column =>
                {
                    column.Item().AlignRight().Text("QUOTATION").FontSize(24).SemiBold();
                    column.Item().AlignRight().Text($"Quote #: {_quotation.QuotationNo}");
                    column.Item().AlignRight().Text($"Date: {_quotation.Date:dd-MMM-yyyy}");
                    if(_quotation.ValidUntil.HasValue)
                        column.Item().AlignRight().Text($"Valid Until: {_quotation.ValidUntil.Value:dd-MMM-yyyy}");
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(40).Column(column =>
            {
                column.Item().Component(new AddressComponent("TO", _quotation.Customer?.CustomerName ?? "", _quotation.Customer?.FullAddress ?? "", _quotation.Customer?.GstNumber ?? ""));

                column.Item().PaddingTop(25).Element(ComposeTable);
                
                column.Item().PaddingTop(25).Element(ComposeTotals);
            });
        }

        void ComposeTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2); // Qty
                    columns.RelativeColumn(2); // Rate
                    columns.RelativeColumn(2); // Amount
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("#");
                    header.Cell().Element(CellStyle).Text("Description");
                    header.Cell().Element(CellStyle).AlignRight().Text("Quantity");
                    header.Cell().Element(CellStyle).AlignRight().Text("Rate");
                    header.Cell().Element(CellStyle).AlignRight().Text("Amount");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold().Color(Colors.White))
                            .Background(Colors.Blue.Medium)
                            .Padding(5);
                    }
                });

                int i = 1;
                foreach (var item in _quotation.Items)
                {
                    table.Cell().Element(CellStyle).Text(i.ToString());
                    table.Cell().Element(CellStyle).Text(item.Description);
                    table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text(item.UnitPrice.ToString("N2"));
                    table.Cell().Element(CellStyle).AlignRight().Text(item.TotalAmount.ToString("N2"));

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
                    }
                    i++;
                }
            });
        }

        void ComposeTotals(IContainer container)
        {
             container.Row(row => 
            {
                row.RelativeItem().Column(c => 
                {
                     c.Item().Text("Terms & Conditions:").Bold();
                     c.Item().Text("1. This quotation is valid for the period mentioned above.");
                     c.Item().Text("2. Taxes as applicable.");
                });

                row.RelativeItem().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Total Amount").Bold().AlignRight();
                        row.RelativeItem().Text(_quotation.TotalAmount.ToString("N2")).Bold().AlignRight();
                    });
                });
            });
        }

        void ComposeFooter(IContainer container)
        {
             container.Row(row =>
             {
                 row.RelativeItem().Column(column =>
                 {
                 });
                 
                 row.RelativeItem().AlignRight().Column(column =>
                 {
                    column.Item().Text($"For {_company.Name}").SemiBold();
                    column.Item().PaddingTop(30).Text("Authorised Signatory").SemiBold();
                 });
             });
        }
    }
}
