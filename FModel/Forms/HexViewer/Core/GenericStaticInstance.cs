//////////////////////////////////////////////
// Apache 2.0  - 2018
// Author : Janus Tida
//////////////////////////////////////////////

namespace WpfHexaEditor.Core
{
    /// <summary>
    /// This class is designed to simplify getting the instance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class GenericStaticInstance<T> where T : class, new()
    {
        private static T _staticInstance;
        public static T StaticInstance => _staticInstance ?? (_staticInstance = new T());
    }
}
