
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TXT.WEAVR.Builder
{
    internal class WeavrBuildUtility
    {

        internal string GetNameFromAsmDef(string filePath)
        {
            using (StreamReader file = File.OpenText(filePath))
            {
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject o2 = (JObject)JToken.ReadFrom(reader);
                    return o2.GetValue("name").ToString();
                }
            }
        }

        internal Guid GenerateGuidFromString(string s)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(s));
                return new Guid(hash);
            }
        }
    }

}
