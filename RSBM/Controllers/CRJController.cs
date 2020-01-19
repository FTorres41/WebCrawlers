using OpenQA.Selenium.Chrome;
//using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using RSBM.Util;
using RSBM.Models;
using RSBM.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Net;
using System.Diagnostics;
using RSBM.WebUtil;
using System.Threading;

namespace RSBM.Controllers
{
    public class CRJController
    {
        #region Variáveis

        public static string Name { get; } = Constants.CRJ_NOME;

        private static ConfigRobot config;
        private static ChromeDriver web;
        private static WebDriverWait wait;
        private static LicitacaoRepository repo;

        private static Dictionary<string, Modalidade> NameToModalidade;
        private static Dictionary<string, int?> Cidades;
        private static List<Orgao> Orgaos;
        public static string PathEditais { get; } = Path.GetTempPath() + DateTime.Now.ToString("yyyyMM") + Name + "\\";
        private static Lote Lote;

        public static HashSet<long> AlreadyInserted { get; private set; }

        private static int NumLicitacoes = 0;
        private static string LogPath { get; } = Path.GetTempPath() + Name + ".txt";
        private static string mensagemErro;

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
                RService.Log("Exception (InitCallBack) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
            }

            RService.Log("Finished " + Name + " at {0}", LogPath);

            EmailHandle.SendMail(LogPath, Name);
        }

        private static void Init()
        {

            try
            {
                //var loadedDriver = WebDriverChrome.LoadWebDriver(Name);
                //web = loadedDriver.Item1;
                //wait = loadedDriver.Item2;
                if (web != null)
                    web.Quit();

                var driver = ChromeDriverService.CreateDefaultService();
                driver.HideCommandPromptWindow = true;
                var op = new ChromeOptions();
                op.AddUserProfilePreference("download.default_directory", PathEditais);
                web = new ChromeDriver(driver, new ChromeOptions(), TimeSpan.FromSeconds(300));
                web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
                wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));

                RService.Log("(Init) " + Name + ": Acessando a homepage... at {0}", LogPath);

                InitializeUtils();

                Navigate();
            }
            catch (Exception e)
            {
                RService.Log("Exception (Init) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
            }
        }

        private static void Navigate()
        {
            int licitIndex = 0, rowCount = 0;

            try
            {
                do
                {
                    web.Navigate().GoToUrl(Constants.CRJ_HOST); //para obter o cookie do portal

                    if (licitIndex == 0)
                        RService.Log("(Navigate) " + Name + ": Acessando as licitações do dia... at {0}", LogPath);

                    web.FindElementById("pesquisarLicitacoesFuturas").Click();

                    Thread.Sleep(3000);

                    var select = new SelectElement(web.FindElementByName("dataTable_length"));

                    select.SelectByIndex(4);

                    Thread.Sleep(8000);
                    
                    var rowArray = web.FindElements(By.TagName("tr")).Where(x => x.GetAttribute("class") == "odd" || x.GetAttribute("class") == "even").ToArray();

                    rowCount = rowArray.Count();

                    if (licitIndex == 0)
                        RService.Log("(Navigate) " + Name + ": Encontradas " + rowCount + " licitações hoje at {0}", LogPath);

                    web.FindElements(By.TagName("tr"))
                                .Where(x => x.GetAttribute("class") == "odd" || x.GetAttribute("class") == "even")
                                .ToArray()[licitIndex].Click();

                    Licitacao licitacao = GetLicitacao();

                    if (licitacao != null && !repo.Exists(licitacao.IdLicitacaoFonte.ToString()))
                    {
                        repo.Insert(licitacao);
                        NumLicitacoes++;
                        RService.Log("(GetLicitacoes) " + Name + ": Licitação " + licitacao.IdLicitacaoFonte + " inserida com sucesso at {0}", LogPath);

                        GetFiles(licitacao);
                    }
                    else
                    {
                        RService.Log("Exception (GetLicitacoes) " + Name + ": Licitação não inserida. Motivo(s): " + 
                                                    (string.IsNullOrEmpty(mensagemErro) ? "Licitação já inserida" : mensagemErro) + " at {0}", LogPath);
                    }

                    licitIndex++;

                } while (licitIndex < rowCount && rowCount != 0);
            }
            catch (Exception e)
            {
                RService.Log("Exception (Navigate): " + e.Message + " / " + e.StackTrace + " at {0}", LogPath);
            }
        }

        private static void InitializeUtils()
        {
            Orgaos = OrgaoRepository.FindByUF(Constants.CRJ_ESTADO_FONTE);
            NameToModalidade = ModalidadeController.GetNameToModalidade();
            Cidades = CidadeController.GetNameToCidade(Constants.CRJ_ESTADO_FONTE);
            repo = new LicitacaoRepository();
            Lote = LoteController.CreateLote(43, Constants.CRJ_ID_FONTE);
            //AlreadyInserted = LicitacaoController.GetAlreadyInserted(Constants.CRJ_HOST);
        }

        private static Licitacao GetLicitacao()
        {
            Licitacao licitacao = new Licitacao();

            licitacao.Lote = Lote;
            licitacao.IdFonte = Constants.CRJ_ID_FONTE;
            var x = web.FindElement(By.Id("idLicitacao")).GetAttribute("value");
            licitacao.IdLicitacaoFonte = long.Parse(x);
            licitacao.LinkSite = Constants.CRJ_HOST;
            licitacao.LinkEdital = Constants.CRJ_LINK_LICITACAO;
            licitacao.EstadoFonte = Constants.CRJ_ESTADO_FONTE;
            licitacao.Estado = Constants.CRJ_ESTADO;
            licitacao.DigitacaoUsuario = 43;

            var title = web.FindElement(By.XPath("//*[@id=\"panelId\"]/div[1]/div/div/p")).Text.Trim();
            var dept = web.FindElement(By.XPath("//*[@id=\"panelId\"]/div[2]/div/div/p[1]")).Text.Trim();
            var situation = web.FindElement(By.XPath("//*[@id=\"panelId\"]/div[3]/div[1]/div/p")).Text.Trim();
            var modal = web.FindElement(By.XPath("//*[@id=\"panelId\"]/div[3]/div[2]/div/p")).Text.Trim();
            var publish = web.FindElement(By.XPath("//*[@id=\"panelId\"]/div[4]/div[1]/div/p")).Text.Trim();
            var opening = web.FindElement(By.XPath("//*[@id=\"panelId\"]/div[4]/div[2]/div/p")).Text.Trim();

            licitacao.Num = title.Split('-')[0].Trim();
            licitacao.Processo = title.Split('-')[0].Trim();
            licitacao.Objeto = title + " Obs.: Os arquivos do edital podem ser obtidos no site do Compras Rio de Janeiro.";
            licitacao.Departamento = dept;
            licitacao.Situacao = situation;
            licitacao.Modalidade = NameToModalidade.ContainsKey(StringHandle.RemoveAccent(modal.ToUpper())) ?
                                                        NameToModalidade[StringHandle.RemoveAccent(modal.ToUpper())] : null;
            if (licitacao.Modalidade == null && licitacao.Processo.Contains("PE"))
            {
                licitacao.Modalidade = NameToModalidade["PREGAO ELETRONICO"];
            }

            var orgao = dept.Split('-')[1].Trim();
            licitacao.Orgao = Orgaos.Exists(o => o.Nome == orgao) ?
                                    Orgaos.First(o => o.Nome == orgao) :
                                    OrgaoRepository.CreateOrgao(orgao, Constants.CRJ_ESTADO_FONTE);
            licitacao.EntregaData = DateHandle.Parse(publish, "dd/MM/yyyy-hh:mm");
            licitacao.AberturaData = DateHandle.Parse(opening, "dd/MM/yyyy-hh:mm");
            licitacao.Cidade = Constants.CRJ_ESTADO;
            licitacao.CidadeFonte = Constants.CRJ_CIDADE_FONTE;

            return LicitacaoController.IsValid(licitacao, out mensagemErro) ? licitacao : null;
        }

        private static Licitacao CreateLicitacao(string link)
        {
            Licitacao licitacao = new Licitacao();

            try
            {
                web.Navigate().GoToUrl(Constants.CRJ_HOST + link);

                licitacao.Lote = Lote;
                licitacao.IdFonte = Constants.CRJ_ID_FONTE;
                licitacao.IdLicitacaoFonte = long.Parse(link.Split('=')[2]);
                licitacao.LinkSite = Constants.CRJ_HOST;
                licitacao.LinkEdital = Constants.CRJ_HOST + link;
                licitacao.EstadoFonte = Constants.CRJ_ESTADO_FONTE;
                licitacao.Estado = Constants.CRJ_ESTADO;
                licitacao.DigitacaoUsuario = 43;

                var licitInfo = web.FindElements(By.ClassName("cx_clara"));
                licitacao.Num = licitInfo[0].Text.Split('-')[0].Trim();
                licitacao.Processo = licitInfo[0].Text.Split('-')[0].Trim();
                licitacao.Objeto = licitInfo[0].Text.Split('-')[2].Trim() + " Obs.: Os arquivos do edital podem ser obtidos no site do Compras Rio de Janeiro.";
                licitacao.Modalidade = NameToModalidade.ContainsKey(StringHandle.RemoveAccent(licitInfo[1].Text.ToUpper())) ?
                                                        NameToModalidade[StringHandle.RemoveAccent(licitInfo[1].Text.ToUpper())] : null;
                if (licitacao.Modalidade == null && licitacao.Processo.Contains("PE"))
                {
                    licitacao.Modalidade = NameToModalidade["PREGAO ELETRONICO"];
                }
                licitacao.Departamento = licitInfo[2].Text.Trim();
                var orgao = licitInfo[2].Text.Split('-')[1].Trim();
                licitacao.Orgao = Orgaos.Exists(o => o.Nome == orgao) ?
                                        Orgaos.First(o => o.Nome == orgao) :
                                        OrgaoRepository.CreateOrgao(orgao, Constants.CRJ_ESTADO_FONTE);
                licitacao.EntregaData = DateHandle.Parse(licitInfo[3].Text.Replace("De ", "").Remove(19, 24), "dd/MM/yyyy-hh:mm");
                licitacao.AberturaData = DateHandle.Parse(licitInfo[4].Text, "dd/MM/yyyy-hh:mm");
                licitacao.Situacao = licitInfo[5].Text.Trim();
                licitacao.Cidade = Constants.CRJ_ESTADO;
                licitacao.CidadeFonte = Constants.CRJ_CIDADE_FONTE;
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacoes) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
            }

            return licitacao;
        }

        #region GetFiles (not working)

        private static void GetFiles(Licitacao licitacao)
        {
            try
            {
                if (!Directory.Exists(PathEditais))
                    Directory.CreateDirectory(PathEditais);

                web.FindElement(By.Id("showEdital")).Click();
                Thread.Sleep(3000);

                var html = web.PageSource;

                var fileRx = new Regex(Constants.CRJ_FILELINK_REGEX);

                if (fileRx.IsMatch(html))
                {
                    var matches = fileRx.Matches(html);
                    var fileLinks = HandleMatchedContent(matches);

                    foreach (var link in fileLinks)
                    {
                        var fileName = link.Split('/')[9];
                        WebHandle.DownloadFileWebRequest(link, PathEditais, fileName);

                        if (LicitacaoArquivoController.CreateLicitacaoArquivo(Name, licitacao, PathEditais, fileName, web.Manage().Cookies.AllCookies))
                        {
                            RService.Log("(GetFiles) " + Name + ": Arquivo " + fileName + " inserido com sucesso para licitação " + licitacao.IdLicitacaoFonte + " at {0}", LogPath);
                        }
                    }
                }

            }
            catch (Exception e)
            {
                RService.Log("Exception (GetFiles) " + Name + ": " + e.Message + " / " + e.StackTrace + " at {0}", LogPath);
            }
        }

        private static List<string> HandleMatchedContent(MatchCollection matches)
        {
            List<string> links = new List<string>();

            foreach (var match in matches)
            {
                var value = match.ToString().Replace("\"><i class=\"material-icons\"", "").Replace("href=\"", "");

                if (!links.Contains(value))
                {
                    links.Add(value);
                }
            }

            return links;
        }

        #endregion

        #endregion
    }
}