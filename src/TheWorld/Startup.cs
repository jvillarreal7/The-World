using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TheWorld.Services;
using Microsoft.Extensions.Configuration;
using TheWorld.Models;
using Newtonsoft.Json.Serialization;
using AutoMapper;
using TheWorld.ViewModels;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TheWorld.wwwroot.Options;

namespace TheWorld
{
    public class Startup
    {
        private IHostingEnvironment _env;
        private IConfigurationRoot _config;
        private const string SecretKey = "somethingsomethingunsafe";
        private readonly SymmetricSecurityKey _signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SecretKey));

        public Startup(IHostingEnvironment env)
        {
            _env = env;

            var builder = new ConfigurationBuilder()
                .SetBasePath(_env.ContentRootPath)
                .AddJsonFile("config.json")
                .AddEnvironmentVariables();

            _config = builder.Build();
        }
        
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(_config);

            services.AddOptions();

            var jwtAppSettingOptions = _config.GetSection(nameof(JwtIssuerOptions));

            services.Configure<JwtIssuerOptions>(options =>
            {
                options.Issuer = jwtAppSettingOptions[nameof(JwtIssuerOptions.Issuer)];
                options.Audience = jwtAppSettingOptions[nameof(JwtIssuerOptions.Audience)];
                options.SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
            });

            if (_env.IsEnvironment("Development") || _env.IsEnvironment("Testing"))
            {
                services.AddScoped<IMailService, DebugMailService>();
            }
            else
            {

            }
            //Implement a real Mail service

            services.AddDbContext<WorldContext>();

            services.AddScoped<IWorldRepository, WorldRepository>();

            services.AddTransient<GeoCoordsService>();

            services.AddTransient<WorldContextSeedData>();

            services.AddIdentity<WorldUser, IdentityRole>(config =>
            {
                config.User.RequireUniqueEmail = true;
                config.Password.RequiredLength = 8;
                config.Cookies.ApplicationCookie.LoginPath = new PathString("/Auth/Login");
                config.Cookies.ApplicationCookie.Events = new CookieAuthenticationEvents()
                {
                    OnRedirectToLogin = async ctx =>
                    {
                        if(ctx.Request.Path.StartsWithSegments("/api") &&
                        ctx.Response.StatusCode == 200)
                        {
                            ctx.Response.StatusCode = 401;
                        }
                        else
                        {
                            ctx.Response.StatusCode = 401;
                        }
                        await Task.Yield();
                    }
                };
            })
            .AddEntityFrameworkStores<WorldContext>();

            services.AddLogging();

            services.AddMvc(config =>
            {
                if(_env.IsProduction())
                {
                    config.Filters.Add(new RequireHttpsAttribute());
                }
                //JWT
                var policy = new AuthorizationPolicyBuilder()
                                .RequireAuthenticatedUser()
                                .Build();
                config.Filters.Add(new AuthorizeFilter(policy));
            })
            .AddJsonOptions(config =>
            {
                config.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env,
            ILoggerFactory loggerFactory, WorldContextSeedData seeder)
        {
            app.UseStaticFiles();

            app.UseIdentity();

            //loggerFactory.AddConsole();

            Mapper.Initialize(config =>
            {
                config.CreateMap<TripViewModel, Trip>().ReverseMap();
                config.CreateMap<StopViewModel, Stop>().ReverseMap();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                loggerFactory.AddDebug(LogLevel.Information);
            }
            else
            {
                loggerFactory.AddDebug(LogLevel.Error);
            }

            /*app.Run(async (context) =>
            {
                await context.Response.WriteAsync("<html><body><h3>Hello World!</h3></body></html>");
            });*/

            //app.UseDefaultFiles();


            app.UseMvc(config =>
            {
                config.MapRoute(
                    name: "Default",
                    template: "{controller}/{action}/{id?}",
                    defaults: new { controller = "App", action = "Index" }
                    );
            });

            seeder.EnsureSeedData().Wait();
        }
    }
}
