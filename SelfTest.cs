using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace ConsoleApplication
{
    public class Test
    {
        public MethodInfo Method { get; set; }
        public Type Type { get; set; }
        public string Name { 
            get {
                return $"{this.Type.Name} › {this.Method.Name}";
            }
        }

        public outcome Outcome { get; set; }
    }

    public class TestAttribute : Attribute
    {

    }

    public static class Assert 
    {
        public static void IsNull(object instance, string message)
        {
            if ( instance != null ) 
            {
                throw new Exception("instance should have been null");
            }
        }
        public static void IsType<T>(object instance, string message)
        {
            if ( !(instance is T) )
            {
                throw new Exception($"{message}: Expected: {typeof(T).Name} Got: {instance.GetType().Name}");
            }
        }

        public static void Contains<T>(IEnumerable<T> collection, T element, string message)
        {
            if ( collection == null ) throw new Exception($"{message}: Collection cannot be null");
            if ( element == null ) throw new Exception($"{message}: Element cannot be null");

            if ( !collection.Any(e => element.Equals(e))) 
            {
                throw new Exception($"Expected to find element {element.ToString()} in collection but did not.");
            }
        }

        public static void Is<T>(T actual, T expected, string name)
        {
            if ( !expected.Equals(actual) ) 
            {
                throw new Exception($"{name}: Expected: `{expected}` Got: `{actual}`");
            }
        }

        public static void Within(int timeout, Action assertions)
        {
            DateTime endTime = DateTime.Now.AddMilliseconds(timeout);
            Exception lastException = null;
            while ( DateTime.Now < endTime ) {
                try 
                {
                    assertions();
                    return;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                }
                
                Thread.Sleep(100);
            }

            throw new TimeoutException($"Assertions did not pass within {timeout}ms: {lastException.Message}", lastException);
        }

        public static void True(bool value, string name)
        {
            if ( !value ) 
            {
                throw new Exception($"{name}: Expected: `true` Got: `false`");
            }
        }
        
        public static void False(bool value, string name)
        {
            if ( value ) 
            {
                throw new Exception($"{name}: Expected: `false` Got: `true`");
            }
        }

        public static void Throws<T>(Action action, string exceptionMessage, string name)
        {
            try 
            {
                action();
            }
            catch ( Exception ex )
            {
                if ( ex.GetType() == typeof(T) && 
                    ex.Message.Equals(exceptionMessage) )
                {
                    return;
                }
            }
            
            throw new Exception("Expected exception to be thrown");
        }
    }

    public class testsuites : List<testsuite>
    {}

    public class testsuite : List<testcase> {
        public string name { get; set; }
    }

    public class testcase : List<outcome> {
        public string name { get; set; }
    }

    public class success : outcome {}
    public class failure : outcome {
        public string Message { get; private set; }
        public string StackTrace { get; private set; }
        public failure(string message, string stackTrace)
        {
            this.Message = message;
            this.StackTrace = stackTrace;
        }
    }
    public abstract class outcome {}


    public static class SelfTest 
    {
        private static void ExecuteTest(Test test)
        {
            try 
            {
                object instance = Activator.CreateInstance(test.Type);
                test.Method.Invoke(instance, new object[]{});
                test.Outcome = new success();
            }
            catch (Exception ex)
            {
                test.Outcome = new failure(ex.InnerException.Message, ex.InnerException.StackTrace);
            }
        }

        public static int Run() {

            Stopwatch overall = Stopwatch.StartNew();
            
            var tests = FindTests().ToList();
            bool parallel = false;
            if ( parallel ) 
            {
                Parallel.ForEach(tests, ExecuteTest);
            } 
            else 
            {
                foreach ( var x in tests) {
                    ExecuteTest(x);
                }
            }

            overall.Stop();

            int total = tests.Count();     
            Console.WriteLine($"{total} tests run in {overall.ElapsedMilliseconds}ms");

            print(tests);

            int failed = tests.Count(o => o.Outcome is failure);
            return failed;
        }

        private static void print(IEnumerable<Test> tests)
        {
            int total = tests.Count();
            
            Console.WriteLine($"Discovered {total} tests");
            Console.ForegroundColor = ConsoleColor.Gray;
            foreach ( var test in tests) {
                bool isSuccess = test.Outcome is failure;
                if ( test.Outcome is failure ) {
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine($"{test.Name}");
                    Console.ForegroundColor = ConsoleColor.Gray;
                    var failure = (failure) test.Outcome;
                    Console.WriteLine($"\t{failure.Message}");
                    Console.WriteLine($"\t{failure.StackTrace}");
                } else if (test.Outcome is success) {
                    
                } else {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"{test.Name} inconclusive");
                }
            };


            int passed = tests.Count(o => o.Outcome is success);
            int failed = tests.Count(o => o.Outcome is failure);
            
            if ( failed > 0 ) {
                Console.ForegroundColor = ConsoleColor.DarkRed;
            } else {
                Console.ForegroundColor = ConsoleColor.DarkGreen;
            }            

            Console.WriteLine($"Passed: {passed}/{total}");
            if ( failed > 0 ) Console.WriteLine($"Failed: {failed}/{total}");
            
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private static IEnumerable<Test> FindTests() 
        {
            return Assembly.GetEntryAssembly()
                .GetTypes()
                .Select(t => {
                    return new {
                        Type = t,
                        TestMethods = t.GetMethods().Where(m => m.GetCustomAttribute(typeof(TestAttribute)) != null)
                    };
                })
                .SelectMany(t => t.TestMethods.Select(tm => new Test {
                    Type = t.Type,
                    Method = tm
                }));
        }
    }
}