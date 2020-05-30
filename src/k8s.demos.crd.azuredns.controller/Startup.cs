using k8s.demos.crd.azuredns.controller.Controllers;
using k8s.demos.crd.azuredns.controller.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Collections.Concurrent;
using static k8s.demos.crd.azuredns.controller.Models.AzureDnsCredential;

namespace k8s.demos.crd.azuredns.controller
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            KubernetesClientConfiguration config;
            if (KubernetesClientConfiguration.IsInCluster())
            {
                config = KubernetesClientConfiguration.InClusterConfig();
                services.AddSingleton<ILeaderSelector, KubernetesLeaderSelector>();
            }
            else
            {
                config = new KubernetesClientConfiguration { Host = "http://localhost:8001" };
                services.AddSingleton<ILeaderSelector, DummyLeaderSelector>();
            }

            services.AddHttpClient("K8s")
                    .AddTypedClient<IKubernetes>((httpClient, serviceProvider) => new Kubernetes(config, httpClient))
                    .ConfigurePrimaryHttpMessageHandler(config.CreateDefaultHttpClientHandler)
                    .AddHttpMessageHandler(KubernetesClientConfiguration.CreateWatchHandler);

            services.AddSingleton<ConcurrentDictionary<string, AzureDnsCredentialSpec>>();
            services.AddSingleton<IDnsClientFactory, AzureDnsClientFactory>();

            services.AddHostedService<AzureDnsCredentialsController>();
            services.AddHostedService<DnsRecordsetController>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}
