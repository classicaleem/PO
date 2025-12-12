using HRPackage.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HRPackage.Documents
{
    public class PurchaseOrderDocument : IDocument
    {
        private readonly PurchaseOrder _po;
        private readonly CompanySettings _company;

        public PurchaseOrderDocument(PurchaseOrder po, CompanySettings company)
        {
            _po = po;
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
                    // page.Foreground().AlignCenter().AlignMiddle().Rotate(-45).Text("PURCHASE ORDER").FontSize(80).FontColor(Colors.Grey.Lighten3).SemiBold();
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
                    column.Item().AlignRight().Text("PURCHASE ORDER").FontSize(24).SemiBold();
                    column.Item().AlignRight().Text($"PO #: {_po.PoNumber}");
                    column.Item().AlignRight().Text($"Date: {_po.PoDate:dd-MMM-yyyy}");
                    column.Item().AlignRight().Text($"Ref: {_po.InternalPoCode}");
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(40).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Component(new AddressComponent("OFFICIAL VENDOR", _po.SupplierName, _po.Customer?.FullAddress, _po.Customer?.GstNumber));
                    row.ConstantItem(50);
                    row.RelativeItem().Component(new AddressComponent("DELIVER TO", _company.Name, $"{_company.AddressLine1}\n{_company.AddressLine2}", _company.GstNumber));
                });

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
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(4);
                    columns.RelativeColumn(2); // HSN
                    columns.RelativeColumn(2); // Qty
                    columns.RelativeColumn(2); // Unit Price
                    columns.RelativeColumn(2); // Line Total
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("#");
                    header.Cell().Element(CellStyle).Text("Description");
                    header.Cell().Element(CellStyle).Text("HSN Code");
                    header.Cell().Element(CellStyle).AlignRight().Text("Quantity");
                    header.Cell().Element(CellStyle).AlignRight().Text("Unit Price");
                    header.Cell().Element(CellStyle).AlignRight().Text("Total");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold().Color(Colors.White))
                            .Background(Colors.Blue.Medium)
                            .Padding(5);
                    }
                });

                int i = 1;
                foreach (var item in _po.Items)
                {
                    table.Cell().Element(CellStyle).Text(i.ToString());
                    table.Cell().Element(CellStyle).Text(item.ItemDescription);
                    table.Cell().Element(CellStyle).Text(item.HsnCode ?? "-");
                    table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text(item.UnitPrice.ToString("N2"));
                    table.Cell().Element(CellStyle).AlignRight().Text(item.LineTotal.ToString("N2"));

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
                     c.Item().Text("1. Please ship items to the delivery address mentioned above.");
                     c.Item().Text("2. Payment terms as per agreement.");
                });

                row.RelativeItem().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Sub Total").AlignRight();
                        row.RelativeItem().Text(_po.PoAmount.ToString("N2")).AlignRight();
                    });

                    // Check if we calculate taxes for PO? Yes we added fields.
                    if (_po.TaxAmount > 0)
                    {
                         column.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Tax").AlignRight();
                            row.RelativeItem().Text(_po.TaxAmount.ToString("N2")).AlignRight();
                        });
                    }

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Round Off").AlignRight();
                        row.RelativeItem().Text(_po.RoundOff.ToString("N2")).AlignRight();
                    });

                    column.Item().PaddingTop(5).BorderTop(1).Row(row =>
                    {
                        row.RelativeItem().Text("Grand Total").Bold().AlignRight();
                        row.RelativeItem().Text(_po.GrandTotal.ToString("N2")).Bold().AlignRight();
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
                    // column.Item().PaddingTop(20).Text("Vendor Signature").SemiBold();
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
