using System.Reflection;
using GraphQL;
using GraphQL.MicrosoftDI;
using GraphQL.Server;
using GraphQL.SystemTextJson;
using GraphQL.Types;
using Orleans;
using Orleans.Hosting;
using IOperationMessageListener = GraphQL.Server.Transports.Subscriptions.Abstractions.IOperationMessageListener;

namespace DragonAttack
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            ConfigureOrleans(builder);
            ConfigureServices(builder.Services);

            var app = builder.Build();
            app.UseCors(builder =>
            {
                builder.AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });

            app.MapGet("/", () => "Hello World!");
            app.UseWebSockets();
            app.UseGraphQLWebSockets<ISchema>();
            app.UseGraphQL<ISchema>();
            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddSingleton<MutationResolvers>();
            services.AddSingleton<QueryResolvers>();
            services.AddSingleton<WatchCharacterResolver>();
            services.AddSingleton<IOperationMessageListener, PlayerContextListener>();

            services.AddSingleton<ISchema>(LoadSchema);
            services.AddGraphQL(builder => builder
                // .AddServer(true)
                .AddHttpMiddleware<ISchema>()
                .AddWebSocketsHttpMiddleware<ISchema>()
                .ConfigureExecutionOptions(options =>
                {
                    options.EnableMetrics = true;
                    var logger = options.RequestServices!.GetRequiredService<ILogger<Program>>();
                    options.UnhandledExceptionDelegate = ctx =>
                    {
                        logger.LogError("{Error} occurred", ctx.OriginalException.Message);
                        return Task.CompletedTask;
                    };
                })
                .AddSystemTextJson()
                .AddErrorInfoProvider(opt => opt.ExposeExceptionStackTrace = true)
                .AddWebSockets()
            );

            services.AddHostedService<DragonSpawner>();
        }

        private static void ConfigureOrleans(WebApplicationBuilder builder)
        {
            builder.Host.UseOrleans(siloBuilder =>
            {
                siloBuilder.UseLocalhostClustering();
                siloBuilder.AddSimpleMessageStreamProvider("default");
                siloBuilder.AddMemoryGrainStorage("PubSubStore");
            });
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
                builder.Types.Include<WatchCharacterResolver>();

                services.GetRequiredService<WatchCharacterResolver>().ConfigureField(builder.Types);

                builder.Types.For("AttackedEvent").IsTypeOf<AttackedEvent>();
                builder.Types.For("GameCharacterEvent").IsTypeOf<IGameCharacterEvent>();

                builder.Types.For(nameof(CharacterEnteredAreaEvent)).IsTypeOf<CharacterEnteredAreaEvent>();
                builder.Types.For("AreaEvent").IsTypeOf<IAreaEvent>();
            });
            
            return schema;
        }
    }
}

