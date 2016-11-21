using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace ConsoleApplication
{
    public class Program
    {
        public static int Main(string[] args)
        {

            if ( args.Length > 0 )
            {
                if ( args[0] == "--selftest") 
                {
                    return SelfTest.Run();
                }
                else 
                {
                    Console.WriteLine($"Unknown argument: {args[0]}");
                    return -1;
                }
            }

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseStartup<Startup>()
                .Build();

            host.Run();


            return 0;
        }
    }

public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        #region snippet_AddSingleton
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.

        }
        #endregion

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();



            var repoTypes = Assembly
                .GetEntryAssembly()
                .GetTypes()
                .Where(t => t.GetInterfaces().Length > 0)
                .Where(t => {
                    return t.GetInterfaces().Length > 0 &&
                    t.GetInterfaces()[0].FullName.Contains('[') && 
                    t.GetInterfaces()[0].FullName.Substring(0, t.GetInterfaces()[0].FullName.IndexOf('[')) == typeof(IRepository<>).FullName; 
                });
            Console.WriteLine("ok");
            foreach ( var t in repoTypes )
            {
                
                Console.WriteLine($"{t.FullName} => {t.GetInterfaces()[0].GetGenericArguments()[0].FullName}");
            }

            app.UseMiddleware<RestApi>();
        }
    }

    public class RestApi
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public RestApi(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<RestApi>();



        }

        public async Task Invoke(HttpContext context)
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("ok");
        }
    }
    
    public class Taests 
    {
        [Test]
        public void thing() {

            IProxyGenerator proxyGenerator = new ProxyGenerator();

            IPersonRepository personRepo = proxyGenerator.CreateInterfaceProxyWithoutTarget<IPersonRepository>(new MyInterceptor<IPersonRepository>());

            var person = new Person {
                FirstName = "John",
                LastName = "Smith",
                Age = 56
            };

            string id = personRepo.Save(person);

            Assert.False(string.IsNullOrEmpty(id), "Save() must return the Id of the object");

            
        }
    }

    public class Person 
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int Age { get; set; }
    }

    public interface IRepository<T>
    {
        IEnumerable<T> GetAll();
        T Get(string id);
        string Save(T obj);
        T Delete(string id);
    }

    public class MyInterceptor<T> : IInterceptor
    {
        // public MyInterceptor(IRepository)
        public void Intercept(IInvocation invocation)
        {
            switch ( invocation.Method.Name ) {
                case "Save":
                    Console.WriteLine("SAVE");
                    invocation.ReturnValue = "ok";
                    break;
                default:
                    Console.WriteLine("unhandled");
                    break;
            }
        }
    }


    public interface IPersonRepository : IRepository<Person> {

    }
}
