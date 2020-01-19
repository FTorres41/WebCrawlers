using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
//using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Support.UI;
using RSBM.Models;
using RSBM.Repository;
using RSBM.Util;
using RSBM.WebUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RSBM.Controllers
{
    public class TCERSController
    {
        #region Declaração de atributos
        public static string Name { get; } = "TCERS";
        private static int NumLicitacoes { get; set; }
        private static string LogPath { get; } = Path.GetTempPath() + Name + ".txt";
        private static string mensagemErro;

        private static ConfigRobot config;
        private static Lote Lote;
        private static LicitacaoRepository repo;
        private static ChromeDriver web;
        //private static PhantomJSDriver web;
        private static WebDriverWait wait;

        private static Dictionary<string, Modalidade> NameToModalidade;
        private static Dictionary<string, int?> Cidades;
        private static List<Orgao> Orgaos;
        public static string PathEditais { get; } = Path.GetTempPath() + DateTime.Now.ToString("yyyyMM") + Name + "/";
        public static HashSet<long> AlreadyInserted { get; private set; }

        #endregion

        #region Métodos
        internal static void InitCallBack(object state)
        {
            try
            {
                config = ConfigRobotController.FindByName(Name);

                if (config.Active == 'Y')
                {
                    if (File.Exists(LogPath))
                        File.Delete(LogPath);

                    config.Status = 'R';
                    ConfigRobotController.Update(config);
                    using (FileStream fs = File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire")) { }
                    Init();

                    //Verifica se teve atualização
                    config = ConfigRobotController.FindByName(Name);

                    //Verifica quantas licitações foram coletadas nessa execução, grava em log.
                    config.NumLicitLast = NumLicitacoes;
                    RService.Log(Name + " find " + NumLicitacoes + " novas licitações at {0}", LogPath);
                    NumLicitacoes = 0;

                    config.LastDate = DateTime.Now;
                }

                //Reprogamando a próxima execução do robô.
                RService.ScheduleMe(config);

                //Atualiza as informações desse robô.
                config.Status = 'W';
                ConfigRobotController.Update(config);

                //Arquivo que indica ao manager que é hora de atualizar as informações.
                File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire");
            }
            catch (Exception e)
            {
                RService.Log("(InitCallBack) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
            }
            finally
            {
                if (web != null)
                {
                    web.Close();
                }
            }

            RService.Log("Finished " + Name + " at {0}", LogPath);

            EmailHandle.SendMail(LogPath, Name);
        }

        private static void Init()
        {
            RService.Log("(Init) " + Name + ": Iniciando busca... at {0}", LogPath);

            try
            {
                repo = new LicitacaoRepository();
                Orgaos = OrgaoRepository.FindByUF(Constants.TCERS_ESTADO_FONTE);
                NameToModalidade = ModalidadeController.GetNameToModalidade();
                Cidades = CidadeController.GetNameToCidade(Constants.TCERS_ESTADO_FONTE);
                Lote = LoteController.CreateLote(43, Constants.TCERS_ID_FONTE);

                if (web != null)
                    web.Quit();

                var driver = ChromeDriverService.CreateDefaultService();
                driver.HideCommandPromptWindow = true;
                var op = new ChromeOptions();
                op.AddUserProfilePreference("download.default_directory", PathEditais);
                web = new ChromeDriver(driver, new ChromeOptions(), TimeSpan.FromSeconds(300));
                web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
                wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));

                web.Navigate().GoToUrl(Constants.TCERS_HOST);
                web.FindElement(By.XPath("//*[@id=\"t_TreeNav_1\"]/div[2]/a")).Click();

                web.FindElement(By.XPath("//*[@id=\"R4830475339138649_actions_button\"]")).Click();
                web.FindElement(By.XPath("//*[@id=\"R4830475339138649_actions_menu_3i\"]")).Click();

                try
                {
                    Actions actions = new Actions(web);
                    actions.MoveToElement(web.FindElement(By.XPath("//*[@id=\"R4830475339138649_actions_menu_3i\"]"))).Perform();
                    actions.MoveToElement(web.FindElement(By.XPath("//*[@id=\"R4830475339138649_actions_menu_3_0_c9\"]"))).Perform();
                    web.FindElement(By.XPath("//*[@id=\"R4830475339138649_actions_menu_3_0_c9\"]")).Click();
                }
                catch (Exception e)
                {
                    RService.Log("(Exception) " + Name + ": Elemento 3 at {0}", LogPath);
                }

                Thread.Sleep(10000);

                var html = web.PageSource;

                var licits = new List<string>();

                Regex licitRx = new Regex("f\\?p=(.*?)\">");
                if (licitRx.IsMatch(html))
                {
                    var matches = licitRx.Matches(html);

                    foreach (var mt in matches)
                    {
                        if (mt.ToString().Contains("ID_LICITACAO"))
                        {
                            var treatedLink = mt.ToString().Replace("\">", "").Replace("amp;", "");
                            licits.Add(Constants.TCERS_BASEURL + treatedLink);
                        }
                    }
                }

                GetLicitacoes(licits);
            }
            catch (Exception e)
            {
                RService.Log("Exception (Init) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
            }
        }

        private static void GetLicitacoes(List<string> licitLinks)
        {
            foreach (var link in licitLinks)
            {
                try
                {
                    if (web != null)
                        web.Quit();

                    var driver = ChromeDriverService.CreateDefaultService();
                    driver.HideCommandPromptWindow = true;
                    var op = new ChromeOptions();
                    op.AddUserProfilePreference("download.default_directory", PathEditais);
                    web = new ChromeDriver(driver, new ChromeOptions(), TimeSpan.FromSeconds(300));
                    web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
                    wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));

                    web.Navigate().GoToUrl(link);

                    CheckReCaptcha(web.PageSource);

                    var situacao = web.FindElement(By.XPath("//*[@id=\"report_38717872527149671_catch\"]/dl/dd[4]")).Text;
                    var andamento = web.FindElement(By.XPath("//*[@id=\"L38976648273068799\"]/div/span[2]"));

                    if (!situacao.Contains("Encerrada") || !andamento.Text.Contains("Ativo"))
                    {
                        Licitacao licitacao = CreateLicitacao(link, situacao);

                        try
                        {

                            if (LicitacaoController.IsValid(licitacao, out mensagemErro) && !LicitacaoController.ExistsTCERS(licitacao.IdLicitacaoFonte, Constants.TCERS_ID_FONTE)/*!AlreadyInserted.Contains(licitacao.IdLicitacaoFonte)*/)
                            {
                                repo.Insert(licitacao);
                                NumLicitacoes++;
                                RService.Log("(GetLicitacoes) " + Name + ": Licitação nº" + licitacao.Num + " salva com sucesso at {0}", LogPath);

                                //GetArquivos(licitacao);
                            }
                            else if (!string.IsNullOrEmpty(mensagemErro))
                            {
                                RService.Log("Exception (GetLicitacoes - Insert) " + Name + ": " + mensagemErro + " at {0}", LogPath);
                            }
                            else
                            {
                                RService.Log("Exception (GetLicitacoes - Insert) " + Name + ": Licitação já capturada anteriormente at {0}", LogPath);
                            }
                        }
                        catch (Exception e)
                        {
                            RService.Log("Exception (GetLicitacoes - Insert) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (web.FindElement(By.Id("g-recaptcha-response")) != null)
                        RService.Log("Exception (GetLicitacoes) " + Name + ": Encontrado recaptcha! at {0}", LogPath);

                    RService.Log("Exception (GetLicitacoes) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
                }
            }
        }

        private static void GetArquivos(Licitacao licitacao)
        {
            if (!Directory.Exists(PathEditais))
            {
                Directory.CreateDirectory(PathEditais);
            }

            if (web != null)
                web.Quit();

            var driver = ChromeDriverService.CreateDefaultService();
            driver.HideCommandPromptWindow = true;
            var op = new ChromeOptions();
            op.AddUserProfilePreference("download.default_directory", PathEditais);
            web = new ChromeDriver(driver, new ChromeOptions(), TimeSpan.FromSeconds(300));
            web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
            wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));

            web.Navigate().GoToUrl(licitacao.LinkEdital);

            try
            {
                var fileLinks = web.FindElements(By.TagName("td"))
                                    .Where(l => l.GetAttribute("headers") == "DOWNLOAD2")
                                    .ToList();

                if (fileLinks.Count > 0)
                {
                    int count = 1;
                    foreach (var fileLink in fileLinks)
                    {
                        fileLink.Click();

                        //if (IsElementPresent(web, By.XPath("//*[@id=\"apex_dialog_" + count + "\"]/iframe")))
                        //{
                        //    fileLink.Click();
                        //}

                        string captchaToken = GetCaptchaToken(Constants.TCERS_DOCUMENTSITEKEY, web.Url);
                        web.SwitchTo().Frame(web.FindElement(By.XPath("//*[@id=\"apex_dialog_" + count + "\"]/iframe")));
                        string fileName = web.FindElement(By.XPath("//*[@id=\"P100_NM_ARQUIVO_DISPLAY\"]")).Text.Split('(')[0].Trim();
                        ((IJavaScriptExecutor)web).ExecuteScript(string.Format("document.getElementById('g-recaptcha-response').value='{0}'", captchaToken));
                        web.FindElement(By.XPath("//*[@id=\"P100_DOWNLOAD\"]")).Click();
                        Thread.Sleep(10000);
                        CreateArquivo(licitacao, fileName);
                        count++;
                        web.FindElement(By.TagName("html")).SendKeys(Keys.Escape);
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetArquivos) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
            }
        }

        private static void CreateArquivo(Licitacao licitacao, string filename)
        {
            try
            {
                if (File.Exists(PathEditais + filename))
                {
                    if (LicitacaoArquivoController.CreateLicitacaoArquivo(Name, licitacao, PathEditais, filename, web.Manage().Cookies.AllCookies))
                    {
                        RService.Log("(CreateArquivo) " + Name + ": Arquivo inserido com sucesso para licitação " + licitacao.Num + " at {0}", LogPath);
                    }

                    File.Delete(PathEditais + filename);
                }
                else
                {
                    RService.Log("Exception (CreateArquivo) " + Name + ": Download do arquivo " + filename + " não foi concluído com sucesso at {0}", LogPath);
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateArquivo) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
            }
        }

        private static string GetCaptchaToken(string siteKey, string siteUrl)
        {
            string captchaToken = string.Empty;
            string requestUrl = string.Format(Constants.TWOCAPTCHA_POST, Constants.TWOCAPTCHA_APIKEY, siteKey, siteUrl);

            try
            {
                WebRequest request = WebRequest.Create(requestUrl);

                using (WebResponse response = request.GetResponse())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string readerResponse = reader.ReadToEnd();

                    if (readerResponse.Length < 3)
                    {
                        captchaToken = string.Empty;
                        return captchaToken;
                    }
                    else
                    {
                        if (readerResponse.Substring(0, 3) == "OK|")
                        {
                            string captcha = readerResponse.Remove(0, 3);

                            for (int i = 0; i < 24; i++)
                            {
                                WebRequest getAnswer = WebRequest.Create(string.Format(Constants.TWOCAPTCHA_GET, Constants.TWOCAPTCHA_APIKEY, captcha));

                                using (WebResponse answerResp = getAnswer.GetResponse())
                                using (StreamReader answerReader = new StreamReader(answerResp.GetResponseStream()))
                                {
                                    string answerResponse = answerReader.ReadToEnd();

                                    if (answerResponse.Length < 3)
                                    {
                                        captchaToken = string.Empty;
                                    }
                                    else
                                    {
                                        if (answerResponse.Substring(0, 3) == "OK|")
                                        {
                                            captchaToken = answerResponse.Remove(0, 3);
                                            return captchaToken;
                                        }
                                        else if (answerResponse != "CAPCHA_NOT_READY")
                                        {
                                            captchaToken = answerResponse;
                                            return captchaToken;
                                        }
                                    }
                                }

                                Thread.Sleep(10000);
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                RService.Log("Exception (GetCaptchaToken) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }

            return captchaToken;
        }

        private static Licitacao CreateLicitacao(string link, string situacao)
        {
            Licitacao licitacao = new Licitacao();

            try
            {
                var title = web.FindElement(By.XPath("//*[@id=\"R12145936251620929\"]/div[1]/div[1]/ul/li[3]/span")).Text;

                var modalidade = HandleModalidade(GetModalidade(title));
                var numero = GetNumero(title).Trim();

                if (numero.Contains("ç  13.303/16  "))
                    numero = numero.Replace("ç  13.303/16  ", "");

                var orgao = web.FindElement(By.XPath("//*[@id=\"report_38717872527149671_catch\"]/dl/dd[1]")).Text.Split('-')[1].Trim();
                var objeto = web.FindElement(By.XPath("//*[@id=\"report_38717872527149671_catch\"]/dl/dd[2]")).Text;

                var dataEntrega = string.Empty;
                var dataAbertura = string.Empty;

                if (web.FindElement(By.XPath("//*[@id=\"report_38717872527149671_catch\"]/dl/dd[3]")).Text.Contains("a"))
                {
                    dataEntrega = web.FindElement(By.XPath("//*[@id=\"report_38717872527149671_catch\"]/dl/dd[3]")).Text.Split('a')[0].TrimEnd();
                    dataAbertura = web.FindElement(By.XPath("//*[@id=\"report_38717872527149671_catch\"]/dl/dd[3]")).Text.Split('a')[1].TrimStart();
                }
                else
                {
                    dataAbertura = web.FindElement(By.XPath("//*[@id=\"report_38717872527149671_catch\"]/dl/dd[3]")).Text.Trim();
                }

                licitacao.AberturaData = DateHandle.Parse(dataAbertura, "dd/MM/yyyy-hh:mm");
                licitacao.Departamento = orgao;
                licitacao.DigitacaoUsuario = 43;
                licitacao.EntregaData = string.IsNullOrEmpty(dataEntrega) ? null : DateHandle.Parse(dataEntrega, "dd/MM/yyyy-hh:mm");
                licitacao.Estado = Constants.TCERS_ESTADO;
                licitacao.EstadoFonte = Constants.TCERS_ESTADO_FONTE;
                licitacao.IdFonte = Constants.TCERS_ID_FONTE;
                licitacao.LinkEdital = link;
                licitacao.LinkSite = Constants.TCERS_LINK;
                licitacao.Lote = Lote;
                HandleModalidade(licitacao, modalidade);
                licitacao.Num = numero;
                licitacao.Objeto = objeto;
                licitacao.Orgao = Orgaos.Exists(o => o.Nome == orgao) ?
                                        Orgaos.First(o => o.Nome == orgao) :
                                        OrgaoRepository.CreateOrgao(orgao, Constants.TCERS_ESTADO_FONTE);
                licitacao.Situacao = situacao;
                var cidade = HandleCidade(orgao);
                licitacao.Cidade = cidade.Item1;
                licitacao.CidadeFonte = cidade.Item2;
                var idLicFonte = Regex.Match(link, @"RETORNO:\d+");
                licitacao.IdLicitacaoFonte = long.Parse(idLicFonte.Value.Split(':')[1]) * 32;
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacao) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
            }

            return licitacao;
        }

        private static void HandleModalidade(Licitacao licitacao, string modalidade)
        {
            Modalidade modal = NameToModalidade.ContainsKey(StringHandle.RemoveAccent(modalidade.ToUpper())) ?
                                            NameToModalidade[StringHandle.RemoveAccent(modalidade.ToUpper())] : null;

            if (modal == null)
            {
                switch (modalidade.Trim())
                {
                    case "Processo de Dispensa Eletrônica":
                        modal = NameToModalidade["DISPENSA DE LICITACAO"];
                        break;
                    case "Licitação Lei ./ Presencial":
                        modal = NameToModalidade["PREGAO PRESENCIAL"];
                        break;
                    case "Licitação Lei ./ Eletrônico":
                        modal = NameToModalidade["PREGAO ELETRONICO"];
                        break;
                    default:
                        modal = NameToModalidade["Licitação"];
                        break;
                }
            }

            licitacao.Modalidade = modal;
        }

        private static Tuple<string, int> HandleCidade(string orgao)
        {
            //trata o valor do órgão para remover o 'PM' no começo
            if (orgao.StartsWith("PM DE"))
                orgao = orgao.Replace("PM DE ", "");
            else if (orgao.StartsWith("PM"))
                orgao = orgao.Replace("PM ", "");

            foreach (var cid in Cidades)
            {
                if (orgao.Equals(cid.Key))
                    return new Tuple<string, int>(cid.Key, (int)cid.Value);

                if (!orgao.Contains("GRANDENSE") && !orgao.Contains("ESTADO") && orgao.Contains(cid.Key))
                    return new Tuple<string, int>(cid.Key, (int)cid.Value);
            }

            return new Tuple<string, int>("PORTO ALEGRE", 7994);
        }

        private static string HandleModalidade(string modalidade)
        {
            if (modalidade.Contains("Chamamento"))
                return "CHAMAMENTO PÚBLICO";
            else if (modalidade.Contains("Tomada de preços"))
                return "TOMADA DE PREÇO";
            else if (modalidade.Contains("RDC"))
                return "RDC";
            else if (modalidade.Contains("Convite"))
                return "CARTA CONVITE";
            else
                return modalidade.Remove(modalidade.Length - 4, 4);
        }

        private static string GetNumero(string title)
        {
            return Regex.Replace(title, "[a-zA-Zãáàéêôõíú]", "");
        }

        private static string GetModalidade(string title)
        {
            return Regex.Replace(title, "\\d+", "");
        }

        private static void CheckReCaptcha(string html)
        {
            if (html.Contains("g-recaptcha-response"))
            {
                RService.Log("(CheckReCaptcha) " + Name + ": Encontrado recaptcha V2! at {0}", LogPath);

                while (html.Contains("g-recaptcha-response"))
                {
                    string captchaToken = GetCaptchaToken(Constants.TCERS_SITEKEY, web.Url);
                    ((IJavaScriptExecutor)web).ExecuteScript(string.Format("document.getElementById('g-recaptcha-response').value='{0}'", captchaToken));
                    web.FindElement(By.XPath("//*[@id=\"P0_CONTINUAR\"]")).Click();
                    html = web.PageSource;
                }
            }
            //else
            //{
            //    RService.Log("(CheckReCaptcha) " + Name + ": NÃO foi encontrado recaptcha V2! at {0}", LogPath);
            //}
        }

        #endregion
    }
}