using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elasticsearch.Net;
using Elasticsearch.Net.Aws;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Formatting.Json;
using Serilog.Sinks.Elasticsearch;

namespace ActionLogAttribute
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment environment)
        {
            Configuration = configuration;

            Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", configuration.GetSection("AWS:AccessKey").Value);
            Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", configuration.GetSection("AWS:SecretKey").Value);
            Environment.SetEnvironmentVariable("AWS_REGION", configuration.GetSection("AWS:Region").Value);

            Log.Logger = new LoggerConfiguration()
            .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(configuration.GetSection("AWS:ElasticUrl").Value))
            {
                ModifyConnectionSettings = conn =>
                {
                    var httpConnection = new AwsHttpConnection(configuration.GetSection("AWS:Region").Value);
                    var pool = new SingleNodeConnectionPool(new Uri(configuration.GetSection("AWS:ElasticUrl").Value));
                    var conf = new ConnectionConfiguration(pool, httpConnection);
                    return conf;
                },
                IndexFormat = "emrelog-{0:yyyy.MM}",
            })
            .CreateLogger();
        }


        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });


            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
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
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
