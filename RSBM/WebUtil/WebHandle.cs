using HtmlAgilityPack;
using NReco.PdfGenerator;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace RSBM.WebUtil
{
    class WebHandle
    {
        private static string CaptchaUser = "licitacoesmais";
        private static string CaptchaPass = "gnp1234";
        public static string ExtensionLastFileDownloaded { get; set; }

        /*Converte uma página html num documento pdf*/
        internal static bool HtmlToPdf(string uri, string file)
        {
            try
            {
                HtmlToPdfConverter pdfConverter = new HtmlToPdfConverter();
                ExtensionLastFileDownloaded = ".pdf";
                pdfConverter.GeneratePdfFromFile(uri.Replace("https", "http"), null, file.EndsWith(ExtensionLastFileDownloaded) ? file : file + ExtensionLastFileDownloaded);
            }
            catch (Exception e)
            {
                RService.Log("Exception WebHandle (HtmlToPdf): " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                return false;
            }
            return true;
        }

        internal static HtmlDocument GetHtmlDocOfPageDefaultEncoding(string uri, NameValueCollection formparameters = null, string metodo = "POST")
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            if (formparameters == null)
            {
                formparameters = new NameValueCollection();
            }
            try
            {
                using (CookieAwareWebClient wc = new CookieAwareWebClient())
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows; U; MSIE 9.0; Windows NT 9.0; en-US)";
                    htmlDoc.LoadHtml(HttpUtility.HtmlDecode(Encoding.Default.GetString(wc.UploadValues(uri, metodo, formparameters))));
                }
            }
            catch (Exception e)
            {
                string parameters = string.Empty;
                foreach (string param in formparameters.Keys)
                    parameters += formparameters[param].ToString() + ";";
                RService.Log("RService Exception WebHandle: (GetHtmlDocOfPage) " + uri + " " + parameters + " " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                return null;
            }
            return htmlDoc;
        }

        /*Retorna o Html de uma página*/
        internal static HtmlDocument GetHtmlDocOfPage(string uri, NameValueCollection formparameters = null, string metodo = "POST")
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            if (formparameters == null)
            {
                formparameters = new NameValueCollection();
            }
            try
            {
                using (CookieAwareWebClient wc = new CookieAwareWebClient())
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    wc.Headers["User-Agent"] = "Mozilla/5.0 (Windows; U; MSIE 9.0; Windows NT 9.0; en-US)";
                    htmlDoc.LoadHtml(HttpUtility.HtmlDecode(Encoding.UTF8.GetString(wc.UploadValues(uri, metodo, formparameters))));
                }
            }
            catch (Exception e)
            {
                string parameters = string.Empty;
                foreach (string param in formparameters.Keys)
                    parameters += formparameters[param].ToString() + ";";
                RService.Log("RService Exception WebHandle: (GetHtmlDocOfPage) " + uri + " " + parameters + " " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                return null;
            }
            return htmlDoc;
        }

        /*Retorna o Html de uma página*/
        internal static HtmlDocument GetHtmlDocOfPage(string uri, Encoding e, NameValueCollection formparameters = null, string metodo = "POST")
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            if (formparameters == null)
            {
                formparameters = new NameValueCollection();
            }
            try
            {
                using (CookieAwareWebClient wc = new CookieAwareWebClient())
                {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    htmlDoc.LoadHtml(HttpUtility.HtmlDecode(e.GetString(wc.UploadValues(uri, metodo, formparameters))));
                }
            }
            catch (Exception ex)
            {
                RService.Log("Exception WebHandle: (GetHtmlDocOfPage)" + ex.Message + " / " + ex.StackTrace + " / " + ex.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                return null;
            }
            return htmlDoc;
        }

        /*Retorna o html de uma página passando pelo captcha é preciso passar o link da imagem
        o linksite é concatenado ao texto retirado da imagem linksite + captcharesolvido*/
        internal static HtmlDocument GetHtmlHandleCaptcha(string linksite, Encoding e, string captchaParameter, string capthcaLink = "", NameValueCollection formparameters = null, string metodo = "POST")
        {
            DeathByCaptcha.Client objSocketClient;
            try
            {
                objSocketClient = new DeathByCaptcha.SocketClient(CaptchaUser, CaptchaPass);
            }
            catch (Exception ex)
            {
                RService.Log("RService Exception WebHandle: (SocketClient)" + ex.Message + " / " + ex.StackTrace + " / " + ex.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                return null;
            }
            DeathByCaptcha.Captcha objCaptcha;
            HtmlDocument htmlDoc = new HtmlDocument();
            RService.Log("RService WebHandle: (Linha 90) at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
            if (formparameters == null)
            {
                formparameters = new NameValueCollection();
            }
            int number = 0;
            while (number < 2)
            {
                try
                {
                    ServicePointManager.ServerCertificateValidationCallback += new System.Net.Security.RemoteCertificateValidationCallback(CustomValidation);

                    using (CookieAwareWebClient wc = new CookieAwareWebClient())
                    using (MemoryStream ms = new MemoryStream(wc.DownloadData(capthcaLink)))
                    {
                        RService.Log("RService WebHandle: (Linha 103) at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                        RService.Log("RService WebHandle [MemoryStream Length: " + ms.Length + "] [CapthcaLink: " + capthcaLink + "]: (Linha 104) at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                        objCaptcha = objSocketClient.Decode(ms, 120);
                        if (objCaptcha.Solved && objCaptcha.Correct)
                        {
                            RService.Log("RService WebHandle: (Linha 108) at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                            objSocketClient.Close();

                            formparameters[captchaParameter] = objCaptcha.Text;

                            htmlDoc.LoadHtml(HttpUtility.HtmlDecode(e.GetString(wc.UploadValues(linksite, metodo, formparameters))));
                            RService.Log("RService WebHandle: (Linha 111) at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    RService.Log("RService Exception WebHandle: (GetHtmlHandleCaptcha)" + ex.Message + " / " + ex.StackTrace + " / " + ex.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                    Thread.Sleep(1000 * 10);
                    objSocketClient = new DeathByCaptcha.SocketClient(CaptchaUser, CaptchaPass);
                    number++;
                }
            }
            return number == 2 ? null : htmlDoc;
        }

        private static bool CustomValidation(object sender, X509Certificate cert, X509Chain chain, System.Net.Security.SslPolicyErrors error)
        {
            return true;
        }

        /*Faz o download de um arquivo do linkFile para o fileNameDotType que deve conter a extensão do arquivo (ex:.pdf ou .doc)*/
        internal static bool DownloadFile(string linkFile, string fileNameDotType)
        {
            try
            {
                using (CookieAwareWebClient wc = new CookieAwareWebClient())
                {
                    wc.DownloadFile(linkFile, fileNameDotType);
                }
            }
            catch (Exception e)
            {
                RService.Log("RService Exception WebHandle: (DownloadFile) [LinkFile : " + linkFile + "] " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                return false;
            }
            return true;
        }

        /*Faz o download de um arquivo, retira a extensão do arquivo do header de resposta*/
        internal static bool DownloadData(string linkFile, string fileName)
        {
            try
            {
                using (CookieAwareWebClient wc = new CookieAwareWebClient())
                {
                    byte[] byt = wc.DownloadData(linkFile);
                    RService.Log("RService WebHandle: (Linha 149) at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                    string fileType = "." + Regex.Match(wc.ResponseHeaders.ToString(), "filename.*\\.(\\w{3})").Groups[1].Value;
                    RService.Log("RService WebHandle: (Linha 151) at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                    File.WriteAllBytes(fileName + fileType, byt);
                    ExtensionLastFileDownloaded = fileType;
                    return true;
                }
            }
            catch (Exception e)
            {
                RService.Log("RService Exception WebHandle: (DownloadData) [LinkFile : " + linkFile + "] " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                return false;
            }
        }

        /*Faz o download de um arquivo mandando os parametros do post com a requisição*/
        internal static bool DownloadDataPost(string linkFile, string fileName, NameValueCollection formparameters = null, string metodo = "POST")
        {
            try
            {
                if (formparameters == null)
                {
                    formparameters = new NameValueCollection();
                }
                using (CookieAwareWebClient wc = new CookieAwareWebClient())
                {
                    byte[] byt = wc.UploadValues(linkFile, metodo, formparameters);
                    string fileType = "." + Regex.Match(wc.ResponseHeaders.ToString(), "filename.*\\.(\\w{3})").Groups[1].Value;
                    File.WriteAllBytes(fileName + fileType, byt);
                    ExtensionLastFileDownloaded = fileType;
                    return true;
                }
            }
            catch (Exception e)
            {
                RService.Log("RService Exception WebHandle: (DownloadDataPost) [LinkFile : " + linkFile + "] " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                return false;
            }
        }

        /*Faz o download de um arquivo usando um valor para o header UserAgent*/
        internal static void DownloadFileWebRequest(string url, string path, string fileName)
        {
            try
            {
                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
                wr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/50.0.2661.102 Safari/537.36";
                var tempName = Guid.NewGuid().ToString();
                using (HttpWebResponse myHttpWebResponse = (HttpWebResponse)wr.GetResponse())
                using (Stream remote = myHttpWebResponse.GetResponseStream())
                using (Stream local = File.Create(path + tempName))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    do
                    {
                        bytesRead = remote.Read(buffer, 0, buffer.Length);
                        local.Write(buffer, 0, bytesRead);
                    } while (bytesRead > 0);
                }

                File.Move(path + tempName, path + fileName);
            }
            catch (Exception e)
            {
                RService.Log("RService Exception WebHandle: (DownloadFileWebRequest) [LinkFile : " + url + "] " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
            }
        }

        internal static bool DownloadFileWebRequest(ReadOnlyCollection<OpenQA.Selenium.Cookie> AllCookies, string url, string path)
        {
            try
            {
                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
                wr.CookieContainer = new CookieContainer();
                foreach (var c in AllCookies)
                {
                    System.Net.Cookie cookie = new System.Net.Cookie(c.Name, c.Value, c.Path, c.Domain);
                    wr.CookieContainer.Add(cookie);
                }
                wr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/50.0.2661.102 Safari/537.36";
                wr.AllowAutoRedirect = true;

                using (HttpWebResponse myHttpWebResponse = wr.GetResponse() as HttpWebResponse)
                using (Stream remote = myHttpWebResponse.GetResponseStream())
                using (Stream local = File.Create(path))
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead;

                    do
                    {
                        bytesRead = remote.Read(buffer, 0, buffer.Length);
                        local.Write(buffer, 0, bytesRead);
                    } while (bytesRead > 0);
                }
                return true;
            }
            catch (Exception e)
            {
                RService.Log("RService Exception WebHandle: (DownloadFileWebRequest) [LinkFile : " + url + "] " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                return false;
            }
        }

        /*Resolve captcha usando api DeathByCaptcha*/
        internal static string ResolveCaptcha(string file)
        {
            string solution = string.Empty;
            DeathByCaptcha.Client objSocketClient;
            try
            {
                objSocketClient = new DeathByCaptcha.SocketClient(CaptchaUser, CaptchaPass);
            }
            catch (Exception e)
            {
                RService.Log("Exception WebHandle: (ResolveCaptcha)" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                return null;
            }
            DeathByCaptcha.Captcha objCaptcha;
            int number = 0;
            while (number < 2)
            {
                try
                {
                    using (FileStream f = new FileStream(file, FileMode.Open, FileAccess.Read))
                    {
                        objCaptcha = objSocketClient.Decode(f, 120);
                        if (objCaptcha.Solved && objCaptcha.Correct)
                        {
                            objSocketClient.Close();
                            solution = objCaptcha.Text;
                        }
                    }

                    break;
                }
                catch (Exception e)
                {
                    Thread.Sleep(1000 * 5);
                    objSocketClient = new DeathByCaptcha.SocketClient(CaptchaUser, CaptchaPass);
                    number++;

                    RService.Log("Exception WebHandle: (ResolveCaptcha)" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                }
            }
            return number == 2 ? null : solution;
        }

        /*retorna a extensão do arquivo original*/
        internal static string GetExtensionFile(string nameFile)
        {
            return Regex.Match(nameFile, @"(\.\w+)$").Value.ToString().ToLower();
        }

        /*Retorna o Html de uma página*/
        internal static HtmlDocument HtmlParaObjeto(string link, Encoding e)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            try
            {
                HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(link);
                wr.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/50.0.2661.102 Safari/537.36";
                using (HttpWebResponse response = (HttpWebResponse)wr.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    htmlDoc.LoadHtml(HttpUtility.HtmlDecode(e.GetString(ms.ToArray())));
                }

                if (string.IsNullOrEmpty(htmlDoc.DocumentNode.InnerHtml))
                {
                    RService.Log("Exception WebHandle: (HtmlParaObjeto) Não foi possivel tranformar o HTML at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                }
            }
            catch (Exception ex)
            {
                RService.Log("Exception WebHandle: (HtmlParaObjeto)" + ex.Message + " / " + ex.StackTrace + " / " + ex.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
            }
            return htmlDoc;
        }

        internal static HttpWebResponse DoRequest(string url)
        {
            CookieContainer cookies = new CookieContainer();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AllowAutoRedirect = true;
            request.CookieContainer = cookies;
            return (HttpWebResponse)request.GetResponse();
        }
    }
}
