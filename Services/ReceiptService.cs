using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using FreshMart.Models;
using System.Globalization;

namespace FreshMart.Services
{
    public class ReceiptService
    {
        private const string BrandColor = "#16a34a"; // Green color
        private const string TextPrimary = "#1f2937";
        private const string TextSecondary = "#4b5563";
        private const string BorderColor = "#e5e7eb";
        private const string TableHeaderBg = "#dcfce7";

        public byte[] GenerateReceipt(Order order, List<OrderItem> items, List<Product> products)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(11));

                    page.Header().Element(header => BuildHeader(header, order));
                    page.Content().Element(content => BuildContent(content, order, items, products));
                    page.Footer().Element(BuildFooter);
                });
            });

            return document.GeneratePdf();
        }

        // ---------------- HEADER ----------------
        private void BuildHeader(IContainer container, Order order)
        {
            container.PaddingBottom(30).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("BizFlow")
                        .FontSize(36)
                        .Black() // Extra bold
                        .FontColor(BrandColor)
                        .LetterSpacing(0.05f);

                    col.Item().Text("SIÊU THỊ THỰC PHẨM SẠCH")
                        .FontSize(11)
                        .SemiBold()
                        .FontColor(TextSecondary)
                        .LetterSpacing(0.1f);
                });

                // RIGHT: PDF TYPE
                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("HÓA ĐƠN ĐẶT HÀNG")
                        .FontSize(20)
                        .Bold()
                        .FontColor(TextPrimary);

                    col.Item().Text($"#{order.OrderId}")
                        .FontSize(12)
                        .FontColor(TextSecondary);
                });
            });
        }

        // ---------------- CONTENT ----------------
        private void BuildContent(IContainer container, Order order, List<OrderItem> items, List<Product> products)
        {
            container.Column(col =>
            {
                // 1. ORDER & CUSTOMER INFO SECTION
                col.Item().Row(row =>
                {
                    // ORDER INFO
                    row.RelativeItem().Column(sub =>
                    {
                        sub.Item().Text("THÔNG TIN ĐƠN HÀNG").Bold().FontSize(12).FontColor(BrandColor);
                        sub.Item().PaddingTop(5).Text($"Mã đơn hàng: #{order.OrderId}");
                        sub.Item().Text($"Ngày đặt: {order.OrderDate:dd/MM/yyyy HH:mm}");
                        
                        string paymentVn = order.PaymentMethod switch {
                            "Cash" => "Tiền mặt (COD)",
                            "Credit Card" => "Thẻ tín dụng",
                            _ => order.PaymentMethod
                        };
                        sub.Item().Text($"Thanh toán: {paymentVn}");
                    });

                    // CUSTOMER INFO
                    row.RelativeItem().Column(sub =>
                    {
                        sub.Item().Text("THÔNG TIN KHÁCH HÀNG").Bold().FontSize(12).FontColor(BrandColor);
                        sub.Item().PaddingTop(5).Text(order.FullName).SemiBold();
                        sub.Item().Text(order.Email);
                        
                        if (!string.IsNullOrEmpty(order.Phone))
                            sub.Item().Text(order.Phone);
                            
                        if (!string.IsNullOrEmpty(order.Address))
                            sub.Item().Text(order.Address).FontSize(10);
                    });
                });

                col.Item().PaddingTop(30);

                // 2. PRODUCT TABLE
                col.Item().Table(table =>
                {
                    // Definition
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(4); // Product
                        columns.ConstantColumn(60); // Qty
                        columns.RelativeColumn(2); // Price
                        columns.RelativeColumn(2); // Total
                    });

                    // Header
                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Sản phẩm");
                        header.Cell().Element(CellStyle).AlignCenter().Text("Số lượng");
                        header.Cell().Element(CellStyle).AlignRight().Text("Đơn giá");
                        header.Cell().Element(CellStyle).AlignRight().Text("Thành tiền");

                        static IContainer CellStyle(IContainer container)
                        {
                            return container.DefaultTextStyle(x => x.SemiBold().FontColor(BrandColor))
                                            .PaddingVertical(10)
                                            .PaddingHorizontal(8)
                                            .Background(TableHeaderBg)
                                            .BorderBottom(2)
                                            .BorderColor(BrandColor);
                        }
                    });

                    // Rows
                    foreach (var item in items)
                    {
                        var product = products.FirstOrDefault(p => p.ProductId == item.ProductId);
                        string productName = product?.Name ?? "Sản phẩm không tồn tại";

                        table.Cell().Element(RowStyle).Text(productName);
                        table.Cell().Element(RowStyle).AlignCenter().Text(item.Quantity.ToString());
                        table.Cell().Element(RowStyle).AlignRight().Text(FormatVND(item.Price));
                        table.Cell().Element(RowStyle).AlignRight().Text(FormatVND(item.Price * item.Quantity));

                        static IContainer RowStyle(IContainer container)
                        {
                            return container.PaddingVertical(10).PaddingHorizontal(8).BorderBottom(1).BorderColor(BorderColor);
                        }
                    }
                });

                // 3. TOTALS
                col.Item().AlignRight().PaddingTop(20).Column(tot =>
                {
                    tot.Item().Row(row => {
                        row.ConstantItem(100).Text("Tạm tính:").FontSize(12).FontColor(TextSecondary);
                        row.ConstantItem(120).AlignRight().Text(FormatVND(order.TotalAmount)).FontSize(12);
                    });

                    tot.Item().PaddingTop(10).Row(row => {
                        row.ConstantItem(150).Text("TỔNG THANH TOÁN:").FontSize(14).Bold().FontColor(TextPrimary);
                        row.ConstantItem(120).AlignRight().Text(FormatVND(order.TotalAmount)).FontSize(16).Black().FontColor(BrandColor);
                    });
                });

                // 4. THANK YOU BOX
                col.Item().PaddingTop(40).AlignCenter().Column(thx =>
                {
                    thx.Item().Text("Cảm ơn bạn đã mua sắm tại BizFlow!")
                        .FontSize(16).Black().FontColor(BrandColor);

                    thx.Item().PaddingTop(5).Text("Chúng tôi rất trân trọng sự tin tưởng và ủng hộ của bạn.")
                        .FontSize(11).FontColor(TextSecondary).Italic();
                });
            });
        }

        // ---------------- FOOTER ----------------
        private void BuildFooter(IContainer container)
        {
            container.AlignCenter().PaddingTop(20).Column(col =>
            {
                col.Item().LineHorizontal(1).LineColor(BorderColor);
                col.Item().PaddingTop(5).Text("© 2026 BizFlow — Hệ thống quản lý bán hàng hiện đại")
                    .FontSize(9)
                    .FontColor(TextSecondary);
            });
        }

        // ---------------- HELPERS ----------------
        private string FormatVND(decimal amount)
        {
            return amount.ToString("N0", new CultureInfo("vi-VN")) + " đ";
        }
    }
}
