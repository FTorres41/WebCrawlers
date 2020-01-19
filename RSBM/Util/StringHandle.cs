using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RSBM.Util
{
    class StringHandle
    {
        /*Remove acentos e caracteres especiais*/
        public static string RemoveAccent(string word)
        {
            return Encoding.ASCII.GetString(
                Encoding.GetEncoding("Cyrillic").GetBytes(word)
            );
        }

        /*Busca pelo padrão*/
        public static MatchCollection GetMatches(string value, string pattern)
        {
            try
            {
                MatchCollection matches = Regex.Matches(value, pattern);
                if (matches.Count > 0)
                {
                    return matches;
                }
            }
            catch (Exception e)
            {
                RService.Log("RService Exception: (GetMatches)" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RService" + ".txt");
            }
            return null;
        }

        /*Busca por regex uma Key em um dicionario*/
        public static int? FindKeyRegex(Dictionary<string, int?> dictionary, string valor)
        {

            if(dictionary.ContainsKey(valor))
            {
                return dictionary[valor].Value;
            }

            foreach (var d in dictionary)
            {
                string valorRegex = Regex.Replace(valor, @"I", "(I|Y)");
                if (Regex.IsMatch(d.Key, valorRegex, RegexOptions.IgnoreCase))
                {
                    return d.Value;
                }
            }
            return null;
        }
    }
}
