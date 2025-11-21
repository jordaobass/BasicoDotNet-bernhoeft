using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Bernhoeft.GRT.Teste.IntegrationTests.Controllers.v1
{
    public class AvisosControllerTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "/api/v1/avisos";
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public AvisosControllerTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private async Task<int> CriarAvisoERetornarIdAsync(string titulo, string mensagem)
        {
            var request = new { Titulo = titulo, Mensagem = mensagem };
            var response = await _client.PostAsJsonAsync(BaseUrl, request);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            // Tenta deserializar no formato { "Data": { "Id": ... } }
            try
            {
                var wrapper = JsonSerializer.Deserialize<ApiResponse>(content, JsonOptions);
                if (wrapper?.Data?.Id > 0)
                    return wrapper.Data.Id;
            }
            catch { }

            // Tenta extrair ID diretamente com regex
            var match = System.Text.RegularExpressions.Regex.Match(content, @"""Id""\s*:\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var id))
                return id;

            return 0;
        }

        #region GET /avisos

        [Fact]
        public async Task GetAvisos_DeveRetornarStatusValido()
        {
            // Act
            var response = await _client.GetAsync(BaseUrl);

            // Assert - Pode ser 200 com lista ou 204 sem conteúdo
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task GetAvisos_QuandoExistemAvisos_DeveRetornarLista()
        {
            // Arrange
            await CriarAvisoERetornarIdAsync("Aviso Teste Lista", "Mensagem teste lista");

            // Act
            var response = await _client.GetAsync(BaseUrl);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Aviso Teste Lista");
        }

        #endregion

        #region GET /avisos/{id}

        [Fact]
        public async Task GetAvisoById_QuandoExiste_DeveRetornar200()
        {
            // Arrange
            var id = await CriarAvisoERetornarIdAsync("Aviso Busca Por Id", "Mensagem busca");

            // Act
            var response = await _client.GetAsync($"{BaseUrl}/{id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Aviso Busca Por Id");
            content.Should().Contain("CriadoEm");
        }

        [Fact]
        public async Task GetAvisoById_QuandoNaoExiste_DeveRetornar404()
        {
            // Act
            var response = await _client.GetAsync($"{BaseUrl}/99999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetAvisoById_QuandoIdZero_DeveRetornarErro()
        {
            // Act
            var response = await _client.GetAsync($"{BaseUrl}/0");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetAvisoById_QuandoIdNegativo_DeveRetornarErro()
        {
            // Act
            var response = await _client.GetAsync($"{BaseUrl}/-1");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        }

        #endregion

        #region POST /avisos

        [Fact]
        public async Task CreateAviso_QuandoDadosValidos_DeveRetornar200()
        {
            // Arrange
            var request = new { Titulo = "Novo Aviso Criado", Mensagem = "Nova mensagem criada" };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Novo Aviso Criado");
            content.Should().Contain("Nova mensagem criada");
            content.Should().Contain("CriadoEm");
        }

        [Fact]
        public async Task CreateAviso_QuandoTituloVazio_DeveRetornar400()
        {
            // Arrange
            var request = new { Titulo = "", Mensagem = "Mensagem válida" };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateAviso_QuandoMensagemVazia_DeveRetornar400()
        {
            // Arrange
            var request = new { Titulo = "Título válido", Mensagem = "" };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateAviso_QuandoTituloExcede50Caracteres_DeveRetornar400()
        {
            // Arrange
            var request = new { Titulo = new string('A', 51), Mensagem = "Mensagem válida" };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateAviso_DeveTerCriadoEmPreenchido()
        {
            // Arrange
            var request = new { Titulo = "Aviso Auditoria Criacao", Mensagem = "Mensagem auditoria" };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var wrapper = JsonSerializer.Deserialize<ApiResponse>(content, JsonOptions);
            wrapper?.Data?.CriadoEm.Should().NotBe(default(DateTime));
        }

        [Fact]
        public async Task CreateAviso_QuandoTituloNulo_DeveRetornar400()
        {
            // Arrange
            var request = new { Titulo = (string)null, Mensagem = "Mensagem válida" };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateAviso_QuandoMensagemNula_DeveRetornar400()
        {
            // Arrange
            var request = new { Titulo = "Título válido", Mensagem = (string)null };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task CreateAviso_QuandoTituloComExatamente50Caracteres_DeveRetornar200()
        {
            // Arrange
            var request = new { Titulo = new string('A', 50), Mensagem = "Mensagem válida" };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CreateAviso_QuandoApenasEspacos_DeveRetornar400()
        {
            // Arrange
            var request = new { Titulo = "   ", Mensagem = "   " };

            // Act
            var response = await _client.PostAsJsonAsync(BaseUrl, request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        #endregion

        #region PUT /avisos/{id}

        [Fact]
        public async Task UpdateAviso_QuandoDadosValidos_DeveRetornar200()
        {
            // Arrange
            var id = await CriarAvisoERetornarIdAsync("Aviso Para Update", "Mensagem original");
            var updateRequest = new { Mensagem = "Mensagem foi atualizada" };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{id}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Mensagem foi atualizada");
            content.Should().Contain("Aviso Para Update"); // Título não deve mudar
        }

        [Fact]
        public async Task UpdateAviso_QuandoNaoExiste_DeveRetornar404()
        {
            // Arrange
            var updateRequest = new { Mensagem = "Mensagem atualizada" };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/99999", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateAviso_QuandoMensagemVazia_DeveRetornar400()
        {
            // Arrange
            var id = await CriarAvisoERetornarIdAsync("Aviso Msg Vazia", "Mensagem original");
            var updateRequest = new { Mensagem = "" };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{id}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateAviso_DeveTerAtualizadoEmPreenchido()
        {
            // Arrange
            var id = await CriarAvisoERetornarIdAsync("Aviso Auditoria Update", "Mensagem original");
            var updateRequest = new { Mensagem = "Mensagem atualizada auditoria" };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{id}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("AtualizadoEm");
        }

        [Fact]
        public async Task UpdateAviso_QuandoIdZero_DeveRetornarErro()
        {
            // Arrange
            var updateRequest = new { Mensagem = "Mensagem atualizada" };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/0", updateRequest);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateAviso_QuandoMensagemNula_DeveRetornar400()
        {
            // Arrange
            var id = await CriarAvisoERetornarIdAsync("Aviso Msg Nula", "Mensagem original");
            var updateRequest = new { Mensagem = (string)null };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{id}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateAviso_QuandoMensagemApenasEspacos_DeveRetornar400()
        {
            // Arrange
            var id = await CriarAvisoERetornarIdAsync("Aviso Espacos", "Mensagem original");
            var updateRequest = new { Mensagem = "   " };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{id}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateAviso_NaoDeveAlterarTitulo()
        {
            // Arrange
            var id = await CriarAvisoERetornarIdAsync("Titulo Original", "Mensagem original");
            var updateRequest = new { Mensagem = "Mensagem atualizada" };

            // Act
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{id}", updateRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Titulo Original");
            content.Should().Contain("Mensagem atualizada");
        }

        [Fact]
        public async Task UpdateAviso_MultiplasAtualizacoes_DeveManterUltimaMensagem()
        {
            // Arrange
            var id = await CriarAvisoERetornarIdAsync("Aviso Multi Update", "Mensagem original");

            // Act - Múltiplas atualizações
            await _client.PutAsJsonAsync($"{BaseUrl}/{id}", new { Mensagem = "Update 1" });
            await _client.PutAsJsonAsync($"{BaseUrl}/{id}", new { Mensagem = "Update 2" });
            var response = await _client.PutAsJsonAsync($"{BaseUrl}/{id}", new { Mensagem = "Update Final" });

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("Update Final");
        }

        #endregion

        #region DELETE /avisos/{id}

        [Fact]
        public async Task DeleteAviso_QuandoExiste_DeveRetornar204()
        {
            // Arrange
            var id = await CriarAvisoERetornarIdAsync("Aviso Para Deletar", "Mensagem deletar");

            // Act
            var response = await _client.DeleteAsync($"{BaseUrl}/{id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task DeleteAviso_QuandoNaoExiste_DeveRetornar404()
        {
            // Act
            var response = await _client.DeleteAsync($"{BaseUrl}/99999");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteAviso_SoftDelete_AvisoNaoDeveAparecerNaBusca()
        {
            // Arrange
            var id = await CriarAvisoERetornarIdAsync("Aviso Soft Delete Test", "Mensagem soft delete");

            // Act - Deletar
            var deleteResponse = await _client.DeleteAsync($"{BaseUrl}/{id}");
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert - Buscar deve retornar 404
            var getResponse = await _client.GetAsync($"{BaseUrl}/{id}");
            getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteAviso_QuandoIdZero_DeveRetornarErro()
        {
            // Act
            var response = await _client.DeleteAsync($"{BaseUrl}/0");

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteAviso_DuploDelete_DeveRetornar404NaSegunda()
        {
            // Arrange
            var id = await CriarAvisoERetornarIdAsync("Aviso Duplo Delete", "Mensagem duplo delete");

            // Act - Primeiro delete
            var firstDelete = await _client.DeleteAsync($"{BaseUrl}/{id}");
            firstDelete.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Act - Segundo delete
            var secondDelete = await _client.DeleteAsync($"{BaseUrl}/{id}");

            // Assert
            secondDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteAviso_NaoDeveAparecerNaListagem()
        {
            // Arrange
            var id = await CriarAvisoERetornarIdAsync("Aviso Para Remover Lista", "Mensagem lista");

            // Act - Deletar
            await _client.DeleteAsync($"{BaseUrl}/{id}");

            // Assert - Não deve aparecer na lista
            var response = await _client.GetAsync(BaseUrl);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("Aviso Para Remover Lista");
        }

        #endregion
    }

    // Classes auxiliares para deserialização
    public class ApiResponse
    {
        public ApiData Data { get; set; }
    }

    public class ApiData
    {
        public int Id { get; set; }
        public bool Ativo { get; set; }
        public string Titulo { get; set; }
        public string Mensagem { get; set; }
        public DateTime CriadoEm { get; set; }
        public DateTime? AtualizadoEm { get; set; }
    }
}
