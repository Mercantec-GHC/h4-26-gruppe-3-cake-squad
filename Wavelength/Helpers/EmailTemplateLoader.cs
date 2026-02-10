//namespace Wavelength.Helpers
//{
//    public static class EmailTemplateLoader
//    {
//        private static readonly Dictionary<string, string> _templates;

//        static EmailTemplateLoader()
//        {
//            _templates = new Dictionary<string, string>();

//            var assembly = typeof(EmailTemplateLoader).Assembly;
//            var resourceNames = assembly.GetManifestResourceNames();

//            foreach (var resource in resourceNames)
//            {
//                if (resource.Contains(".Templates.") && resource.EndsWith(".html"))
//                {
//                    using var stream = assembly.GetManifestResourceStream(resource);
//                    using var reader = new StreamReader(stream);

//                    var content = reader.ReadToEnd();

//                    var key = Path.GetFileNameWithoutExtension(resource)
//                        .Replace("Wavelength.Templates.", string.Empty)
//                        .ToLower();

//                    _templates[key] = content;
//                }
//            }
//        }

//        public static string GetTemplate(string name)
//        {
//            if (_templates.TryGetValue(name, out var html))
//                return html;

//            throw new FileNotFoundException($"Template '{name}' blev ikke fundet.");
//        }
//    }

//}
