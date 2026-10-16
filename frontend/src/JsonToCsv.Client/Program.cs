using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using JsonToCsv.Client;
using JsonToCsv.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// URL del backend configurable en wwwroot/appsettings.json (clave ApiBaseUrl)
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5100/";
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
builder.Services.AddScoped<ConverterApiClient>();

await builder.Build().RunAsync();
