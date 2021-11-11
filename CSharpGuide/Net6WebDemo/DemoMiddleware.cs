using Microsoft.AspNetCore.Http.Features;

namespace Net6WebDemo
{
    public class DemoMiddleware
    {
        private readonly RequestDelegate _next;
        private IServiceScopeFactory _scopeFactory;

        public DemoMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _next = next;
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var existingFeature = httpContext.Features.Get<IServiceProvidersFeature>();

            // All done if request services is set
            if (existingFeature?.RequestServices != null)
            {
                await _next.Invoke(httpContext);
                return;
            }

            using (var feature = new RequestServicesFeature(httpContext, _scopeFactory))
            {
                try
                {
                    httpContext.Features.Set<IServiceProvidersFeature>(feature);
                    await _next.Invoke(httpContext);
                }
                finally
                {
                    httpContext.Features.Set(existingFeature);
                }
            }
        }
    }
}
