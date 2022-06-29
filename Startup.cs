
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;

namespace EEBUS
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
            services.AddControllersWithViews();

            services.AddSingleton<MDNSClient>();
            services.AddSingleton<MDNSService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, MDNSClient mDNSClient, MDNSService mDNSService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });

            var webSocketOptions = new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(1)
            };

            app.UseWebSockets(webSocketOptions);

            app.UseMiddleware<WebsocketJsonMiddleware>();

            // configure our EEBUS mDNS properties
            mDNSService.AddProperty("id", Guid.NewGuid().ToString());
            mDNSService.AddProperty("path", "/ship/");
            mDNSService.AddProperty("ski", Guid.NewGuid().ToString());
            mDNSService.AddProperty("register", "true");

            // start our mDNS services
            mDNSClient.Run();
            mDNSService.Run();
        }
    }
}
