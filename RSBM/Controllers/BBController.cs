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
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace RSBM.Controllers
{
    class BBController
    {
        #region Declaracao de variveis
        private static ChromeDriver web;
        //private static PhantomJSDriver web;
        private static WebDriverWait wait;

        private static List<string> numUFs = new List<string>();
        private static int NumLicitacoes;
        private static int NumCaptcha = 0;
        private static int IdFonte { get; } = 509;

        private static Lote Lote;
        private static ConfigRobot config;
        private static LicitacaoRepository Repo;

        private static Dictionary<string, Orgao> NameToOrgao;
        private static Dictionary<string, Modalidade> NameToModalidade;
        private static Dictionary<string, string> UfToCapital;
        private static Dictionary<string, Dictionary<string, int?>> UfToNomeCidadeToIdCidade;

        private static bool TryReload;
        private static bool isRM;
        private static string mensagemErro;
        private static int NumArquivosLicitacao;

        public static string Name { get; } = "BB";
        public static string Remaining { get; } = "BBRM";
        public static string PathEditais { get; } = Path.GetTempPath() + DateTime.Now.ToString("yyyyMM") + Name + "/";
        #endregion

        /*retorna o robo que está executando*/
        public static string GetNameRobot()
        {
            return isRM ? Remaining : Name;
        }

        /*Método pelo qual o serviço inicia o robô no Timer agendado.*/
        internal static void InitCallBack(object state)
        {
            try
            {
                isRM = false;

                //Busca as informações do robô no banco de dados.
                config = ConfigRobotController.FindByName(Name);

                //Se o robô estiver ativo inicia o processamento.
                if (config.Active == 'Y')
                {
                    // Deleta o último arquivo de log.
                    if (File.Exists(Path.GetTempPath() + Name + ".txt"))
                        File.Delete(Path.GetTempPath() + Name + ".txt");

                    config.Status = 'R';
                    ConfigRobotController.Update(config);
                    using (FileStream fs = File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire")) { }
                    Init();

                    //Verifica se teve atualização
                    config = ConfigRobotController.FindByName(Name);

                    //Verifica quantas licitações foram coletadas nessa execução, grava em log.
                    config.NumLicitLast = NumLicitacoes;
                    RService.Log(Name + " find " + NumLicitacoes + " novas licitações at {0}", Path.GetTempPath() + Name + ".txt");
                    RService.Log(Name + " consumiu " + NumCaptcha + " captchas at {0}", Path.GetTempPath() + Name + ".txt");
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
                RService.Log("Exception (InitCallBack) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");

                if (web != null)
                    web.Close();
            }

            RService.Log("Finished " + Name + " at {0}", Path.GetTempPath() + Name + ".txt");

            EmailHandle.SendMail(Path.GetTempPath() + Name + ".txt", Name);
        }

        /*Metodo que inicia o processamento de dados*/
        private static void Init()
        {
            RService.Log("(Init) " + Name + ": Começando o processamento... " + "at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                Repo = new LicitacaoRepository();

                /*Realiza a atualização das situações das licitações já inseridas*/
                //UpdateLicitacoes();

                /*Inicia listas e dictionary's com informações necessárias*/
                NameToOrgao = OrgaoController.GetNomeUfToOrgao();
                NameToModalidade = ModalidadeController.GetNameToModalidade();
                UfToCapital = CityUtil.GetUfToCapital();
                UfToNomeCidadeToIdCidade = CidadeController.GetUfToNameCidadeToIdCidade();
                Lote = LoteController.CreateLote(43, IdFonte);

                TryReload = true;
                if (DoSearch())
                {
                    /*Percorre cada licitação encontrada*/
                    foreach (var numUf in numUFs)
                    {
                        /*Acessa a página da licitação*/
                        try
                        {
                            if (!LicitacaoController.Exists(numUf.Split(':')[0]))
                            {
                                /*Cria o objeto licitação com as informações da página*/
                                Licitacao licitacao = CreateLicitacao(numUf);

                                if (licitacao != null)
                                {
                                    Repo.Insert(licitacao);
                                    NumLicitacoes++;
                                    RService.Log("(Init) " + Name + ": Licitação inserida com sucesso at {0}", Path.GetTempPath() + Name + ".txt");

                                    /*Segmenta a licitação recém criada*/
                                    //SegmentarLicitacao(licitacao);
                                    DownloadEdAndCreatLicArq(licitacao);
                                }
                                else
                                {
                                    if (licitacao != null && licitacao.Orgao != null)
                                        RService.Log("Exception (CreateLicitacao) " + Name + ": A licitação de nº " + licitacao.Num + " e órgão " + licitacao.Orgao.Nome + " - " + licitacao.Orgao.Estado + " não foi salva - Motivo(s): " + mensagemErro + " at {0}", Path.GetTempPath() + Name + ".txt");
                                    else
                                        RService.Log("Exception (CreateLicitacao) " + Name + ": A licitação de nº " + numUf.Split(':')[0]+ " não foi salva - Motivo(s): " + mensagemErro + " at {0}", Path.GetTempPath() + Name + ".txt");
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            RService.Log("Exception (Init - CreateLicitacao) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (Init) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        private static void SegmentarLicitacao(Licitacao licitacao)
        {
            List<Segmento> segmentos = SegmentoController.CreateListaSegmentos(licitacao);

            foreach (Segmento segmento in segmentos)
            {
                int matchCount = 0;
                var palavrasChave = segmento.PalavrasChave.Split(';');

                foreach (var palavrachave in palavrasChave)
                {
                    if (licitacao.Objeto.ToUpper().Contains(palavrachave))
                    {
                        matchCount++;
                    }
                }

                if (matchCount >= 4)
                {
                    LicitacaoSegmento licSeg = new LicitacaoSegmento()
                    {
                        IdLicitacao = licitacao.Id,
                        IdSegmento = segmento.IdSegmento
                    };

                    LicitacaoSegmentoRepository repoLS = new LicitacaoSegmentoRepository();
                    repoLS.Insert(licSeg);
                }
            }
        }

        private static void UpdateLicitacoes()
        {
            try
            {
                /*Inicia navegador fantasma para atualização de licitações*/
                if (web != null)
                    web.Quit();

                var driver = ChromeDriverService.CreateDefaultService();
                driver.HideCommandPromptWindow = true;
                var op = new ChromeOptions();
                op.AddUserProfilePreference("download.default_directory", PathEditais);
                web = new ChromeDriver(driver, new ChromeOptions(), TimeSpan.FromSeconds(300));
                web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
                wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));


                List<Licitacao> licitacoes = LicitacaoController.FindBySituationBB();

                foreach (var licitacao in licitacoes)
                {
                    web.Navigate().GoToUrl(licitacao.LinkEdital);

                    HandleReCaptcha();

                    var situacaoDiv = web.FindElement(By.XPath("//*[@id=\"divConsultarDetalhesLicitacao\"]/fieldset/div[11]"));

                    if (situacaoDiv != null)
                    {
                        string situacao = web.FindElement(By.XPath("//*[@id=\"divConsultarDetalhesLicitacao\"]/fieldset/div[11]")).Text.TrimStart().TrimEnd();

                        if (!string.IsNullOrEmpty(situacao) && situacao != licitacao.Situacao)
                        {
                            licitacao.Situacao = situacao;
                            Repo.Update(licitacao);

                            RService.Log("(UpdateLicitacoes) " + Name + ": Situação da licitação " + licitacao.Id + " atualizada para " + situacao + " at {0}", Path.GetTempPath() + Name + ".txt");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (UpdateLicitacoes) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            finally
            {
                if (web != null)
                    web.Close();
            }
        }

        private static void HandleReCaptcha()
        {
            var token = GetCaptchaToken(Constants.BB_SITEKEY, web.Url);
            web.ExecuteScript(string.Format("document.getElementById('g-recaptcha-response').value = '{0}'", token));
            web.FindElement(By.XPath("//*[@id=\"consultarDetalhesLicitacaoForm\"]/input[3]")).Click();
        }

        /*Faz a pesquisa das licitações publicadas*/
        private static bool DoSearch()
        {
            RService.Log("(DoSearch) " + Name + ": Fazendo pesquisas..." + " at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                /*Inicia o navegador fantasma para a busca de licitações*/
                if (web != null)
                    web.Quit();

                var driver = ChromeDriverService.CreateDefaultService();
                driver.HideCommandPromptWindow = true;
                var op = new ChromeOptions();
                op.AddUserProfilePreference("download.default_directory", PathEditais);
                web = new ChromeDriver(driver, new ChromeOptions(), TimeSpan.FromSeconds(300));
                web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
                wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));


                numUFs = new List<string>();

                /*Acessa a pagina inicial para obter o cookie*/
                web.Navigate().GoToUrl(string.Format("http://{0}", Constants.BB_HOST));
                Thread.Sleep(2000);

                /*Acessa página de pesquisa*/
                for (int i = 2; i < 4; i++)
                {
                    try
                    {
                        web.Navigate().GoToUrl(Constants.BB_SITE);
                        Thread.Sleep(5000);
                        if (web.PageSource.Contains("Ocorrência de processamento"))
                        {
                            web.FindElement(By.XPath("//*[@id=\"cabecalho_menu_interno\"]/div[2]/div[3]/ul/li[1]/a")).Click();
                            Thread.Sleep(1000);
                        }
                        web.FindElement(By.XPath("//*[@id=\"licitacaoPesquisaSituacaoForm\"]/div[5]/span/input")).Clear();
                        web.FindElement(By.XPath("//*[@id=\"licitacaoPesquisaSituacaoForm\"]/div[5]/span/input")).SendKeys(i == 2 ? "Publicada" : "Acolhimento de propostas");
                        web.FindElement(By.XPath("//*[@id=\"licitacaoPesquisaSituacaoForm\"]/div[13]/div[1]")).Click();

                        web.ExecuteScript(GetScriptFillCaptchaXPath("//*[@id=\"img_captcha\"]", "pQuestionAvancada"));
                        web.FindElement(By.XPath("//*[@id=\"licitacaoPesquisaSituacaoForm\"]/div[14]/input")).Click();

                        Thread.Sleep(2000);
                        new SelectElement(web.FindElements(By.Name("tCompradores_length"))[0]).SelectByText("Todos");
                        Thread.Sleep(2000);

                        IWebElement table = web.FindElement(By.Id("tCompradores"));
                        string innerHtml = Regex.Replace(HttpUtility.HtmlDecode(table.GetAttribute("innerHTML")).Trim(), @"\s+", " ");

                        FindLicitNumsAndUfs(innerHtml);
                    }
                    catch (Exception e)
                    {
                        RService.Log("Exception (DoSearch) Falha na pesquisa na situação " + i + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
                    }
                }

                RService.Log("(DoSearch) " + Name + ": " + numUFs.Count + " novas licitaçoes encontradas. at {0}", Path.GetTempPath() + Name + ".txt");

                return true;
            }
            catch (Exception e)
            {
                RService.Log("Exception (DoSearch) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");

                return false;
            }
        }

        /*Acessa página dos arquivos de edital passando pelo captcha*/
        private static void DownloadEdAndCreatLicArq(Licitacao licitacao)
        {
            RService.Log("(DownloadEdAndCreatLicArq) " + GetNameRobot() + ": Visualizando arquivos de edital, licitação... " + licitacao.IdLicitacaoFonte.ToString() + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");

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


                web.Navigate().GoToUrl(licitacao.LinkEdital);

                HandleReCaptcha();

                if (TryReload)
                {
                    foreach (var a in web.FindElements(By.TagName("a")))
                    {
                        if (!string.IsNullOrEmpty(a.GetAttribute("onclick")) && a.GetAttribute("onclick").Contains("Listar documentos"))
                        {
                            web.ExecuteScript(a.GetAttribute("onclick"));
                            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("idImgListarAnexo")));

                            web.ExecuteScript(GetScriptFillCaptcha("idImgListarAnexo", "inputCaptchaAnexosLicitacao"));
                            Thread.Sleep(1000);

                            web.ExecuteScript("document.getElementById('botao_continuar').click()");
                            Thread.Sleep(2000);

                            //var select = web.FindElement(By.TagName("select"));
                            //new SelectElement(select).SelectByValue("-1");

                            MatchCollection linkForm = Regex.Matches(web.PageSource, "numeroLicitacao=(.+?)&amp;");

                            if (linkForm.Count > 0)
                                DownloadEditais(FindDocLinks(licitacao.IdLicitacaoFonte.ToString()), licitacao, linkForm);

                            if (Directory.Exists(PathEditais))
                                Directory.Delete(PathEditais, true);

                            break;
                        }
                    }
                }
                else
                {
                    web.ExecuteScript(GetScriptFillCaptcha("idImgListarAnexo", "inputCaptchaAnexosLicitacao"));
                    Thread.Sleep(1000);

                    web.ExecuteScript("document.getElementById('botao_continuar').click()");
                    Thread.Sleep(2000);

                    //var select = web.FindElement(By.XPath("//*[@id=\"tDocumento_length\"]/label/select"));
                    //new SelectElement(select).SelectByText("Todos");

                    MatchCollection linkForm = Regex.Matches(web.PageSource, "numeroLicitacao=(.+?)&amp;sem-reg=true");

                    DownloadEditais(FindDocLinks(licitacao.IdLicitacaoFonte.ToString()), licitacao, linkForm);

                    if (Directory.Exists(PathEditais))
                        Directory.Delete(PathEditais, true);
                }

            }
            catch (Exception e)
            {
                if (TryReload)
                {
                    RService.Log("Exception (DownloadEdAndCreatLicArq) " + GetNameRobot() + ": Falha na visualização, tentando novamente... " + licitacao.IdLicitacaoFonte.ToString() + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");

                    TryReload = false;
                    DownloadEdAndCreatLicArq(licitacao);
                }
                else
                {
                    RService.Log("Exception (DownloadEdAndCreatLicArq) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
                }

                TryReload = true;
            }
        }

        /*Guarda numa lista o num e uf de cada licitação econtrada na pesquisa*/
        private static List<string> FindLicitNumsAndUfs(string innerHtml)
        {
            RService.Log("(FindLicitNumsAndUfs) " + Name + ": Buscando licitações.." + " at {0}", Path.GetTempPath() + Name + ".txt");

            try
            {
                var numRx = new Regex("detalhar\\(\\d+\\)");
                var stateRx = new Regex("UF: <b>\\w+<\\/b>");

                if (numRx.IsMatch(innerHtml) && stateRx.IsMatch(innerHtml))
                {
                    var numMatches = numRx.Matches(innerHtml);
                    var stateMatches = stateRx.Matches(innerHtml);

                    for (int i = 0; i < numMatches.Count; i++)
                    {
                        string num = numMatches[i].Value.Replace("detalhar(", "").Replace(")", "");
                        string uf = stateMatches[i].Value.Replace("UF: <b>", "").Replace("</b>", "");
                        string numUf = num + ":" + uf;
                        numUFs.Add(numUf);
                    }
                }
                else
                {
                    RService.Log("(FindLicitNumsAndUfs) " + Name + ": Não foram encontradas licitações para análise at {0}", Path.GetTempPath() + Name + ".txt");
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (FindLicitNumsAndUfs) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }

            return numUFs;
        }

        /*Guarda numa lista os links dos arquivos de edital para um licitação*/
        private static List<string> FindDocLinks(string num)
        {
            RService.Log("(FindDocLinks) " + GetNameRobot() + ": Buscando editais da licitação.. " + num + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");

            List<string> links = new List<string>();
            try
            {
                int attachmentCount = 1;

                foreach (var ai in web.FindElements(By.TagName("img")))
                {
                    if (!string.IsNullOrEmpty(ai.GetAttribute("onclick")) && ai.GetAttribute("onclick").Contains("downloadAnexo"))
                    {
                        links.Add(string.Format(Constants.BB_LINK_ANEXO, num, attachmentCount));
                        attachmentCount++;
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (FindDocLinks) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            }

            return links;
        }

        /*Criando a licitação com as informações da página*/
        private static Licitacao CreateLicitacao(string numUf)
        {
            RService.Log("(CreateLicitacao) " + Name + ": Criando licitação... " + numUf.Split(':')[0] + " at {0}", Path.GetTempPath() + Name + ".txt");

            Licitacao licitacao = new Licitacao();
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


                web.Navigate().GoToUrl(Constants.BB_HOST);
                web.Navigate().GoToUrl(string.Format(Constants.BB_LINK_LICITACAO, numUf.Split(':')[0]));
                Thread.Sleep(2000);

                HandleReCaptcha();

                MatchCollection helper;
                licitacao.IdLicitacaoFonte = long.Parse(numUf.Split(':')[0]);
                licitacao.EstadoFonte = numUf.Split(':')[1];

                licitacao.Lote = Lote;
                licitacao.LinkSite = Constants.BB_HOST;
                licitacao.LinkEdital = string.Format(Constants.BB_LINK_LICITACAO, numUf.Split(':')[0]);
                licitacao.IdFonte = IdFonte;
                licitacao.Excluido = 0;
                licitacao.SegmentoAguardandoEdital = 0;
                licitacao.DigitacaoUsuario = 43; //Robo

                licitacao.Estado = numUf.Split(':')[1];

                IWebElement div = web.FindElement(By.Id("conteudo"));
                string innerHtml = Regex.Replace(HttpUtility.HtmlDecode(div.GetAttribute("innerHTML")).Trim(), @"\s+", " ");

                helper = StringHandle.GetMatches(innerHtml, "(?i)>\\s{0,1}Resumo da licit.*?\">(.*?)</div>");
                licitacao.Objeto = helper != null ? helper[0].Groups[1].Value.Trim() : null;

                helper = StringHandle.GetMatches(innerHtml, "(?i)>\\s{0,1}Edital.*?\">(.*?)</div>");
                licitacao.Num = helper != null ? helper[0].Groups[1].Value.Trim() : null;

                helper = StringHandle.GetMatches(innerHtml, "(?i)>\\s{0,1}Processo.*?\">(.*?)</div>");
                licitacao.Processo = helper != null ? helper[0].Groups[1].Value.Trim() : null;

                helper = StringHandle.GetMatches(innerHtml, "(?i)>\\s{0,1}Modalidade/tipo.*?\">(.*?)</div>");
                string modalidade = StringHandle.RemoveAccent(innerHtml != null ? helper[0].Groups[1].Value.Trim() : "").ToUpper();
                if (modalidade == "LRE")
                    modalidade = "Licitacao";

                licitacao.Modalidade = NameToModalidade.ContainsKey(modalidade.ToUpper()) ? NameToModalidade[modalidade.ToUpper()] : null;
                if (licitacao.Modalidade.Modalidades == "Pregão")
                {
                    licitacao.Modalidade.Modalidades = "Pregão Eletrônico";
                    licitacao.Modalidade.Id = 24;
                }

                helper = StringHandle.GetMatches(innerHtml, "(?i)>\\s{0,1}Situa.*?o da licita.*?o.*?\">(.*?)</div>");
                licitacao.Situacao = helper != null ? helper[0].Groups[1].Value.Trim() : null;

                helper = StringHandle.GetMatches(innerHtml, "(?i)>\\s{0,1}Abertura das propostas.*?\">(.*?)</div>");
                licitacao.AberturaData = DateHandle.Parse(helper != null ? helper[0].Groups[1].Value.Trim() : null, "dd/MM/yyyy-hh:mm");

                helper = StringHandle.GetMatches(innerHtml, "(?i)>\\s{0,1}Início acolhimento de propostas.*?\">(.*?)</div>");
                licitacao.EntregaData = DateHandle.Parse(helper != null ? helper[0].Groups[1].Value.Trim() : null, "dd/MM/yyyy-hh:mm");

                helper = StringHandle.GetMatches(innerHtml, "(?i)>\\s{0,1}Cliente.*?\">(.*?)</div>");
                string orgaoDepartamento = helper != null ? helper[0].Groups[1].Value.Trim() : null;

                helper = StringHandle.GetMatches(orgaoDepartamento, "/(.*)");
                licitacao.Departamento = helper != null ? helper[0].Groups[1].Value.Trim() : null;

                helper = StringHandle.GetMatches(orgaoDepartamento, "(.*?)/");
                string orgao = helper != null ? helper[0].Groups[1].Value.Trim() : null;

                if (orgao != null)
                {
                    licitacao.Orgao = OrgaoController.GetOrgaoByNameAndUf(orgao.Trim().ToUpper() + ":" + numUf.Split(':')[1].Trim().ToUpper(), NameToOrgao);
                }
                else
                {
                    licitacao.Orgao = OrgaoController.FindById(390);//NÃO ESPECIFICADO
                }

                HandleCidade(licitacao, orgao);
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacao) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            finally
            {
                if (web != null)
                    web.Close();
            }

            return LicitacaoController.IsValid(licitacao, out mensagemErro) ? licitacao : null;
        }

        private static void HandleCidade(Licitacao licitacao, string orgao)
        {
            Dictionary<string, int?> ufToCidade = UfToNomeCidadeToIdCidade.ContainsKey(licitacao.EstadoFonte) ? UfToNomeCidadeToIdCidade[licitacao.EstadoFonte] : null;
            List<Cidade> cities = new CidadeRepository().FindByUf(licitacao.EstadoFonte);

            string cidadeEstado = CityUtil.FindCity(licitacao);
            string cityMatch = GetCityMatch(ufToCidade, orgao);

            if (orgao.Contains("MUNICIPIO"))
            {
                licitacao.Cidade = orgao.Replace("MUNICIPIO DE ", "").Replace("MUNICIPIO DA ", "");
                licitacao.CidadeFonte = ufToCidade.ContainsKey(StringHandle.RemoveAccent(licitacao.Cidade.ToUpper())) ? ufToCidade[StringHandle.RemoveAccent(licitacao.Cidade.ToUpper())] : CityUtil.GetCidadeFonte(licitacao.Cidade, ufToCidade);
                return;
            }

            if (orgao.Contains("PREFEITURA MUNICIPAL"))
            {
                licitacao.Cidade = orgao.Replace("PREFEITURA MUNICIPAL DE ", "").Replace("PREFEITURA MUNICIPAL DA ", "");
                licitacao.CidadeFonte = ufToCidade.ContainsKey(StringHandle.RemoveAccent(licitacao.Cidade.ToUpper())) ? ufToCidade[StringHandle.RemoveAccent(licitacao.Cidade.ToUpper())] : CityUtil.GetCidadeFonte(licitacao.Cidade, ufToCidade);
                return;
            }

            if (!string.IsNullOrEmpty(cidadeEstado))
            {
                licitacao.Cidade = cidadeEstado.Split('/')[0];
                licitacao.CidadeFonte = Convert.ToInt16(cidadeEstado.Split('/')[2]);
                return;
            }

            if (!string.IsNullOrEmpty(cityMatch))
            {
                licitacao.Cidade = cityMatch.ToUpper();
                licitacao.CidadeFonte = ufToCidade.ContainsKey(StringHandle.RemoveAccent(cityMatch.ToUpper())) ? ufToCidade[StringHandle.RemoveAccent(cityMatch.ToUpper())] : CityUtil.GetCidadeFonte(licitacao.Cidade, ufToCidade);
                return;
            }

            if (licitacao.Cidade == null || licitacao.CidadeFonte == null)
            {
                if (UfToCapital.ContainsKey(licitacao.EstadoFonte))
                {
                    licitacao.Cidade = UfToCapital[licitacao.EstadoFonte];
                    licitacao.CidadeFonte = ufToCidade != null ? ufToCidade.ContainsKey(StringHandle.RemoveAccent(licitacao.Cidade.ToUpper())) ? ufToCidade[StringHandle.RemoveAccent(licitacao.Cidade.ToUpper())] : null : null;
                }
                else
                {
                    Orgao orgaoDb = OrgaoRepository.FindOrgao(licitacao.Departamento);

                    if (orgaoDb != null)
                    {
                        licitacao.Cidade = UfToCapital.ContainsKey(orgaoDb.Estado) ? UfToCapital[orgaoDb.Estado] : null;
                        licitacao.CidadeFonte = ufToCidade != null ?
                                                    ufToCidade.ContainsKey(StringHandle.RemoveAccent(licitacao.Cidade.ToUpper())) ?
                                                            ufToCidade[StringHandle.RemoveAccent(licitacao.Cidade.ToUpper())]
                                                                : null
                                                                    : null;
                    }
                }
            }
        }

        private static string GetCityMatch(Dictionary<string, int?> ufToCidade, string orgao)
        {
            string cityMatch = string.Empty;

            foreach (var cidade in ufToCidade)
            {
                if (orgao.Contains(cidade.Key))
                {
                    cityMatch = cidade.Key;
                }
            }

            return cityMatch;
        }

        /*Baixa os arquivos de edital*/
        private static void DownloadEditais(List<string> editais, Licitacao licitacao, MatchCollection linkForm)
        {
            try
            {
                RService.Log("(DownloadEditais) " + GetNameRobot() + ": Consultando arquivos de edital, licitação... " + licitacao.IdLicitacaoFonte + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");

                if (!Directory.Exists(PathEditais))
                {
                    Directory.CreateDirectory(PathEditais);
                }

                for (int i = 0; i < editais.Count; i++)
                {
                    //Busca os arquivos da licitação
                    LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                    List<LicitacaoArquivo> arquivosAntigos = repoArq.FindByLicitacao(licitacao.Id);

                    var edital = editais[i];
                    bool isExist = false;

                    string nameFile = string.Format("Anexo{0}Licitacao{1}", i, licitacao.Id);

                    foreach (var arquivoAntigo in arquivosAntigos)
                    {
                        /*Verifica se o arquivo já existe*/
                        if (nameFile.Equals(arquivoAntigo.NomeArquivoFonte))
                        {
                            isExist = true;
                            break;
                        }
                    }
                    if (!isExist)
                    {
                        if (LicitacaoArquivoController.CreateLicitacaoArquivo(GetNameRobot(), licitacao, edital, PathEditais, nameFile, web.Manage().Cookies.AllCookies))
                            NumArquivosLicitacao++;
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (DownloadEditais) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            }
        }

        /*Resolve o captcha e preenche campo com a string obtida*/
        private static string GetScriptFillCaptchaXPath(string imageXPath, string inputId)
        {
            RService.Log("(GetScriptFillCaptcha) " + GetNameRobot() + ": Resolvendo captcha... " + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");

            string tempImg = string.Empty, tempImgCrop = string.Empty;

            try
            {
                tempImg = Path.GetTempPath() + Guid.NewGuid().ToString() + ".jpg";
                tempImgCrop = Path.GetTempPath() + Guid.NewGuid().ToString() + ".jpg";

                web.GetScreenshot().SaveAsFile(tempImg, ScreenshotImageFormat.Jpeg);
                Bitmap image = (Bitmap)Image.FromFile(tempImg);
                GetCaptchaImg(web.FindElement(By.XPath(imageXPath)), image, tempImgCrop);

                string script = string.Format("document.getElementById('{0}').value = '{1}'", inputId,
                    WebHandle.ResolveCaptcha(tempImgCrop).ToLower());

                NumCaptcha++;

                return script;
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetScriptFillCaptcha) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
                return null;
            }
            finally
            {
                if (File.Exists(tempImg))
                    File.Delete(tempImg);
                if (File.Exists(tempImgCrop))
                    File.Delete(tempImgCrop);
            }
        }

        private static string GetScriptFillCaptcha(string imageId, string inputId)
        {
            RService.Log("(GetScriptFillCaptcha) " + GetNameRobot() + ": Resolvendo captcha... " + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");

            string tempImg = string.Empty, tempImgCrop = string.Empty;

            try
            {
                tempImg = Path.GetTempPath() + Guid.NewGuid().ToString() + ".jpg";
                tempImgCrop = Path.GetTempPath() + Guid.NewGuid().ToString() + ".jpg";

                web.GetScreenshot().SaveAsFile(tempImg, ScreenshotImageFormat.Jpeg);
                Bitmap image = (Bitmap)Image.FromFile(tempImg);
                GetCaptchaImg(web.FindElement(By.Id(imageId)), image, tempImgCrop);

                string script = string.Format("document.getElementById('{0}').value = '{1}'", inputId,
                    WebHandle.ResolveCaptcha(tempImgCrop).ToLower());

                NumCaptcha++;

                return script;
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetScriptFillCaptcha) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
                return null;
            }
            finally
            {
                if (File.Exists(tempImg))
                    File.Delete(tempImg);
                if (File.Exists(tempImgCrop))
                    File.Delete(tempImgCrop);
            }
        }

        /*Recorta imagem do captcha da img da pag para enviar pro decodificador*/
        private static void GetCaptchaImg(IWebElement element, Bitmap screenShot, string cutCaptchaFile)
        {
            RService.Log("(GetCaptchaImg) " + GetNameRobot() + ": Achando captcha... " + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            try
            {
                Point p = element.Location;

                int eleWidth = element.Size.Width;
                int eleHeight = element.Size.Height;

                Size size = new Size(eleWidth, eleHeight);
                Rectangle r = new Rectangle(p, size);

                Image ib = screenShot.Clone(r, screenShot.PixelFormat);

                ib.Save(cutCaptchaFile);

                screenShot.Dispose();
                ib.Dispose();
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetCaptchaImg) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            }
        }

        //Preenche o formulario para fazer o download e recupera os cookies
        private static void FillForm(string parametros)
        {
            try
            {
                string url = string.Format("http://www.licitacoes-e.com.br/aop/lct/licitacao/consulta/CadastrarInteressedital.jsp?{0}", parametros);
                web.Navigate().GoToUrl(url);

                web.ExecuteScript(string.Format("document.getElementById('nomeEmpresa').value = '{0}'", "Otavio"));
                web.ExecuteScript(string.Format("document.getElementById('endereco').value = '{0}'", "Rua das Aguias"));
                web.ExecuteScript(string.Format("document.getElementById('bairro').value = '{0}'", "Curitiba"));
                web.ExecuteScript(string.Format("document.getElementById('cidade').value = '{0}'", "Diadema"));
                web.ExecuteScript(string.Format("document.getElementById('cep').value = '{0}'", "81000-000"));
                web.ExecuteScript(string.Format("document.getElementById('uf').value = '{0}'", "AM"));
                web.ExecuteScript(string.Format("document.getElementById('codigoTipoDocumentoCnpj').checked = '{0}'", "true"));
                web.ExecuteScript(string.Format("document.getElementById('textoDocumentoPessoaJuridica').value = '{0}'", "46824418000114"));

                web.ExecuteScript(string.Format("document.getElementById('contato').value = '{0}'", "Keller Ches"));
                web.ExecuteScript(string.Format("document.getElementById('ddd').value = '{0}'", "31"));
                web.ExecuteScript(string.Format("document.getElementById('telefone').value = '{0}'", "98879528"));
                web.ExecuteScript(string.Format("document.getElementById('email').value = '{0}'", "aloalo@gmail.com"));

                web.ExecuteScript("document.getElementsByClassName('button clear col')[0].click();");

            }
            catch (Exception e)
            {
                RService.Log("Exception (FillForm) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
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
    }
}
