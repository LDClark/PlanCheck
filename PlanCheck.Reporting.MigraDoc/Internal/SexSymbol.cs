using System;
using System.Reflection;

namespace PlanCheck.Reporting.MigraDoc.Internal
{
    internal class SexSymbol
    {
        private const string MaleSymbolResourceName = "Resources.male-symbol.png";
        private const string FemaleSymbolResourceName = "Resources.female-symbol.png";
        private const string OtherSymbolResourceName = "Resources.other-symbol.png";

        private readonly Sex _sex;

        public SexSymbol(Sex sex)
        {
            _sex = sex;
        }

        public string GetMigraDocFileName()
        {
            return ConvertToMigraDocFileName(LoadResource(SexSymbolResourceName()));
        }

        // Use special feature in MigraDoc, where instead of using a real file name,
        // it uses a special name that reads the image from memory
        // (see http://www.pdfsharp.net/wiki/MigraDoc_FilelessImages.ashx)
        private string ConvertToMigraDocFileName(byte[] image)
        {
            return $"base64:{Convert.ToBase64String(image)}";
        }

        private byte[] LoadResource(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fullName = $"{assembly.GetName().Name}.{name}";
            using (var stream = assembly.GetManifestResourceStream(fullName))
            {
                if (stream == null)
                {
                    throw new ArgumentException($"No resource with name {name}");
                }

                var count = (int)stream.Length;
                var data = new byte[count];
                stream.Read(data, 0, count);
                return data;
            }
        }

        private string SexSymbolResourceName()
        {
            switch (_sex)
            {
                case Sex.Male: return MaleSymbolResourceName;
                case Sex.Female: return FemaleSymbolResourceName;
                case Sex.Other: return OtherSymbolResourceName;

                // Should never reach here
                default: return string.Empty;
            }
        }
    }
}
