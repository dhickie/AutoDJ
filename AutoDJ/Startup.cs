using AutoDJ.Middleware;
using AutoDJ.Options;
using AutoDJ.Providers;
using AutoDJ.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutoDJ
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Options
            services.Configure<SpotifyOptions>(Configuration.GetSection("Spotify"));
            services.Configure<AppOptions>(Configuration.GetSection("App"));

            // Providers
            services.AddSingleton<IModeProvider, ModeProvider>();

            // Services
            services.AddSingleton<IHttpClient, HttpClientWrapper>();
            services.AddSingleton<ISpotifyService, SpotifyService>();
            services.AddSingleton<IPersistenceService, PersistenceService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseAuthenticationMiddleware();
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
