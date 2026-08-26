using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AIstudentskillexchange.Data;
using AIstudentskillexchange.Models;
using AIstudentskillexchange.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure the SQL Server Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Configure ASP.NET Core Identity for Authentication
builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

// AI Recommendation Module (peer recommendations + AI skill analysis)
builder.Services.AddAiRecommendationModule(builder.Configuration);

// 3. Add MVC Controller and View support
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages(); 

// Peer Discovery and Skill Matching Module (student search + skill matching)
builder.Services.AddPeerDiscoveryModule(builder.Configuration);

var app = builder.Build();

// 4. Configure the HTTP request pipeline (Middleware)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 5. Define standard MVC routing paths
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages(); 

app.Run();