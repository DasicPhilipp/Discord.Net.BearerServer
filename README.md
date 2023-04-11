# Introduction
This is a simple ASP.NET server to get bearer tokens of users when they join your server using your bot.

If you don't have a website or something else, but you need to receive a bearer token, deploying of this plain server will help you.

# Step by step instruction
`appsettings.json`
```json
{
  "Messages": {
    "CodeIsMissing": "400: Bad Request (the code is missing)", // the response if there is no "code" key in the query
    "OnFailingRequest": "400: Bad Request (Discord has denied the code or your content is wrong)"
  },
  "Uris": {
    "ApplicationUrl": "http://localhost:5000", // write here the IP of your sever and a port if you wish to
    "AuthHabRoute": "/bearer/discord", // SignalR endpoint
    "CodePageRoute": "/connections/discord", // the endpoint for codes
    "RedirectLocationOnSuccess": "https://discord.com" // redirects the user to this URI on success
  }
}
```

## Step 1
Follow [Discord Application OAuth2](https://discord.com/developers/applications/) and set up a proper URI of the server to listen incoming codes.
The default server `appconfig.json` uses `http://localhost:5000/connections/discord`. Write your IP instead of `localhost` and a port if you want to.

Also copy **Client ID** and **Client Secret**, you will need them later.
![image](https://user-images.githubusercontent.com/66508993/231248143-d5ab7766-2e1b-4e2c-b93e-cb934e3c556b.png)

## Step 2
Follow "URL Generator" menu and select the scopes you need. Select the redirect URL in the dropdown lower. Copy the generated URL and give it to users.
The users should use it to authorize themselves. As a result, a temporary code will be generated and passed into your query. It looks like
`http://localhost:5000/connections/discord?code=`. After receiving the code, the server exchanges it for the user's acces token by making a `POST` request.

If your really want to, you can read more in [Discord Developer Portal](https://discord.com/developers/docs/topics/oauth2)
![image](https://user-images.githubusercontent.com/66508993/231252305-daa1f86f-4c6c-4f1c-8e5e-45aaaabb5e58.png)

## Step 3
Configure the server. It throws an exception if you haven't installed **Client ID** and **Client Secret**. Launch the server once and follow the path:
Windsows: `C:\ProgramData\BearerServer\secrets.json`
Linux: `user\share\BearerServer\secrets.json`

Then past your data in the json file:
```json
{
  "ClientId": "client_id",
  "ClientSecret": "client_secret"
}
```

## Step 4
If everything if fine and the code has been exchanged, then the user is redirected to the `"RedirectLocationOnSuccess"` ("https://discord.com" by default, you can replace
it to your invite link or something else) set in `appconfig.json`. Meanwhile, the server sends the access token and some extra data using SignalR.
To get it in your bot application, you have to write a simple SignalR client ([install the package with NuGet])(https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client):

```cs
using Microsoft.AspNetCore.SignalR.Client;
//...

HubConnection connection = new HubConnectionBuilder()
    .WithUrl("http://localhost:5000/bearer/discord") // "/bearer/discord" is "AuthHubRoute" property in appsettings.json
    .WithAutomaticReconnect()
    .Build();

connection.On<string>("ReceiveBearerToken", json => // "ReceiverBearerToken" is a hardcoded server value, you cannot change it
{
    Console.WriteLine(json);
});

await connection.StartAsync();
```

Once the server will receive the user's access token, it will send it to your client as json:
```json
{
  "access_token": "token",
  "token_type": "Bearer",
  "expires_in": 604800,
  "refresh_token": "refresh_token",
  "scope": "identify"
}
```
The token is valid for one week. Then you have to refresh it, sending a `POST` request to the `https://discord.com/api/oauth2/token` with the following parameters:

`client_id` - your application's client id  
`client_secret` - your application's client secret  
`grant_type` - must be set to refresh_token  
`refresh_token` - the user's refresh token

## Step 5
Finally, when you received all the data you needed, you can do some actions you have set in [Scopes](https://discord.com/developers/docs/topics/oauth2#shared-resources).
For example, you can get the user's connections or invite the users into your guild.
