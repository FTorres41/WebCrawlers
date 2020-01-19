using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RSBM.Util
{
    class FileHandle
    {

        public static readonly object _lock = new object();

        //Cria um nome único pro arq temporario do edital.
        internal static string GetATemporaryFileName()
        {
            lock (_lock) {
                Thread.Sleep(100);
                byte[] nameHash = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(DateTime.Now.ToString("ddMMyyyyHHmmssff")));
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < nameHash.Length - 1; i++)
                    builder.Append(nameHash[i].ToString("x2"));

                return DateTime.Now.ToString("ddMMyyyyHHmmssff") + builder.ToString();
            }
        }

    }
}
