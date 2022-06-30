
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Claims;
using System.Threading.Tasks;

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

            services.Configure<KestrelServerOptions>(kestrelOptions =>
            {
                kestrelOptions.ConfigureHttpsDefaults(httpOptions =>
                {
                    // TODO: Load EEBUS-compatible server cert: httpOptions.ServerCertificate = new X509Certificate2("path", "password");
                    httpOptions.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                    httpOptions.SslProtocols = SslProtocols.Tls12;
                    httpOptions.OnAuthenticate = (connectionContext, authenticationOptions) =>
                    {
                        authenticationOptions.EnabledSslProtocols = SslProtocols.Tls12;

                        if (Environment.OSVersion.Platform == PlatformID.Unix)
                        {
                            var ciphers = new List<TlsCipherSuite>()
                            {
                                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256,
                                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_CCM_8,
                                TlsCipherSuite.TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256
                            };

                            authenticationOptions.CipherSuitesPolicy = new CipherSuitesPolicy(ciphers);
                        }
                    };
                });
            });

            services.AddSingleton<CertificateValidation>();

            services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate(options =>
            {
                options.AllowedCertificateTypes = CertificateTypes.All;
                options.Events = new CertificateAuthenticationEvents
                {
                    OnCertificateValidated = context =>
                    {
                        CertificateValidation validationService = context.HttpContext.RequestServices.GetService<CertificateValidation>();

                        if (validationService.ValidateCertificate(context.ClientCertificate))
                        {
                            var claims = new[]
                            {
                                new Claim(
                                    ClaimTypes.NameIdentifier,
                                    context.ClientCertificate.Subject,
                                    ClaimValueTypes.String, context.Options.ClaimsIssuer),

                                new Claim(
                                    ClaimTypes.Name,
                                    context.ClientCertificate.Subject,
                                    ClaimValueTypes.String, context.Options.ClaimsIssuer)
                            };

                            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));

                            context.Success();
                        }
                        else
                        {
                            context.Fail("Invalid EEBUS certificate!");
                        }

                        return Task.CompletedTask;
                    }
                };
            });

            services.AddAuthorization();

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
                KeepAliveInterval = TimeSpan.FromSeconds(50),
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
