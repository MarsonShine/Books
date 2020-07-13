namespace DynamicGenerateCode {
    public class Templates {
        public static object CreateNewExtensionTemplate() {
            return new DynamicGenerateCode.Extension();
        }

        public static bool CallMethodTemplate(object ExtensionObj, string argument) {
            var extension = (DynamicGenerateCode.Extension) ExtensionObj;
            return extension.DoWork(argument);
        }
    }
}