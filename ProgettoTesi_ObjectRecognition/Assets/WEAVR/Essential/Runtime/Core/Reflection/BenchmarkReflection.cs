//#define BENCHMARK
#if BENCHMARK
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System;
using System.Reflection;
using BenchmarkDotNet.Diagnostics.Windows;
using FastMember;
using Sigil;

namespace ReflectionBenchmarks
{
    [Config(typeof(Config))]
    public class Program
    {
        private class Config : ManualConfig
        {
            public Config() {
                Add(new MemoryDiagnoser());
            }
        }

        private static BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static string propertyName = "Host";

        private static TestUri testUri;
        private static Object @object;
        private static Type @class;
        private static PropertyInfo property;
        private static FastProperty fastProperty;
        private static TypeAccessor accessor; // FastMember
        private static Func<TestUri, string> getter;
        private static Action<TestUri, string> setter;

        public static Func<TestUri, string> getDelegate;
        public static Action<TestUri, string> setDelegate;
        public static Delegate getDelegateDynamic, setDelegateDynamic;

        //private static bool allowNonPublicFieldAccess = false;
        private static bool allowNonPublicFieldAccess = true;

        static Program() {
            testUri = new TestUri("SomeHost");
            @object = testUri;
            @class = testUri.GetType();
            property = @class.GetProperty(propertyName, bindingFlags);
            fastProperty = new FastProperty(property, createGet: true, createSet: true, nonPublic: allowNonPublicFieldAccess);

            // Using FastMember - https://github.com/mgravell/fast-member
            accessor = TypeAccessor.Create(@class, allowNonPublicAccessors: allowNonPublicFieldAccess);

            var funcType = Type.GetType("System.Func`2[ReflectionBenchmarks.Program+TestUri, System.String]");
            getDelegate = (Func<TestUri, string>)Delegate.CreateDelegate(funcType, property.GetGetMethod(nonPublic: allowNonPublicFieldAccess));
            getDelegateDynamic = Delegate.CreateDelegate(funcType, property.GetGetMethod(nonPublic: allowNonPublicFieldAccess));

            var actionType = Type.GetType("System.Action`2[ReflectionBenchmarks.Program+TestUri, System.String]");
            setDelegate = (Action<TestUri, string>)Delegate.CreateDelegate(actionType, property.GetSetMethod(nonPublic: allowNonPublicFieldAccess));
            setDelegateDynamic = Delegate.CreateDelegate(actionType, property.GetSetMethod(nonPublic: allowNonPublicFieldAccess));

            var setterEmiter = Emit<Action<TestUri, string>>
                .NewDynamicMethod("SetTestUriProperty")
                .LoadArgument(0)
                .LoadArgument(1)
                .Call(property.GetSetMethod(nonPublic: allowNonPublicFieldAccess))
                .Return();
            setter = setterEmiter.CreateDelegate();

            var getterEmiter = Emit<Func<TestUri, string>>
                .NewDynamicMethod("GetTestUriProperty")
                .LoadArgument(0)
                .Call(property.GetGetMethod(nonPublic: allowNonPublicFieldAccess))
                .Return();
            getter = getterEmiter.CreateDelegate();
        }

        static void Main(string[] args) {
            var summary = BenchmarkRunner.Run<Program>();
        }

        [Benchmark(Baseline = true)]
        public string GetViaProperty() {
            return testUri.PublicHost;
        }

        [Benchmark]
        public string GetViaDelegate() {
            return getDelegate(testUri);
        }

        [Benchmark]
        public string GetViaILEmit() {
            return getter(testUri);
        }

        [Benchmark]
        public string GetViaCompiledExpressionTrees() {
            return (string)fastProperty.Get(testUri);
        }

        [Benchmark]
        public string GetViaFastMember() {
            return (string)accessor[testUri, "PublicHost"];
        }

        [Benchmark]
        public string GetViaReflectionWithCaching() {
            return (string)property.GetValue(testUri, null);
        }

        [Benchmark]
        public string GetViaReflection() {
            Type @class = testUri.GetType();
            PropertyInfo property = @class.GetProperty(propertyName, bindingFlags);
            return (string)property.GetValue(testUri, null);
        }

        [Benchmark]
        public string GetViaDelegateDynamicInvoke() {
            return (string)getDelegateDynamic.DynamicInvoke(testUri);
        }

        [Benchmark]
        public void SetViaProperty() {
            testUri.PublicHost = "Testing";
        }

        [Benchmark]
        public void SetViaDelegate() {
            setDelegate(testUri, "Testing");
        }

        [Benchmark]
        public void SetViaILEmit() {
            setter(testUri, "Testing");
        }

        [Benchmark]
        public void SetViaCompiledExpressionTrees() {
            fastProperty.Set(testUri, "Testing");
        }

        [Benchmark]
        public void SetViaFastMember() {
            accessor[testUri, "PublicHost"] = "Testing";
        }

        [Benchmark]
        public void SetViaReflectionWithCaching() {
            property.SetValue(testUri, "Testing", null);
        }

        [Benchmark]
        public void SetViaReflection() {
            Type @class = testUri.GetType();
            PropertyInfo property = @class.GetProperty(propertyName, bindingFlags);
            property.SetValue(testUri, "Testing", null);
        }

        [Benchmark]
        public void SetViaDelegateDynamicInvoke() {
            setDelegateDynamic.DynamicInvoke(testUri, "Testing");
        }

        public class TestUri
        {
            public TestUri(Uri uri) {
                Host = uri.Host;
            }

            public TestUri(string host) {
                Host = host;
            }

            private string host;
            private string Host {
                get { return host; }
                set { host = value; }
            }

            public string PublicHost {
                get { return host; }
                set { host = value; }
            }
        }
    }
}
#endif