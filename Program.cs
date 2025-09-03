var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var api = app.MapGroup("/api");

api.MapGet("/", () => "Hello World!");

// Launch the application.
app.Run();