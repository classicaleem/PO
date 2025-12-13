using FastReport;
using FastReport.Export.PdfSimple;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
// using QuestPDF.Infrastructure;
using FastReport.Table;
using FastReport.Utils;
using FastReport.Data;
using HRPackage.Models;
using System.Drawing;
using QuestPDF.Fluent;
// using QuestPDF.Infrastructure; // Removed to avoid Color ambiguity

namespace HRPackage.Services
{
    public interface IReportService
    {
        byte[] GeneratePurchaseOrderPdf(PurchaseOrder po, CompanySettings company);
        byte[] GenerateInvoicePdf(Invoice invoice, CompanySettings company);
        byte[] GenerateQuotationPdf(Quotation quotation, CompanySettings company);
        byte[] GenerateDeliveryChallanPdf(DeliveryChallan dc, CompanySettings company);
    }

    public class ReportService : IReportService
    {
        private readonly IWebHostEnvironment _env;

        public ReportService(IWebHostEnvironment env)
        {
            _env = env;
            Config.WebMode = true; 
        }

        public byte[] GeneratePurchaseOrderPdf(PurchaseOrder po, CompanySettings company)
        {
            string reportPath = Path.Combine(_env.WebRootPath, "reports", "PurchaseOrder.frx");
            if (File.Exists(reportPath))
            {
                 using var customReport = new Report();
                 customReport.Load(reportPath);
                 customReport.RegisterData(new[] { po }, "PurchaseOrder");
                 customReport.RegisterData(po.Items, "PoItems");
                 customReport.RegisterData(new[] { company }, "Company");
                 customReport.Prepare();
                 using var customMs = new MemoryStream();
                 var customPdfExport = new PDFSimpleExport();
                 customPdfExport.Export(customReport, customMs);
                 return customMs.ToArray();
            }

            using var report = new Report();
            var page = new ReportPage();
            page.Name = "Page1";
            report.Pages.Add(page);

            // Watermark
            // Watermark
            string watermarkPath = Path.Combine(_env.WebRootPath, "images", "logo.jpg");
            if (File.Exists(watermarkPath))
            {
                page.Watermark.Image = new Bitmap(watermarkPath);
                page.Watermark.Enabled = true;
                page.Watermark.ShowImageOnTop = false; // Background
                // page.Watermark.ImageTransparency = 0.8f; // Commented out to ensure build
            }

            // Bounds
            float pageWidth = page.PaperWidth - page.LeftMargin - page.RightMargin; // mm

            // 1. Report Title Band
            var reportTitle = new ReportTitleBand();
            reportTitle.Name = "ReportTitle";
            reportTitle.Height = Units.Millimeters * 40;
            page.ReportTitle = reportTitle;

            // Logo
            string logoPath = Path.Combine(_env.WebRootPath, "images", "logo.jpg");
            if (File.Exists(logoPath))
            {
                var logoImg = new PictureObject();
                logoImg.Name = "Logo";
                logoImg.Image = new Bitmap(logoPath);
                logoImg.Bounds = new RectangleF(0, 0, Units.Millimeters * 25, Units.Millimeters * 25);
                // logoImg.SizeMode = FastReport.PictureBoxSizeMode.Zoom;
                reportTitle.Objects.Add(logoImg);
            }

            // Company Info (Left, next to logo)
            var companyText = new TextObject();
            companyText.Name = "CompanyInfo";
            companyText.Bounds = new RectangleF(Units.Millimeters * 30, 0, Units.Millimeters * 80, Units.Millimeters * 25);
            companyText.Text = $"{company.Name}\n{company.AddressLine1}\n{company.AddressLine2}\nGST: {company.GstNumber}\nEmail: {company.Email}";
            companyText.Font = new Font("Arial", 9);
            reportTitle.Objects.Add(companyText);

            // Title "PURCHASE ORDER" (Right)
            var titleText = new TextObject();
            titleText.Name = "Title";
            titleText.Bounds = new RectangleF(Units.Millimeters * 110, 0, Units.Millimeters * 80, Units.Millimeters * 10);
            titleText.Text = "PURCHASE ORDER";
            titleText.HorzAlign = HorzAlign.Right;
            titleText.Font = new Font("Arial", 16, FontStyle.Bold);
            reportTitle.Objects.Add(titleText);

            // PO Details (Right)
            var poDetailsText = new TextObject();
            poDetailsText.Name = "PoDetails";
            poDetailsText.Bounds = new RectangleF(Units.Millimeters * 110, Units.Millimeters * 12, Units.Millimeters * 80, Units.Millimeters * 20);
            poDetailsText.Text = $"PO #: {po.PoNumber}\nDate: {po.PoDate:dd-MMM-yyyy}\nRef: {po.InternalPoCode}";
            poDetailsText.HorzAlign = HorzAlign.Right;
            poDetailsText.Font = new Font("Arial", 10);
            reportTitle.Objects.Add(poDetailsText);

            // 2. Data Band (Items)
            var dataBand = new DataBand();
            dataBand.Name = "Data";
            page.Bands.Add(dataBand);

            // Header for Table (Vendor Info & Ship To)
            var infoTable = new TableObject();
            infoTable.Name = "InfoTable";
            infoTable.Bounds = new RectangleF(0, Units.Millimeters * 30, Units.Millimeters * 190, Units.Millimeters * 25);
            infoTable.ColumnCount = 2;
            infoTable.RowCount = 2;
            
            // Col widths
            infoTable.Columns[0].Width = Units.Millimeters * 95;
            infoTable.Columns[1].Width = Units.Millimeters * 95;

            // Headers
            infoTable[0, 0].Text = "VENDOR";
            infoTable[0, 0].Font = new Font("Arial", 8, FontStyle.Bold);
            infoTable[0, 0].Border.Lines = BorderLines.Bottom;
            
            infoTable[1, 0].Text = "SHIP TO";
            infoTable[1, 0].Font = new Font("Arial", 8, FontStyle.Bold);
            infoTable[1, 0].Border.Lines = BorderLines.Bottom;

            // Content
            infoTable[0, 1].Text = $"{po.SupplierName}\n{po.Customer?.FullAddress}\nGST: {po.Customer?.GstNumber}";
            infoTable[0, 1].Font = new Font("Arial", 9);
            
            infoTable[1, 1].Text = $"{company.Name}\n{company.AddressLine1}\n{company.AddressLine2}\nGST: {company.GstNumber}";
            infoTable[1, 1].Font = new Font("Arial", 9);

            reportTitle.Objects.Add(infoTable);


            // 3. Items Table Header
            var headerBand = new PageHeaderBand();
            headerBand.Name = "Header";
            headerBand.Height = Units.Millimeters * 10;
            page.PageHeader = headerBand;

            var tableHeader = new TableObject();
            tableHeader.Name = "TableHeader";
            tableHeader.Bounds = new RectangleF(0, 0, Units.Millimeters * 190, Units.Millimeters * 8);
            tableHeader.RowCount = 1;
            tableHeader.ColumnCount = 6;
            
            // Columns: #, Desc, HSN, Qty, Price, Total
            float[] colWidths = { 10, 80, 20, 25, 25, 30 }; // Total 190
            for(int i=0; i<6; i++) tableHeader.Columns[i].Width = Units.Millimeters * colWidths[i];

            string[] headers = { "#", "Description", "HSN", "Qty", "Price", "Total" };
            for(int i=0; i<6; i++)
            {
                tableHeader[i, 0].Text = headers[i];
                tableHeader[i, 0].Font = new Font("Arial", 9, FontStyle.Bold);
                tableHeader[i, 0].Fill = new SolidFill(Color.FromArgb(230, 230, 230));
                tableHeader[i, 0].Border.Lines = BorderLines.All;
                tableHeader[i, 0].HorzAlign = (i >= 3) ? HorzAlign.Right : HorzAlign.Left;
                if(i==0) tableHeader[i,0].HorzAlign = HorzAlign.Center;
            }
            headerBand.Objects.Add(tableHeader);


            // 4. Items Data
            dataBand.Height = Units.Millimeters * 6;
            var tableData = new TableObject();
            tableData.Name = "TableData";
            tableData.Bounds = new RectangleF(0, 0, Units.Millimeters * 190, Units.Millimeters * 6);
            tableData.RowCount = 1;
            tableData.ColumnCount = 6;
            for(int i=0; i<6; i++) tableData.Columns[i].Width = Units.Millimeters * colWidths[i];

            // Mapping
            // Since we can't easily bind list directly without registering data, we'll iterate
            // But FastReport works best with DataSources.
            // For simplicity in Code-First without RegisterData, we can just build the table row by row
            // ACTUALLY: Code-first loop adding objects to band is easier for simple lists.
            
            // Let's use a TableObject that grows? No, better to use the Band mechanism.
            // We will manually build a TableObject with N rows in the ReportTitle or Detail?
            // "DataBand" is repeated for each record.
            
            // Alternative: Build one big table in DataBand? No.
            // Manual Approach: Create a TABLE inside the ReportTitle (or Summary) is risky for page breaks.
            // Correct Code-First: Register BusinessObject. Only Register Parent PO.
            RegisterAndEnable(report, new[] { po }, "PurchaseOrder"); 
            
            // Register "PoItems" MANUALLY as a BusinessObjectDataSource to ensure it is available as a table
            report.RegisterData(po.Items, "PoItems");
            var poItemsDs = report.GetDataSource("PoItems");
            if (poItemsDs != null)
            {
                poItemsDs.Enabled = true;
                EnableAll(poItemsDs);
                dataBand.DataSource = poItemsDs;
            }

            
            // Now we need text objects inside DataBand bound to data
            // Or a TableObject with one row configured to print?
            // Simpler: Just text objects lined up.
            
            float curX = 0;
            
            // #
            var txtNo = CreateText(dataBand, "[PoItems.LineNumber]", curX, colWidths[0], HorzAlign.Center);
            curX += colWidths[0];

            // Desc
            var txtDesc = CreateText(dataBand, "[PoItems.ItemDescription]", curX, colWidths[1], HorzAlign.Left);
            curX += colWidths[1];

            // HSN
            var txtHsn = CreateText(dataBand, "[PoItems.HsnCode]", curX, colWidths[2], HorzAlign.Left);
            curX += colWidths[2];

            // Qty
            var txtQty = CreateText(dataBand, "[PoItems.Quantity]", curX, colWidths[3], HorzAlign.Right);
            curX += colWidths[3];

            // Price
            var txtPrice = CreateText(dataBand, "[PoItems.UnitPrice]", curX, colWidths[4], HorzAlign.Right);
            txtPrice.Format = new FastReport.Format.NumberFormat() { UseLocale = false, DecimalDigits = 2 };
            curX += colWidths[4];

            // Total
            var txtTotal = CreateText(dataBand, "[PoItems.LineTotal]", curX, colWidths[5], HorzAlign.Right);
            txtTotal.Format = new FastReport.Format.NumberFormat() { UseLocale = false, DecimalDigits = 2 };
            
            // 5. Totals (Report Summary)
            var summaryBand = new ReportSummaryBand();
            summaryBand.Name = "Summary";
            summaryBand.Height = Units.Millimeters * 50;
            page.ReportSummary = summaryBand;

            float rightStart = Units.Millimeters * 130;
            float rightWidth = Units.Millimeters * 60;
            float lineH = Units.Millimeters * 6;
            float curY = Units.Millimeters * 5;

            AddSummaryRow(summaryBand, "Sub Total:", po.PoAmount, rightStart, curY, rightWidth);
            curY += lineH;

            if (po.TaxAmount > 0)
            {
                AddSummaryRow(summaryBand, "Tax:", po.TaxAmount, rightStart, curY, rightWidth);
                curY += lineH;
            }
            
            AddSummaryRow(summaryBand, "Round Off:", po.RoundOff, rightStart, curY, rightWidth);
            curY += lineH;

            // Grand Total
            var gtLabel = new TextObject();
            gtLabel.Bounds = new RectangleF(rightStart, curY, rightWidth/2, lineH);
            gtLabel.Text = "Grand Total:";
            gtLabel.Font = new Font("Arial", 10, FontStyle.Bold);
            gtLabel.HorzAlign = HorzAlign.Right;
            summaryBand.Objects.Add(gtLabel);

            var gtValue = new TextObject();
            gtValue.Bounds = new RectangleF(rightStart + rightWidth/2, curY, rightWidth/2, lineH);
            gtValue.Text = po.GrandTotal.ToString("N2");
            gtValue.Font = new Font("Arial", 10, FontStyle.Bold);
            gtValue.HorzAlign = HorzAlign.Right;
            summaryBand.Objects.Add(gtValue);


            // Prepare
            report.Prepare();
            
            // Export
            using var ms = new MemoryStream();
            var pdfExport = new PDFSimpleExport();
            pdfExport.Export(report, ms);
            return ms.ToArray();
        }

        public byte[] GenerateInvoicePdf(Invoice invoice, CompanySettings company)
        {
            // Use QuestPDF for Invoice
            string logoPath = Path.Combine(_env.WebRootPath, "images", "logo.jpg");
            var document = new InvoiceDocument(invoice, company, logoPath);
            return document.GeneratePdf();
        }

        public byte[] GenerateQuotationPdf(Quotation quotation, CompanySettings company)
        {
            string reportPath = Path.Combine(_env.WebRootPath, "reports", "Quotation.frx");
            if (File.Exists(reportPath))
            {
                 using var customReport = new Report();
                 customReport.Load(reportPath);
                 customReport.RegisterData(new[] { quotation }, "Quotation");
                 customReport.RegisterData(quotation.Items, "QItems");
                 customReport.RegisterData(new[] { company }, "Company");
                 customReport.Prepare();
                 using var customMs = new MemoryStream();
                 var customPdfExport = new PDFSimpleExport();
                 customPdfExport.Export(customReport, customMs);
                 return customMs.ToArray();
            }

            using var report = new Report();
            var page = new ReportPage();
            report.Pages.Add(page);

            // Watermark
            // Watermark
            string watermarkPath = Path.Combine(_env.WebRootPath, "images", "logo.jpg");
            if (File.Exists(watermarkPath))
            {
                page.Watermark.Image = new Bitmap(watermarkPath);
                page.Watermark.Enabled = true;
                page.Watermark.ShowImageOnTop = false; // Background
                // page.Watermark.ImageTransparency = 0.8f; 
            }
            float pageWidth = page.PaperWidth - page.LeftMargin - page.RightMargin;

            // Header
            var reportTitle = new ReportTitleBand { Height = Units.Millimeters * 40 };
            page.ReportTitle = reportTitle;

            // Logo
            string logoPath = Path.Combine(_env.WebRootPath, "images", "logo.jpg");
            if (File.Exists(logoPath))
            {
                var logoImg = new PictureObject
                {
                    Image = new Bitmap(logoPath),
                    Bounds = new RectangleF(0, 0, Units.Millimeters * 25, Units.Millimeters * 25),
                    // SizeMode = FastReport.PictureBoxSizeMode.Zoom
                };
                reportTitle.Objects.Add(logoImg);
            }

            // Title
            var titleText = new TextObject
            {
                Bounds = new RectangleF(Units.Millimeters * 30, 0, pageWidth - Units.Millimeters * 30, Units.Millimeters * 10),
                Text = "QUOTATION",
                HorzAlign = HorzAlign.Center,
                Font = new Font("Arial", 18, FontStyle.Bold)
            };
            reportTitle.Objects.Add(titleText);

            // Company Info
            var companyText = new TextObject
            {
                Bounds = new RectangleF(Units.Millimeters * 30, Units.Millimeters * 12, Units.Millimeters * 80, Units.Millimeters * 20),
                Text = $"{company.Name}\n{company.AddressLine1}\n{company.AddressLine2}\nGST: {company.GstNumber}",
                Font = new Font("Arial", 9)
            };
            reportTitle.Objects.Add(companyText);

            // Quotation Details
            var qDetails = new TextObject
            {
                Bounds = new RectangleF(Units.Millimeters * 120, Units.Millimeters * 12, Units.Millimeters * 70, Units.Millimeters * 20),
                Text = $"Quotation No: {quotation.QuotationNo}\nDate: {quotation.Date:dd-MMM-yyyy}\nValid Until: {(quotation.ValidUntil.HasValue ? quotation.ValidUntil.Value.ToString("dd-MMM-yyyy") : "N/A")}",
                HorzAlign = HorzAlign.Right,
                Font = new Font("Arial", 9)
            };
            reportTitle.Objects.Add(qDetails);

            // Items
            var dataBand = new DataBand();
            page.Bands.Add(dataBand);

            var headerBand = new PageHeaderBand { Height = Units.Millimeters * 8 };
            page.PageHeader = headerBand;

            float[] colWidths = { 10, 100, 30, 50 };
            string[] headers = { "#", "Description", "Qty", "Price" };
            float curX = 0;
            for (int i = 0; i < 4; i++)
            {
                CreateText(headerBand, headers[i], curX, colWidths[i], (i >= 2) ? HorzAlign.Right : HorzAlign.Left)
                    .Font = new Font("Arial", 9, FontStyle.Bold);
                curX += colWidths[i];
            }

            report.RegisterData(quotation.Items, "QItems");
            var qItemsDs = report.GetDataSource("QItems");
            if (qItemsDs != null)
            {
                qItemsDs.Enabled = true;
                EnableAll(qItemsDs);
                dataBand.DataSource = qItemsDs;
            }
            dataBand.Height = Units.Millimeters * 6;

            curX = 0;
            CreateText(dataBand, "[Row#]", curX, colWidths[0], HorzAlign.Center); curX += colWidths[0];
            CreateText(dataBand, "[QItems.Description]", curX, colWidths[1], HorzAlign.Left); curX += colWidths[1];
            CreateText(dataBand, "[QItems.Quantity]", curX, colWidths[2], HorzAlign.Right); curX += colWidths[2];
            CreateText(dataBand, "[QItems.UnitPrice]", curX, colWidths[3], HorzAlign.Right).Format = new FastReport.Format.NumberFormat { UseLocale = false, DecimalDigits = 2 };

            report.Prepare();
            using var ms = new MemoryStream();
            new PDFSimpleExport().Export(report, ms);
            return ms.ToArray();
        }

        public byte[] GenerateDeliveryChallanPdf(DeliveryChallan dc, CompanySettings company)
        {
            string reportPath = Path.Combine(_env.WebRootPath, "reports", "DeliveryChallan.frx");
            if (File.Exists(reportPath))
            {
                 using var customReport = new Report();
                 customReport.Load(reportPath);
                 customReport.RegisterData(new[] { dc }, "DeliveryChallan");
                 customReport.RegisterData(dc.Items, "DcItems");
                 customReport.RegisterData(new[] { company }, "Company");
                 customReport.Prepare();
                 using var customMs = new MemoryStream();
                 var customPdfExport = new PDFSimpleExport();
                 customPdfExport.Export(customReport, customMs);
                 return customMs.ToArray();
            }

            using var report = new Report();
            var page = new ReportPage();
            report.Pages.Add(page);

            // Watermark
            // Watermark
            string watermarkPath = Path.Combine(_env.WebRootPath, "images", "logo.jpg");
            if (File.Exists(watermarkPath))
            {
                page.Watermark.Image = new Bitmap(watermarkPath);
                page.Watermark.Enabled = true;
                page.Watermark.ShowImageOnTop = false; // Background
                // page.Watermark.ImageTransparency = 0.8f; 
            }
            float pageWidth = page.PaperWidth - page.LeftMargin - page.RightMargin;

            // Header
            var reportTitle = new ReportTitleBand { Height = Units.Millimeters * 40 };
            page.ReportTitle = reportTitle;

            string logoPath = Path.Combine(_env.WebRootPath, "images", "logo.jpg");
            if (File.Exists(logoPath))
            {
                reportTitle.Objects.Add(new PictureObject
                {
                    Image = new Bitmap(logoPath),
                    Bounds = new RectangleF(0, 0, Units.Millimeters * 25, Units.Millimeters * 25),
                        // SizeMode = FastReport.PictureBoxSizeMode.Zoom
                });
            }

            reportTitle.Objects.Add(new TextObject
            {
                Bounds = new RectangleF(Units.Millimeters * 30, 0, pageWidth - Units.Millimeters * 30, Units.Millimeters * 10),
                Text = "DELIVERY CHALLAN",
                HorzAlign = HorzAlign.Center,
                Font = new Font("Arial", 18, FontStyle.Bold)
            });

            reportTitle.Objects.Add(new TextObject
            {
                Bounds = new RectangleF(Units.Millimeters * 30, Units.Millimeters * 12, Units.Millimeters * 80, Units.Millimeters * 20),
                Text = $"{company.Name}\n{company.AddressLine1}\n{company.AddressLine2}\nGST: {company.GstNumber}",
                Font = new Font("Arial", 9)
            });

            reportTitle.Objects.Add(new TextObject
            {
                Bounds = new RectangleF(Units.Millimeters * 120, Units.Millimeters * 12, Units.Millimeters * 70, Units.Millimeters * 20),
                Text = $"DC No: {dc.DcNumber}\nDate: {dc.DcDate:dd-MMM-yyyy}\nVehicle: {dc.VehicleNo}",
                HorzAlign = HorzAlign.Right,
                Font = new Font("Arial", 9)
            });

            // Target Company / Customer
            var customerInfo = new TextObject
            {
                Bounds = new RectangleF(0, Units.Millimeters * 35, pageWidth, Units.Millimeters * 10),
                Text = $"To: {dc.TargetCompany ?? dc.Customer?.CustomerName}",
                Font = new Font("Arial", 10, FontStyle.Bold),
                Border = { Lines = BorderLines.Bottom }
            };
            reportTitle.Objects.Add(customerInfo);


            // Items
            var dataBand = new DataBand();
            page.Bands.Add(dataBand);
            var headerBand = new PageHeaderBand { Height = Units.Millimeters * 8 };
            page.PageHeader = headerBand;

            float[] colWidths = new float[] { 10, 80, 20, 20, 60 };
            string[] headers = new string[] { "#", "Description", "Qty", "Unit", "Remarks" };
            float curX = 0;
            for (int i = 0; i < 5; i++)
            {
                CreateText(headerBand, headers[i], curX, colWidths[i], (i == 2) ? HorzAlign.Right : HorzAlign.Left)
                    .Font = new Font("Arial", 9, FontStyle.Bold);
                curX += colWidths[i];
            }

            report.RegisterData(dc.Items, "DcItems");
            var dcItemsDs = report.GetDataSource("DcItems");
            if (dcItemsDs != null)
            {
                dcItemsDs.Enabled = true;
                EnableAll(dcItemsDs);
                dataBand.DataSource = dcItemsDs;
            }
            dataBand.Height = Units.Millimeters * 6;

            curX = 0;
            CreateText(dataBand, "[Row#]", curX, colWidths[0], HorzAlign.Center); curX += colWidths[0];
            CreateText(dataBand, "[DcItems.Description]", curX, colWidths[1], HorzAlign.Left); curX += colWidths[1];
            CreateText(dataBand, "[DcItems.Quantity]", curX, colWidths[2], HorzAlign.Right); curX += colWidths[2];
            CreateText(dataBand, "[DcItems.Unit]", curX, colWidths[3], HorzAlign.Center); curX += colWidths[3];
            CreateText(dataBand, "[DcItems.Remarks]", curX, colWidths[4], HorzAlign.Left);

            report.Prepare();
            using var ms = new MemoryStream();
            new PDFSimpleExport().Export(report, ms);
            return ms.ToArray();
        }
        private TextObject CreateText(BandBase band, string text, float x, float width, HorzAlign align)
        {
            var t = new TextObject();
            t.Bounds = new RectangleF(Units.Millimeters * x, 0, Units.Millimeters * width, Units.Millimeters * 6);
            t.Text = text;
            t.HorzAlign = align;
            t.VertAlign = VertAlign.Center;
            t.Border.Lines = BorderLines.All;
            t.Font = new Font("Arial", 9);
            band.Objects.Add(t);
            return t;
        }

        private void AddSummaryRow(BandBase band, string label, decimal val, float x, float y, float width)
        {
            var l = new TextObject();
            l.Bounds = new RectangleF(x, y, width/2, Units.Millimeters * 6);
            l.Text = label;
            l.HorzAlign = HorzAlign.Right;
            l.Font = new Font("Arial", 9);
            band.Objects.Add(l);

            var v = new TextObject();
            v.Bounds = new RectangleF(x + width/2, y, width/2, Units.Millimeters * 6);
            v.Text = val.ToString("N2");
            v.HorzAlign = HorzAlign.Right;
            v.Font = new Font("Arial", 9);
            band.Objects.Add(v);
        }

        private void RegisterAndEnable(Report report, System.Collections.IEnumerable data, string name)
        {
            report.RegisterData(data, name);
            var ds = report.GetDataSource(name);
            if (ds != null)
            {
                EnableAll(ds);
            }
        }

        private void EnableAll(DataSourceBase ds)
        {
            ds.Enabled = true;
            foreach (FastReport.Data.Column col in ds.Columns)
            {
                EnableColumns(col);
            }
        }

        private void EnableColumns(FastReport.Data.Column col)
        {
            col.Enabled = true;
            foreach (FastReport.Data.Column child in col.Columns)
            {
                EnableColumns(child);
            }
        }
    }
}
