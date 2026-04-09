using ASB.POC.API.Endpoints;
using ASB.POC.Application;
using ASB.POC.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ASB.POC API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapControllers();
app.MapMessagesEndpoints();

app.Run();
