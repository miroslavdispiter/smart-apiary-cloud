using SmartApiary.Application.Features.Spraying.DTOs;

namespace SmartApiary.Application.Interfaces
{
    public interface ISprayingPdfService
    {
        byte[] GenerateSprayingHistoryPdf(
            IReadOnlyCollection<SprayingScheduleDto> schedules,
            string? plotName,
            DateTime? from,
            DateTime? to);
    }
}
