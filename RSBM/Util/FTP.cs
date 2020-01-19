using RSBM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RSBM.Util
{
    class FTP
    {
        public static string Adrss = "ftp://www.licitacoesmais.com.br/public_html/admin/files/licitacoes/";
        public static string Pwd = "wZ8v234cVv6s6";
        public static string UName = "admin_lm";

        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Adress { get; set; }
        public string Password { get; set; }
        public string UserName { get; set; }

        public FTP(string filePath, string fileName, string adress, string password, string userName)
        {
            FilePath = filePath;
            FileName = fileName;
            Adress = adress;
            Password = password;
            UserName = userName;
        }
        /*Envia um arquivo por ftp*/
        internal static bool SendFileFtp(FTP ftpHandle, string nomeRobo)
        {
            bool segundaTentativa = false;
            do
            {
                try
                {
                    if (File.Exists(ftpHandle.FilePath + ftpHandle.FileName))
                    {
                        RService.Log("FTP: Iniciando envio at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                        Uri uri = new Uri(ftpHandle.Adress + ftpHandle.FileName);
                        FtpWebRequest ftp = (FtpWebRequest)WebRequest.Create(uri);
                        ftp.Credentials = new NetworkCredential(ftpHandle.UserName, ftpHandle.Password);
                        ftp.KeepAlive = false;
                        ftp.Method = WebRequestMethods.Ftp.UploadFile;
                        ftp.UseBinary = true;

                        RService.Log("FTP: Abrindo stream arquivo local at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");

                        using (FileStream fStream = File.OpenRead(ftpHandle.FilePath + ftpHandle.FileName))
                        {
                            RService.Log("FTP: Arquivo [FileName: " + ftpHandle.FilePath + ftpHandle.FileName + "] at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                            byte[] buffer = new byte[fStream.Length];
                            ftp.ContentLength = buffer.Length;

                            using (Stream stream = ftp.GetRequestStream())
                            {
                                RService.Log("RService FTP: Enviando... at {0}", Path.GetTempPath() + nomeRobo + ".txt");
                                fStream.CopyTo(stream);
                            }
                        }
                        RService.Log("RService FTP Finished at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                        return true;
                    }
                    else
                        RService.Log("RService Exception FTP: (SendFileFtp) Arquivo " + ftpHandle.FilePath + ftpHandle.FileName + " para o envio não existe at {0}", Path.GetTempPath() + nomeRobo + ".txt");

                }
                catch (Exception e)
                {
                    RService.Log("RService Exception FTP: (SendFileFtp: " + ftpHandle.FilePath + ftpHandle.FileName + ")" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + nomeRobo + ".txt");

                    /*Faz uma segunda tentativa de envio*/
                    segundaTentativa = !segundaTentativa;
                    if (segundaTentativa)
                    {
                        RService.Log("FTP: Segunda tentativa de envio... at {0}", Path.GetTempPath() + nomeRobo + ".txt");
                        Thread.Sleep(5000);
                    }
                    else
                        return false;
                }
            }
            while (segundaTentativa);

            return false;
        }
    }
}
