using FluentValidation;

namespace Bernhoeft.GRT.Teste.Application.Requests.Commands.v1.Validations
{
    public class CreateAvisoRequestValidator : AbstractValidator<CreateAvisoRequest>
    {
        public CreateAvisoRequestValidator()
        {
            RuleFor(x => x.Titulo)
                .NotNull().WithMessage("O título é obrigatório.")
                .NotEmpty().WithMessage("O título não pode ser vazio.")
                .MaximumLength(50).WithMessage("O título deve ter no máximo 50 caracteres.");

            RuleFor(x => x.Mensagem)
                .NotNull().WithMessage("A mensagem é obrigatória.")
                .NotEmpty().WithMessage("A mensagem não pode ser vazia.");
        }
    }
}
