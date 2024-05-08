# Health Check de aplicações em ASP. NET Core

Em ambientes distribuídos, principalmente em um ecossistema que utilize microsserviços, a monitoração da saúde das aplicações é de extrema importância. Como citado no artigo de [Damian Edwards](https://devblogs.microsoft.com/dotnet/asp-net-core-2-2-0-preview1-healthcheck/), um load balancer, por exemplo, ao verificar que uma aplicação não está respondendo pode redirecionar a requisição; e em ambientes de contêineres o orquestrador pode aplicar uma política para a situação, como reiniciar a aplicação.

No ASP.NET Core é possível implementar o monitoramento da saúde da aplicação e seus recursos, como banco de dados ou informações de memória e disco sem complicações.

## Implementação

A aplicação de exemplo foi construída em .NET 6, porém também é válida para .NET 5 e Core. 

Após criarmos uma WEB API, na classe Program.cs da nossa API precisamos incluir as seguintes linhas:

```csharp
builder.Services.AddHealthChecks();
```

```csharp
app.MapHealthChecks("/health");
```

Desta maneira ao executarmos nossa API teremos o seguinte resultado:

![hc1](https://github.com/a-renner/healthcheck-aspnetcore/assets/110235420/c57727bb-c8e0-4cde-b4a8-6807c7b759f5)

Incluindo apenas essas linhas já temos o retorno indicando o status da aplicação.

## Incluindo monitoramento de recursos

Para verificarmos o status da conexão com o banco de dados podemos utilizar os pacotes do projeto [Xabaril](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks#ui-storage-providers). No exemplo verificaremos se uma database do RavenDB está disponível para a aplicação.

Começamos adicionando o pacote AspNetCore.HealthChecks.RavenDB pela CLI do .NET:

```csharp
dotnet add package AspNetCore.HealthChecks.RavenDB --version 6.0.2
```

E logo em seguida incluímos a verificação:

```csharp
builder.Services.AddHealthChecks()
    .AddRavenDB(options =>
    {
        options.Urls = new[] { "Url do cluster" };
        options.Database = "Nome da Database";
    });

```

## Personalizando o retorno do Health Check

Também é possível personalizar o retorno da nossa rota. Nesse exemplo eu criei um novo endpoint de HealthCheck, porém estou customizando o JSON de retorno:

```csharp
app.MapHealthChecks("/health-info",
    new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            var result = JsonSerializer.Serialize(
                new
                {
                    Name = "Minha API",
                    status = report.Status.ToString(),
                    Info = report.Entries.Select(e => new
                    {
                        key = e.Key, 
                        Status = Enum.GetName(typeof(HealthStatus), e.Value.Status),
                        Error = e.Value.Exception?.Message
                    })
                });
            context.Response.ContentType = MediaTypeNames.Application.Json;
            await context.Response.WriteAsync(result);
        }
    }
);
```

Agora ao realizar a requisição para a rota “/health-info” terei o seguinte JSON de retorno:

```json
{
  "Name": "Minha API",
  "status": "Healthy",
  "Info": [
    {
      "key": "ravendb",
      "Status": "Healthy",
      "Error": null
    }
  ]
}
```

## Interface gráfica

É possível visualizarmos o HealthCheck com uma interface amigável através de pacotes do já mencionado [Xabaril](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks#ui-storage-providers).

Comandos para a instalação:

```csharp
dotnet add package AspNetCore.HealthChecks.UI -v 6.0.5
dotnet add package AspNetCore.HealthChecks.UI.Client -v 6.0.5
dotnet add package AspNetCore.HealthChecks.UI.InMemory.Storage -v 6.0.5
```

Com os pacotes instalados podemos adicionar o HealthCheckUI na aplicação. Nos trechos de códigos seguintes estamos criando um novo endpoint chamado “/health-ui” e estamos o consumindo na UI no endereço “/monitor”

```csharp
builder.Services.AddHealthChecksUI(opt =>
{
    opt.AddHealthCheckEndpoint("Minha API", "/health-ui");
}).AddInMemoryStorage();
```

```csharp
app.UseHealthChecks("/health-ui", new HealthCheckOptions
{
    Predicate = p => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
```

```csharp
app.UseHealthChecksUI(options =>
{
    options.UIPath = "/monitor";
});
```

Ao acessarmos o endereço “/monitor” podemos visualizar os recursos configurados.

![hc2](https://github.com/a-renner/healthcheck-aspnetcore/assets/110235420/8fa9c669-7712-416e-a9d9-c01f98c2ead2)

Caso necessário podemos alterar os arquivos CSS e Javascript da dashboard. Caso tenha interesse existe um exemplo no tópico **UI Style and branding customization** do [Xabaril](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks#ui-storage-providers).

### O projeto está disponível no [GitHub](https://github.com/arirennerf/healthcheck-aspnetcore).

## Referências:

https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-6.0

https://devblogs.microsoft.com/dotnet/asp-net-core-2-2-0-preview1-healthcheck/

[Xabaril/AspNetCore.Diagnostics.HealthChecks: Enterprise HealthChecks for ASP.NET Core Diagnostics Package (github.com)](https://github.com/Xabaril/AspNetCore.Diagnostics.HealthChecks#ui-storage-providers)
