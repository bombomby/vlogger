using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using VL.Components.Database;
using VL.Components.Services;
using VL.Service.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddSingleton<WeatherForecastService>();
builder.Services.AddSingleton<JobService>();
builder.Services.AddSingleton<ETWService>();
builder.Services.AddMudServices();

builder.Services.AddDbContextFactory<JobContext>(opt => opt.UseSqlite($"Data Source={JobContext.DbName}.db"));

builder.WebHost.UseKestrel().UseUrls($"http://localhost:6000", $"https://localhost:6001");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();


