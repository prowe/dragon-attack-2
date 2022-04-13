using System.Reflection;
using GraphQL;
using GraphQL.Server;
using GraphQL.Types;

namespace DragonAttack
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureServices(builder.Services);

            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");
            app.UseGraphQL<ISchema>();
            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<CounterHolder>();
            services.AddSingleton<MutationResolvers>();
            services.AddSingleton<QueryResolvers>();

            services.AddSingleton<ISchema>(LoadSchema);
            GraphQL.MicrosoftDI.GraphQLBuilderExtensions.AddGraphQL(services)
                .AddServer(true)
                .ConfigureExecution(options =>
                {
                    options.EnableMetrics = true;
                    var logger = options.RequestServices!.GetRequiredService<ILogger<Program>>();
                    options.UnhandledExceptionDelegate = ctx => logger.LogError("{Error} occurred", ctx.OriginalException.Message);
                })
                .AddSystemTextJson()
                .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = true);
        }

        private static Schema LoadSchema(IServiceProvider services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("server.schema.graphql");
            if (stream == null)
            {
                throw new NullReferenceException($"Cannot find schema among: {string.Join(',', assembly.GetManifestResourceNames())}");
            }
            using var reader = new StreamReader(stream);
            var sdl = reader.ReadToEnd();
            var schema = Schema.For(sdl, builder =>
            {
                builder.ServiceProvider = services;
                builder.Types.Include<MutationResolvers>();
                builder.Types.Include<QueryResolvers>();
            });
            return schema;
        }
    }
}

