using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Serialization;
using Bernhoeft.GRT.Core.Extensions;
using Bernhoeft.GRT.Teste.Api.Middlewares;
using Bernhoeft.GRT.Teste.Api.Swashbuckle;
using Bernhoeft.GRT.Teste.Application.Requests.Queries.v1;
using Bernhoeft.GRT.Teste.Infra.Persistence.InMemory.Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using Unchase.Swashbuckle.AspNetCore.Extensions.Extensions;

var builder = WebApplication.CreateBuilder(args);

if (Debugger.IsAttached)
    builder.Configuration.AddJsonFile("appsettings.Debugger.json", true);

builder.WebHost.UseKestrel(options =>
{
    options.AddServerHeader = false; /* Header com Informações do Servidor. */
    options.Limits.MaxRequestLineSize = 32 * 1024; /* Tamanho máximo do URL (em bytes). */
    // TODO: Avaliar se 50MB é adequado para esta API. Para APIs de CRUD simples, considerar reduzir para 1-5MB.
    // Valores altos podem expor a API a ataques de negação de serviço (DoS) por consumo de memória.
    options.Limits.MaxRequestBodySize = 50 * 1024 * 1024; /* Tamanho máximo da solicitação (em bytes). */
    // TODO: Configurar taxas mínimas em produção para evitar ataques Slowloris (conexões lentas que consomem recursos).
    // Exemplo: new MinDataRate(bytesPerSecond: 100, gracePeriod: TimeSpan.FromSeconds(10))
    options.Limits.MinResponseDataRate = null; /* Taxa mínima de dados de resposta (bytes/segundo). */
    options.Limits.MinRequestBodyDataRate = null; /* Taxa mínima de dados de solicitação (bytes/segundo). */
    // TODO: Configurar timeout para requisições em produção para evitar conexões penduradas.
    // Exemplo:
    // options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(2);
    // options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
    if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
        options.ListenAnyIP(port);
});

// Adicionando os serviços no container.
builder.Services.AddMemoryCache()
                .AddControllers(options =>
                {
                    options.MaxModelBindingCollectionSize = int.MaxValue;
                    options.CacheProfiles.Add("DefaultCache", new CacheProfile
                    {
                        Duration = 3600,
                        Location = ResponseCacheLocation.Client
                    });
                })
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                });

builder.Services.AddEndpointsApiExplorer();

// Configurando o versionamento.
builder.Services.AddApiVersioning(p =>
                {
                    p.DefaultApiVersion = new ApiVersion(1, 0);
                    p.ReportApiVersions = true;
                    p.AssumeDefaultVersionWhenUnspecified = true;
                })
                .AddApiExplorer(p =>
                {
                    p.GroupNameFormat = "'Teste API v'VVV";
                    p.SubstituteApiVersionInUrl = true;
                });

builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
builder.Services.AddSwaggerGen(options =>
{
    options.IgnoreObsoleteProperties();
    options.AddGRTSwaggerGenFilters();
    options.AddEnumsWithValuesFixFilters(option =>
    {
        option.ApplySchemaFilter = true;
        option.ApplyParameterFilter = true;
        option.ApplyDocumentFilter = true;
        option.IncludeDescriptions = true;
        option.IncludeXEnumRemarks = true;
        option.DescriptionSource = DescriptionSources.DescriptionAttributesThenXmlComments;
    });

    Directory.GetFiles(AppContext.BaseDirectory, "*.xml").ForEach(xmlFile =>
    {
        options.IncludeXmlComments(xmlFile, true);
    });
});

// Configurando o MediatR.
builder.Services.AddMediatR(options => options.RegisterServicesFromAssemblyContaining<GetAvisosRequest>());

// Adicionar Context de Conexão com Banco de Dados SqlServer GRT.
builder.Services.AddDbContext();

// Outros Serviços.
builder.Services.RegisterServicesFromAssemblyContaining<GetAvisosRequest>();

// Adicionando Fluent Validation.
ValidatorOptions.Global.DefaultClassLevelCascadeMode = CascadeMode.Stop;
ValidatorOptions.Global.DefaultRuleLevelCascadeMode = CascadeMode.Stop;
ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("pt-BR");
builder.Services.AddFluentValidationAutoValidation(options => options.DisableDataAnnotationsValidation = true)
                .AddFluentValidationClientsideAdapters()
                .AddValidatorsFromAssemblyContaining<GetAvisosRequest>();
builder.Services.AddFluentValidationRulesToSwagger();

// Configure Some Options
builder.Services.Configure<FormOptions>(options => options.ValueCountLimit = int.MaxValue)
                .Configure<RouteOptions>(options => options.LowercaseUrls = true);

// TODO: Configurar Rate Limiting para proteger a API contra abuso
// Exemplo:
// builder.Services.AddRateLimiter(options =>
// {
//     options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
//         RateLimitPartition.GetFixedWindowLimiter(
//             partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
//             factory: _ => new FixedWindowRateLimiterOptions
//             {
//                 AutoReplenishment = true,
//                 PermitLimit = 100,
//                 Window = TimeSpan.FromMinutes(1)
//             }));
//     options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
// });
// Requer: using System.Threading.RateLimiting;

// Configurando a Pipeline do HTTP Request.
var app = builder.Build();

// Middleware de tratamento global de exceções (deve ser o primeiro)
app.UseGlobalExceptionHandler();

app.UseForwardedHeaders(new()
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

if (app.Environment.IsDevelopment() || app.Environment.IsStaging() || Debugger.IsAttached)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        apiVersionDescriptionProvider?.ApiVersionDescriptions?.Reverse().ForEach(description =>
        {
            options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName);
        });

        options.RoutePrefix = string.Empty;
    });
}

// TODO: Configurar CORS de forma mais restritiva em produção
// A configuração atual permite qualquer origem (SetIsOriginAllowed = true), o que é adequado para desenvolvimento
// Em produção, especifique as origens permitidas explicitamente:
// Exemplo:
// app.UseCors(options => options.WithOrigins("https://meusite.com", "https://app.meusite.com")
//                               .AllowAnyHeader()
//                               .AllowAnyMethod()
//                               .AllowCredentials());
app.UseCors(options => options.WithOrigins()
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials()
                              .SetIsOriginAllowed(origin => true));
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
await app.RunAsync();

public partial class Program
{ }