namespace Bernhoeft.GRT.Teste.Domain.Entities
{
    public partial class AvisoEntity
    {
        public int Id { get; private set; }
        public bool Ativo { get; set; } = true;
        public string Titulo { get; set; }
        public string Mensagem { get; set; }
        public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;
        public DateTime? AtualizadoEm { get; private set; }

        public void AtualizarMensagem(string mensagem)
        {
            Mensagem = mensagem;
            AtualizadoEm = DateTime.UtcNow;
        }

        public void Desativar()
        {
            Ativo = false;
            AtualizadoEm = DateTime.UtcNow;
        }
    }
}