using System.Net.Mime;
using System.Text.Json;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddRavenDB(options =>
    {
        options.Urls = new[] { "http://localhost:8080/" };
        options.Database = "MinhaOrganizacao";
    });

builder.Services.AddHealthChecksUI(opt =>
{
    opt.AddHealthCheckEndpoint("Minha API", "/health-ui");
}).AddInMemoryStorage();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

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

app.UseHealthChecks("/health-ui", new HealthCheckOptions
{
    Predicate = p => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.UseHealthChecksUI(options =>
{
    options.UIPath = "/monitor";
});

app.Run();