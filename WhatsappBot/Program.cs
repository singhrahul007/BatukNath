



using Microsoft.EntityFrameworkCore;
using WhatsappBot.Data;
using WhatsappBot.Model;
using WhatsappBot.Services;

var builder = WebApplication.CreateBuilder(args);
// Load appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
// Force HTTP + HTTPS port binding
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5161); // HTTP
    options.ListenLocalhost(5160, listenOptions =>
    {
        listenOptions.UseHttps(); // HTTPS
    });
});


// Add servicesb

// Add DB Context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddSingleton<INlpService, NlpService>();

builder.Services.AddControllers();
builder.Services.AddScoped<IWhatsAppApiService, MockWhatsAppApiService>();
builder.Services.AddSingleton<WhatsAppService>();
builder.Services.AddSingleton<ManualIntent>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();       

var app = builder.Build();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

// Enable routing
app.UseRouting();
app.Use(async (context, next) =>
{
    context.Response.Headers["ngrok-skip-browser-warning"] = "true";
    await next();
});
app.Use(async (context, next) =>
{
    if (context.Request.Headers.UserAgent.ToString().Contains("facebookexternalua"))
    {
        context.Response.Headers["ngrok-skip-browser-warning"] = "true";
    }

    await next();
});
app.Use(async (context, next) =>
{
    context.Request.Headers["ngrok-skip-browser-warning"] = "true";
    context.Response.Headers["ngrok-skip-browser-warning"] = "true";
    await next();
});
app.Use(async (context, next) =>
{
    context.Response.Headers["ngrok-skip-browser-warning"] = "true";
    context.Request.Headers["User-Agent"] = "curl";

    await next();
});

app.MapControllers();  // <-- MOST IMPORTANT

app.MapGet("/", () => "WhatsApp Bot API Running...");

app.Run();
