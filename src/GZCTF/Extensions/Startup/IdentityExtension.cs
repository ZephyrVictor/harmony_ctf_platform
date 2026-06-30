using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;

namespace GZCTF.Extensions.Startup;

internal static class IdentityExtension
{
    extension(WebApplicationBuilder builder)
    {
        public void ConfigureIdentity()
        {
            builder.Services.AddDataProtection().PersistKeysToDbContext<AppDbContext>();

            builder.Services.AddAuthentication(o =>
                {
                    o.DefaultScheme = IdentityConstants.ApplicationScheme;
                    o.DefaultSignInScheme = IdentityConstants.ExternalScheme;
                })
                .AddIdentityCookies(options =>
                {
                    // Cookie names are unique to this deployment so they don't collide with
                    // other GZCTF instances on the same domain (e.g. THUCTF at thuctf.tsinghua.edu.cn/
                    // while we're at /harmonyctf/). Same-name cookies with different paths still get
                    // sent together by the browser, and relying on RFC 6265 path ordering is fragile.
                    options.ApplicationCookie?.Configure(auth =>
                    {
                        auth.Cookie.Name = "HarmonyCTF_Token";
                        auth.SlidingExpiration = true;
                        auth.ExpireTimeSpan = TimeSpan.FromDays(7);
                    });
                    options.ExternalCookie?.Configure(auth =>
                        auth.Cookie.Name = "HarmonyCTF_External");
                    options.TwoFactorUserIdCookie?.Configure(auth =>
                        auth.Cookie.Name = "HarmonyCTF_TwoFactor");
                });

            builder.Services.AddIdentityCore<UserInfo>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequireNonAlphanumeric = false;
                    options.SignIn.RequireConfirmedEmail = true;

                    // Allow all characters in username
                    options.User.AllowedUserNameCharacters = string.Empty;
                })
                .AddSignInManager<SignInManager<UserInfo>>()
                .AddUserManager<UserManager<UserInfo>>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddErrorDescriber<TranslatedIdentityErrorDescriber>()
                .AddDefaultTokenProviders();

            builder.Services.Configure<DataProtectionTokenProviderOptions>(o =>
                o.TokenLifespan = TimeSpan.FromHours(3)
            );
        }
    }
}
