using Bernhoeft.GRT.Core.Enums;
using Bernhoeft.GRT.Core.Interfaces.Results;
using Bernhoeft.GRT.Core.Models;
using Bernhoeft.GRT.Teste.Application.Requests.Commands.v1;
using Bernhoeft.GRT.Teste.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Bernhoeft.GRT.Teste.Application.Handlers.Commands.v1
{
    public class DeleteAvisoHandler : IRequestHandler<DeleteAvisoRequest, IOperationResult<bool>>
    {
        private readonly IServiceProvider _serviceProvider;

        private IAvisoRepository _avisoRepository => _serviceProvider.GetRequiredService<IAvisoRepository>();

        public DeleteAvisoHandler(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public async Task<IOperationResult<bool>> Handle(DeleteAvisoRequest request, CancellationToken cancellationToken)
        {
            var entity = await _avisoRepository.ObterPorIdAsync(request.Id, TrackingBehavior.NoTracking, cancellationToken);

            if (entity == null)
                return OperationResult<bool>.ReturnNotFound();

            await _avisoRepository.ExcluirAsync(request.Id, cancellationToken);

            return OperationResult<bool>.ReturnNoContent();
        }
    }
}
