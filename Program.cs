using System;
using System.Collections.Generic;
using Castle.DynamicProxy;

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

            Console.WriteLine("Hello World!");

            

            return 0;
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
