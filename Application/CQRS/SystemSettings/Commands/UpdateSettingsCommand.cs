using Application.CQRS.SystemSettings.DTOs;
using MediatR;

namespace Application.CQRS.SystemSettings.Commands
{
    public record UpdateSettingsCommand(SystemSettingsDto Dto) : IRequest;
}
