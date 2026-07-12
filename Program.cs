using HospitalFlow.Data;
using HospitalFlow.Hubs;
using HospitalFlow.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- DATABASE CONFIGURATION (ROBUST PARSER) ---
var rawUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
string connectionString;

if (!string.IsNullOrEmpty(rawUrl) && rawUrl.StartsWith("postgres"))
{
    // 1. Normalize the URL so the Uri class can read it
    var cleanUrl = rawUrl.Replace("postgresql://", "postgres://");
    var databaseUri = new Uri(cleanUrl);
    var userInfo = databaseUri.UserInfo.Split(':');

    // 2. Extract parts and handle the -1 Port issue
    var host = databaseUri.Host;
    var port = databaseUri.Port == -1 ? 5432 : databaseUri.Port;
    var database = databaseUri.AbsolutePath.TrimStart('/');
    var user = userInfo[0];
    var password = userInfo[1];

    // 3. Build a standard string Npgsql is guaranteed to accept
    connectionString = $"Host={host};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
}
else
{
    // Use local connection string if DATABASE_URL isn't set
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("Connection string not found.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));
// ----------------------------------------------

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddHttpClient<IAiAssistantService, GroqDeepSeekService>();

var app = builder.Build();

// Automatically apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapHub<HospitalHub>("/hospitalHub");
app.MapRazorPages().WithStaticAssets();

app.Run();