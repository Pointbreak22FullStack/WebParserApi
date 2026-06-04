using Domain.Entities;
using Domain.Interfaces;
using MediatR;

namespace Application.CQRS.SystemSettings.Commands
{
    public class UpdateSettingsHandler : IRequestHandler<UpdateSettingsCommand>
    {
        private readonly ISystemSettingsRepository _repository;
        public UpdateSettingsHandler(ISystemSettingsRepository repository) => _repository = repository;

        public async Task Handle(UpdateSettingsCommand request, CancellationToken ct)
        {
            var settings = await _repository.GetAsync(ct) ?? new Domain.Entities.SystemSettings { Id = 1 };
            var dto = request.Dto;

            settings.DelayBetweenActionsMs = dto.DelayBetweenActionsMs;
            settings.MaxParallelTasks      = dto.MaxParallelTasks;
            settings.RetryOnError          = dto.RetryOnError;
            settings.HeadlessMode          = dto.HeadlessMode;
            settings.FingerprintEngine     = dto.FingerprintEngine;
            settings.RotateUserAgent       = dto.RotateUserAgent;
            settings.SimulateHumanBehavior = dto.SimulateHumanBehavior;
            settings.GenerationMode        = dto.GenerationMode;
            settings.CheckIntervalMinutes  = dto.CheckIntervalMinutes;
            settings.AutoLikeEnabled       = dto.AutoLikeEnabled;
            settings.ReportCompetitors     = dto.ReportCompetitors;
            settings.TelegramBotToken      = dto.TelegramBotToken;
            settings.TelegramChatId        = dto.TelegramChatId;
            settings.UpdatedAt             = DateTime.UtcNow;

            await _repository.UpdateAsync(settings, ct);
        }
    }
}
