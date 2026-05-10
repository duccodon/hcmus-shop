using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.Common;
using hcmus_shop.Models.DTOs;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hcmus_shop.Services.Invoices
{
    public class InvoiceService : IInvoiceService
    {
        public async Task<Result<string>> GenerateInvoicePdfAsync(OrderDto order, string outputPath)
        {
            try
            {
                if (order is null)
                {
                    return Result<string>.Failure("Order is required.");
                }

                if (string.IsNullOrWhiteSpace(outputPath))
                {
                    return Result<string>.Failure("Output path is required.");
                }

                var htmlPath = Path.Combine(Path.GetTempPath(), $"invoice-{order.OrderId}.html");
                await File.WriteAllTextAsync(htmlPath, BuildHtml(order), Encoding.UTF8);

                var edgePath = ResolveEdgePath();
                if (edgePath is null)
                {
                    return Result<string>.Failure("Microsoft Edge is required to export invoice PDF.");
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = edgePath,
                    Arguments = $"--headless --disable-gpu --print-to-pdf=\"{outputPath}\" \"file:///{htmlPath.Replace("\\", "/")}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process is null)
                {
                    return Result<string>.Failure("Failed to start invoice export process.");
                }

                await process.WaitForExitAsync();
                if (process.ExitCode != 0 || !File.Exists(outputPath))
                {
                    return Result<string>.Failure("Invoice PDF export failed.");
                }

                return Result<string>.Success(outputPath);
            }
            catch (Exception ex)
            {
                return Result<string>.Failure(ex.Message);
            }
        }

        private static string BuildHtml(OrderDto order)
        {
            var lineItems = string.Join(
                Environment.NewLine,
                order.OrderItems.Select(item =>
                    $@"<tr>
                        <td>{Escape(item.Instance.Product?.Name ?? "Unknown product")}</td>
                        <td>{Escape(item.Instance.SerialNumber)}</td>
                        <td>{item.Quantity}</td>
                        <td>{FormatCurrency(item.UnitSalePrice)}</td>
                        <td>{FormatCurrency(item.UnitSalePrice * item.Quantity)}</td>
                    </tr>"));

            return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"" />
    <title>Invoice {Escape(order.OrderId)}</title>
    <style>
        body {{ font-family: Segoe UI, Arial, sans-serif; margin: 32px; color: #1f2937; }}
        h1 {{ margin-bottom: 6px; }}
        .meta {{ margin-bottom: 24px; }}
        .meta p {{ margin: 4px 0; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 16px; }}
        th, td {{ border: 1px solid #d1d5db; padding: 10px; text-align: left; }}
        th {{ background: #f3f4f6; }}
        .summary {{ margin-top: 20px; width: 320px; margin-left: auto; }}
        .summary td {{ border: none; padding: 6px 0; }}
        .total {{ font-size: 18px; font-weight: 700; }}
    </style>
</head>
<body>
    <h1>HCMUS Shop Invoice</h1>
    <div class=""meta"">
        <p><strong>Order ID:</strong> {Escape(order.OrderId)}</p>
        <p><strong>Customer:</strong> {Escape(order.Customer?.Name ?? "Walk-in")}</p>
        <p><strong>Phone:</strong> {Escape(order.Customer?.Phone ?? "-")}</p>
        <p><strong>Handled by:</strong> {Escape(order.User.FullName)}</p>
        <p><strong>Status:</strong> {Escape(order.Status)}</p>
        <p><strong>Created:</strong> {Escape(FormatDate(order.CreatedAt))}</p>
    </div>

    <table>
        <thead>
            <tr>
                <th>Product</th>
                <th>Serial</th>
                <th>Qty</th>
                <th>Unit Price</th>
                <th>Line Total</th>
            </tr>
        </thead>
        <tbody>
            {lineItems}
        </tbody>
    </table>

    <table class=""summary"">
        <tr><td>Subtotal</td><td>{FormatCurrency(order.Subtotal)}</td></tr>
        <tr><td>Discount</td><td>{FormatCurrency(order.DiscountAmount)}</td></tr>
        <tr class=""total""><td>Final Amount</td><td>{FormatCurrency(order.FinalAmount)}</td></tr>
    </table>
</body>
</html>";
        }

        private static string? ResolveEdgePath()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe")
            };

            return candidates.FirstOrDefault(File.Exists);
        }

        private static string Escape(string value)
        {
            return System.Net.WebUtility.HtmlEncode(value);
        }

        private static string FormatCurrency(double value)
        {
            return value.ToString("N0", CultureInfo.InvariantCulture) + " VND";
        }

        private static string FormatDate(string value)
        {
            return DateTime.TryParse(value, out var parsed)
                ? parsed.ToLocalTime().ToString("dd/MM/yyyy HH:mm")
                : value;
        }
    }
}
