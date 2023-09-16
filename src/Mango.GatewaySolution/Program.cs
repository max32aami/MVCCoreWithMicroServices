using Mango.GatewaySolution.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);
builder.AddAppAuthentications();
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration); // this is registration--> like reistring Ocelot in application
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.UseOcelot().GetAwaiter().GetResult(); // this is injeting or configuring in pipeline
app.Run();
