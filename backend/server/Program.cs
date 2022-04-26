using System.Reflection;
// using GraphQL;
// using GraphQL.MicrosoftDI;
// using GraphQL.Server;
// using GraphQL.SystemTextJson;
// using IOperationMessageListener = GraphQL.Server.Transports.Subscriptions.Abstractions.IOperationMessageListener;
// using GraphQL.Types;
using Orleans;
using Orleans.Hosting;

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

            app.UseWebSockets();
            app.MapGraphQL();
            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddTransient<INPCController, DragonController>();
            services.AddSingleton<Mutation>();
            services.AddSingleton<Query>();
            services.AddSingleton<Subscription>();

            // services.AddSingleton<ISchema>(LoadSchema());
            services.AddGraphQLServer()
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .AddSubscriptionType<Subscription>()
                .AddType<IGameCharacterEvent>()
                .AddType<AttackedEvent>();

            // services.AddHostedService<DragonSpawner>();
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

        private static ISchema LoadSchema()
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream("server.schema.graphql");
            if (stream == null)
            {
                throw new NullReferenceException($"Cannot find schema among: {string.Join(',', assembly.GetManifestResourceNames())}");
            }
            using var reader = new StreamReader(stream);
            var sdl = reader.ReadToEnd();
            var schema = SchemaBuilder.New()
                .AddDocumentFromString(sdl)
                .AddQueryType<Query>()
                .AddMutationType<Mutation>()
                .AddSubscriptionType<Subscription>()
                .Create();
            return schema;
        }
    }
}

