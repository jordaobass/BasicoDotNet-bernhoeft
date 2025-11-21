using Bernhoeft.GRT.Core.Enums;
using Bernhoeft.GRT.Core.Interfaces.Results;
using Bernhoeft.GRT.Core.Models;
using Bernhoeft.GRT.Teste.Application.Requests.Queries.v1;
using Bernhoeft.GRT.Teste.Application.Responses.Queries.v1;
using Bernhoeft.GRT.Teste.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Bernhoeft.GRT.Teste.Application.Handlers.Queries.v1
{
    public class GetAvisoByIdHandler : IRequestHandler<GetAvisoByIdRequest, IOperationResult<GetAvisoByIdResponse>>
    {
        private readonly IServiceProvider _serviceProvider;

        private IAvisoRepository _avisoRepository => _serviceProvider.GetRequiredService<IAvisoRepository>();

        public GetAvisoByIdHandler(IServiceProvider serviceProvider) => _serviceProvider = serviceProvider;

        public async Task<IOperationResult<GetAvisoByIdResponse>> Handle(GetAvisoByIdRequest request, CancellationToken cancellationToken)
        {
            var result = await _avisoRepository.ObterPorIdAsync(request.Id, TrackingBehavior.NoTracking, cancellationToken);

            if (result == null)
                return OperationResult<GetAvisoByIdResponse>.ReturnNotFound();

            return OperationResult<GetAvisoByIdResponse>.ReturnOk((GetAvisoByIdResponse)result);
        }
    }
}
