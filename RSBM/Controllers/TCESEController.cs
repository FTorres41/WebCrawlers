using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
//using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Support.UI;
using RSBM.Models;
using RSBM.Repository;
using RSBM.Util;
using RSBM.WebUtil;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RSBM.Controllers
{
    class TCESEController
    {
        #region Declaração de variáveis
        private static WebDriverWait wait;
        private static ChromeDriver web;

        private static ConfigRobot config;
        private static LicitacaoRepository repo;
        private static Lote Lote;
        private static List<Licitacao> licitacoes = new List<Licitacao>();

        private static Dictionary<string, Modalidade> NameToModalidade;
        private static Dictionary<string, Orgao> NameToOrgao;
        private static Dictionary<string, int?> Cidades;

        public static string name { get; } = "TCESE";
        private static int numLicitacoes = 0;
        private static string logPath = Path.GetTempPath() + name + ".txt";
        private static string mensagemErro = string.Empty;
        private static bool temLicitacao = false;
        #endregion

        #region Métodos e funções
        internal static void InitCallBack(object state)
        {
            try
            {
                config = ConfigRobotController.FindByName(name);

                if (config.Active == 'Y')
                {
                    if (File.Exists(logPath))
                        File.Delete(logPath);

                    config.Status = 'R';
                    ConfigRobotController.Update(config);

                    using (FileStream fs = File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire")) { }
                    Init();

                    config = ConfigRobotController.FindByName(name);

                    config.NumLicitLast = numLicitacoes;
                    RService.Log(name + " find " + numLicitacoes + " novas licitações at {0}", logPath);
                    numLicitacoes = 0;

                    config.LastDate = DateTime.Now;
                }

                RService.ScheduleMe(config);

                config.Status = 'W';
                ConfigRobotController.Update(config);

                File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire");
            }
            catch (Exception e)
            {
                RService.Log("Exception (InitCallBack) " + name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", logPath);
            }

            RService.Log("Finished " + name + " at {0}", logPath);

            EmailHandle.SendMail(logPath, name);
        }

        private static void Init()
        {
            RService.Log("(Init) " + name + ": Começando o processamento.. at {0}", logPath);

            try
            {
                Lote = LoteController.CreateLote(43, 1250);
                Cidades = CidadeController.GetNameToCidade(Constants.TCESE_UF);
                NameToOrgao = OrgaoController.GetNomeUfToOrgao();
                NameToModalidade = ModalidadeController.GetNameToModalidade();

                for (int i = 0; i < Constants.TCESE_MUN_CODE.Count; i++)
                {
                    HandleLicitacoes(i);
                }

                //repo = new LicitacaoRepository();
                //repo.Insert(licitacoes);
            }
            catch (Exception e)
            {
                RService.Log("Exception (Init) " + name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", logPath);
            }
        }

        private static void HandleLicitacoes(int index)
        {
            RService.Log("(HandleLicitacoes) " + name + ": Consultando licitações da " + Constants.TCESE_MUN_NAME[index] + " at {0}", logPath);

            //LoadWebDriver();
            LoadDriver();

            try
            {
                string urlMunicipio = string.Format(Constants.TCESE_MUN_PAGE, Constants.TCESE_MUN_CODE[index], Constants.TCESE_MUN_NAME[index].Replace(" ", "%20"));
                temLicitacao = false;

                web.Navigate().GoToUrl(urlMunicipio);

                var licits = web.FindElements(By.TagName("a")).Where(x => x.GetAttribute("innerText").Contains("Ano/Número:"));

                repo = new LicitacaoRepository();

                foreach (var licit in licits)
                {
                    var dates = StringHandle.GetMatches(licit.Text, @"\d+/\d+/\d+");

                    if (Convert.ToDateTime(dates[0].Value) > DateTime.Today)
                    {
                        temLicitacao = true;
                        var licitLink = licit.GetAttribute("href");
                        Licitacao licitacao = CreateLicitacao(licitLink, index);

                        if (licitacao != null)
                        {
                            if (!LicitacaoController.Exists(licitacao.IdLicitacaoFonte.ToString()))
                            {
                                repo.Insert(licitacao);
                                //licitacoes.Add(licitacao);

                                RService.Log("(HandleLicitacoes) " + name + ": Licitação Nº" + licitacao.IdLicitacaoFonte + " inserida com sucesso at {0}", logPath);
                                numLicitacoes++;
                            }
                        }
                        else
                        {
                            RService.Log("Exception (CreateLicitacao) " + name + ": Licitação não salva. Motivo: " + mensagemErro + " at {0}", logPath);
                        }
                    }
                }

                if (!temLicitacao)
                    RService.Log("(HandleLicitacoes) " + name + ": " + Constants.TCESE_MUN_NAME[index] + " não possui licitações novas at {0}", logPath);
            }
            catch (Exception e)
            {
                RService.Log("Exception (HandleLicitacoes) " + name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", logPath);
            }
            finally
            {
                if (web != null)
                    web.Close();
            }
        }
      
        private static Licitacao CreateLicitacao(string licitLink, int index)
        {
            ChromeDriver webLicit = null;

            if (web != null)
                web.Quit();

            var driver = ChromeDriverService.CreateDefaultService();
            driver.HideCommandPromptWindow = true;
            var op = new ChromeOptions();
            webLicit = new ChromeDriver(driver, new ChromeOptions(), TimeSpan.FromSeconds(300));
            webLicit.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
            wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));

            Licitacao licitacao = new Licitacao();
            licitacao.IdLicitacaoFonte = Convert.ToInt64(licitLink.Split('=')[1].Split('&')[0]);
            licitacao.IdFonte = 1250;
            licitacao.Estado = Constants.TCESE_ESTADO;
            licitacao.EstadoFonte = Constants.TCESE_UF;
            licitacao.LinkEdital = licitLink;
            licitacao.LinkSite = Constants.TCESE_HOST;
            licitacao.DigitacaoUsuario = 43;
            licitacao.Lote = Lote;
            //licitacao.DigitacaoData = null;
            //licitacao.ProcessamentoData = null;

            try
            {
                webLicit.Navigate().GoToUrl(licitLink);
                string licitText = webLicit.FindElement(By.XPath("//*[@id=\"aspnetForm\"]/table[3]/tbody/tr/td[2]")).Text;
                var textInfo = new CultureInfo("pt-BR").TextInfo;

                string dataAb = webLicit.FindElement(By.Id("ctl00_ContentPlaceHolder1_lblCte_DtLicitacaoMIA")).Text;
                licitacao.AberturaData = Convert.ToDateTime(StringHandle.GetMatches(dataAb, @"\d+/\d+/\d+")[0].ToString());
                string dataEnt = webLicit.FindElement(By.Id("ctl00_ContentPlaceHolder1_lblCto_DtModificacaoMIA")).Text;
                licitacao.EntregaData = Convert.ToDateTime(dataEnt);

                licitacao.Cidade = Constants.TCESE_MUN_NAME[index].Replace("PREFEITURA MUNICIPAL DE ", "");
                licitacao.CidadeFonte = Cidades.ContainsKey(licitacao.Cidade) ? Cidades[licitacao.Cidade] : CityUtil.GetCidadeFonte(licitacao.Cidade, Cidades);

                licitacao.Departamento = Constants.TCESE_MUN_NAME[index];
                licitacao.Endereco = webLicit.FindElement(By.Id("ctl00_ContentPlaceHolder1_lblCte_LocalMIA")).Text;
                string modal = StringHandle.GetMatches(licitText, @"Modalidade:(.*?)Número")[0].ToString().Split(':')[1].Replace(" Número", "").Trim();
                licitacao.Modalidade = NameToModalidade.ContainsKey(StringHandle.RemoveAccent(modal.ToUpper())) ? NameToModalidade[StringHandle.RemoveAccent(modal.ToUpper())] : null;
                licitacao.Num = StringHandle.GetMatches(licitText, "Ano:(.*)")[0].ToString().Replace("Ano:", "").Replace("\r", "");
                licitacao.Objeto = webLicit.FindElement(By.Id("ctl00_ContentPlaceHolder1_lblCto_ResumoMIA")).Text;
                licitacao.Observacoes = webLicit.FindElement(By.Id("ctl00_ContentPlaceHolder1_lblCto_ConteudoMIA")).Text;
                licitacao.Orgao = OrgaoController.GetOrgaoByNameAndUf(textInfo.ToTitleCase(licitacao.Departamento) + ":SE", NameToOrgao);
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacao) " + name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", logPath);
            }
            finally
            {
                if (webLicit != null)
                    webLicit.Close();
            }

            return LicitacaoController.IsValid(licitacao, out mensagemErro) ? licitacao : null;
        }
        #region OLD
        //private static PhantomJSDriver LoadWebDriver()
        //{
        //    PhantomJSDriver web = new PhantomJSDriver();

        //    try
        //    {
        //        if (web != null)
        //            web.Quit();

        //        var driver = PhantomJSDriverService.CreateDefaultService();
        //        driver.HideCommandPromptWindow = true;

        //        var options = new PhantomJSOptions();

        //        web = new PhantomJSDriver(driver, options, TimeSpan.FromSeconds(120));
        //        web.Manage().Window.Maximize();
        //        web.Manage().Timeouts().SetPageLoadTimeout(TimeSpan.FromSeconds(120));
        //        web.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(120));

        //        wait = new WebDriverWait(web, TimeSpan.FromSeconds(120));

        //    }
        //    catch (Exception e)
        //    {
        //        RService.Log("Exception (Reload) " + name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + name + ".txt");

        //        if (web != null)
        //            web.Quit();
        //    }

        //    return web;
        //}
        #endregion

        private static void LoadDriver()
        {
            //Tuple<ChromeDriver, WebDriverWait> loadDriver = WebDriverChrome.LoadWebDriver(typeof(TCESEController).Name);
            //web = loadDriver.Item1;
            //wait = loadDriver.Item2;
            if (web != null)
                web.Quit();

            var driver = ChromeDriverService.CreateDefaultService();
            driver.HideCommandPromptWindow = true;

            web = new ChromeDriver(driver, new ChromeOptions(), TimeSpan.FromSeconds(300));
            web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
            wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));
        }

        private static Tuple<ChromeDriver, WebDriverWait> LoadDriverScope()
        {
            Tuple<ChromeDriver, WebDriverWait> loadDriver;
            return loadDriver = WebDriverChrome.LoadWebDriver(typeof(TCESEController).Name);
        }
        #endregion
    }
}
