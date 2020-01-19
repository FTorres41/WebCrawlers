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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace RSBM.Controllers
{
    class TCEPIController
    {
        #region Declaração de variáveis
        public static string name { get; } = "TCEPI";
        private static string logPath = Path.GetTempPath() + name + ".txt";
        public static string pathEditais { get; } = Path.GetTempPath() + name + DateTime.Now.ToString("yyyyMM") + "/";

        private static List<Licitacao> licitacoes = new List<Licitacao>();
        private static ConfigRobot config;
        private static LicitacaoRepository repo;
        private static Lote lote;

        private static Dictionary<string, Modalidade> NameToModalidade;
        private static Dictionary<string, Orgao> NameToOrgao;
        private static Dictionary<string, int?> Cidades;

        private static List<string> licitNums;
        private static int numLicitacoes = 0, currentPage = 1;
        private static string mensagemErro;

        private static ChromeDriver web;
        //private static PhantomJSDriver web;
        private static WebDriverWait wait;
        private static HashSet<long> alreadyInserted;
        #endregion

        #region Métodos e funções
        internal static void InitCallBack(object state)
        {
            //Busca as configurações de execução do robô no banco.
            config = ConfigRobotController.FindByName(name);

            try
            {
                if (config.Active == 'Y')
                {
                    //Deleta o log antigo para criar o novo.
                    if (File.Exists(Path.GetTempPath() + name + ".txt"))
                        File.Delete(Path.GetTempPath() + name + ".txt");

                    config.Status = 'R';
                    ConfigRobotController.Update(config);
                    using (FileStream fs = File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire")) { }

                    Init();

                    //Atualiza as informações do robô de acordo com a última execução.
                    config = ConfigRobotController.FindByName(name);
                    config.NumLicitLast = numLicitacoes;
                    RService.Log(name + " find " + numLicitacoes + " at {0}", logPath);
                    numLicitacoes = 0;

                    config.Status = 'W';
                    config.LastDate = DateTime.Now;
                    ConfigRobotController.Update(config);
                }

                //Agenda a próxima execução do robô.
                RService.ScheduleMe(config);

                //Arquivo que indica ao manager que é hora de atualizar as informações.
                File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire");
            }
            catch (Exception e)
            {
                RService.Log("Exception (InitCallBack) " + name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", logPath);
            }
            finally
            {
                //Finaliza e deleta as instâncias do navegador e da pasta
                if (web != null)
                    web.Close();

                if (Directory.Exists(pathEditais))
                    Directory.Delete(pathEditais);
            }

            RService.Log("Finished " + name + " at {0}", logPath);

            //Envia o log por e-mail
            EmailHandle.SendMail(logPath, name);
        }

        private static void Init()
        {
            RService.Log(name + ": Começando o processamento... at {0}", logPath);

            try
            {
                //Inicializa as listas e variáveis que serão usadas pelo robô
                currentPage = 1;
                lote = LoteController.CreateLote(43, 1442);
                Cidades = CidadeController.GetNameToCidade(Constants.TCEPI_UF);
                NameToOrgao = OrgaoController.GetNomeUfToOrgao();
                NameToModalidade = ModalidadeController.GetNameToModalidade();
                alreadyInserted = LicitacaoController.GetAlreadyInserted(1442, DateTime.Today.AddMonths(-3));

                GetLicitacoes();
            }
            catch (Exception e)
            {
                RService.Log("Exception (Init) " + name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", logPath);
            }
        }

        //Método para pegar as licitações do site do TCE-PI
        private static void GetLicitacoes()
        {
            try
            {
                //var webdriver = WebDriverChrome.LoadWebDriver(name);
                //var webdriver = WebDriverPhantomJS.LoadWebDriver(name, web);
                //web = webdriver.Item1;
                //wait = webdriver.Item2;

                if (web != null)
                    web.Quit();

                var driver = ChromeDriverService.CreateDefaultService();
                driver.HideCommandPromptWindow = true;

                var op = new ChromeOptions();
                op.AddUserProfilePreference("download.default_directory", TCEPIController.pathEditais);

                web = new ChromeDriver(driver, op, TimeSpan.FromSeconds(300));
                web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
                wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));

                web.Navigate().GoToUrl(Constants.TCEPI_SITE);
                wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@id=\"formMural:btnPesquisar\"]/span[2]")));
                web.FindElement(By.XPath("//*[@id=\"formMural:btnPesquisar\"]/span[2]")).Click();
                Thread.Sleep(3000);

                /*Laço que se repete até o fim das licitações. O fim é demarcado pela quantidade de licitações na lista licitNums:
                 *em cada página o número é 50, menos na última página. Quando o número for diferente de 50, ele termina*/
                do
                {
                    licitNums = new List<string>();

                    RService.Log("(GetLicitacoes) " + name + ": Acessando página " + currentPage + " at {0}", logPath);

                    string pageHtml = web.PageSource.ToString();
                    var licits = StringHandle.GetMatches(pageHtml, @"\?id=\d{6}");

                    //Pega os números das licitações da página em questão
                    foreach (var licit in licits)
                    {
                        string licNum = licit.ToString().Replace("?id=", "");
                        licitNums.Add(licNum);
                    }

                    RService.Log("(GetLicitacoes) " + name + ": Acessando licitações da página " + currentPage + " at {0}", logPath);

                    foreach (string lic in licitNums)
                    {
                        if (!alreadyInserted.Contains(Int64.Parse(lic)))
                        {
                            Licitacao licitacao = CreateLicitacao(lic);
                            if (licitacao != null && !LicitacaoController.Exists(licitacao.IdLicitacaoFonte.ToString()))
                            {
                                repo = new LicitacaoRepository();
                                try
                                {
                                    repo.Insert(licitacao);
                                    RService.Log("(GetLicitacoes) " + name + ": Licitação " + licitacao.IdLicitacaoFonte + " inserida com sucesso at {0}", logPath);
                                    numLicitacoes++;

                                    GetFiles(licitacao);
                                }
                                catch (Exception e)
                                {
                                    RService.Log("Exception (GetLicitacoes) " + name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " / Mensagem de erro: " + mensagemErro + " at {0}", logPath);
                                }
                            }
                        }
                    }

                    //webdriver = WebDriverChrome.LoadWebDriver(name);
                    //web = webdriver.Item1;
                    //wait = webdriver.Item2;

                    web = new ChromeDriver(driver, op, TimeSpan.FromSeconds(300));
                    web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
                    wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));

                    web.Navigate().GoToUrl(Constants.TCEPI_SITE);
                    web.FindElement(By.XPath("//*[@id=\"formMural:btnPesquisar\"]/span[2]")).Click();
                    for (int i = 0; i < currentPage; i++)
                    {
                        wait.Until(ExpectedConditions.ElementExists(By.XPath("//*[@id=\"formListaLic:listaLic_paginator_top\"]/a[3]")));
                        web.FindElement(By.XPath("//*[@id=\"formListaLic:listaLic_paginator_top\"]/a[3]")).Click();
                        Thread.Sleep(10000);
                    }

                    currentPage++;

                } while (licitNums.Count() == 10);
            }
            catch (Exception e)
            {
                StackTrace st = new StackTrace(e, true);
                var frame = st.GetFrame(st.FrameCount - 1);
                var line = frame.GetFileLineNumber();
                RService.Log("Exception (GetLicitacoes) " + name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at line " + line + " at {0}", logPath);
            }
        }

        //Cria a licitação em questão com os respectivos dados
        private static Licitacao CreateLicitacao(string lic)
        {
            Licitacao licitacao = new Licitacao();

            try
            {
                licitacao.DigitacaoUsuario = 43;
                licitacao.Estado = Constants.TCEPI_ESTADO;
                licitacao.EstadoFonte = Constants.TCEPI_UF;
                licitacao.IdLicitacaoFonte = Convert.ToInt64(lic);
                licitacao.IdFonte = 1442;
                licitacao.LinkEdital = string.Format(Constants.TCEPI_LICIT, lic);
                licitacao.LinkSite = Constants.TCEPI_HOST;
                licitacao.Lote = lote;

                web.Navigate().GoToUrl(licitacao.LinkEdital);

                string licitacaoHtmlText = web.PageSource.ToString();

                var helper = StringHandle.GetMatches(licitacaoHtmlText, "negrito\">(.|\n)*?</span");
                licitacao.Processo = helper[1].ToString().Replace("negrito\">", "").Replace("</span", "").Replace("Pregão ", "");
                licitacao.Observacoes = null;

                licitacao.AberturaData = Convert.ToDateTime(helper[2].ToString().Replace("negrito\">", "").Replace("</span", ""));
                licitacao.EntregaData = Convert.ToDateTime(helper[7].ToString().Replace("negrito\">", "").Replace("</span", ""));
                licitacao.ValorMax = helper[4].ToString().Replace("negrito\">", "").Replace("</span", "");

                var num = web.FindElement(By.XPath("//*[@id=\"j_idt23_content\"]/div[1]/div[2]/span"));
                licitacao.Num = num.Text.Split('º')[1].Trim();
                var modalidade = num.Text.Split('º')[0].Replace(" N", "").Trim();
                licitacao.Modalidade = NameToModalidade.ContainsKey(StringHandle.RemoveAccent(modalidade.ToUpper())) ? NameToModalidade[StringHandle.RemoveAccent(modalidade.ToUpper())] : null;

                if (licitacao.Modalidade == null)
                    licitacao.Modalidade = NameToModalidade["PREGAO"];

                var objeto = web.FindElement(By.XPath("//*[@id=\"j_idt23_content\"]/div[4]/div[2]/label"));
                licitacao.Objeto = objeto.Text.Trim();

                var depto = web.FindElement(By.XPath("//*[@id=\"j_idt20_content\"]/div/div[2]/h2"));
                licitacao.Departamento = depto.Text.Trim();
                licitacao.Orgao = OrgaoController.GetOrgaoByNameAndUf(licitacao.Departamento + ":PI", NameToOrgao);

                if (licitacao.Departamento.Contains("P. M. DE "))
                {
                    var textInfo = new CultureInfo("pt-BR").TextInfo;
                    string cidade = licitacao.Departamento.Remove(0, 9);
                    licitacao.Cidade = textInfo.ToTitleCase(cidade);
                    licitacao.CidadeFonte = Cidades.ContainsKey(cidade) ? Cidades[licitacao.Cidade.ToUpper()] : CityUtil.GetCidadeFonte(licitacao.Cidade, Cidades);
                }
                else
                {
                    foreach (var cid in Cidades)
                    {
                        if (licitacao.Objeto.Contains(cid.Key))
                        {
                            licitacao.Cidade = cid.Key;
                            licitacao.CidadeFonte = cid.Value;
                        }
                    }

                    if (string.IsNullOrEmpty(licitacao.Cidade))
                    {
                        licitacao.Cidade = "Teresina";
                        licitacao.CidadeFonte = 5721;
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacao) " + name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", logPath);
            }

            return LicitacaoController.IsValid(licitacao, out mensagemErro) ? licitacao : null;
        }

        //Pega os arquivos (edital e anexos) da licitação e depois os deleta
        private static void GetFiles(Licitacao licitacao)
        {
            try
            {
                //var webdriver = WebDriverChrome.LoadWebDriver(name);
                //web = webdriver.Item1;
                //wait = webdriver.Item2;
                if (web != null)
                    web.Quit();

                var driver = ChromeDriverService.CreateDefaultService();
                driver.HideCommandPromptWindow = true;
                var op = new ChromeOptions();
                op.AddUserProfilePreference("download.default_directory", pathEditais);
                web = new ChromeDriver(driver, new ChromeOptions(), TimeSpan.FromSeconds(300));
                web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
                wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));

                //Cria diretório específico para os arquivos
                if (!Directory.Exists(pathEditais))
                    Directory.CreateDirectory(pathEditais);

                web.Navigate().GoToUrl(licitacao.LinkEdital);

                if (web.PageSource.Contains("Arquivos"))
                {
                    var accordionItems = web.FindElements(By.ClassName("ui-accordion-header"));

                    foreach (var item in accordionItems)
                    {
                        try
                        {
                            if (item.Text.Contains("Arquivos"))
                            {
                                item.Click();
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            RService.Log("Exception (GetFiles/Accordion) " + e.Message + " at {0}", logPath);
                        }
                    }
                }

                var downloadButtons = web.FindElements(By.ClassName("ui-button-text-icon-left"));

                foreach (var button in downloadButtons)
                {
                    try
                    {
                        button.Click();
                        Thread.Sleep(10000);
                    }
                    catch (Exception e)
                    {
                        RService.Log("Exception (GetFiles/FileButton) " + e.Message + " at {0}", logPath);
                    }
                }

                string[] files = Directory.GetFiles(pathEditais);

                foreach (var file in files)
                {
                    string fileName = file.Split('/')[1];
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        #region AWS
                        RService.Log("(GetFiles) " + name + ": Enviando o arquivo para Amazon S3... " + fileName + " at {0}", logPath);

                        if (AWS.SendObject(licitacao, pathEditais, fileName))
                        {
                            LicitacaoArquivo licitacaoArq = new LicitacaoArquivo();
                            licitacaoArq.NomeArquivo = fileName;
                            licitacaoArq.NomeArquivoOriginal = name + DateTime.Now.ToString("yyyyMMddHHmmss");
                            licitacaoArq.NomeArquivoFonte = name;
                            licitacaoArq.Status = 0;
                            licitacaoArq.IdLicitacao = licitacao.Id;

                            LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                            repoArq.Insert(licitacaoArq);

                            if (File.Exists(pathEditais + fileName))
                                File.Delete(pathEditais + fileName);

                            RService.Log("(GetFiles) " + name + ": Arquivo " + fileName + " enviado com sucesso para Amazon S3" + " at {0}", logPath);
                        }
                        else
                        {
                            RService.Log("Exception (GetFiles) " + name + ": Erro ao enviar o arquivo para Amazon (CreateLicitacaoArquivo) {0}", logPath);
                        }

                        #endregion
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetFiles) " + name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " / " + e.InnerException + " at {0}", logPath);
            }
        }
        #endregion
    }
}
