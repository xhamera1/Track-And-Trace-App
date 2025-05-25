using _10.Models;
using _10.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Konfiguracja MySqlSettings
builder.Services.Configure<MySqlSettings>(builder.Configuration.GetSection("MySqlSettings"));

var mySqlPassword = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");
var mySqlUser = Environment.GetEnvironmentVariable("MYSQL_USER");

builder.Services.AddOptions<MySqlSettings>()
    .PostConfigure<ILogger<Program>>((mySqlSettings, logger) =>
    {
        if (!string.IsNullOrEmpty(mySqlPassword) && !string.IsNullOrEmpty(mySqlUser))
        {
            mySqlSettings.Password = mySqlPassword;
            mySqlSettings.User = mySqlUser;
        }
        else
        {
            logger?.LogWarning("MYSQL_PASSWORD or MYSQL_USER environment variable not set. Using password from appsettings.json (if any).");
        }
    });

// Rejestracja DataSeederService
builder.Services.AddScoped<IDataSeederService, DataSeederService>();

var app = builder.Build();

// Uruchomienie DataSeederService
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var seeder = services.GetRequiredService<IDataSeederService>();
        await seeder.InitializeAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
        throw new ApplicationException("Database seeding failed.", ex);
    }
}

if (!app.Environment.IsDevelopment())
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


app.Run();
