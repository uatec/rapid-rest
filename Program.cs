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
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;

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

           services.AddRouting();
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
                })
                .Select(t => new Registration {
                    Name = t.GetInterfaces()[0].GetGenericArguments()[0].Name,
                    ResourceType = t.GetInterfaces()[0].GetGenericArguments()[0]
                });

            foreach ( var t in repoTypes )
            {
                
                Console.WriteLine($"{t.Name} => {t.ResourceType.FullName}");
            }

            var routeBuilder = new RouteBuilder(app);

            var store = new InMemoryStore();

            routeBuilder.MapGet("api/v1/{type}/{*id}", context => {
                var type = context.GetRouteValue("type");
                var id = context.GetRouteValue("id");
                if ( id == null ) {
                    var items = store.GetAll();
                    var response = JsonConvert.SerializeObject(items);
                    return context.Response.WriteAsync(response);
                }
                var item = store.Get((string) id);
                var itemResponse = JsonConvert.SerializeObject(item);
                return context.Response.WriteAsync(itemResponse);
            });  
            
            routeBuilder.MapPost("api/v1/{type}/{*id}", context =>
            {
                string type = (string) context.GetRouteValue("type");
                string id = (string) context.GetRouteValue("id");
                var serializer = new JsonSerializer();
                using ( var bodyReader = new StreamReader(context.Request.Body))
                {
                    var obj = JObject.Parse(bodyReader.ReadToEnd());
                    if ( id == null && obj["id"] != null) {
                        id = obj["id"].Value<string>();
                    }
                    if ( obj["id"] == null && id != null) {
                        obj["id"] = id;
                    }
                    if ( id == null ) {
                        id = Guid.NewGuid().ToString();
                        obj["id"] = new JValue(id);
                    }
                    store.Add(obj);
                    context.Response.Headers.Add("Location", $"api/v1/{type}/{id}");
                    context.Response.StatusCode = 201;
                    return context.Response.WriteAsync(string.Empty);
                }
            });  
            routeBuilder.MapDelete("api/v1/{type}/{id}", context =>
            {
                var type = context.GetRouteValue("type");
                var id = context.GetRouteValue("id");
                
                var item = store.Delete((string) id);
                var itemResponse = JsonConvert.SerializeObject(item);
                return context.Response.WriteAsync(itemResponse);
            });  

            var routes = routeBuilder.Build();
            app.UseRouter(routes);
            
        }
    }

    public class InMemoryStore {

        private List<JObject> _data = new List<JObject>();

        public IEnumerable<JObject> GetAll() 
        {
            return _data;
        }

        public void Add(JObject obj)
        {
            // TODO: Dedupe by Id?
            _data.Add(obj);
        }

        public JObject Get(string id)
        {
            var output = _data.Single(d => d["id"].Value<string>() == id);            
            return output;
        }

        public JObject Delete(string id)
        {
            var objectToDelete = _data.Single(d => d["id"].Value<string>() == id);
            _data.Remove(objectToDelete);
            return objectToDelete;
        }
    }
    
    public class Registration
    {
        public string Name { get; set; }

        public Type ResourceType { get; set; }
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
