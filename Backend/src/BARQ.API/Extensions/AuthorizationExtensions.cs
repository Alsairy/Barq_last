using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BARQ.API.Extensions
{
    public static class AuthorizationExtensions
    {
        /// <summary>
        /// Configure a default fallback policy that requires authenticated users
        /// unless an endpoint explicitly allows anonymous access.
        /// </summary>
        public static IServiceCollection AddDefaultAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
            return services;
        }
    }
}
