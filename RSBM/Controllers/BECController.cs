using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using RSBM.Models;
using RSBM.Repository;
using RSBM.Util;
using RSBM.WebUtil;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace RSBM.Controllers
{
    class BECController
    {
        #region Declaração de variaveis
        private static List<Modalidade> Modalidades;

        private static Dictionary<string, int?> Cidades;

        private static LicitacaoRepository Repo;
        private static Lote Lote;
        private static Orgao Orgao;
        private static ConfigRobot config;
        private static List<Licitacao> licitacoes = new List<Licitacao>();

        private static int ModalidadeCount;
        private static int CurrentPage;
        private static int NumLicitacoes;
        private static string mensagemErro;
        private static string href;

        public static string Name { get; } = "BEC";
        public static string PathEdital { get; } = Path.GetTempPath() + DateTime.Now.ToString("yyyyMM") + Name + "/";
        public static WebDriverWait wait { get; private set; }
        public static ChromeDriver web { get; private set; }
        //public static PhantomJSDriver web { get; set; }
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
                    config.NumLicitLast = NumLicitacoes;
                    RService.Log(Name + " find " + NumLicitacoes + " novas licitações at {0}", Path.GetTempPath() + Name + ".txt");
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
            }

            RService.Log("Finished " + Name + " at {0}", Path.GetTempPath() + Name + ".txt");

            EmailHandle.SendMail(Path.GetTempPath() + Name + ".txt", Name);
        }

        /*Inicia o processamento do robot*/
        private static void Init()
        {
            RService.Log("(Init) " + Name + ": Começando o processamento.. " + "at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                ModalidadeCount = 0;

                Modalidades = new List<Modalidade>();
                Modalidades.Add(ModalidadeController.FindById(1));
                Modalidades.Add(ModalidadeController.FindById(14));
                Orgao = OrgaoController.FindById(388);
                Lote = LoteController.CreateLote(43, 507);

                /*Lista das licitacoes que já existem para bec.sp.gov*/
                //AlreadyInserted = LicitacaoController.GetAlreadyInserted(Constants.BEC_SITE);

                /*Lista das cidades para o estado*/
                Cidades = CidadeController.GetNameToCidade(Constants.BEC_UF);

                //Define os pontos de partida
                List<string> urls = new List<string>();

                urls.Add(Constants.BEC_LINK_MODALIDADE_2);
                urls.Add(Constants.BEC_LINK_MODALIDADE_5);

                /*Percorre cada modalidade. Como o portal BEC usa javascript para gerar as licitações, foi preciso
                 *criar dois caminhos, uma para cada modalidade: para Carta Convite, usa-se o HtmlAgilityPack e
                 *para Dispensa de Licitação, usa-se o Selenium (PhantomJS), que consegue lidar com javascript*/
                foreach (string uri in urls)
                {
                    if (!uri.Contains("Dispensa"))
                        GetConvites(uri);
                    else
                        GetDispensas(uri);
                }

                if (Directory.Exists(PathEdital))
                    Directory.Delete(PathEdital, true);

            }
            catch (Exception e)
            {
                RService.Log("Exception (Init)" + Name + ":" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            finally
            {
                if (NumLicitacoes <= 0)
                    LoteController.Delete(Lote);

                if (web != null)
                    web.Close();
            }
        }

        //Pega as licitações da modalidade Carta Convite
        private static void GetConvites(string uri)
        {
            RService.Log("(GetConvites) " + Name + ": Começando o processamento de licitações da modalidade Carta Convite " + "at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                /*Lista dos parametros do post*/
                NameValueCollection post = new NameValueCollection();

                /*Percorre cada uma das naturezas da modalidade*/
                foreach (var attr in WebHandle.GetHtmlDocOfPageDefaultEncoding(uri, post).DocumentNode.Descendants("span").Where(x => x.Attributes.Contains("id")
                     && x.Attributes["id"].Value.ToString().Contains(Constants.BEC_ID_NATUREZA)))
                {

                    /*Link para uma das naturezas*/
                    string urin = attr.SelectSingleNode("a").Attributes["href"].Value.ToString();

                    //post = new NameValueCollection();
                    CurrentPage = 2;
                    int count = 21;

                    /*Percorre as páginas de uma natureza (ex: 1;2;3)*/
                    HtmlDocument pagehtml = WebHandle.GetHtmlDocOfPageDefaultEncoding(urin, post);
                    while (pagehtml != null && count == 21)
                    {
                        RService.Log("(GetConvites) " + Name + ": Percorrendo os links da página.. " + (CurrentPage - 1) + " at {0}", Path.GetTempPath() + Name + ".txt");
                        //Teste para verificar licitação específica
                        //GetTestOC();

                        //Pega as licitações de cada página (OC's)
                        count = GetOC(pagehtml);
                        //Pega o html da próxima página
                        pagehtml = WebHandle.GetHtmlDocOfPageDefaultEncoding(urin, GetFormParameters(pagehtml, CurrentPage));
                        //Numero da proxima página
                        CurrentPage++;
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetConvites) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }

        }

        //Pega as licitações da modalidade Dispensa de Licitação
        private static void GetDispensas(string uri)
        {
            RService.Log("(GetDispensas) " + Name + ": Começando o processamento de licitações da modalidade Dispensa de Licitação " + "at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                int index = 2;
                List<string> listaOC = new List<string>();

                //LoadWebDriver();
                LoadDriver();

                //Acessa a página uma primeira vez para pegar a lista de licitações e salva os números numa lista de strings
                web.Navigate().GoToUrl(uri);
                Thread.Sleep(5000);
                web.FindElement(By.XPath("//*[@id=\"lblLoginFornecedor\"]/a")).Click();
                Thread.Sleep(5000);
                web.FindElement(By.Id("ctl00_c_area_conteudo_bt33022_Pesquisa")).Click();

                var OCs = web.FindElements(By.TagName("tr")).Where(x =>
                          x.GetAttribute("class").Contains("ItemStylePregao") ||
                          x.GetAttribute("class").Contains("AlternatingPregao")).ToList();

                foreach (var oc in OCs)
                {
                    var linkOC = oc.FindElement(By.TagName("a"));

                    if (linkOC.Text.Contains("OC") && !linkOC.Text.Contains("X9"))
                    {
                        string numOC = linkOC.Text + ";1" + linkOC.Text.Replace(DateTime.Today.Year.ToString() + "OC", DateTime.Today.Year.ToString().Remove(0, 2));
                        listaOC.Add(numOC);
                    }
                }

                //Com base na lista de strings, analisa cada uma para ver se ela existe ou não no banco
                foreach (var dispensa in listaOC)
                {
                    try
                    {
                        if (web == null)
                            LoadDriver();

                        if (!LicitacaoController.ExistsBEC(dispensa.Split(';')[0]))
                        {
                            web.Navigate().GoToUrl(uri);
                            web.FindElement(By.Id("ctl00_c_area_conteudo_bt33022_Pesquisa")).Click();
                            Thread.Sleep(3000);

                            var rows = web.FindElements(By.TagName("tr")).Where(x =>
                                       x.GetAttribute("class").Contains("ItemStylePregao") ||
                                       x.GetAttribute("class").Contains("AlternatingPregao")).ToList();
                            foreach (var row in rows)
                            {
                                var link = row.FindElement(By.TagName("a"));

                                /*Dependendo do valor do index, segue uma das duas opções, pois a estrutura da ID
                                    *que puxa o municipio e o objeto muda de acordo com o valor do index da licitação*/
                                if (link.Text == dispensa.Split(';')[0] && index < 10 && !LicitacaoController.ExistsBEC(dispensa.Split(';')[0]))
                                {
                                    string municipio = row.FindElement(By.Id(string.Format("ctl00_c_area_conteudo_grdvOC_publico_ctl0{0}_lbl_municipio", index))).GetAttribute("innerText").ToString();
                                    string objeto = row.FindElement(By.Id(string.Format("ctl00_c_area_conteudo_grdvOC_publico_ctl0{0}_lbl_natureza_despesa", index))).GetAttribute("innerText").ToString();
                                    string situacao = row.FindElement(By.Id(string.Format("ctl00_c_area_conteudo_grdvOC_publico_ctl0{0}_lbl_status", index))).GetAttribute("innerText").ToString();
                                    link.Click();
                                    Thread.Sleep(2000);
                                    HandleCreate(web, dispensa.Split(';')[0], municipio, objeto, situacao);
                                    break;
                                }
                                else if (link.Text == dispensa.Split(';')[0] && index > 9 && !LicitacaoController.ExistsBEC(dispensa.Split(';')[0]))
                                {
                                    string municipio = row.FindElement(By.Id(string.Format("ctl00_c_area_conteudo_grdvOC_publico_ctl{0}_lbl_municipio", index))).GetAttribute("innerText").ToString();
                                    string objeto = row.FindElement(By.Id(string.Format("ctl00_c_area_conteudo_grdvOC_publico_ctl{0}_lbl_natureza_despesa", index))).GetAttribute("innerText").ToString();
                                    string situacao = row.FindElement(By.Id(string.Format("ctl00_c_area_conteudo_grdvOC_publico_ctl{0}_lbl_status", index))).GetAttribute("innerText").ToString();
                                    link.Click();
                                    Thread.Sleep(2000);
                                    HandleCreate(web, dispensa.Split(';')[0], municipio, objeto, situacao);
                                    break;
                                }
                                index++;
                            }
                            index = 2;
                        }
                    }
                    catch (Exception e)
                    {
                        RService.Log("Exception (GetDispensas) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
                        if (web != null)
                        {
                            web.Close();
                            //web = null;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetDispensas) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        /*Retorna os parametros do post para acessar a próxima página*/
        private static NameValueCollection GetFormParameters(HtmlDocument htmlDoc, int page)
        {
            NameValueCollection formData = new NameValueCollection();
            try
            {
                string viewstate = htmlDoc.DocumentNode.Descendants().SingleOrDefault(x => x.Attributes.Contains("name") && x.Attributes["name"].Value.Equals("__VIEWSTATE")).Attributes["value"].Value.ToString();
                string eventvalidation = htmlDoc.DocumentNode.Descendants().SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("__EVENTVALIDATION")).Attributes["value"].Value.ToString();

                formData["ctl00_ToolkitScriptManager1_HiddenField"] = "";
                formData["__EVENTTARGET"] = "ctl00$ContentPlaceHolder1$gvResumoNatureza";
                formData["__EVENTARGUMENT"] = string.Format("Page${0}", page);
                formData["__VIEWSTATE"] = viewstate;
                formData["__VIEWSTATEENCRYPTED"] = "";
                formData["__EVENTVALIDATION"] = eventvalidation;
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetFormParameters)" + Name + ":" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
                formData = null;
            }
            return formData;
        }

        /*Percorre os links de cada página*/
        private static int GetOC(HtmlDocument htmlDoc)
        {
            int count = 1;

            try
            {
                NameValueCollection formData = new NameValueCollection();

                HtmlNode table = htmlDoc.DocumentNode.Descendants("table").FirstOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Equals("ctl00_ContentPlaceHolder1_gvResumoNatureza"));
                List<HtmlNode> trs = table.Descendants("tr").ToList();
                HtmlNode tr = trs[count];

                while (count <= trs.Count)
                {
                    if (tr.ChildNodes.Count == 6)
                    {
                        //string href = Constants.BEC_LINK_OC + link.SelectSingleNode("a").InnerText.ToString();
                        href = tr.Descendants("a").FirstOrDefault(x => x.Attributes.Contains("href")).GetAttributeValue("href", "").Replace("Edital", "Fornecedores_Dados_OC");
                        string ocnum = tr.Descendants("a").FirstOrDefault(x => x.Attributes.Contains("href")).InnerText;

                        string situacao = tr.ChildNodes[4].InnerText;

                        if (!situacao.Contains("Interposi"))
                        {
                            HandleCreate(WebHandle.GetHtmlDocOfPageDefaultEncoding(href, formData), ocnum, situacao);
                        }
                        else
                        {
                            RService.Log("Exception (GetOC)" + Name + ": Página da licitação fora do ar na fonte (Erro interno do servidor - 500) at {0}", Path.GetTempPath() + Name + ".txt");
                        }

                        count++;
                        if (count < trs.Count)
                            tr = trs[count];
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetOC)" + Name + ":" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }

            return count;
        }

        /*Cria os objetos Licitacao, Lote e LicitacaoArquivo fazendo as verificações necessárias.*/
        private static void HandleCreate(HtmlDocument htmlDoc, string ocnum, string situacao)
        {
            try
            {
                Regex regex = new Regex("\\d{4}OC");
                string ocn = ocnum;
                ocnum = "1" + regex.Replace(ocnum, DateTime.Now.ToString("yy"));

                //Criar um novo lote se for preciso, verifica o status da oc e também se já esta salva no bd
                if (!string.IsNullOrEmpty(ocn) && !LicitacaoController.ExistsBEC(ocn))
                {
                    //Preenche os dados da licitação e retorna para inserir na lista
                    Licitacao licitacao = CreateLicitacao(htmlDoc, ocnum, situacao);
                    if (licitacao != null && LicitacaoController.IsValid(licitacao, out mensagemErro))
                    {
                        licitacao.Observacoes = ocn;
                        licitacao.LinkEdital = href.Replace("OC_Item", "Edital");
                        Repo = new LicitacaoRepository();
                        Repo.Insert(licitacao);

                        //licitacoes.Add(licitacao);
                        RService.Log("(HandleCreate) " + Name + ": inserida com sucesso at {0}", Path.GetTempPath() + Name + ".txt");

                        CreateLicitacaoArquivo(licitacao, licitacao.LinkEdital);

                        NumLicitacoes++;
                    }
                    else
                    {
                        RService.Log("Exception (HandleCreate) " + Name + ": A licitação não foi salva - Motivo(s): " + mensagemErro + " at {0}", Path.GetTempPath() + Name + ".txt");
                    }
                }
                else if (!string.IsNullOrEmpty(ocn) && LicitacaoController.ExistsBEC(ocn) && LicitacaoController.SituacaoAlteradaBEC(ocn, situacao))
                {
                    int id = LicitacaoController.GetIdByObservacoes(ocn);
                    LicitacaoController.UpdateSituacaoByIdLicitacao(id, situacao);
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (HandleCreate)" + Name + ":" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        /*Cria os objetos Licitacao, Lote e LicitacaoArquivo fazendo as verificações necessárias.*/
        private static void HandleCreate(ChromeDriver web, string ocnum, string municipio, string objeto, string situacao)
        {
            try
            {
                Regex regex = new Regex("\\d{4}OC");
                string ocn = ocnum;
                ocnum = "1" + regex.Replace(ocnum, DateTime.Now.ToString("yy"));

                //Criar um novo lote se for preciso, verifica o status da oc e também se já esta salva no bd
                if (!string.IsNullOrEmpty(ocn) && !LicitacaoController.ExistsBEC(ocn))
                {
                    //Preenche os dados da licitação e retorna para inserir na lista
                    Licitacao licitacao = CreateLicitacao(web, ocnum, municipio, objeto, situacao);
                    if (licitacao != null && !LicitacaoController.IsValid(licitacao, out mensagemErro))
                    {
                        licitacao.Observacoes = ocn;
                        licitacao.LinkEdital = web.Url.Replace("OC_ITEM", "Edital");
                        Repo = new LicitacaoRepository();
                        Repo.Insert(licitacao);
                        //licitacoes.Add(licitacao);
                        RService.Log("(HandleCreate) " + Name + ": inserida com sucesso at {0}", Path.GetTempPath() + Name + ".txt");

                        CreateLicitacaoArquivo(licitacao, licitacao.LinkEdital);

                        NumLicitacoes++;
                    }
                    else
                    {
                        RService.Log("Exception (HandleCreate)" + Name + ": Licitação não salva. Motivo: " + mensagemErro + " at {0}", Path.GetTempPath() + Name + ".txt");
                    }
                }
                else if (!string.IsNullOrEmpty(ocn) && LicitacaoController.ExistsBEC(ocn) && LicitacaoController.SituacaoAlteradaBEC(ocn, situacao))
                {
                    int id = LicitacaoController.GetIdByObservacoes(ocn);
                    LicitacaoController.UpdateSituacaoByIdLicitacao(id, situacao);
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (HandleCreate)" + Name + ":" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        /*Cria o objeto licitacao arquivo, com o nome do arquivo do edital e a licitacao referente*/
        private static void CreateLicitacaoArquivo(Licitacao licitacao, string linkEdital)
        {
            RService.Log("(CreateLicitacaoArquivo) " + Name + ": Criando arquivo de edital da OC.. " + "at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                /*Transforma o html do edital em pdf, salva numa pasta temp e depois envia para um diretório FTP*/
                if (!Directory.Exists(PathEdital))
                {
                    Directory.CreateDirectory(PathEdital);
                }

                string fileName = FileHandle.GetATemporaryFileName();

                if (WebHandle.HtmlToPdf(linkEdital, PathEdital + fileName))
                {
                    #region FTP
                    //if (FTP.SendFileFtp(new FTP(PathEdital, fileName + WebHandle.ExtensionLastFileDownloaded, FTP.Adrss, FTP.Pwd, FTP.UName), Name))
                    //{
                    //    LicitacaoArquivo licitacaoArq = new LicitacaoArquivo();
                    //    licitacaoArq.NomeArquivo = fileName + WebHandle.ExtensionLastFileDownloaded;
                    //    licitacaoArq.NomeArquivoOriginal = Name + DateTime.Now.ToString("yyyyMMddHHmmss");
                    //    licitacaoArq.Status = 0;
                    //    licitacaoArq.IdLicitacao = licitacao.Id;

                    //    LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                    //    repoArq.Insert(licitacaoArq);

                    //    if (File.Exists(PathEdital + fileName + WebHandle.ExtensionLastFileDownloaded))
                    //    {
                    //        File.Delete(PathEdital + fileName + WebHandle.ExtensionLastFileDownloaded);
                    //    }
                    //}
                    //else
                    //{
                    //    RService.Log("(CreateLicitacaoArquivo) " + Name + ": error sending the file by FTP (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + Name + ".txt");
                    //}
                    #endregion

                    #region AWS
                    RService.Log("(CreateLicitacaoArquivo) " + Name + ": Enviando arquivo para Amazon S3... " + fileName + " at {0}", Path.GetTempPath() + Name + ".txt");

                    if (AWS.SendObject(licitacao, PathEdital, fileName + ".pdf"))
                    {
                        LicitacaoArquivo licitacaoArq = new LicitacaoArquivo();
                        licitacaoArq.NomeArquivo = fileName + WebHandle.ExtensionLastFileDownloaded;
                        licitacaoArq.NomeArquivoOriginal = Name + DateTime.Now.ToString("yyyyMMddHHmmss");
                        licitacaoArq.Status = 0;
                        licitacaoArq.IdLicitacao = licitacao.Id;

                        LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                        repoArq.Insert(licitacaoArq);

                        if (File.Exists(PathEdital + fileName + WebHandle.ExtensionLastFileDownloaded))
                        {
                            File.Delete(PathEdital + fileName + WebHandle.ExtensionLastFileDownloaded);
                        }

                        RService.Log("(CreateLicitacaoArquivo) " + Name + ": Arquivo " + fileName + " enviado com sucesso para Amazon S3" + " at {0}", Path.GetTempPath() + Name + ".txt");
                    }
                    else
                    {
                        RService.Log("Exception (CreateLicitacaoArquivo) " + Name + ": Erro ao enviar o arquivo para Amazon (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + Name + ".txt");
                    }
                    #endregion
                }
                else
                {
                    RService.Log("(CreateLicitacaoArquivo) " + Name + ": erro ao converter HTML para PDF (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + Name + ".txt");
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacaoArquivo) " + Name + ":" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        /*Cria uma nova licitação para a modalidade Carta Convite*/
        private static Licitacao CreateLicitacao(HtmlDocument htmDoc, string ocnum, string situacao)
        {
            RService.Log("(CreateLicitacao) " + Name + ": Criando licitação.. " + ocnum + " at {0}", Path.GetTempPath() + Name + ".txt");
            Licitacao licitacao = new Licitacao();

            try
            {
                licitacao.Lote = Lote;
                licitacao.Num = ocnum;
                licitacao.IdLicitacaoFonte = long.Parse(ocnum);
                licitacao.SegmentoAguardandoEdital = 0;
                licitacao.DigitacaoUsuario = 43; //Robo

                licitacao.Modalidade = Modalidades[0];
                licitacao.LinkSite = Constants.BEC_SITE;
                licitacao.Orgao = Orgao;
                licitacao.IdFonte = 507;
                licitacao.Excluido = 0;
                licitacao.Situacao = situacao;

                int count = 0;
                /*Tabela com as informações da OC*/
                var dadosOc = htmDoc.DocumentNode.Descendants().SingleOrDefault(x => x.Id == "ctl00_DetalhesOfertaCompra1_UpdatePanel1");
                /*Percorre todas as colunas de todas as linhas dessa tabela*/
                List<HtmlNode> inf = dadosOc.Descendants("td").ToList();
                foreach (var data in inf)
                {
                    if (data.InnerText.Trim().Contains("Proposta"))
                    {
                        MatchCollection matches = StringHandle.GetMatches(data.InnerText.Trim(), @"(\d{2}\/\d{2}\/\d{4}\s+\d{2}:\d{2}:\d{2})");
                        if (matches != null)
                        {
                            licitacao.EntregaData = DateHandle.Parse(matches[0].Groups[1].Value, "dd/MM/yyyy hh:mm:ss");
                            licitacao.AberturaData = DateHandle.Parse(matches[1].Groups[1].Value, "dd/MM/yyyy hh:mm:ss");

                            if (licitacao.AberturaData < DateTime.Today)
                                return null;
                        }
                    }
                    else if (data.InnerText.Trim().Contains("UC"))
                    {
                        licitacao.Departamento = data.InnerText.Split(':')[1].Trim();
                    }
                    count++;
                }

                var dadosUC = htmDoc.DocumentNode.Descendants().FirstOrDefault(x => x.Id == "formulario");
                /*Percorre todas as colunas de todas as linhas dessa tabela*/
                List<HtmlNode> infUC = dadosUC.Descendants("span").ToList();
                foreach (var info in infUC)
                {
                    if (info.Id.Equals("ctl00_c_area_conteudo_wuc_dados_oc1_txt_endereco_uge"))
                    {
                        licitacao.Endereco = info.InnerText.Trim();
                    }
                    else if (info.Id.Equals("ctl00_c_area_conteudo_wuc_dados_oc1_txt_local_entrega"))
                    {
                        string localidade = info.InnerText.Split('-').Last().ToString();
                        string[] cidadeEstado = localidade.Split('/');
                        string cidade = cidadeEstado.Last().ToString().ToLower().Trim();

                        //CultureInfo para poder tornar apenas as iniciais maiúsculas
                        var textInfo = new CultureInfo("pt-BR").TextInfo;

                        licitacao.Cidade = textInfo.ToTitleCase(cidade).ToString();
                        licitacao.Estado = Constants.BEC_ESTADO;
                        licitacao.EstadoFonte = Constants.BEC_UF;

                        cidade = licitacao.Cidade.ToString();

                        licitacao.CidadeFonte = Cidades.ContainsKey(cidade) ? Cidades[cidade] : CityUtil.GetCidadeFonte(cidade, Cidades);
                    }
                    else if (info.Id.Equals("ctl00_c_area_conteudo_wuc_dados_oc1_txt_natureza_despesa"))
                    {
                        licitacao.Objeto = "Contratação de " + info.InnerText.ToString().Trim();
                    }
                }

                NameValueCollection formData = new NameValueCollection();
                href = href.Replace("Fornecedores_Dados_OC", "OC_Item");
                var htmItens = WebHandle.GetHtmlDocOfPageDefaultEncoding(href, formData);
                licitacao.ItensLicitacao = licitacao.ItensLicitacao ?? new List<ItemLicitacao>();

                /*Contador das linhas da tabela*/
                CreateItensLicitacao(htmItens, licitacao);
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacao)" + Name + ":" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }

            return LicitacaoController.IsValid(licitacao, out mensagemErro) ? licitacao : null;
        }

        /*Cria uma nova licitação para a modalidade Dispensa de Licitação.*/
        private static Licitacao CreateLicitacao(ChromeDriver web, string ocnum, string municipio, string objeto, string situacao)
        {
            RService.Log("(CreateLicitacao) " + Name + ": Criando licitação.. " + ocnum + " at {0}", Path.GetTempPath() + Name + ".txt");

            Licitacao licitacao = new Licitacao();

            try
            {
                licitacao.Lote = Lote;
                licitacao.Num = ocnum;
                licitacao.IdLicitacaoFonte = long.Parse(ocnum);
                licitacao.SegmentoAguardandoEdital = 0;
                licitacao.DigitacaoUsuario = 43; //Robo

                licitacao.Modalidade = Modalidades[1];
                licitacao.LinkSite = Constants.BEC_SITE;
                licitacao.Orgao = Orgao;
                licitacao.IdFonte = 507;
                licitacao.Excluido = 0;
                licitacao.Situacao = situacao;

                licitacao.Departamento = web.FindElement(By.Id("ctl00_DetalhesOfertaCompra1_txtNomUge")).Text;

                //Busca as datas dentro da página da licitação
                string dates = web.FindElement(By.Id("ctl00_DetalhesOfertaCompra1_txtPerCotEletronica")).Text.Replace(" às", "").Replace(" a", "");
                MatchCollection matches = StringHandle.GetMatches(dates, @"(\d{2}\/\d{2}\/\d{4}\s+\d{2}:\d{2}:\d{2})");
                if (matches != null)
                {
                    licitacao.EntregaData = DateHandle.Parse(matches[0].Groups[1].Value, "dd/MM/yyyy hh:mm:ss");
                    licitacao.AberturaData = DateHandle.Parse(matches[1].Groups[1].Value, "dd/MM/yyyy hh:mm:ss");

                    if (licitacao.AberturaData < DateTime.Today)
                        return null;
                }

                //CultureInfo para tratar o nome do município
                var textInfo = new CultureInfo("pt-BR").TextInfo;
                municipio = municipio.ToLower();

                licitacao.Cidade = textInfo.ToTitleCase(municipio).ToString();
                licitacao.Estado = Constants.BEC_ESTADO;
                licitacao.EstadoFonte = Constants.BEC_UF;
                licitacao.CidadeFonte = Cidades.ContainsKey(licitacao.Cidade) ? Cidades[licitacao.Cidade] : CityUtil.GetCidadeFonte(licitacao.Cidade, Cidades);

                licitacao.Objeto = "Contratação de " + objeto;

                //Acessa a página com os itens da licitação
                web.FindElement(By.XPath("//*[@id=\"topMenu\"]/li[2]/a")).Click();
                Thread.Sleep(3000);
                licitacao.ItensLicitacao = licitacao.ItensLicitacao ?? new List<ItemLicitacao>();
                CreateItensLicitacao(web, licitacao);

            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacao)" + Name + ":" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }

            return LicitacaoController.IsValid(licitacao, out mensagemErro) ? licitacao : null;
        }

        private static void CreateItensLicitacao(ChromeDriver web, Licitacao licitacao)
        {
            RService.Log("(CreateItensLicitacao) " + Name + ": Criando itens do objeto da licitação num.. " + licitacao.IdLicitacaoFonte + " at {0}", Path.GetTempPath() + Name + ".txt");
            int count;

            var itens = web.FindElements(By.TagName("tr"));

            //Percorre cada linha da tabela com os itens, cria cada registro no banco e os adiciona à licitação
            foreach (var row in itens)
            {
                var text = row.GetAttribute("style").ToString();
                if (row.GetAttribute("style").Contains("background-color: ") && !row.GetAttribute("style").Contains("color: white; background-color: rgb(107, 105, 107); font-weight: bold;"))
                {
                    count = 0;
                    ItemLicitacao item = new ItemLicitacao();
                    foreach (var td in row.FindElements(By.TagName("td")))
                    {
                        switch (count)
                        {
                            case 0:
                                break;
                            case 1:
                                item.Numero = int.Parse(td.Text);
                                break;
                            case 2:
                                item.Codigo = td.Text;
                                break;
                            case 3:
                                item.DescricaoDetalhada = td.Text;
                                item.Descricao = item.DescricaoDetalhada.Length > 50 ? item.DescricaoDetalhada.Substring(0, 50) + "..." : item.DescricaoDetalhada;
                                break;
                            case 4:
                                item.Quantidade = int.Parse(td.Text);
                                break;
                            case 5:
                                item.Unidade = td.Text;
                                break;
                        }
                        count++;
                    }
                    item.MargemPreferencia = "0";
                    item.Decreto7174 = "0";
                    licitacao.ItensLicitacao.Add(item);
                }
            }
        }

        private static void CreateItensLicitacao(HtmlDocument htmDoc, Licitacao licitacao)
        {
            RService.Log("(CreateItensLicitacao) " + Name + ": Criando itens do objeto da licitação num.. " + licitacao.IdLicitacaoFonte + " at {0}", Path.GetTempPath() + Name + ".txt");

            int count;
            HtmlNode dadosOc;
            int rows = 1;
            /*Pega o html da table com o id dgItensOc pra essa OC*/
            dadosOc = htmDoc.DocumentNode.Descendants().FirstOrDefault(x => x.Id == "ctl00_c_area_conteudo_grdv_item");
            /*Percorre as linhas da tabela exceto o Header que tem o atributo class HeaderStyle*/
            foreach (var row in dadosOc.Descendants("tr")/*.Where(x => x.Attributes.Contains("class") && !x.Attributes["class"].Value.Contains("HeaderSt"))*/)
            {
                if (!row.InnerHtml.Contains("th scope="))
                {
                    ItemLicitacao item = new ItemLicitacao();
                    /*Contador usado para as colunas da tabela*/
                    count = 0;
                    /*Percorre cada coluna de uma linha da tabela Intes da Oferta de Compra*/
                    foreach (var data in row.Descendants("td"))
                    {
                        /*A terceira coluna da tabela de Itens da Oferta de Compra contém a descrição do Item.*/
                        switch (count)
                        {
                            case 0:
                                break;
                            case 1:
                                item.Numero = int.Parse(data.InnerText.Trim());
                                break;
                            case 2:
                                item.Codigo = data.InnerText.Trim();
                                break;
                            case 3:
                                string desc = data.InnerText.Trim();
                                item.DescricaoDetalhada = desc;
                                item.Descricao = desc.Length > 50 ? desc.Substring(0, 50) + "..." : desc;
                                break;
                            case 4:
                                item.Quantidade = int.Parse(data.InnerText.Trim());
                                break;
                            case 5:
                                item.Unidade = data.InnerText.Trim();
                                break;
                        }
                        count++;
                    }

                    item.MargemPreferencia = "0";
                    item.Decreto7174 = "0";
                    licitacao.ItensLicitacao.Add(item);
                    rows++;
                }
            }
        }
        #region OLD
        //Método para carregar o navegador fantasma para a modalidade Dispensa de Licitação
        //private static void LoadWebDriver()
        //{
        //    try
        //    {
        //        if (web != null)
        //            web.Quit();

        //        var driver = PhantomJSDriverService.CreateDefaultService();
        //        driver.HideCommandPromptWindow = true;

        //        var options = new PhantomJSOptions();

        //        web = new PhantomJSDriver(driver, options, TimeSpan.FromSeconds(120));
        //        web.Manage().Window.Maximize();
        //        web.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(120));

        //        wait = new WebDriverWait(web, TimeSpan.FromSeconds(120));

        //    }
        //    catch (Exception e)
        //    {
        //        RService.Log("Exception (Reload) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");

        //        if (web != null)
        //            web.Quit();
        //    }
        //}
        #endregion

        private static void LoadDriver()
        {
            if (web != null)
                web.Quit();

            var driver = ChromeDriverService.CreateDefaultService();
            driver.HideCommandPromptWindow = true;
            var op = new ChromeOptions();
            op.AddUserProfilePreference("download.default_directory", PathEdital);
            web = new ChromeDriver(driver, new ChromeOptions(), TimeSpan.FromSeconds(300));
            web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
            wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));
        }

    }
}