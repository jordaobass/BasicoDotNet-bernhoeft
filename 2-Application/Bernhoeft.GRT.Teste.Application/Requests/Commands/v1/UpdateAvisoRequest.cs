using Bernhoeft.GRT.Core.Interfaces.Results;
using Bernhoeft.GRT.Teste.Application.Responses.Commands.v1;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Bernhoeft.GRT.Teste.Application.Requests.Commands.v1
{
    public class UpdateAvisoRequest : IRequest<IOperationResult<UpdateAvisoResponse>>
    {
        [FromRoute(Name = "id")]
        public int Id { get; set; }

        [FromBody]
        public UpdateAvisoRequestBody Body { get; set; }
    }

    public class UpdateAvisoRequestBody
    {
        public string Mensagem { get; set; }
    }
}
