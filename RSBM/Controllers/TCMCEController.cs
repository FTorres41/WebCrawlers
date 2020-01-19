using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using RSBM.Models;
using RSBM.Repository;
using RSBM.Util;
using RSBM.WebUtil;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace RSBM.Controllers
{
    class TCMCEController
    {
        #region Declaração de variáveis
        public static string Name { get; } = "TCMCE";
        private static int NumLicitacoes;
        private static int NumCaptcha = 0;
        private static int CurrentPage = 1;
        private static string mensagemErro;
        public static string PathEdital = Path.GetTempPath() + DateTime.Now.ToString("yyyyMM") + Name + "/";

        private static Dictionary<string, Modalidade> NameToModalidade;
        private static Dictionary<string, Orgao> NameToOrgao;
        private static Dictionary<string, int?> Cidades;

        private static ConfigRobot config;
        private static LicitacaoRepository repo;
        private static Lote Lote;
        private static ChromeDriver web;
        private static WebDriverWait wait;
        private static FirefoxDriver ffweb;
        #endregion

        #region Métodos
        public static void InitCallBack(object state)
        {
            try
            {
                config = ConfigRobotController.FindByName(Name);

                if (config.Active == 'Y')
                {
                    if (File.Exists(Path.GetTempPath() + Name + ".txt"))
                        File.Delete(Path.GetTempPath() + Name + ".txt");

                    config.Status = 'R';
                    ConfigRobotController.Update(config);

                    using (FileStream fs = File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire")) { }
                    Init();

                    config = ConfigRobotController.FindByName(Name);

                    config.NumLicitLast = NumLicitacoes;
                    RService.Log(Name + " find " + NumLicitacoes + " novas licitações at {0}", Path.GetTempPath() + Name + ".txt");
                    RService.Log(Name + " consumiu " + NumLicitacoes + " captchas at {0}", Path.GetTempPath() + Name + ".txt");
                    NumLicitacoes = 0;

                    config.LastDate = DateTime.Now;
                }

                RService.ScheduleMe(config);

                config.Status = 'W';
                ConfigRobotController.Update(config);

                File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire");
            }
            catch (Exception e)
            {
                RService.Log("Exception (InitCallBack) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }

            RService.Log("Finished " + Name + " at {0}", Path.GetTempPath() + Name + ".txt");

            EmailHandle.SendMail(Path.GetTempPath() + Name + ".txt", Name);
        }

        internal static void Init()
        {
            RService.Log("(Init) " + Name + ": Começando o processamento.. at {0}", Path.GetTempPath() + Name + ".txt");

            try
            {
                //Inicializa as listas e variáveis que serão usadas pelo robô
                CurrentPage = 1;
                Lote = LoteController.CreateLote(43, 1249);
                repo = new LicitacaoRepository();
                Cidades = CidadeController.GetNameToCidade(Constants.TCMCE_UF);
                NameToOrgao = OrgaoController.GetNomeUfToOrgao();
                NameToModalidade = ModalidadeController.GetNameToModalidade();

                HtmlDocument htmlDoc = WebHandle.GetHtmlDocOfPage(string.Format(Constants.TCMCE_PAGE, CurrentPage, DateTime.Today.ToString("dd-MM-yyyy"), DateTime.Today.AddYears(1).ToString("dd-MM-yyyy")), Encoding.GetEncoding("UTF-8"));

                //O GetLastPage pega o código Html e o vasculha para encontrar o valor da última página
                int lastPage = GetLastPage(htmlDoc);

                while (CurrentPage != lastPage)
                {
                    RService.Log("(Init) " + Name + ": Percorrendo os links da página.. " + CurrentPage + " at {0}", Path.GetTempPath() + Name + ".txt");

                    HtmlNode licList = htmlDoc.DocumentNode.Descendants("table").SingleOrDefault(x => x.Id.Equals("table"));

                    foreach (var lic in licList.Descendants("tr"))
                    {
                        if (!lic.InnerHtml.Contains("Licitação"))
                        {
                            HandleCreate(lic);
                        }
                    }

                    CurrentPage++;
                    htmlDoc = WebHandle.GetHtmlDocOfPage(string.Format(Constants.TCMCE_PAGE, CurrentPage, DateTime.Today.ToString("dd-MM-yyyy"), DateTime.Today.AddYears(1).ToString("dd-MM-yyyy")), Encoding.GetEncoding("ISO-8859-1"));
                }

            }
            catch (Exception e)
            {
                RService.Log("Exception (Init) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        //Função para identificar a quantidade de páginas
        private static int GetLastPage(HtmlDocument htmlDoc)
        {
            int lastPage = 0;

            try
            {
                string code = htmlDoc.DocumentNode.ChildNodes[2].ChildNodes[5].ChildNodes[5].ChildNodes[9].ChildNodes[3].ChildNodes[30].ChildNodes[1].ChildNodes[1].ChildNodes[17].InnerHtml;
                lastPage = Convert.ToInt16(StringHandle.GetMatches(code, @"page\/\d+\/mun")[0].ToString().Split('/')[1]);
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetLastPage) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }

            return lastPage;
        }

        private static void HandleCreate(HtmlNode lic)
        {
            try
            {
                string link = string.Format(Constants.TCMCE_HOST + lic.ChildNodes[1].ChildNodes[0].GetAttributeValue("href", "").ToString());
                string[] licParts = link.Split('/');
                string licNum = licParts[7] + licParts[9];

                HtmlDocument licPage = WebHandle.GetHtmlDocOfPage(link, Encoding.GetEncoding("UTF-8"));
                string situacao = Regex.Replace(StringHandle.GetMatches(licPage.DocumentNode.InnerHtml, @"Situação:( *)<b>(.*)</b")[0].ToString(), @"Situação:|<b>|</b", "").Trim();

                if (!string.IsNullOrEmpty(licNum) && !LicitacaoController.Exists(licNum))
                {
                    Licitacao l = CreateLicitacao(licPage, link, licNum, situacao);
                    if (l != null)
                    {
                        repo = new LicitacaoRepository();
                        repo.Insert(l);
                        RService.Log("(HandleCreate) " + Name + ": Licitação nº" + licNum + " inserida com sucesso at {0}", Path.GetTempPath() + Name + ".txt");
                        NumLicitacoes++;

                        GetEditalAndArquivos(licPage, l);
                    }
                    else
                    {
                        RService.Log("Exception (HandleCreate) " + Name + ": Licitação não salva. Motivo: " + mensagemErro + " at {0}", Path.GetTempPath() + Name + ".txt");
                    }
                }
                else if (!string.IsNullOrEmpty(licNum) && LicitacaoController.Exists(licNum) && LicitacaoController.SituacaoAlterada(licNum, situacao))
                {
                    Licitacao licitacao = LicitacaoController.GetByIdLicitacaoFonte(licNum);
                    licitacao.Situacao = situacao;

                    repo = new LicitacaoRepository();
                    repo.Update(licitacao);
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (HandleCreate) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        private static Licitacao CreateLicitacao(HtmlDocument licPage, string link, string num, string situacao)
        {
            RService.Log("(CreateLicitacao) " + Name + ": Criando licitação " + num + " at {0}", Path.GetTempPath() + Name + ".txt");

            Licitacao licitacao = new Licitacao();

            try
            {
                licitacao.IdLicitacaoFonte = Convert.ToInt64(num);
                licitacao.IdFonte = 1249;
                licitacao.Estado = Constants.TCMCE_ESTADO;
                licitacao.EstadoFonte = Constants.TCMCE_UF;
                licitacao.LinkEdital = link;
                licitacao.LinkSite = Constants.TCMCE_HOST;
                licitacao.Lote = Lote;

                var licInfo = licPage.DocumentNode.ChildNodes[2].ChildNodes[5].ChildNodes[5].InnerHtml;

                string city = Regex.Replace(StringHandle.GetMatches(licInfo, @"h2>(.*)\|")[0].ToString(), @"h2>|\|", "").Trim();
                string orgao = Regex.Replace(StringHandle.GetMatches(licInfo, @"\|(.*)<")[0].ToString(), @"(\|)|<", "").Trim();
                string numero = Regex.Replace(StringHandle.GetMatches(licInfo, @"h1>(.*)</h1")[0].ToString(), @"h1>Licitação:|</h1", "").Trim();
                string obj = Regex.Replace(StringHandle.GetMatches(licInfo, @"Objeto:( *)<b>(.|\n)*?</b>")[0].ToString(), @"Objeto:|<b>|</b>", "").Trim();
                string modal = Regex.Replace(StringHandle.GetMatches(licInfo, @"Modalidade:( *)<b>(.*)</b>")[0].ToString(), @"Modalidade:|<b>|</b>", "").Split('|')[0].Trim();
                if (modal == "Concorrência Pública")
                    modal = "Concorrência";
                string dataAb = Regex.Replace(StringHandle.GetMatches(licInfo, @"Data( *)de( *)Abertura:( *)<b>(.*)</b>")[0].ToString(), @"Data( *)de( *)Abertura:|<b>|</b>", "").Split('|')[0].Trim();
                string horaAb = Regex.Replace(StringHandle.GetMatches(licInfo, @"Hora( *)da( *)Abertura:( *)<b>(.*)</b>")[0].ToString(), @"Hora( *)da( *)Abertura:|<b>|</b>", "").Trim();
                string endereco = Regex.Replace(StringHandle.GetMatches(licInfo, @"Local:( *)<b>(.*)</b>")[0].ToString(), @"Local:|<b>|</b>", "").Trim();
                string processo = Regex.Replace(StringHandle.GetMatches(licInfo, @"Administrativo:( *)<b>(.*)<")[0].ToString(), @"Administrativo:|<b>|<", "").Trim();
                string dpto = "";
                if (Regex.IsMatch(licInfo, @"Órgãos</b>", RegexOptions.IgnoreCase))
                    dpto = Regex.Replace(StringHandle.GetMatches(licInfo, @"Órgãos</b>(.|\n)*?</li")[0].ToString(), @"Órgãos|</b>|<ul>|<li>|</li", "").Trim();
                string obs = Regex.Replace(StringHandle.GetMatches(licInfo, @"Objeto/Lote/Item(.|\n)*?</b")[0].ToString(), @"Objeto/Lote/Item:|<b>|</b", "").Trim();

                city = city.ToLower();
                var textInfo = new CultureInfo("pt-BR").TextInfo;

                licitacao.Cidade = textInfo.ToTitleCase(city).ToString();
                licitacao.CidadeFonte = Cidades.ContainsKey(licitacao.Cidade.ToUpper()) ? Cidades[licitacao.Cidade.ToUpper()] : CityUtil.GetCidadeFonte(licitacao.Cidade, Cidades);

                licitacao.Departamento = dpto;
                licitacao.Orgao = OrgaoController.GetOrgaoByNameAndUf(orgao + ":CE", NameToOrgao);
                if (licitacao.Orgao.Nome == "Prefeitura Municipal")
                    licitacao.Orgao.Nome = "Prefeitura Municipal de " + licitacao.Cidade;
                licitacao.Num = numero;
                licitacao.Processo = processo;
                licitacao.AberturaData = DateHandle.Parse(dataAb + " " + horaAb, "dd/MM/yyyy hh:mm:ss");
                licitacao.EntregaData = licitacao.AberturaData;
                licitacao.Modalidade = NameToModalidade.ContainsKey(StringHandle.RemoveAccent(modal.ToUpper())) ? NameToModalidade[StringHandle.RemoveAccent(modal.ToUpper())] : null;
                licitacao.Situacao = situacao;
                licitacao.Objeto = obj;
                licitacao.Observacoes = obs;
                licitacao.Endereco = endereco;

                licitacao.DigitacaoUsuario = 43; //Id do Robô no sistema LM
                //licitacao.DigitacaoData = null;
                //licitacao.ProcessamentoData = null;
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacao) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }

            return LicitacaoController.IsValid(licitacao, out mensagemErro) ? licitacao : null;
        }


        private static void GetEditalAndArquivos(HtmlDocument licPage, Licitacao l)
        {
            try
            {
                int fileCount = 0;
                string files = licPage.DocumentNode.ChildNodes[2].ChildNodes[5].ChildNodes[5].ChildNodes[9].ChildNodes[3].ChildNodes[3].ChildNodes[5].InnerHtml;
                MatchCollection fileLinks = StringHandle.GetMatches(files, @"/down(.*)>");
                foreach (var file in fileLinks)
                {
                    string fileUrl = l.LinkEdital.Replace("detalhes", "baixarArquivo") + file.ToString().Split('\"')[0];

                    //l.Observacoes = "Link para download de arquivo referente ao edital: " + fileUrl + "\n";
                    //repo = new LicitacaoRepository();
                    //repo.Update(l);

                    GetFile(l, fileUrl, files, fileCount);
                    fileCount++;
                }

            }
            catch (Exception e)
            {
                RService.Log("Exception (GetEditalAndArquivos) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        private static void GetFile(Licitacao l, string fileUrl, string files, int count)
        {
            try
            {
                //Tuple<ChromeDriver, WebDriverWait> loadDriver = WebDriverChrome.LoadWebDriver(Name);
                //web = loadDriver.Item1;
                //wait = loadDriver.Item2;

                if (web != null)
                    web.Quit();

                var driver = ChromeDriverService.CreateDefaultService();
                driver.HideCommandPromptWindow = true;
                var op = new ChromeOptions();
                op.AddUserProfilePreference("download.default_directory", PathEdital);
                web = new ChromeDriver(driver, new ChromeOptions(), TimeSpan.FromSeconds(300));
                web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
                wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));

                if (!Directory.Exists(PathEdital)) Directory.CreateDirectory(PathEdital);

                web.Navigate().GoToUrl(fileUrl);

                web.FindElement(By.Id("captcha_usuario")).SendKeys(GetScriptFillCaptcha(web, "captcha_usuario").ToLower());
                web.FindElement(By.Id("enviar")).Click();
                Thread.Sleep(15000);

                string fileName = StringHandle.GetMatches(files, @"uploads/(.*).pdf")[count].ToString().Replace("uploads/", "");

                #region AWS

                if (AWS.SendObject(l, PathEdital, fileName))
                {
                    LicitacaoArquivo licitacaoArq = new LicitacaoArquivo();
                    licitacaoArq.NomeArquivo = fileName;
                    licitacaoArq.NomeArquivoOriginal = Name + DateTime.Now.ToString("yyyyMMddHHmmss");
                    licitacaoArq.NomeArquivoFonte = Name;
                    licitacaoArq.Status = 0;
                    licitacaoArq.IdLicitacao = l.Id;

                    LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                    repoArq.Insert(licitacaoArq);

                    if (File.Exists(PathEdital + fileName))
                        File.Delete(PathEdital + fileName);

                    RService.Log("(GetFiles) " + Name + ": Arquivo " + fileName + " enviado com sucesso para Amazon S3" + " at {0}", Path.GetTempPath() + Name + ".txt");
                }
                else
                {
                    RService.Log("Exception (GetFiles) " + Name + ": Erro ao enviar o arquivo para Amazon (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + Name + ".txt");
                }

                #endregion
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetFile) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            finally
            {
                if (web != null)
                    web.Close();
            }
        }

        private static string GetScriptFillCaptcha(ChromeDriver web, string input)
        {
            RService.Log("(GetScriptFillCaptcha) " + Name + ": Resolvendo captcha... at {0}", Path.GetTempPath() + Name + ".txt");
            string script = string.Empty;
            Bitmap image;

            try
            {
                string tempImg = Path.GetTempPath() + "tempImg.png";
                string tempImgCrop = Path.GetTempPath() + "tempImgCrop.png";
                web.GetScreenshot().SaveAsFile(tempImg, ScreenshotImageFormat.Png);

                using (Stream bmpStream = File.Open(tempImg, FileMode.Open))
                {
                    Image img = Image.FromStream(bmpStream);

                    image = new Bitmap(img);
                }

                GetCaptchaImg(web.FindElement(By.XPath("//*[@id=\"form1\"]/div[1]/img")), image, tempImgCrop);

                script = WebHandle.ResolveCaptcha(tempImgCrop);

                if (File.Exists(tempImg))
                    File.Delete(tempImg);
                if (File.Exists(tempImgCrop))
                    File.Delete(tempImgCrop);

                NumCaptcha++;
            }
            catch (Exception ex)
            {
                RService.Log("Exception (GetScriptFillCaptcha) " + Name + ": " + ex.Message + " / " + ex.StackTrace + " / " + ex.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }

            return script;
        }

        private static void GetCaptchaImg(IWebElement webElement, Bitmap image, string tempImgCrop)
        {
            RService.Log("(GetCaptchaImg) " + Name + ": Achando captcha... " + "at {0}", Path.GetTempPath() + Name + ".txt");

            try
            {
                Point p = webElement.Location;

                int eleWidth = webElement.Size.Width;
                int eleHeight = webElement.Size.Height;

                Size size = new Size(eleWidth, eleHeight);
                Rectangle r = new Rectangle(p, size);

                Image ib = image.Clone(r, image.PixelFormat);

                ib.Save(tempImgCrop);

                if (image != null)
                    image.Dispose();

                if (ib != null)
                    ib.Dispose();
            }
            catch (Exception ex)
            {
                RService.Log("Exception (GetCaptchaImg) " + Name + ": " + ex.Message + " / " + ex.StackTrace + " / " + ex.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        #region OLD
        //private static void LoadWebDriver()
        //{
        //    try
        //    {
        //        if (web != null)
        //            web.Quit();

        //        var driver = ChromeDriverService.CreateDefaultService();
        //        driver.HideCommandPromptWindow = true;

        //        var options = new ChromeOptions();
        //        options.AddUserProfilePreference("profile.default_content_settings.popups", 0);
        //        options.AddUserProfilePreference("download.default_directory", Path.GetTempPath());
        //        options.AddUserProfilePreference("plugins.always_open_pdf_externally", true);

        //        web = new ChromeDriver(driver, options, TimeSpan.FromSeconds(180));
        //        web.Manage().Window.Maximize();
        //        web.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(180));

        //        wait = new WebDriverWait(web, TimeSpan.FromSeconds(1000));

        //    }
        //    catch (Exception e)
        //    {
        //        RService.Log("Exception (LoadWebDriver) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");

        //        if (web != null)
        //            web.Quit();
        //    }
        //}
        #endregion

        private static void LoadFirefoxDriver()
        {
            try
            {
                if (ffweb != null)
                    ffweb.Quit();

                var driver = FirefoxDriverService.CreateDefaultService();
                driver.HideCommandPromptWindow = true;

                var options = new FirefoxOptions();
                options.SetPreference("browser.download.dir", Path.GetTempPath());
                options.SetPreference("browser.helperApps.neverAsk.saveToDisk", "application/pdf");
                options.SetPreference("pdfjs.disabled", true);
                options.SetPreference("browser.download.manager.showWhenStarting", false);
                options.SetPreference("browser.download.folderList", 2);
                options.SetPreference("browser.helperApps.alwaysAsk.force", true);

                ffweb = new FirefoxDriver(driver, options, TimeSpan.FromSeconds(180));
                ffweb.Manage().Window.Maximize();
                ffweb.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(180);

                wait = new WebDriverWait(ffweb, TimeSpan.FromSeconds(10000));
            }
            catch (Exception e)
            {
                RService.Log("Exception (LoadFirefoxDriver) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");

                if (ffweb != null)
                    ffweb.Quit();
            }
        }
        #endregion
    }
}
