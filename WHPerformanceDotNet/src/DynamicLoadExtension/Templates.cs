namespace DynamicLoadExtension
{
    /// <summary>
    /// 模板文件，用来使用 IL Generation
    /// </summary>
    public class Templates
    {
        public static object CreateNewExtensionTemplate()
        {
            return new Extension();
        }

        public static bool CallMethodTemplate(object ExtensionObj, string argument)
        {
            var extension = (Extension)ExtensionObj;
            return extension.DoWork(argument);
        }
    }
}
