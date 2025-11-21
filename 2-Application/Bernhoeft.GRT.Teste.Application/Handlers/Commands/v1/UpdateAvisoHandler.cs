using Bernhoeft.GRT.Core.Enums;
using Bernhoeft.GRT.Core.Interfaces.Results;
using Bernhoeft.GRT.Core.Models;
using Bernhoeft.GRT.Teste.Application.Requests.Commands.v1;
using Bernhoeft.GRT.Teste.Application.Responses.Commands.v1;
using Bernhoeft.GRT.Teste.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Bernhoeft.GRT.Teste.Application.Handlers.Commands.v1
{
    public class UpdateAvisoHandler : IRequestHandler<UpdateAvisoRequest, IOperationResult<UpdateAvisoResponse>>
    {
        private readonly IServiceProvider _serviceProvider;

        private IAvisoRepository _avisoRepository => _serviceProvider.GetRequiredService<IAvisoRepository>();

        public UpdateAvisoHandler(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public async Task<IOperationResult<UpdateAvisoResponse>> Handle(UpdateAvisoRequest request, CancellationToken cancellationToken)
        {
            var entity = await _avisoRepository.ObterPorIdAsync(request.Id, TrackingBehavior.Default, cancellationToken);

            if (entity == null)
                return OperationResult<UpdateAvisoResponse>.ReturnNotFound();

            entity.AtualizarMensagem(request.Body.Mensagem);

            var result = await _avisoRepository.AtualizarAsync(entity, cancellationToken);

            return OperationResult<UpdateAvisoResponse>.ReturnOk((UpdateAvisoResponse)result);
        }
    }
}
