using System;
using BaGet.Core;
using BaGet.Web;
using Microsoft.Extensions.DependencyInjection;

namespace BaGet
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddBaGetWebApplication(
            this IServiceCollection services,
            Action<BaGetApplication> configureAction)
        {
            // Configure routing and MVC options  
            services
                .AddRouting(options => options.LowercaseUrls = true)
                .AddControllers()
                .AddApplicationPart(typeof(PackageContentController).Assembly)
                .AddJsonOptions(options =>
                {
                    // Updated to use DefaultIgnoreCondition  
                    options.JsonSerializerOptions.DefaultIgnoreCondition =
                        System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                });

            services.AddRazorPages();
            services.AddHttpContextAccessor();
            services.AddTransient<IUrlGenerator, BaGetUrlGenerator>();

            // Adding the BaGet application services  
            services.AddBaGetApplication(configureAction);

            return services;
        }
    }
}
