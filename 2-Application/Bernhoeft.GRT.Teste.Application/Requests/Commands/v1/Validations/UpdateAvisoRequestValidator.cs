using FluentValidation;

namespace Bernhoeft.GRT.Teste.Application.Requests.Commands.v1.Validations
{
    public class UpdateAvisoRequestValidator : AbstractValidator<UpdateAvisoRequest>
    {
        public UpdateAvisoRequestValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("O ID deve ser maior que zero.");

            RuleFor(x => x.Body)
                .NotNull().WithMessage("O corpo da requisição é obrigatório.");

            When(x => x.Body != null, () =>
            {
                RuleFor(x => x.Body.Mensagem)
                    .NotNull().WithMessage("A mensagem é obrigatória.")
                    .NotEmpty().WithMessage("A mensagem não pode ser vazia.");
            });
        }
    }
}
