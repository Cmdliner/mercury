using System.Text;
using Mercury.Api.Data;
using Mercury.Api.Services;
using Mercury.Payments;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(options => options
    .UseNpgsql(builder.Configuration.GetConnectionString("Mercury"))
    .UseSnakeCaseNamingConvention());

builder.Services.Configure<PaystackOptions>(builder.Configuration.GetSection("Paystack"));
builder.Services.Configure<NombaOptions>(builder.Configuration.GetSection("Nomba"));
builder.Services.AddHttpClient<PaystackCollector>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<PaystackOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers
        .AuthenticationHeaderValue("Bearer", options.SecretKey);
});
builder.Services.AddHttpClient("Nomba", (sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<NombaOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
});

builder.Services.AddIdentityCore<IdentityUser<Guid>>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>();

var jwtSigningKey = builder.Configuration.GetSection("Jwt:SigningKey").Value ??
                    throw new InvalidOperationException("Jwt:SigningKey is not configured.");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true, ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey))
    };
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
// builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSingleton<INombaTokenProvider, NombaTokenProvider>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IPaymentCollector, PaystackCollector>();
builder.Services.AddScoped<IPaymentCollector, NombaCollector>();
builder.Services.AddScoped<PaymentCollectorFactory>();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();