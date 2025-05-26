using Microsoft.Extensions.Logging;
using PoultrySlaughterPOS.Models.DTOs;
using PoultrySlaughterPOS.Models.Entities;
using PoultrySlaughterPOS.Services.Interfaces;
using System.Drawing.Printing;
using System.IO;
using System.Text;

namespace PoultrySlaughterPOS.Services.Implementations
{
    /// <summary>
    /// Enterprise-grade printing service implementation for POS document generation
    /// Provides comprehensive invoice printing, receipt generation, and report output capabilities
    /// </summary>
    public class PrintingService : IPrintingService
    {
        private readonly ILogger<PrintingService> _logger;
        private string _currentPrinterName;
        private PrintDocument? _currentPrintDocument;

        public PrintingService(ILogger<PrintingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _currentPrinterName = GetDefaultPrinterName();
        }

        public async Task<ServiceResult<bool>> PrintInvoiceAsync(Invoice invoice)
        {
            try
            {
                _logger.LogInformation("Printing invoice {InvoiceNumber} for customer {CustomerName}",
                    invoice.InvoiceNumber, invoice.Customer.CustomerName);

                var printContent = GenerateInvoicePrintContent(invoice);
                var result = await PrintDocumentAsync(printContent, $"Invoice-{invoice.InvoiceNumber}");

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Successfully printed invoice {InvoiceNumber}", invoice.InvoiceNumber);
                    return ServiceResult<bool>.Success(true, "Invoice printed successfully");
                }

                return ServiceResult<bool>.Failure("Failed to print invoice", "PRINT_ERROR");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing invoice {InvoiceNumber}", invoice.InvoiceNumber);
                return ServiceResult<bool>.Failure("Invoice printing failed", "PRINT_ERROR", ex);
            }
        }

        public async Task<ServiceResult<bool>> PrintReceiptAsync(Payment payment)
        {
            try
            {
                _logger.LogInformation("Printing payment receipt for customer {CustomerId}, Amount: {Amount:C}",
                    payment.CustomerId, payment.Amount);

                var printContent = GenerateReceiptPrintContent(payment);
                var result = await PrintDocumentAsync(printContent, $"Receipt-{payment.PaymentId}");

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Successfully printed payment receipt for Payment ID: {PaymentId}", payment.PaymentId);
                    return ServiceResult<bool>.Success(true, "Receipt printed successfully");
                }

                return ServiceResult<bool>.Failure("Failed to print receipt", "PRINT_ERROR");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing payment receipt for Payment ID: {PaymentId}", payment.PaymentId);
                return ServiceResult<bool>.Failure("Receipt printing failed", "PRINT_ERROR", ex);
            }
        }

        public async Task<ServiceResult<bool>> PrintReportAsync(DailySummaryReportDto report)
        {
            try
            {
                _logger.LogInformation("Printing daily summary report for {ReportDate:yyyy-MM-dd}", report.ReportDate);

                var printContent = GenerateReportPrintContent(report);
                var result = await PrintDocumentAsync(printContent, $"DailyReport-{report.ReportDate:yyyyMMdd}");

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Successfully printed daily summary report for {ReportDate:yyyy-MM-dd}", report.ReportDate);
                    return ServiceResult<bool>.Success(true, "Report printed successfully");
                }

                return ServiceResult<bool>.Failure("Failed to print report", "PRINT_ERROR");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing daily summary report for {ReportDate:yyyy-MM-dd}", report.ReportDate);
                return ServiceResult<bool>.Failure("Report printing failed", "PRINT_ERROR", ex);
            }
        }

        public async Task<ServiceResult<bool>> PrintCustomerStatementAsync(CustomerAccountSummaryDto statement)
        {
            try
            {
                _logger.LogInformation("Printing customer statement for {CustomerName}", statement.CustomerName);

                var printContent = GenerateCustomerStatementPrintContent(statement);
                var result = await PrintDocumentAsync(printContent, $"Statement-{statement.CustomerId}");

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Successfully printed customer statement for {CustomerName}", statement.CustomerName);
                    return ServiceResult<bool>.Success(true, "Customer statement printed successfully");
                }

                return ServiceResult<bool>.Failure("Failed to print customer statement", "PRINT_ERROR");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error printing customer statement for {CustomerName}", statement.CustomerName);
                return ServiceResult<bool>.Failure("Customer statement printing failed", "PRINT_ERROR", ex);
            }
        }

        public async Task<ServiceResult<string>> GenerateInvoicePdfAsync(Invoice invoice)
        {
            try
            {
                _logger.LogInformation("Generating PDF for invoice {InvoiceNumber}", invoice.InvoiceNumber);

                // This would integrate with a PDF generation library like iTextSharp
                // For now, returning a placeholder implementation
                var pdfPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "PoultrySlaughterPOS_PDFs",
                    $"Invoice-{invoice.InvoiceNumber}-{DateTime.Now:yyyyMMdd-HHmmss}.pdf");

                // Ensure directory exists
                var directory = Path.GetDirectoryName(pdfPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Simulate PDF generation
                await Task.Delay(1000);
                await File.WriteAllTextAsync(pdfPath, GenerateInvoicePrintContent(invoice));

                _logger.LogInformation("Successfully generated PDF for invoice {InvoiceNumber} at {PdfPath}",
                    invoice.InvoiceNumber, pdfPath);

                return ServiceResult<string>.Success(pdfPath, "PDF generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF for invoice {InvoiceNumber}", invoice.InvoiceNumber);
                return ServiceResult<string>.Failure("PDF generation failed", "PDF_ERROR", ex);
            }
        }

        public async Task<ServiceResult<bool>> ConfigurePrinterAsync(string printerName)
        {
            try
            {
                _logger.LogInformation("Configuring printer: {PrinterName}", printerName);

                var availablePrintersResult = await GetAvailablePrintersAsync();
                if (!availablePrintersResult.IsSuccess || availablePrintersResult.Data == null)
                {
                    return ServiceResult<bool>.Failure("Unable to retrieve available printers", "PRINTER_ERROR");
                }

                if (!availablePrintersResult.Data.Contains(printerName))
                {
                    return ServiceResult<bool>.Failure($"Printer '{printerName}' not found", "PRINTER_NOT_FOUND");
                }

                _currentPrinterName = printerName;
                _logger.LogInformation("Successfully configured printer: {PrinterName}", printerName);

                return ServiceResult<bool>.Success(true, "Printer configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error configuring printer: {PrinterName}", printerName);
                return ServiceResult<bool>.Failure("Printer configuration failed", "PRINTER_ERROR", ex);
            }
        }

        public async Task<ServiceResult<List<string>>> GetAvailablePrintersAsync()
        {
            try
            {
                _logger.LogDebug("Retrieving available printers");

                await Task.Run(() => { }); // Make async for consistency

                var printers = new List<string>();
                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    printers.Add(printerName);
                }

                _logger.LogDebug("Found {PrinterCount} available printers", printers.Count);
                return ServiceResult<List<string>>.Success(printers, "Available printers retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available printers");
                return ServiceResult<List<string>>.Failure("Failed to retrieve printers", "PRINTER_ERROR", ex);
            }
        }

        private async Task<ServiceResult<bool>> PrintDocumentAsync(string content, string documentName)
        {
            try
            {
                var printCompleted = false;
                var printError = false;
                Exception? printException = null;

                _currentPrintDocument = new PrintDocument
                {
                    PrinterSettings = { PrinterName = _currentPrinterName },
                    DocumentName = documentName
                };

                _currentPrintDocument.PrintPage += (sender, e) =>
                {
                    try
                    {
                        if (e.Graphics != null)
                        {
                            var font = new System.Drawing.Font("Arial", 10);
                            var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
                            var rect = e.MarginBounds;

                            e.Graphics.DrawString(content, font, brush, rect);
                        }
                    }
                    catch (Exception ex)
                    {
                        printException = ex;
                        printError = true;
                    }
                };

                _currentPrintDocument.EndPrint += (sender, e) =>
                {
                    printCompleted = true;
                };

                _currentPrintDocument.Print();

                // Wait for print completion with timeout
                var timeout = TimeSpan.FromSeconds(30);
                var startTime = DateTime.Now;

                while (!printCompleted && !printError && DateTime.Now - startTime < timeout)
                {
                    await Task.Delay(100);
                }

                if (printError)
                {
                    throw printException ?? new InvalidOperationException("Print operation failed");
                }

                if (!printCompleted)
                {
                    throw new TimeoutException("Print operation timed out");
                }

                return ServiceResult<bool>.Success(true, "Document printed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during document printing: {DocumentName}", documentName);
                return ServiceResult<bool>.Failure("Document printing failed", "PRINT_ERROR", ex);
            }
            finally
            {
                _currentPrintDocument?.Dispose();
                _currentPrintDocument = null;
            }
        }

        private string GenerateInvoicePrintContent(Invoice invoice)
        {
            var content = new StringBuilder();
            content.AppendLine("=".PadRight(50, '='));
            content.AppendLine($"{"فاتورة مبيعات".PadLeft(30)}");
            content.AppendLine("=".PadRight(50, '='));
            content.AppendLine();
            content.AppendLine($"رقم الفاتورة: {invoice.InvoiceNumber}");
            content.AppendLine($"التاريخ: {invoice.InvoiceDate:yyyy/MM/dd HH:mm}");
            content.AppendLine($"اسم الزبون: {invoice.Customer.CustomerName}");
            content.AppendLine($"رقم الشاحنة: {invoice.Truck.TruckNumber}");
            content.AppendLine();
            content.AppendLine("-".PadRight(50, '-'));
            content.AppendLine($"الوزن الإجمالي: {invoice.GrossWeight:F2} كجم");
            content.AppendLine($"وزن الأقفاص: {invoice.CagesWeight:F2} كجم");
            content.AppendLine($"الوزن الصافي: {invoice.NetWeight:F2} كجم");
            content.AppendLine($"سعر الوحدة: {invoice.UnitPrice:F2} ريال/كجم");
            content.AppendLine();
            content.AppendLine($"المبلغ الإجمالي: {invoice.TotalAmount:F2} ريال");
            if (invoice.DiscountPercentage > 0)
            {
                content.AppendLine($"نسبة الخصم: {invoice.DiscountPercentage}%");
                content.AppendLine($"مبلغ الخصم: {invoice.TotalAmount * (invoice.DiscountPercentage / 100):F2} ريال");
            }
            content.AppendLine($"المبلغ النهائي: {invoice.FinalAmount:F2} ريال");
            content.AppendLine();
            content.AppendLine($"الرصيد السابق: {invoice.PreviousBalance:F2} ريال");
            content.AppendLine($"الرصيد الحالي: {invoice.CurrentBalance:F2} ريال");
            content.AppendLine();
            if (!string.IsNullOrWhiteSpace(invoice.Notes))
            {
                content.AppendLine($"ملاحظات: {invoice.Notes}");
                content.AppendLine();
            }
            content.AppendLine("=".PadRight(50, '='));
            content.AppendLine($"{"شكراً لتعاملكم معنا".PadLeft(25)}");
            content.AppendLine("=".PadRight(50, '='));

            return content.ToString();
        }

        private string GenerateReceiptPrintContent(Payment payment)
        {
            var content = new StringBuilder();
            content.AppendLine("=".PadRight(40, '='));
            content.AppendLine($"{"إيصال دفع".PadLeft(25)}");
            content.AppendLine("=".PadRight(40, '='));
            content.AppendLine();
            content.AppendLine($"رقم الإيصال: {payment.PaymentId}");
            content.AppendLine($"التاريخ: {payment.PaymentDate:yyyy/MM/dd HH:mm}");
            content.AppendLine($"اسم الزبون: {payment.Customer.CustomerName}");
            content.AppendLine();
            content.AppendLine($"المبلغ المدفوع: {payment.Amount:F2} ريال");
            content.AppendLine($"طريقة الدفع: {payment.PaymentMethod}");
            if (!string.IsNullOrWhiteSpace(payment.ReferenceNumber))
            {
                content.AppendLine($"رقم المرجع: {payment.ReferenceNumber}");
            }
            content.AppendLine();
            if (!string.IsNullOrWhiteSpace(payment.Notes))
            {
                content.AppendLine($"ملاحظات: {payment.Notes}");
                content.AppendLine();
            }
            content.AppendLine("=".PadRight(40, '='));
            content.AppendLine($"{"شكراً لكم".PadLeft(20)}");
            content.AppendLine("=".PadRight(40, '='));

            return content.ToString();
        }

        private string GenerateReportPrintContent(DailySummaryReportDto report)
        {
            var content = new StringBuilder();
            content.AppendLine("=".PadRight(60, '='));
            content.AppendLine($"{"تقرير يومي شامل".PadLeft(35)}");
            content.AppendLine($"{"تاريخ التقرير: " + report.ReportDate:yyyy/MM/dd}".PadLeft(40));
            content.AppendLine("=".PadRight(60, '='));
            content.AppendLine();
            content.AppendLine($"إجمالي الوزن المحمل: {report.TotalInitialWeight:F2} كجم");
            content.AppendLine($"إجمالي الوزن المباع: {report.TotalSalesWeight:F2} كجم");
            content.AppendLine($"إجمالي الفقد: {report.TotalWeightLoss:F2} كجم");
            content.AppendLine($"متوسط نسبة الفقد: {report.AverageLossPercentage:F2}%");
            content.AppendLine($"إجمالي المبيعات: {report.TotalSalesAmount:F2} ريال");
            content.AppendLine($"عدد الفواتير: {report.TotalInvoicesCount}");
            content.AppendLine();
            content.AppendLine("تفاصيل الشاحنات:");
            content.AppendLine("-".PadRight(60, '-'));

            foreach (var truckReport in report.TruckReports)
            {
                content.AppendLine($"شاحنة {truckReport.TruckNumber}:");
                content.AppendLine($"  الوزن المحمل: {truckReport.InitialLoadWeight:F2} كجم");
                content.AppendLine($"  الوزن المباع: {truckReport.TotalSalesWeight:F2} كجم");
                content.AppendLine($"  الفقد: {truckReport.WeightDifference:F2} كجم ({truckReport.LossPercentage:F2}%)");
                content.AppendLine($"  المبيعات: {truckReport.TotalSalesAmount:F2} ريال");
                content.AppendLine();
            }

            content.AppendLine("=".PadRight(60, '='));
            return content.ToString();
        }

        private string GenerateCustomerStatementPrintContent(CustomerAccountSummaryDto statement)
        {
            var content = new StringBuilder();
            content.AppendLine("=".PadRight(60, '='));
            content.AppendLine($"{"كشف حساب زبون".PadLeft(35)}");
            content.AppendLine("=".PadRight(60, '='));
            content.AppendLine();
            content.AppendLine($"اسم الزبون: {statement.CustomerName}");
            content.AppendLine($"رقم الهاتف: {statement.PhoneNumber ?? "غير محدد"}");
            content.AppendLine($"العنوان: {statement.Address ?? "غير محدد"}");
            content.AppendLine();
            content.AppendLine($"الرصيد الحالي: {statement.CurrentBalance:F2} ريال");
            content.AppendLine($"حد الائتمان: {statement.CreditLimit:F2} ريال");
            content.AppendLine($"الائتمان المتاح: {statement.AvailableCredit:F2} ريال");
            content.AppendLine();
            content.AppendLine($"إجمالي المشتريات: {statement.TotalPurchases:F2} ريال");
            content.AppendLine($"إجمالي المدفوعات: {statement.TotalPayments:F2} ريال");
            content.AppendLine($"عدد الفواتير: {statement.TotalInvoicesCount}");
            content.AppendLine($"الفواتير غير المدفوعة: {statement.UnpaidInvoicesCount}");
            content.AppendLine();
            content.AppendLine("آخر المعاملات:");
            content.AppendLine("-".PadRight(60, '-'));

            foreach (var invoice in statement.RecentInvoices.Take(5))
            {
                content.AppendLine($"{invoice.InvoiceDate:yyyy/MM/dd} - {invoice.InvoiceNumber}");
                content.AppendLine($"  المبلغ: {invoice.FinalAmount:F2} ريال - {(invoice.IsPaid ? "مدفوعة" : "غير مدفوعة")}");
            }

            content.AppendLine();
            content.AppendLine("=".PadRight(60, '='));
            return content.ToString();
        }

        private string GetDefaultPrinterName()
        {
            try
            {
                var defaultPrinter = new PrinterSettings();
                return defaultPrinter.PrinterName;
            }
            catch
            {
                return PrinterSettings.InstalledPrinters.Count > 0 ?
                    PrinterSettings.InstalledPrinters[0] : "Microsoft Print to PDF";
            }
        }
    }
}