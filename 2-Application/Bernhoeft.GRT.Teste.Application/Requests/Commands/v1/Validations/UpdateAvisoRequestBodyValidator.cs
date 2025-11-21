using FluentValidation;

namespace Bernhoeft.GRT.Teste.Application.Requests.Commands.v1.Validations
{
    public class UpdateAvisoRequestBodyValidator : AbstractValidator<UpdateAvisoRequestBody>
    {
        public UpdateAvisoRequestBodyValidator()
        {
            RuleFor(x => x.Mensagem)
                .NotNull().WithMessage("A mensagem é obrigatória.")
                .NotEmpty().WithMessage("A mensagem não pode ser vazia.");
        }
    }
}
