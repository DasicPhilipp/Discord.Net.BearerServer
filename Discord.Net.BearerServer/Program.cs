using Discord.Net.BearerServer;
using Discord.Net.BearerServer.Configuration;
using Microsoft.AspNetCore.SignalR;
using System.Net;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

Config.SetUp(builder.Configuration
    .AddJsonFile("appsettings.json", false, true)
    .Build());

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

HttpClient httpClient = new HttpClient();

app.MapHub<AuthHub>(Config.Uris.AuthHabRoute);
app.MapGet(Config.Uris.CodePageRoute, async context =>
{
    string? code = context.Request.Query["code"];
    if (string.IsNullOrEmpty(code))
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        await context.Response.WriteAsync(Config.Messages.CodeIsMissing);
        return;
    }
    
    HttpRequestMessage request = new HttpRequestMessage
    {
        Method = HttpMethod.Post,
        RequestUri = new Uri("https://discord.com/api/oauth2/token"),
        Headers =
        {
            { HttpRequestHeader.ContentType.ToString(), "application/x-www-form-urlencoded" }
        },
        Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "client_id", Config.Secrets.ClientId },
            { "client_secret", Config.Secrets.ClientSecret },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", $"{Config.Uris.ApplicationUrl}{Config.Uris.CodePageRoute}" }
        })
    };

    HttpResponseMessage message = await httpClient.SendAsync(request);
    if (!message.IsSuccessStatusCode)
    {
        await context.Response.WriteAsync(Config.Messages.OnFailingRequest);
        return;
    }

    string jsonResponse = await message.Content.ReadAsStringAsync();
    
    IHubContext<AuthHub> hubContext = context.RequestServices.GetRequiredService<IHubContext<AuthHub>>();
    await hubContext.Clients.All.SendAsync("ReceiveBearerToken", jsonResponse);

    context.Response.StatusCode = (int)HttpStatusCode.OK;
    context.Response.Redirect(Config.Uris.RedirectLocationOnSuccess);
});

app.Run(Config.Uris.ApplicationUrl);