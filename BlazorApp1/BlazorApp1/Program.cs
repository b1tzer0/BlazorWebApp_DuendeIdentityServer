using BlazorApp1.BFF;
using BlazorApp1.Components;
using BlazorApp1.Configuration;

using Duende.AccessTokenManagement.OpenIdConnect;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddServerComponents()
    .AddWebAssemblyComponents();

//builder.Services.AddScoped<AuthenticationStateProvider, BffAuthenticationStateProvider>();
builder.Services.AddTransient<AntiforgeryHandler>();

var baseAddressUri = builder.Configuration.GetValue<string>("BaseAddressUri")
    ?? throw new ArgumentNullException("Base Address Uri is null");

builder.Services.AddHttpContextAccessor();

builder.Services.AddBff();

builder.Services.AddHttpClient("ServerAPI", client => client.BaseAddress = new Uri(baseAddressUri))
    .AddHttpMessageHandler<AntiforgeryHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ServerAPI"));
builder.Services.AddSingleton<IUserTokenStore, CustomTokenStore>();


var oidcOptions = builder.Configuration.GetSection("Oidc").Get<OidcOptions>()
    ?? throw new Exception("Oidc options not found in configuration");

builder.Services
    .AddAuthentication(
        options =>
        {
            options.DefaultScheme = "cookie";
            options.DefaultChallengeScheme = "oidc";
            options.DefaultSignOutScheme = "oidc";
        })
    .AddCookie(
        "cookie",
        options =>
        {
            options.Cookie.Name = "__Host-BWA";
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
            options.Events.OnSigningOut = async e =>
            {
                // revoke refresh token on sign-out
                await e.HttpContext.RevokeRefreshTokenAsync();
            };
        })
    .AddOpenIdConnect(
        "oidc",
        options =>
        {
            options.Authority = oidcOptions.Authority;

            options.ClientId = oidcOptions.ClientId;
            options.ClientSecret = oidcOptions.ClientSecret;
            options.ResponseType = "code";
            options.ResponseMode = "query";

            options.Scope
                   .Clear();
            options.Scope
                   .Add("openid");
            options.Scope
                   .Add("profile");
            options.Scope
                   .Add("offline_access");

            options.MapInboundClaims = false;
            options.SaveTokens = true;
            options.GetClaimsFromUserInfoEndpoint = true;

            options.TokenValidationParameters = new()
            {
                NameClaimType = "name",
                RoleClaimType = "role"
            };

            options.Events.OnTokenValidated = async n =>
            {
                var svc = n.HttpContext.RequestServices.GetRequiredService<IUserTokenStore>();
                var exp = DateTimeOffset.UtcNow.AddSeconds(Double.Parse(n.TokenEndpointResponse.ExpiresIn));
                await svc.StoreTokenAsync(n.Principal, new()
                {
                    AccessToken = n.TokenEndpointResponse.AccessToken,
                    Expiration = exp,
                    RefreshToken = n.TokenEndpointResponse.RefreshToken
                });
            };
        });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseBff();
app.UseAuthorization();
app.MapBffManagementEndpoints();
app.MapRazorComponents<App>()
    .AddServerRenderMode()
    .AddWebAssemblyRenderMode();

app.Run();
