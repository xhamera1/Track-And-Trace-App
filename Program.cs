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

builder.Services.AddHttpClient<IGeocodingService, NominatimGeocodingService>();

builder.Services.Configure<NominatimGeocodingOptions>(options =>
{
    options.UserAgent = "PackageTrackingApp/1.0";
    options.ContactEmail = "admin@packagetracking.com";
    options.TimeoutSeconds = 10;
    options.MaxResults = 1;
    options.Language = "en";
});

builder.Services.AddDistributedMemoryCache(); 
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Home/Index";
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

app.UseSession();
app.UseAuthentication();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
