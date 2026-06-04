using Application.CQRS.SystemSettings.DTOs;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.SystemSettings.Queries
{
    public class GetSettingsHandler : IRequestHandler<GetSettingsQuery, SystemSettingsDto>
    {
        private readonly ISystemSettingsRepository _repository;
        public GetSettingsHandler(ISystemSettingsRepository repository) => _repository = repository;

        public async Task<SystemSettingsDto> Handle(GetSettingsQuery request, CancellationToken ct)
        {
            var s = await _repository.GetAsync(ct);

            return new SystemSettingsDto
            {
                DelayBetweenActionsMs  = s.DelayBetweenActionsMs,
                MaxParallelTasks       = s.MaxParallelTasks,
                RetryOnError           = s.RetryOnError,
                HeadlessMode           = s.HeadlessMode,
                FingerprintEngine      = s.FingerprintEngine,
                RotateUserAgent        = s.RotateUserAgent,
                SimulateHumanBehavior  = s.SimulateHumanBehavior,
                GenerationMode         = s.GenerationMode,
                CheckIntervalMinutes   = s.CheckIntervalMinutes,
                AutoLikeEnabled        = s.AutoLikeEnabled,
                ReportCompetitors      = s.ReportCompetitors,
                TelegramBotToken       = s.TelegramBotToken,
                TelegramChatId         = s.TelegramChatId
            };
        }
    }
}
