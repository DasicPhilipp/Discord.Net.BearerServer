using Discord.Net.BearerServer;
using Microsoft.AspNetCore.SignalR;
using System.Net;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

HttpClient httpClient = new HttpClient();

app.MapHub<AuthHub>("/bearer/discord");
app.MapGet("/connections/discord", async context =>
{
    string? code = context.Request.Query["code"];
    if (string.IsNullOrEmpty(code))
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        await context.Response.WriteAsync("400: Bad Request (the code is missing)");
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
            { "client_id", "***REMOVED***" },
            { "client_secret", "***REMOVED***" },
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", "https://localhost:44327/connections/discord" }
        })
    };

    HttpResponseMessage message = await httpClient.SendAsync(request);
    if (!message.IsSuccessStatusCode)
    {
        await context.Response.WriteAsync("400: Bad Request");
        return;
    }

    string jsonResponse = await message.Content.ReadAsStringAsync();
    
    IHubContext<AuthHub> hubContext = context.RequestServices.GetRequiredService<IHubContext<AuthHub>>();
    await hubContext.Clients.All.SendAsync("ReceiveBearerToken", jsonResponse);

    context.Response.StatusCode = (int)HttpStatusCode.OK;
    context.Response.Redirect("https://discord.com");
});

app.Run();