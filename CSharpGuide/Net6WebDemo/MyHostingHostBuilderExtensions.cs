namespace Net6WebDemo
{
    public class MyHostingHostBuilderExtensions
    {
        public static IHostBuilder ConfigureAppConfiguration(IHostBuilder hostBuilder, Action<IConfigurationBuilder> configureDelegate)
        {
            return hostBuilder.ConfigureAppConfiguration(delegate (HostBuilderContext context, IConfigurationBuilder builder)
            {
                configureDelegate(builder);
            });
        }
    }
}
