using Microsoft.EntityFrameworkCore;
using _10.Data;
using _10.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();



var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
}
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
);

builder.Services.AddScoped<ICourierService, CourierService>();
builder.Services.AddScoped<IPackageAuthorizationService, PackageAuthorizationService>();
builder.Services.AddScoped<IPackageLocationService, PackageLocationService>();

// Add HttpClient for geocoding service
builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();

// Configure geocoding service options
builder.Services.Configure<NominatimGeocodingOptions>(options =>
{
    options.UserAgent = "PackageTrackingApp/1.0";
    options.ContactEmail = "admin@packagetracking.com"; // Replace with your contact email
    options.TimeoutSeconds = 10;
    options.MaxResults = 1;
    options.Language = "en";
});

// Add session services
builder.Services.AddDistributedMemoryCache(); // Required for session state
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1); // Set session timeout to 1 hour
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Home/Index";
        // options.ExpireTimeSpan = TimeSpan.FromHours(1);
    });
//

var app = builder.Build();



if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession(); // Add session middleware
app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
