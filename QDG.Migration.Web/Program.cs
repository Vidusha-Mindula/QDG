using Microsoft.EntityFrameworkCore;
using QDG.Migration.Core.Interfaces;
using QDG.Migration.Core.Services;
using QDG.Migration.Data;
using QDG.Migration.Data.Repositories;
using QDG.Migration.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
});
builder.Services.AddRazorPages();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("AppDatabase") ?? "Data Source=app_v2.db"));

builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<ITableService, TableService>();
builder.Services.AddScoped<IRelationshipService, RelationshipService>();
builder.Services.AddScoped<IMigrationService, MigrationService>();
builder.Services.AddScoped<IDependencyResolver, DependencyResolver>();
builder.Services.AddScoped<IDatabaseAnalyzerService, DatabaseAnalyzerService>();
builder.Services.AddScoped<IConfigRepository, ConfigRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IUserService, UserService>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? "ThisIsASecretKeyForDevelopmentOnly1234567890";
var key = System.Text.Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            context.Token = context.Request.Cookies["AuthToken"];
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            context.Response.Redirect("/Account/Login");
            context.HandleResponse();
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var connectionString = builder.Configuration.GetConnectionString("AppDatabase") ?? "Data Source=app_v2.db";
    var dbPath = connectionString.Replace("Data Source=", "").Split(';')[0].Trim();

    if (!Path.IsPathRooted(dbPath))
    {
        dbPath = Path.Combine(AppContext.BaseDirectory, dbPath);
    }

    var directory = Path.GetDirectoryName(dbPath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    {
        Directory.CreateDirectory(directory);
        Console.WriteLine($"Created directory: {directory}");
    }

    Console.WriteLine($"Database will be created at: {dbPath}");

    try
    {
        dbContext.Database.EnsureCreated();
        Console.WriteLine("Database created successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error creating database: {ex.Message}");
        throw;
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
