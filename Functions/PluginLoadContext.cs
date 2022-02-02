using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;

namespace GraphLC_IDE.Functions
{
    public class PluginLoadContext<T> : AssemblyLoadContext
    {
        private AssemblyDependencyResolver _resolver;
        public PluginLoadContext(string pluginPath, bool isCollectible) : base(isCollectible)
        {
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }
        //加载依赖项
        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }
            return null;
        }
        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }
            return IntPtr.Zero;
        }

        public static List<T> CreateCommands(string[] pluginPaths)
        {
            List<Assembly> _assemblies = new List<Assembly>();
            foreach (var pluginPath in pluginPaths)
            {
                string pluginLocation = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pluginPath.Replace('\\', Path.DirectorySeparatorChar)));
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(o => o.Location == pluginLocation);
                //根据程序集的物理位置判断当前域中是否存在该类库，如果不存在就读取，如果存在就从当前程序域中读取，由于AssemblyLoadContext已经做了相应的上下文隔离
                //，所以即便是名称一样位置一样也可以重复加载，执行也可以按照预期执行，但由于会重复加载程序集，就会造成内存一直增加导致内存泄漏
                if (assembly == null)
                {
                    PluginLoadContext<T> pluginLoadContext = new PluginLoadContext<T>(pluginLocation, true);
                    assembly = pluginLoadContext.LoadFromAssemblyName(new AssemblyName(Path.GetFileNameWithoutExtension(pluginLocation)));
                }
                _assemblies.Add(assembly);
            }
            var results = new List<T>();
            foreach (var assembly in _assemblies)
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (typeof(T).IsAssignableFrom(type))
                    {
                        T result = (T)Activator.CreateInstance(type);
                        if (result != null)
                        {
                            results.Add(result);
                        }
                    }
                }
            }
            return results;
        }
    }
}
