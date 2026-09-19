var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "K8 Resources Demo - Application is running!");

app.Run();