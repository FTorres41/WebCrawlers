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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace RSBM.Controllers
{
    class TCEPRController
    {
        #region Declaracao de variaveis
        private static ChromeDriver web;
        private static WebDriverWait wait;

        private static bool TryReach = true;

        private static int CurrentPg;
        private static int ItemCurrentReTry = -1;
        private static int TotalLic = 0;
        private static string mensagemErro;

        private static List<Licitacao> licitacoes = new List<Licitacao>();
        private static Lote Lote;
        private static ConfigRobot config;
        private static LicitacaoRepository Repo;

        private static List<int> Tens = new List<int>() { 11 };
        private static DateTime CurrentDay;
        private static DateTime FinalDay;
        private static string CidadeAtualKey;
        private static string CidadeAtual;

        public static string Name { get; } = "TCEPR";

        private static Dictionary<string, Orgao> NameToOrgao;
        private static Dictionary<string, Modalidade> NameToModalidade;
        private static Dictionary<string, string> dicCidades;

        #endregion

        /*Método pelo qual o serviço inicia o robô no Timer agendado.*/
        internal static void InitCallBack(object state)
        {
            try
            {
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
                    RService.Log(Name + " find " + TotalLic + " novas licitações at {0}", Path.GetTempPath() + Name + ".txt");
                    config.NumLicitLast = TotalLic;
                
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
            }

            RService.Log("Finished " + Name + " at {0}", Path.GetTempPath() + Name + ".txt");

            EmailHandle.SendMail(Path.GetTempPath() + Name + ".txt", Name);
        }

        /*Inicia a busca*/
        private static void Init()
        {
            RService.Log("(Init) " + Name + ": Começando o processamento.." + " at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                /*Inicia navegador fantasma*/
                LoadDriver();

                /*Acessa o site do TCE*/
                web.Navigate().GoToUrl(Constants.TCEPR_SITE);

                /*Busca os itens do filtro por cidades*/
                GetCidades();

                /*Configura os parametros da busca e inicia demais componentes necessários*/
                if (config.PreTypedDate != null)
                {
                    CurrentDay = config.PreTypedDate.Value.AddDays(-2);
                    FinalDay = config.PreTypedDate.Value;
                    config.PreTypedDate = null;
                    ConfigRobotController.Update(config);
                }
                else
                {
                    CurrentDay = DateTime.Now.AddDays(-2);
                    FinalDay = DateTime.Now;
                }

                NameToModalidade = ModalidadeController.GetNameToModalidade();
                NameToOrgao = OrgaoController.GetNomeUfToOrgao();
                Lote = LoteController.CreateLote(43, 510);
                Repo = new LicitacaoRepository();

                CurrentPg = 1;
                TotalLic = 0;

                /*Procura licitacoes por cidade pra ointervalo de dias indicado*/
                foreach (var cidade in dicCidades)
                {
                    CidadeAtualKey = cidade.Key;
                    CidadeAtual = cidade.Value;
                    /*Pesquisa licitações com base nos parametros (ano; dtAbertura)*/
                    if (DoSearch(CurrentDay, FinalDay))
                    {
                        /*Pra cada pag que a pesquisa retornar*/
                        do
                        {
                            /*Percorre licitações de uma pag*/
                            if (!GetLicitacoes())
                            {
                                //Segunda e ultima tentativa, se não conseguir continua com a pesquisa para a proxima cidade
                                DoSearch(CurrentDay, FinalDay);
                                GetLicitacoes();
                            }

                        } while (HasNextPage());
                    }

                    Tens = new List<int>() { 11 };
                    CurrentPg = 1;
                }

                //Repo.Insert(licitacoes);
            }
            catch (Exception e)
            { 
                RService.Log("Exception (Init) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            finally
            {
                if (TotalLic <= 0)
                    LoteController.Delete(Lote);
                if (web != null)
                    web.Close();
            }
        }
      
        /*Verifica se existe uma próxima pagina*/
        private static bool HasNextPage()
        {
            try
            {
                bool hasNext = false;
                try
                {
                    string txt = string.Format(@"javascript:__doPostBack\(\'ctl00\$ContentPlaceHolder1\$gvListaLicitacoes\',\'Page\${0}\'\)",
                                     CurrentPg + 1);
                    if (Regex.IsMatch(web.PageSource, @txt, RegexOptions.IgnoreCase))
                    {
                        hasNext = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_gvListaLicitacoes"))
                                     .FindElements(By.TagName("a"))
                                     .Select(x => GetPagNum(x.GetAttribute("href")))
                                     .Contains(CurrentPg + 1);
                    }
                }
                catch (Exception)
                {
                    return false;
                }

                if (hasNext)
                {
                    CurrentPg++;
                    web.FindElementByLinkText(string.Format("javascript:__doPostBack('ctl00$ContentPlaceHolder1$gvListaLicitacoes','Page${0}')", CurrentPg)).Click();
                    //web.ExecuteScript(string.Format("javascript:__doPostBack('ctl00$ContentPlaceHolder1$gvListaLicitacoes','Page${0}')", CurrentPg));
                    wait.Until((d) => { Thread.Sleep(2000); return true; });
                    if (CurrentPg == 10 + Tens.Max())
                    {
                        Tens.Add(CurrentPg);
                    }
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                RService.Log("Exception (HasNextPage) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
                return false;
            }
        }

        /*Pega o numero da pagina do atributo onclick*/
        private static int GetPagNum(string v)
        {
            string pattern = @"Page\$(\d+)\'";
            int outI = -1;
            Match match = Regex.Match(v, pattern);
            if (match.Success)
            {
                int.TryParse(match.Groups[1].Value, out outI);
            }
            return outI;
        }

        /*Percorre licitações de uma pag*/
        private static bool GetLicitacoes()
        {
            RService.Log("(GetLicitacoes) " + Name + ": Percorrendo licitações da página.. " + CurrentPg + " at {0}", Path.GetTempPath() + Name + ".txt");

            int i = 0;
            try
            {
                if (web.FindElement(By.Id("ctl00_ContentPlaceHolder1_gvListaLicitacoes")) != null &&
                        web.FindElement(By.Id("ctl00_ContentPlaceHolder1_gvListaLicitacoes")).
                                               FindElements(By.TagName("input")) != null)
                {
                    if (ItemCurrentReTry != -1)
                    {
                        i = ItemCurrentReTry;
                        GoToCurrentPg();
                    }

                    /*Pega o numero de licitacoes da pag*/
                    int num = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_gvListaLicitacoes")).
                                                   FindElements(By.TagName("input")).Count;

                    /*pra cada um dos itens da pagina*/
                    for (; i < num; i++)
                    {
                        /*Tenta acessar uma licitação na pagina, em seguida tenta voltar pra pág atual se não conseguir continua pra próxima pesquisa*/
                        //web.ExecuteScript(string.Format("javascript:__doPostBack('ctl00$ContentPlaceHolder1$gvListaLicitacoes','${0}');return false;", i));
                        wait.Until(ExpectedConditions.ElementToBeClickable(By.TagName("input")));
                        web.ExecuteScript(string.Format("document.getElementsByTagName('input')[{0}].click()", i + 39));
                        try
                        {
                            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("ctl00_ContentPlaceHolder1_tbnmExecutor")));
                            /*Create Licitacao */
                            if (!CreateLicitacao() && ItemCurrentReTry == -1)
                            {
                                ItemCurrentReTry = i;
                                return false;
                            }
                        }
                        catch (Exception e)
                        {
                            RService.Log("Exception (GetLicitacoes) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
                            if (ItemCurrentReTry == -1)
                            {
                                ItemCurrentReTry = i;
                                return false;
                            }
                        }

                        //Volta sempre pra primeira pag.
                        web.FindElementById("ctl00_ContentPlaceHolder1_ucProcessoCompra1_lbtVoltar").Click();
                        //web.ExecuteScript("document.getElementById('ctl00_ContentPlaceHolder1_ucProcessoCompra1_lbtVoltar').click()");
                        try
                        {
                            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("ctl00_ContentPlaceHolder1_gvListaLicitacoes")));
                            if (web.FindElement(By.Id("ctl00_ContentPlaceHolder1_gvListaLicitacoes")).FindElements(By.TagName("input")) == null ||
                                web.FindElement(By.Id("ctl00_ContentPlaceHolder1_gvListaLicitacoes")).FindElements(By.TagName("input")).ToList().Count <= 0)
                            {
                                if (ItemCurrentReTry == -1)
                                {
                                    ItemCurrentReTry = i;
                                    return false;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            RService.Log("Exception (GetLicitacoes) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
                            if (ItemCurrentReTry == -1)
                            {
                                ItemCurrentReTry = i;
                                return false;
                            }
                        }
                        //Voltar para a pag corrente.
                        GoToCurrentPg();
                        ItemCurrentReTry = -1;
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetLicitacoes) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
                return true;
            }
        }

        /*Cria uma nova licitacao e o lote se for necessário*/
        public static bool CreateLicitacao()
        {
            try
            {
                string entidadeExecutora = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_tbnmExecutor")).GetAttribute("value");
                string objeto = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_tbdsObjeto")).GetAttribute("value");
                string numLicitacao = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_tbnrProcessoEdital")).GetAttribute("value");
                string preco = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_tbvlReferencia")).GetAttribute("value");
                string dotacao = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_tbnrDotacaoOrcamentaria")).GetAttribute("value");
                string modalidade = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_tbdsModalidadeLicitacao")).GetAttribute("value");
                string numProcesso = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_tbnrEditalOrigem")).GetAttribute("value");
                string dataLancamento = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_tbdtLancamentoPublicacao")).GetAttribute("value");
                string precoMax = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_tbvlReferencia")).GetAttribute("value");
                string dataAberturaEntrega = "";
                if (CidadeAtual == "Diamante do Oeste")
                    CidadeAtual = "Diamante d'Oeste";

                if (Regex.IsMatch(web.PageSource, @"ctl00_ContentPlaceHolder1_tbdtAberturaLicitacao", RegexOptions.IgnoreCase))
                {
                    dataAberturaEntrega = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_tbdtAberturaLicitacao")).GetAttribute("value");

                    //Caso a licitação tenha o campo NOVA Data Abertura preenchido
                    if (Regex.IsMatch(web.PageSource, @"ctl00_ContentPlaceHolder1_tbdtAberturaNova", RegexOptions.IgnoreCase))
                    {
                        string dataNovaAberturaEntrega = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_tbdtAberturaNova")).GetAttribute("value");

                        if (!string.IsNullOrEmpty(dataNovaAberturaEntrega))
                            dataAberturaEntrega = dataNovaAberturaEntrega;
                    }

                    string cod = GenerateCod(entidadeExecutora, dataLancamento, numProcesso, preco);
                    string scod = cod.ToString().Substring(0, 19);

                    long numLong = 0;
                    long numScod = long.TryParse(scod, out numLong) ? numLong : 0;

                    if (!string.IsNullOrEmpty(scod) && !LicitacaoController.Exists(scod))
                    {
                        if (!LicitacaoController.Exists(objeto, preco, "Dotação Orçamentária: " + dotacao, numLicitacao, numProcesso, Constants.TCEPR_HOST))
                        {
                            RService.Log("(CreateLicitacao) " + Name + ": Criando licitação num... " + scod + " at {0}", Path.GetTempPath() + Name + ".txt");

                            Licitacao licitacao = new Licitacao();
                            licitacao.Objeto = objeto;
                            licitacao.Processo = numProcesso;
                            licitacao.IdLicitacaoFonte = numScod;
                            licitacao.IdFonte = 510;
                            licitacao.Excluido = 0;
                            licitacao.SegmentoAguardandoEdital = 0;
                            licitacao.DigitacaoUsuario = 43; //Robo
                            licitacao.Lote = Lote;
                            //licitacao.DigitacaoData = null;
                            //licitacao.ProcessamentoData = null;

                            licitacao.Observacoes = "Dotação Orçamentária: " + dotacao + " / Data de lançamento do edital pela entidade executora: " + dataLancamento;
                            licitacao.LinkSite = Constants.TCEPR_HOST;
                            licitacao.Cidade = CidadeAtual;
                            licitacao.CidadeFonte = CidadeController.GetIdCidade(CidadeAtual, Constants.TCEPR_UF);
                            licitacao.EstadoFonte = Constants.TCEPR_UF;
                            licitacao.Estado = Constants.TCEPR_ESTADO;
                            licitacao.Modalidade = NameToModalidade.ContainsKey(StringHandle.RemoveAccent(modalidade).ToUpper()) ? NameToModalidade[StringHandle.RemoveAccent(modalidade).ToUpper()] : null;

                            licitacao.Num = numLicitacao;
                            licitacao.AberturaData = DateHandle.Parse(dataAberturaEntrega, "dd/MM/yyyy");
                            licitacao.EntregaData = DateHandle.Parse(dataAberturaEntrega, "dd/MM/yyyy");
                            licitacao.ValorMax = precoMax;
                            licitacao.Orgao = OrgaoController.GetOrgaoByNameAndUf(entidadeExecutora.Trim() + ":PR", NameToOrgao);

                            if (LicitacaoController.IsValid(licitacao, out mensagemErro))
                            {
                                Repo.Insert(licitacao);
                                //licitacoes.Add(licitacao);
                                RService.Log("Licitacao salva com sucesso at {0}", Path.GetTempPath() + Name + ".txt");
                                TotalLic++;
                            }
                            else
                            {
                                if (licitacao.Orgao != null)
                                    RService.Log("Exception (CreateLicitacao) " + Name + ": A licitação de nº " + licitacao.Num + " e órgão " + licitacao.Orgao.Nome + " - " + licitacao.Orgao.Estado + " não foi salva - Motivo(s): " + mensagemErro + " at {0}", Path.GetTempPath() + Name + ".txt");
                                else
                                    RService.Log("Exception (CreateLicitacao) " + Name + ": A licitação de nº " + licitacao.Num + " não foi salva - Motivo(s): " + mensagemErro + " at {0}", Path.GetTempPath() + Name + ".txt");
                            }
                        }
                    }                   
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacao) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            return true;
        }

        /*Gera um codigo unico para cada licitacao*/
        private static string GenerateCod(string entidadeExecutora, string dataAberturaEntrega, string processo, string preco)
        {
            string cod = string.Empty;
            string[] splitEx = entidadeExecutora.Split(' ');

            StringBuilder sb = new StringBuilder();

            sb.Append(DateHandle.Parse(dataAberturaEntrega, "dd/MM/yyyy").Value.ToString("yyMMdd"));
            sb.Append(Regex.Replace(processo, @"\D+", ""));
            sb.Append(preco.Replace(",", "").Replace(".", ""));
            sb.Append(BitConverter.ToInt32(Encoding.ASCII.GetBytes(splitEx[0] + splitEx[splitEx.Length - 1]), 0).ToString());

            cod = sb.ToString();

            //se o codigo gerado tiver menos de 19 caracteres, coloca um 0 no fim
            if (sb.ToString().Length < 19)
            {
                cod = sb.ToString().PadRight(19, '0');
            }
            return cod;
        }

        /*Voltar pra pagina atual*/
        private static void GoToCurrentPg()
        {
            //Se a pag em questão não for a primeira e estiver dentro da primeira dezena visivel no site, acessa a pag diretamente.
            if (CurrentPg > 1 && CurrentPg <= Tens.Min())
            {
                web.ExecuteScript(string.Format("javascript:__doPostBack('ctl00$ContentPlaceHolder1$gvListaLicitacoes','Page${0}')", CurrentPg));
                wait.Until((d) => { Thread.Sleep(2000); return true; });
                if (web.FindElement(By.Id("ctl00_ContentPlaceHolder1_gvListaLicitacoes")).
                         FindElements(By.TagName("a")).Select(x => GetPagNum(x.GetAttribute("href")))
                            .Contains(CurrentPg))
                {
                    ReLoadOnError();
                    web.ExecuteScript(string.Format("javascript:__doPostBack('ctl00$ContentPlaceHolder1$gvListaLicitacoes','Page${0}')", CurrentPg));
                    wait.Until((d) => { Thread.Sleep(2000); return true; });
                }
            }
            //Se não, se a pag em questão não for a primeira, mas a pag não estiver visivel na primeira dezena, acessa a dezena atual ex: (11, 21, 31) e depois acessa a pag.
            else if (CurrentPg > 1 && CurrentPg > 11)
            {
                if (ReachCurrentPg())
                {
                    web.ExecuteScript(string.Format("javascript:__doPostBack('ctl00$ContentPlaceHolder1$gvListaLicitacoes','Page${0}')", CurrentPg));
                    wait.Until((d) => { Thread.Sleep(2000); return true; });
                }
            }
        }

        /*Faz a pesquisa para um dia de acordo com o parametro*/
        private static bool DoSearch(DateTime initialDay, DateTime endDay)
        {
            RService.Log("(DoSearch) " + Name + ": Fazendo pesquisa para o município: " + CidadeAtual + ", do dia: " + initialDay.ToString("dd/MM/yyyy") + " até o dia:" + endDay.ToString("dd/MM/yyyy") + " at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                LoadDriver();

                web.Navigate().GoToUrl(Constants.TCEPR_SITE);

                web.ExecuteScript(string.Format("document.getElementById('ctl00_ContentPlaceHolder1_ddlMunicipio').value = '{0}'", CidadeAtualKey));
                web.ExecuteScript(string.Format("document.getElementById('ctl00_ContentPlaceHolder1_tbnrAno').value = {0}", initialDay.Year));
                web.ExecuteScript(string.Format("document.getElementById('ctl00_ContentPlaceHolder1_ucdtRegistroDe_tbData').value = '{0}'", initialDay.ToString("dd/MM/yyyy")));
                web.ExecuteScript(string.Format("document.getElementById('ctl00_ContentPlaceHolder1_ucdtRegistroAte_tbData').value = '{0}'", endDay.ToString("dd/MM/yyyy")));

                web.ExecuteScript("document.getElementById('ctl00_ContentPlaceHolder1_btnPesquisar').click()");
                /*Espera até que a pesquisa pro dia retorne algo*/
                Thread.Sleep(5000);

                string registro = web.FindElement(By.Id("ctl00_ContentPlaceHolder1_lbQuantidade")).Text;
                RService.Log("(DoSearch) " + Name + ": " + registro + " at {0}", Path.GetTempPath() + Name + ".txt");


                wait.Until((d) => { return !string.IsNullOrEmpty(web.FindElement(By.Id("ctl00_ContentPlaceHolder1_lbQuantidade")).Text); });
                if (!web.FindElement(By.Id("ctl00_ContentPlaceHolder1_lbQuantidade")).Text.ToUpper().Equals("NENHUM REGISTRO ENCONTRADO COM ESSES FILTROS"))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (DoSearch) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            return false;
        }

        /*Recarrega a pagina e faz a pesquisa para o dia corrente em caso de erro*/
        private static void ReLoadOnError()
        {
            //LoadWebDriver();
            LoadDriver();

            web.Navigate().GoToUrl(Constants.TCEPR_SITE);
            DoSearch(CurrentDay, FinalDay);
        }

        /*Acessa as páginas acima da décima primeira*/
        private static bool ReachCurrentPg()
        {
            foreach (int desz in Tens)
            {
                web.ExecuteScript(string.Format("javascript:__doPostBack('ctl00$ContentPlaceHolder1$gvListaLicitacoes','Page${0}')", desz));
                wait.Until((d) => { Thread.Sleep(2000); return true; });
                if (web.FindElement(By.Id("ctl00_ContentPlaceHolder1_gvListaLicitacoes")).
                   FindElements(By.TagName("a")).Select(x => GetPagNum(x.GetAttribute("href")))
                    .Contains(desz))
                {
                    if (TryReach)
                    {
                        TryReach = false;
                        ReLoadOnError();
                        ReachCurrentPg();
                    }
                    else
                    {
                        TryReach = true;
                        return false;
                    }
                }
            }
            TryReach = true;
            return true;
        }

        private static void GetCidades()
        {
            try
            {
                dicCidades = new Dictionary<string, string>();

                /*Acessa o site do TCE*/
                web.Navigate().GoToUrl(Constants.TCEPR_SITE);

                var element = web.FindElementByXPath("//select[@name='ctl00$ContentPlaceHolder1$ddlMunicipio']");
                var options = element.FindElements(By.TagName("option"));

                foreach (var item in options)
                {
                    if (!Regex.IsMatch(item.Text, @"Selecionar"))
                    {
                        dicCidades.Add(item.GetAttribute("value"), Regex.Replace(item.Text, @"\r", "").Trim());
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetCidades) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        private static void LoadDriver()
        {
            if (web != null)
                web.Quit();

            var driver = ChromeDriverService.CreateDefaultService();
            driver.HideCommandPromptWindow = true;
            var op = new ChromeOptions();
            web = new ChromeDriver(driver, new ChromeOptions(), TimeSpan.FromSeconds(300));
            web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
            wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));
        }
    }
}
