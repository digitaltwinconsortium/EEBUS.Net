
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace EEBUS
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        private bool ValidateClientCert(X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // auto accept mode is active, register flag is set in discovery service
            return true;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllersWithViews();

            services.Configure<KestrelServerOptions>(kestrelOptions =>
            {
                kestrelOptions.ConfigureHttpsDefaults(httpOptions =>
                {
                    httpOptions.ServerCertificate = CertificateGenerator.GenerateCert(Dns.GetHostName());
                    httpOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    httpOptions.ClientCertificateValidation = ValidateClientCert;
                    httpOptions.SslProtocols = SslProtocols.Tls12;
                    httpOptions.OnAuthenticate = (connectionContext, authenticationOptions) =>
                    {
                        authenticationOptions.EnabledSslProtocols = SslProtocols.Tls12;
                    };
                });
            });

            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate(options =>
            {
                options.AllowedCertificateTypes = CertificateTypes.All;
            });

            services.AddAuthorization();

            services.AddSingleton<MDNSClient>();
            services.AddSingleton<MDNSService>();
            services.AddSingleton<SPINE>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, MDNSClient mDNSClient, MDNSService mDNSService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

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
                KeepAliveInterval = TimeSpan.FromSeconds(50)
            };

            app.UseWebSockets(webSocketOptions);

            app.UseMiddleware<SHIPMiddleware>();

            // configure our EEBUS mDNS properties
            mDNSService.AddProperty("id", "ID:MICROSOFT-Azure-EEBUS-Gateway-100;");
            mDNSService.AddProperty("path", "/ship/");
            mDNSService.AddProperty("register", "true");

            // start our mDNS services
            mDNSClient.Run();
            mDNSService.Run();
        }
    }
}
