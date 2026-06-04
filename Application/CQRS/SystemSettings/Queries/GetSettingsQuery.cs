using Application.CQRS.SystemSettings.DTOs;
using MediatR;

namespace Application.CQRS.SystemSettings.Queries
{
    public record GetSettingsQuery : IRequest<SystemSettingsDto>;
}
