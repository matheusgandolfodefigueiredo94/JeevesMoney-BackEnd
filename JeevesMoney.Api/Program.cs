using JeevesMoney.Application.Interfaces;
using JeevesMoney.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    // Em produção, é bom limitar os proxies, mas para o Render,
    // "Clear" é geralmente seguro, pois ele controla a rede.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowDevAndProd",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200", "https://jeevesmoney.onrender.com") // Permite o seu frontend
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var supabaseConfig = builder.Configuration.GetSection("Supabase");
var supabaseAuthority = supabaseConfig["Authority"];
var supabaseAudience = supabaseConfig["Audience"];
var supabaseJwtSecret = supabaseConfig["JwtSecret"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // A "Authority" é a URL que emite os tokens
        options.Authority = supabaseAuthority;

        // O "Audience" padrão do Supabase
        options.Audience = supabaseAudience;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            // Define o Issuer e Audience válidos
            ValidIssuer = supabaseAuthority,
            ValidAudience = supabaseAudience,

            // Esta é a parte crucial: validar a chave de assinatura
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseJwtSecret))
        };
    });

builder.Services.AddAuthorization();

// Register Brapi HttpClient. BaseAddress points to Brapi host.
var brapiApiKey = builder.Configuration.GetValue<string>("Brapi:ApiKey");
builder.Services.AddHttpClient<IStockService, BrapiStockService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("Brapi:BaseUrl") ?? "https://brapi.dev");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    if (!string.IsNullOrEmpty(brapiApiKey))
    {
        // Use Authorization: Bearer {API_KEY}
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", brapiApiKey);
    }
});

// Keep Yahoo as alternative implementation registered by name (optional)
builder.Services.AddHttpClient("Yahoo", client =>
{
    client.BaseAddress = new Uri("https://query1.finance.yahoo.com");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Swagger / OpenAPI (Swashbuckle) - simple configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
app.UseHttpsRedirection();

// Swagger middleware - available in all environments; you can limit to Development if desired
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "JeevesMoney API v1");
    c.RoutePrefix = "swagger"; // serve at /swagger
});

app.UseCors("AllowDevAndProd");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
