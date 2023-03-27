using Duende.IdentityServer;
using Marvin.IDP.DbContexts;
using Marvin.IDP.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Marvin.IDP;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configures IIS our-of-proc settings
        builder.Services.Configure<IISOptions>(options =>
        {
            options.AuthenticationDisplayName = "Windows";
            options.AutomaticAuthentication = false;
        });
        // ..or configures IIS in-proc settings
        builder.Services.Configure<IISServerOptions>(options => {
            options.AuthenticationDisplayName = "Windows";
            options.AutomaticAuthentication = false;
        });

        // uncomment if you want to add a UI
        builder.Services.AddRazorPages();

        builder.Services.AddScoped<IPasswordHasher<Entities.User>, PasswordHasher<Entities.User>>();
        builder.Services.AddScoped<ILocalUserService, LocalUserService>();

        builder.Services.AddDbContext<IdentityDbContext>(options =>
        {
            options.UseSqlite(builder.Configuration.GetConnectionString("MarvinIdentityDBConnectionString"));
        });

        builder.Services.AddIdentityServer(options =>
        {
            // https://docs.duendesoftware.com/identityserver/v6/fundamentals/resources/api_scopes#authorization-based-on-scopes
            options.EmitStaticAudienceClaim = true;
        })
        .AddProfileService<LocalUserProfileService>()
        .AddInMemoryIdentityResources(Config.IdentityResources)
        .AddInMemoryApiScopes(Config.ApiScopes)
        .AddInMemoryApiResources(Config.ApiResources)
        .AddInMemoryClients(Config.Clients);

        builder.Services
            .AddAuthentication()
            .AddOpenIdConnect("AAD", "Azure Active Directory", options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.Authority = "https://login.microsoftonline.com/6d0f1164-b591-4a96-8cf2-b478ed138977/v2.0";
                options.ClientId = "5bfd2164-152f-4037-8441-c8fa80c3d93b";
                options.ClientSecret = ".WK8Q~n89yhVxRxD~Ezu9OKJtB6y4Bd.Ubl4vbs2";
                options.ResponseType = "code";
                options.CallbackPath = new PathString("/signin-aad/");
                options.SignedOutCallbackPath = new PathString("/signout-aad/");
                options.Scope.Add("email");
                options.Scope.Add("offline_access");
                options.SaveTokens = true;
            });

        builder.Services
            .AddAuthentication()
            .AddFacebook("Facebook", options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
                options.AppId = "240468823883776";
                options.AppSecret = "8c3ebcb40632c1837355c701bd03f462";
            });

        return builder.Build();
    }
    
    public static WebApplication ConfigurePipeline(this WebApplication app)
    { 
        app.UseSerilogRequestLogging();
    
        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        // uncomment if you want to add a UI
        app.UseStaticFiles();
        app.UseRouting();

        app.UseIdentityServer();

        // uncomment if you want to add a UI
        app.UseAuthorization();
        app.MapRazorPages().RequireAuthorization();

        return app;
    }
}
