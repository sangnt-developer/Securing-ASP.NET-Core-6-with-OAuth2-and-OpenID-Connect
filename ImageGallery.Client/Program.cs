using ImageGallery.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Net.Http.Headers;
using System.IdentityModel.Tokens.Jwt;
using static System.Net.WebRequestMethods;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(configure => 
        configure.JsonSerializerOptions.PropertyNamingPolicy = null);

// Make sure keeping the orginal claim types.
// Not transform it to other different claim types based on WS-Security (Microsoft).
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();


// Add required services for require access token.
builder.Services.AddAccessTokenManagement();

// create an HttpClient used for accessing the API
builder.Services.AddHttpClient("APIClient", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ImageGalleryAPIRoot"]);
    client.DefaultRequestHeaders.Clear();
    client.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");
})
// Add handler to attach bearer token to outgoing requests to API.
.AddUserAccessTokenHandler();

builder.Services.AddHttpClient("IDPClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:44300/");
});

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.AccessDeniedPath = new PathString("/Authentication/AccessDenied");
})
.AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
{
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.Authority = "https://localhost:44300";
    options.ClientId = "imagegalleryclient";
    options.ClientSecret = "secret";
    options.ResponseType = "code";

    // these scopes is requested by middleware
    //options.Scope.Add("openid");
    //options.Scope.Add("profile");

    // these "signin-oidc" callbackPath is default path of middleware
    //options.CallbackPath = new PathString("signin-oidc");

    // SignedOutCallbackPath: default = host:port/signout-callback-oidc
    // To change, set SignedOutCallbackPath
    // eg: SignoutCallbackPath = "pathaftersignout"

    options.GetClaimsFromUserInfoEndpoint = true;
    options.SaveTokens = true;

    // Remove claim actiosn on "aud" claim type
    options.ClaimActions.Remove("aud");

    // Delete claims types
    options.ClaimActions.DeleteClaim("sid");
    options.ClaimActions.DeleteClaim("idp");

    options.Scope.Add("roles");
    //options.Scope.Add("imagegalleryapi.fullaccess");
    options.Scope.Add("imagegalleryapi.read");
    options.Scope.Add("imagegalleryapi.write");
    options.Scope.Add("country");
    options.Scope.Add("offline_access");

    options.ClaimActions.MapJsonKey("role", "role");
    options.ClaimActions.MapUniqueJsonKey("country", "country");
    options.TokenValidationParameters = new ()
    {
        NameClaimType = "given_name",
        RoleClaimType = "role"
    };
});

builder.Services.AddAuthorization(authorizationOptions =>
{
    authorizationOptions.AddPolicy("UserCanAddImage", AuthorizationPolicies.CanAddImage());
});

var app = builder.Build();

// ure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Gallery}/{action=Index}/{id?}");

app.Run();
