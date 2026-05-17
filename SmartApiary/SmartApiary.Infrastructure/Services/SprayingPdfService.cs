using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartApiary.Application.Features.Spraying.DTOs;
using SmartApiary.Application.Interfaces;

namespace SmartApiary.Infrastructure.Services
{
    internal class SprayingPdfService : ISprayingPdfService
    {
        public byte[] GenerateSprayingHistoryPdf(
            IReadOnlyCollection<SprayingScheduleDto> schedules,
            string? plotName,
            DateTime? from,
            DateTime? to)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(ComposeHeader);

                    page.Content().PaddingTop(10).Column(col =>
                    {
                        // Metadata
                        col.Item().PaddingBottom(8).Column(meta =>
                        {
                            meta.Item().Text(text =>
                            {
                                text.Span("Digital Spraying Card")
                                    .FontSize(14).Bold().FontColor("#92400E");
                            });

                            meta.Item().PaddingTop(4).Row(row =>
                            {
                                row.RelativeItem().Text(text =>
                                {
                                    text.DefaultTextStyle(s => s.FontSize(9).FontColor("#6B7280"));
                                    if (plotName is not null)
                                    {
                                        text.Span("Plot: ").SemiBold();
                                        text.Span(plotName);
                                        text.Span("   ");
                                    }
                                    if (from.HasValue)
                                    {
                                        text.Span("From: ").SemiBold();
                                        text.Span(from.Value.ToLocalTime().ToString("dd.MM.yyyy"));
                                        text.Span("   ");
                                    }
                                    if (to.HasValue)
                                    {
                                        text.Span("To: ").SemiBold();
                                        text.Span(to.Value.ToLocalTime().ToString("dd.MM.yyyy"));
                                    }
                                });

                                row.ConstantItem(160).AlignRight().Text(text =>
                                {
                                    text.DefaultTextStyle(s => s.FontSize(9).FontColor("#6B7280"));
                                    text.Span("Generated: ").SemiBold();
                                    text.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                                });
                            });

                            meta.Item().PaddingTop(2).Text(text =>
                            {
                                text.DefaultTextStyle(s => s.FontSize(9).FontColor("#6B7280"));
                                text.Span("Total records: ").SemiBold();
                                text.Span(schedules.Count.ToString());
                            });
                        });

                        // Divider
                        col.Item().PaddingBottom(8).LineHorizontal(1).LineColor("#D97706");

                        // Table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(3.0f);
                                columns.RelativeColumn(1.2f);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.3f);
                            });

                            table.Header(header =>
                            {
                                var headers = new[] { "Plot", "Date & Time", "Duration", "Preparation", "Status", "Notified" };
                                foreach (var h in headers)
                                {
                                    header.Cell()
                                        .Background("#B45309")
                                        .Padding(6)
                                        .Text(text =>
                                        {
                                            text.Span(h).Bold().FontSize(9).FontColor(Colors.White);
                                        });
                                }
                            });

                            if (schedules.Count == 0)
                            {
                                table.Cell().ColumnSpan(6)
                                    .Padding(16)
                                    .AlignCenter()
                                    .Text(text =>
                                    {
                                        text.Span("No records found.").FontSize(10).FontColor("#9CA3AF").Italic();
                                    });
                            }
                            else
                            {
                                var rowIndex = 0;
                                foreach (var s in schedules)
                                {
                                    var bg = rowIndex % 2 == 0 ? "#FFFBEB" : "#FFFFFF";

                                    rowIndex++;

                                    var statusColor = s.StatusName switch
                                    {
                                        "Scheduled" => "#D97706",
                                        "Completed" => "#059669",
                                        "Cancelled" => "#DC2626",
                                        _ => "#6B7280"
                                    };

                                    Cell(table, s.PlotName, bg);
                                    Cell(table, s.ScheduledAt.ToLocalTime().ToString("dd.MM.yyyy HH:mm"), bg);
                                    Cell(table, $"{s.DurationHours}h", bg);
                                    Cell(table, s.Preparation ?? "—", bg);

                                    // Status cell with colour
                                    table.Cell()
                                        .Background(bg)
                                        .Padding(6)
                                        .Text(text =>
                                        {
                                            text.Span(s.StatusName).FontSize(9).FontColor(statusColor).SemiBold();
                                        });

                                    Cell(table, $"{s.NotifiedBeekeeperCount} 🐝", bg);
                                }
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.DefaultTextStyle(s => s.FontSize(8).FontColor("#9CA3AF"));
                        text.Span("Smart Apiary — FTN Novi Sad   |   Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        private static void ComposeHeader(IContainer container)
        {
            container.PaddingBottom(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text(text =>
                    {
                        text.Span("🐝 Smart Apiary").FontSize(20).Bold().FontColor("#92400E");
                    });
                    col.Item().Text(text =>
                    {
                        text.Span("Pesticide Treatment Record").FontSize(11).FontColor("#B45309");
                    });
                });
            });
        }

        private static void Cell(TableDescriptor table, string content, string bg)
        {
            table.Cell()
                .Background(bg)
                .Padding(6)
                .Text(text =>
                {
                    text.Span(content).FontSize(9).FontColor("#374151");
                });
        }
    }
}