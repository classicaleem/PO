using HRPackage.Models;
using HRPackage.Helpers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace HRPackage.Documents
{
    public class InvoiceDocument : IDocument
    {
        private readonly Invoice _invoice;
        private readonly CompanySettings _company;

        public InvoiceDocument(Invoice invoice, CompanySettings company)
        {
            _invoice = invoice;
            _company = company;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Margin(15);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));
                page.Content().Element(ComposeTableStructure);
            });
        }

        void ComposeTableStructure(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                // 1. Header Row
                table.Cell().ColumnSpan(2).Element(ComposeHeaderRow);

                // 2. Sender / Invoice Details
                table.Cell().Element(cell => BorderedCell(cell).Element(ComposeSenderDetails));
                table.Cell().Element(cell => BorderedCell(cell).Element(ComposeInvoiceDetails));

                // 3. To / PO Details
                table.Cell().Element(cell => BorderedCell(cell).Element(ComposeRecipientDetails));
                table.Cell().Element(cell => BorderedCell(cell).Element(ComposePoDetails));

                // 4. Ship To / DC Details
                table.Cell().Element(cell => BorderedCell(cell).Element(ComposeShippingDetails));
                table.Cell().Element(cell => BorderedCell(cell).Element(ComposeDcDetails));

                // 5. Items Table - This is the main focus with proper description space
                table.Cell().ColumnSpan(2).Element(cell => BorderedCell(cell).Padding(0).Element(ComposeItemsTable));

                // 6. Remarks / Totals
                table.Cell().ColumnSpan(2).Element(cell => BorderedCell(cell).Padding(0).Element(ComposeFooterTotals));

                // 7. Signatures / Declaration
                table.Cell().ColumnSpan(2).Element(cell => BorderedCell(cell).Element(ComposeSignatures));
            });
        }

        void ComposeHeaderRow(IContainer container)
        {
            container.Border(1).BorderColor(Colors.Black).Height(65).Row(row =>
            {
                row.RelativeItem(1.5f).Padding(5).Column(col =>
                {
                    var logoPath = _company.LogoPath;
                    if (string.IsNullOrEmpty(logoPath) || !System.IO.File.Exists(logoPath))
                        logoPath = "wwwroot/images/logo.jpg";
                    col.Item().MaxHeight(55).Image(logoPath).FitArea();
                });

                row.RelativeItem(1.5f).AlignCenter().AlignMiddle()
                    .Text("INVOICE").FontSize(16).Bold();

                row.RelativeItem(1).BorderLeft(1).Padding(5).AlignCenter().AlignMiddle()
                    .Text("Original for Recipient").FontSize(10);
            });
        }

        void ComposeSenderDetails(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text($"From : {_company.Name}").Bold();
                col.Item().Text($"{_company.AddressLine1}, {_company.AddressLine2}").FontSize(8);
                col.Item().Text($"Email : {_company.Email}").FontSize(8);
                col.Item().Text($"Ph: {_company.Phone}").FontSize(8);
                col.Item().Text($"GST NO : {_company.GstNumber}").Bold();
            });
        }

        void ComposeInvoiceDetails(IContainer container)
        {
            container.Column(col =>
            {
                LabelValue(col, "Invoice No", _invoice.InvoiceNumber);
                LabelValue(col, "Invoice Date", _invoice.InvoiceDate.ToString("dd-MMM-yyyy"));
            });
        }

        void ComposeRecipientDetails(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text($"To :{_invoice.CustomerName}").Bold();
                col.Item().Text(_invoice.Customer?.FullAddress ?? "").FontSize(8);
                col.Item().Text($"Ph: {_invoice.Customer?.ContactNumber}").FontSize(8);
                col.Item().Text($"GST NO : {_invoice.Customer?.GstNumber}").Bold();
            });
        }

        void ComposePoDetails(IContainer container)
        {
            container.Column(col =>
            {
                LabelValue(col, "Po No", _invoice.PoNumber ?? "-");
                LabelValue(col, "Po Date", "-");
                LabelValue(col, "Contact Name", _invoice.CustomerName ?? "-");
                LabelValue(col, "Contact No", _invoice.Customer?.ContactNumber ?? "-");
                LabelValue(col, "SIM DC No", "-");
            });
        }

        void ComposeShippingDetails(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Text($"Ship To : {(!string.IsNullOrEmpty(_invoice.ShippingAddress) ? "See Address" : _invoice.CustomerName)}").Bold();
                col.Item().Text(!string.IsNullOrEmpty(_invoice.ShippingAddress) ? _invoice.ShippingAddress : _invoice.Customer?.FullAddress).FontSize(8);
            });
        }

        void ComposeDcDetails(IContainer container)
        {
            container.Column(col =>
            {
                LabelValue(col, "Your DC No", _invoice.InternalPoCode ?? "-");
            });
        }

        void ComposeItemsTable(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);           // S.no
                    columns.RelativeColumn(3);           // Description - WIDER
                    columns.ConstantColumn(60);            // HSN Code
                    columns.ConstantColumn(70);            // Unit Price
                    columns.ConstantColumn(35);            // Qty
                    columns.ConstantColumn(70);            // Amount
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("S.no").FontSize(8);
                    header.Cell().Element(HeaderCell).Text("Description").FontSize(8);
                    header.Cell().Element(HeaderCell).Text("Hsncode").FontSize(8);
                    header.Cell().Element(HeaderCell).AlignRight().Text("Unit Price").FontSize(8);
                    header.Cell().Element(HeaderCell).AlignCenter().Text("Qty").FontSize(8);
                    header.Cell().Element(HeaderCell).AlignRight().Text("Amount").FontSize(8);
                });

                int i = 1;
                foreach (var item in _invoice.Items)
                {
                    // Serial Number
                    table.Cell().Element(BodyCell).AlignCenter().Text(i.ToString()).FontSize(8);

                    // Description - With minimum height for proper spacing
                    table.Cell().Element(cell => BodyCell(cell).MinHeight(25)).Column(col =>
                    {
                        col.Item().Text(item.ItemDescription).FontSize(8).LineHeight(2);
                    });

                    // HSN Code
                    table.Cell().Element(BodyCell).Text(item.HsnCode ?? "-").FontSize(8);

                    // Unit Price
                    table.Cell().Element(BodyCell).AlignRight().Text(item.UnitPrice.ToString("0.00")).FontSize(8);

                    // Quantity
                    table.Cell().Element(BodyCell).AlignCenter().Text(item.Quantity.ToString()).FontSize(8);

                    // Amount
                    table.Cell().Element(BodyCell).AlignRight().Text(item.LineAmount.ToString("0.00")).FontSize(8);

                    i++;
                }

                // Add empty rows for visual spacing (matching your invoice)
                for (int j = i; j <= 2; j++)
                {
                    table.Cell().Element(BodyCell).Text("");
                    table.Cell().Element(cell => BodyCell(cell).MinHeight(25)).Text("");
                    table.Cell().Element(BodyCell).Text("");
                    table.Cell().Element(BodyCell).Text("");
                    table.Cell().Element(BodyCell).Text("");
                    table.Cell().Element(BodyCell).Text("");
                }

                // Total Row
                table.Cell().ColumnSpan(4).Element(BodyCell).AlignRight().Text("Total").Bold().FontSize(9);
                table.Cell().ColumnSpan(2).Element(BodyCell).AlignRight().Text(_invoice.TotalAmount.ToString("0.00")).Bold().FontSize(9);
            });
        }

        void ComposeFooterTotals(IContainer container)
        {
            container.Row(row =>
            {
                // Left Side: Remarks
                row.RelativeItem(2).BorderRight(1).Padding(5).Column(col =>
                {
                    col.Item().Text("Remarks").Bold().FontSize(8);
                    col.Item().Height(40).Text("").FontSize(8);

                    col.Item().PaddingTop(8).Text("Grand Total in Words").Bold().FontSize(8);
                    var amountInWords = NumberToWords.ConvertAmount((double)_invoice.GrandTotal);
                    col.Item().Text($"Rupees {amountInWords}").Italic().FontSize(8);
                });

                // Right Side: Tax Breakdown
                row.RelativeItem(1).Column(col =>
                {
                    var cgstAmt = _invoice.TotalAmount * _invoice.CgstPercent / 100;
                    var sgstAmt = _invoice.TotalAmount * _invoice.SgstPercent / 100;
                    var igstAmt = _invoice.TotalAmount * _invoice.IgstPercent / 100;

                    TableTotal(col, $"Cgst @{_invoice.CgstPercent}%", cgstAmt.ToString("0.00"), 8);
                    TableTotal(col, $"Sgst @{_invoice.SgstPercent}%", sgstAmt.ToString("0.00"), 8);
                    TableTotal(col, $"Igst @{_invoice.IgstPercent}%", igstAmt.ToString("0.00"), 8);
                    TableTotal(col, "Road Freight / Transp", "0.00", 8);
                    TableTotal(col, "Round Off", _invoice.RoundOff.ToString("0.00"), 8);

                    col.Item().BorderTop(1).Padding(3).Row(r =>
                    {
                        r.RelativeItem().Text("Grand Total (₹)").Bold().FontSize(9);
                        r.RelativeItem().AlignRight().Text(_invoice.GrandTotal.ToString("0.00")).Bold().FontSize(9);
                    });
                });
            });
        }

        void ComposeSignatures(IContainer container)
        {
            container.Column(col =>
            {
                col.Item().Padding(3).Text("Declaration").Bold().FontSize(8);
                col.Item().Padding(3).Text("We declare that the this invoice shows the actual price of the goods described and that all particulars are true and correct").FontSize(7);

                col.Item().BorderTop(1).Row(row =>
                {
                    row.RelativeItem().Height(40).BorderRight(1).Column(c =>
                    {
                        c.Item().Padding(5).AlignCenter().Text("Customer's Seal and Signature").FontSize(7).Bold();
                    });

                    row.RelativeItem().Height(40).Column(c =>
                    {
                        c.Item().Padding(5).AlignRight().Text($"For {_company.Name}").FontSize(7).Bold();
                        c.Item().ExtendVertical().AlignBottom().AlignRight().Padding(5).Text("Authorised Signature").FontSize(7);
                    });
                });

                col.Item().BorderTop(1).AlignCenter().Padding(2).Text("This is a Computer Generated Invoice").FontSize(7);
            });
        }

        // Helpers
        static IContainer BorderedCell(IContainer container) =>
            container.Border(1).BorderColor(Colors.Black).Padding(3);

        static IContainer HeaderCell(IContainer container) =>
            container.Border(1).BorderColor(Colors.Black).Padding(2).DefaultTextStyle(x => x.Bold());

        static IContainer BodyCell(IContainer container) =>
            container.Border(1).BorderColor(Colors.Black).Padding(2);

        void LabelValue(ColumnDescriptor col, string label, string value)
        {
            col.Item().Row(row =>
            {
                row.ConstantItem(70).Text(label).Bold().FontSize(8);
                row.ConstantItem(10).Text(":").FontSize(8);
                row.RelativeItem().Text(value).FontSize(8);
            });
        }

        void TableTotal(ColumnDescriptor col, string label, string value, int fontSize)
        {
            col.Item().Padding(2).Row(r =>
            {
                r.RelativeItem().Text(label).FontSize(fontSize);
                r.RelativeItem().AlignRight().Text(value).FontSize(fontSize);
            });
        }
    }
}