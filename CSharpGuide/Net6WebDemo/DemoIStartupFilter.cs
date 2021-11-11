namespace Net6WebDemo
{
    public class DemoIStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return builder =>
            {
                builder.UseMiddleware<DemoMiddleware>();
                next(builder);
            };
        }
    }
}
