using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace FModel
{
    class Program
    {
        [STAThreadAttribute]
        public static void Main()
        {
            Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>();
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            IEnumerable<string> resources = executingAssembly.GetManifestResourceNames().Where(n => n.EndsWith(".dll"));

            foreach (string resource in resources)
            {
                using (Stream stream = executingAssembly.GetManifestResourceStream(resource))
                {
                    if (stream == null) { continue; }

                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    try
                    {
                        assemblies.Add(resource, Assembly.Load(bytes));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Print(string.Format("Failed to load: {0}, Exception: {1}", resource, ex.Message));
                    }
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                AssemblyName assemblyName = new AssemblyName(e.Name);
                string path = string.Format("{0}.dll", assemblyName.Name);

                if (assemblies.ContainsKey(path))
                {
                    return assemblies[path];
                }

                return null;
            };
            App.Main();
        }
    }
}
