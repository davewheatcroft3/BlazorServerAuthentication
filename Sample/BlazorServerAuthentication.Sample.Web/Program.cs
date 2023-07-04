using BlazerServerAuthentication;
using BlazerServerAuthentication.Sample.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services
    .AddBlazorServerAuthentication(builder.Configuration, options =>
    {
        options.UseIdTokenForHttpAuthentication = true;
        options.RefreshExpiryClockSkewInMinutes = 2;
    })
    .AddAuthorizedHttpClient<WeatherForecastApiClient>((sp, h) =>
    {
        var baseUrl = builder.Configuration["Api:BaseUrl"];
        h.BaseAddress = new Uri(baseUrl!);
    });

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

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.UseBlazorServerAuthentication();
app.MapFallbackToPage("/_Host");

app.Run();
