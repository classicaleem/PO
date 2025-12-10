using HRPackage.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HRPackage.Documents
{
    public class DeliveryChallanDocument : IDocument
    {
        private readonly DeliveryChallan _dc;
        private readonly CompanySettings _company;

        public DeliveryChallanDocument(DeliveryChallan dc, CompanySettings company)
        {
            _dc = dc;
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
                    
                    // Watermark Removed
                    // page.Foreground().AlignCenter().AlignMiddle().Rotate(-45).Text("DELIVERY CHALLAN").FontSize(60).FontColor(Colors.Grey.Lighten3).SemiBold();
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
                    column.Item().AlignRight().Text("DELIVERY CHALLAN").FontSize(24).SemiBold();
                    column.Item().AlignRight().Text($"DC #: {_dc.DcNumber}");
                    column.Item().AlignRight().Text($"Date: {_dc.DcDate:dd-MMM-yyyy}");
                    if(!string.IsNullOrEmpty(_dc.VehicleNo))
                        column.Item().AlignRight().Text($"Vehicle No: {_dc.VehicleNo}");
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(40).Column(column =>
            {
                column.Item().Row(row =>
                {
                    row.RelativeItem().Component(new AddressComponent("CONSIGNEE", _dc.TargetCompany, _dc.Customer?.FullAddress ?? "", ""));
                    row.ConstantItem(50);
                    row.RelativeItem().Component(new AddressComponent("FROM", _company.Name, $"{_company.AddressLine1}\n{_company.AddressLine2}", ""));
                });

                column.Item().PaddingTop(25).Element(ComposeTable);
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
                    columns.RelativeColumn(2); // Quantity
                    columns.RelativeColumn(2); // Unit
                    columns.RelativeColumn(3); // Remarks
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("#");
                    header.Cell().Element(CellStyle).Text("Description");
                    header.Cell().Element(CellStyle).AlignRight().Text("Quantity");
                    header.Cell().Element(CellStyle).Text("Unit");
                    header.Cell().Element(CellStyle).Text("Remarks");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.DefaultTextStyle(x => x.SemiBold().Color(Colors.White))
                            .Background(Colors.Blue.Medium)
                            .Padding(5);
                    }
                });

                int i = 1;
                foreach (var item in _dc.Items)
                {
                    table.Cell().Element(CellStyle).Text(i.ToString());
                    table.Cell().Element(CellStyle).Text(item.Description);
                    table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                    table.Cell().Element(CellStyle).Text(item.Unit);
                    table.Cell().Element(CellStyle).Text(item.Remarks);

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
                    }
                    i++;
                }
            });
        }

        void ComposeFooter(IContainer container)
        {
             container.Row(row =>
             {
                 row.RelativeItem().Column(column =>
                 {
                    column.Item().PaddingTop(40).Text("Receiver's Signature & Stamp").SemiBold();
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
