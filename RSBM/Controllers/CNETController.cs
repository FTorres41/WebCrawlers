using HtmlAgilityPack;
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace RSBM.Controllers
{
    class CNETController
    {
        #region Declaracao de variaveis
        private static ChromeDriver web;
        //private static PhantomJSDriver web;
        private static WebDriverWait wait;

        public static string Name { get; } = "CNET";
        public static string Historic { get; } = "HT";

        private static int CurrentPage;
        private static int NumLicitacoes;
        private static int NumHistoricos;
        public static string PathEditais { get; } = Path.GetTempPath() + DateTime.Now.ToString("yyyyMM") + Name + "/";

        private static Dictionary<string, Modalidade> NameToModalidade;
        private static Dictionary<string, Orgao> NameToOrgao;
        private static Dictionary<string, string> UfToCapital;
        private static Dictionary<string, Dictionary<string, int?>> UfToNomeCidadeToIdCidade;

        private static Lote Lote;
        private static ConfigRobot config;
        private static LicitacaoRepository Repo;
        private static bool TryReload;
        private static string mensagemErro;
        private static bool isHT;

        private static List<string> licitacoesHistorico;
        #endregion

        public static string GetNameRobot()
        {
            return isHT ? Name + Historic : Name;
        }

        /*Método pelo qual o serviço inicia o robô no Timer agendado.*/
        internal static void InitCallBack(object state)
        {
            try
            {
                isHT = false;

                //Busca as informações do robô no banco de dados.
                config = ConfigRobotController.FindByName(GetNameRobot());

                //Se o robô estiver ativo inicia o processamento.
                if (config.Active == 'Y')
                {
                    // Deleta o último arquivo de log.
                    if (File.Exists(Path.GetTempPath() + GetNameRobot() + ".txt"))
                        File.Delete(Path.GetTempPath() + GetNameRobot() + ".txt");

                    config.Status = 'R';
                    ConfigRobotController.Update(config);
                    using (FileStream fs = File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire")) { }
                    Init();

                    //Verifica se teve atualização
                    config = ConfigRobotController.FindByName(GetNameRobot());

                    config.NumLicitLast = NumLicitacoes;
                    RService.Log(GetNameRobot() + " find " + NumLicitacoes + " novas licitações at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
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
                RService.Log("Exception (InitCallBack) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            }

            RService.Log("Finished " + GetNameRobot() + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");

            EmailHandle.SendMail(Path.GetTempPath() + GetNameRobot() + ".txt", GetNameRobot());
        }

        /*Incia o processamento do robô*/
        private static void Init()
        {
            RService.Log("(Init) " + GetNameRobot() + ": Começando o processamento.. " + "at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");

            try
            {
                //Pega a data do banco, se não tiver pega a atual.
                string date = DateTime.Today.ToString("dd/MM/yyyy");
                if (config.PreTypedDate != null)
                {
                    date = config.PreTypedDate.Value.ToString("dd/MM/yyyy");
                    config.PreTypedDate = null;
                    ConfigRobotController.Update(config);
                }

                CurrentPage = 1;

                NameToModalidade = ModalidadeController.GetNameToModalidade();
                NameToOrgao = OrgaoController.GetNomeUfToOrgao();
                UfToCapital = CityUtil.GetUfToCapital();
                UfToNomeCidadeToIdCidade = CidadeController.GetUfToNameCidadeToIdCidade();
                Lote = LoteController.CreateLote(43, 508);
                Repo = new LicitacaoRepository();

                //test();

                /*Acessa a primeira página de licitações para o dia corrente*/
                HtmlDocument htmlDoc = WebHandle.GetHtmlDocOfPage(string.Format(Constants.CN_SITE, date, date, CurrentPage), Encoding.GetEncoding("ISO-8859-1"));

                /*Para cada página com licitações*/
                while (htmlDoc.DocumentNode.Descendants("form").
                    Where(x => x.Attributes.Contains("name") && x.Attributes["name"].Value.Contains("Form")).ToList().Count > 0)
                {
                    /*Coleta licitações de uma página*/
                    HandleCreate(htmlDoc, date);
                    /*Acessa próxima pág*/
                    CurrentPage++;
                    htmlDoc = WebHandle.GetHtmlDocOfPage(string.Format(Constants.CN_SITE, date, date, CurrentPage), Encoding.GetEncoding("ISO-8859-1"));
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (Init) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            }
            finally
            {
                if (NumLicitacoes <= 0)
                    LoteController.Delete(Lote);
            }
        }

        /*Método pelo qual o serviço inicia o robô no Timer agendado.*/
        internal static void HistoricCallBack(object state)
        {
            try
            {
                isHT = true;

                //Busca as informações do robô no banco de dados.
                config = ConfigRobotController.FindByName(GetNameRobot());

                //Se o robô estiver ativo inicia o processamento.
                if (config.Active == 'Y')
                {
                    // Deleta o último arquivo de log.
                    if (File.Exists(Path.GetTempPath() + GetNameRobot() + ".txt"))
                        File.Delete(Path.GetTempPath() + GetNameRobot() + ".txt");

                    config.Status = 'R';
                    ConfigRobotController.Update(config);
                    using (FileStream fs = File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire")) { }
                    HistoricFiles();

                    //Verifica se teve atualização
                    config = ConfigRobotController.FindByName(GetNameRobot());

                    //Verifica quantas licitações foram coletadas nessa execução, grava em log.
                    config.NumLicitLast = NumHistoricos;
                    RService.Log(GetNameRobot() + " find " + NumHistoricos + " novos itens de histórico de licitações at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
                    NumHistoricos = 0;

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
                RService.Log("Exception (HistoricCallBack) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            }

            RService.Log("Finished " + GetNameRobot() + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");

            EmailHandle.SendMail(Path.GetTempPath() + GetNameRobot() + ".txt", GetNameRobot());
        }

        /*Busca arquivos de edital para licitações antigas que tenham data de abertura maior do que o dia corrente.*/
        private static void HistoricFiles()
        {
            RService.Log("(HistoricFiles) " + GetNameRobot() + ": Buscando licitações..." + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            try
            {
                DateTime dataLimite = DateTime.Today.AddDays(-90);

                /*Busca licitações com a data de abertura anterior a 90 dias, ou que ainda não aconteceu*/
                //List<Licitacao> licitacoes = LicitacaoController.FindBySiteHistoric(Constants.CN_HOST, dataLimite);
                List<Licitacao> licitacoes = LicitacaoController.FindByRangeHistoric(Constants.CN_HOST, dataLimite);

                RService.Log("(HistoricFiles) " + GetNameRobot() + ": " + licitacoes.Count + " licitacões encontradas at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");

                licitacoesHistorico = new List<string>();

                foreach (Licitacao licitacao in licitacoes)
                {
                    RService.Log("(HistoricFiles) " + GetNameRobot() + ": Consultando ata e histórico da licitação " + licitacao.IdLicitacaoFonte + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
                    ConsultaAtaPregao(licitacao);
                    GetHistoricos(licitacao, GetNameRobot());
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (HistoricFiles) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            }
        }

        /*Percorre a pag criando licitações e lotes*/
        private static void HandleCreate(HtmlDocument htmlDoc, string date)
        {
            RService.Log("(HandleCreate) " + GetNameRobot() + ": Percorrendo licitações do dia " + date + " da página.. " + CurrentPage + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            try
            {
                foreach (var table in htmlDoc.DocumentNode.Descendants("table").
                    Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value.Equals("td")))
                {
                    /**/
                    string innerHtml = Regex.Replace(table.InnerHtml.Trim(), @"\s+", " ");
                    string idLicitacaoFonte = DateTime.Parse(date).ToString("yyMMdd") + StringHandle.GetMatches(innerHtml, "(?i)td_titulo_campo.*?>(.*?)</")[0].Groups[1].Value.Trim();
                    HtmlNode input = table.Descendants("input").SingleOrDefault(x => x.Attributes.Contains("name") && x.Attributes["name"].Value == "itens");
                    string linkEdital = Constants.CN_EDITAL_LINK + StringHandle.GetMatches(input.Attributes["onclick"].Value, @"'(?i)(.*?)'\);")[0].Groups[1].Value.Trim();
                    string uasg = StringHandle.GetMatches(innerHtml, "(?i)UASG:(.*?)<br>")[0].Groups[1].Value.Trim();
                    string pregao = StringHandle.GetMatches(innerHtml, "(?i)Nº(.*?)</b>")[0].Groups[1].Value.Trim();
                    string modalidadeText = StringHandle.RemoveAccent(StringHandle.GetMatches(innerHtml, "(?i)UASG:.*?<b>(.*?)Nº")[0].Groups[1].Value.Trim().ToUpper());
                    Modalidade modalTemp = NameToModalidade.ContainsKey(modalidadeText) ? NameToModalidade[modalidadeText] : null;

                    TryReload = true;
                    /*Cria uma licitação com as informações da pág*/
                    Licitacao licit = CreateLicitacao(idLicitacaoFonte, innerHtml, linkEdital);

                    if (licit != null && string.IsNullOrEmpty(mensagemErro) && !LicitacaoController.ExistsCNET(licit.IdLicitacaoFonte))
                    {
                        try
                        {
                            Repo.Insert(licit);
                            RService.Log("(HandleCreate) " + Name + ": Licitação " + licit.IdLicitacaoFonte + " inserida com sucesso at {0}", Path.GetTempPath() + Name + ".txt");
                            //GetHistoricos(licit, GetNameRobot());
                            //GetFiles(licit);
                            NumLicitacoes++;
                            //SegmentarLicitacaoPorItens(licit);
                        }
                        catch (Exception e)
                        {
                            RService.Log("Exception (CreateLicitacao) " + Name + ": A licitação de nº " + licit.Num + " não foi salva - Motivo(s): " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
                        }
                    }
                    else
                    {
                        if (licit.Orgao != null)
                            RService.Log("Exception (CreateLicitacao) " + Name + ": A licitação de nº " + licit.Num + " e órgão " + licit.Orgao.Nome + " - " + licit.Orgao.Estado + " não foi salva - Motivo(s): " + (mensagemErro == "" ? "Licitação já capturada" : mensagemErro) + " at {0}", Path.GetTempPath() + Name + ".txt");
                        else
                            RService.Log("Exception (CreateLicitacao) " + Name + ": A licitação de nº " + licit.Num + " não foi salva - Motivo(s): " + mensagemErro + " at {0}", Path.GetTempPath() + Name + ".txt");
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (HandleCreate) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            }
        }

        private static void RandomSleep()
        {
            Random rnd = new Random(5000);
            Thread.Sleep(rnd.Next());
        }

        private static void GetFiles(Licitacao licit)
        {
            try
            {
                if (!Directory.Exists(PathEditais))
                    Directory.CreateDirectory(PathEditais);

                string fileName = string.Format("AnexoLicitacao{0}", licit.Id), downloadedFile = string.Empty;

                if (web != null)
                    web.Quit();

                var driver = ChromeDriverService.CreateDefaultService();
                driver.HideCommandPromptWindow = true;
                var op = new ChromeOptions();
                op.AddUserProfilePreference("download.default_directory", PathEditais);
                web = new ChromeDriver(driver, new ChromeOptions(), TimeSpan.FromSeconds(300));
                web.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
                wait = new WebDriverWait(web, TimeSpan.FromSeconds(300));

                web.Navigate().GoToUrl(licit.LinkEdital);

                web.ExecuteScript(GetScriptFillCaptchaXPath("//*[@id=\"form1\"]/table/tbody/tr[1]/td/table/tbody/tr/td[2]/span/img", "idLetra"));
                web.FindElement(By.Id("idSubmit")).Click();

                Thread.Sleep(15000);

                downloadedFile = Directory.GetFiles(PathEditais)[0].Split('/')[1];

                if (File.Exists(PathEditais + downloadedFile))
                {
                    RService.Log("(GetFiles) " + Name + ": Enviando o arquivo para Amazon S3... " + fileName + " at {0}", Path.GetTempPath() + Name + ".txt");

                    if (AWS.SendObject(licit, PathEditais, downloadedFile))
                    {
                        LicitacaoArquivo licitacaoArq = new LicitacaoArquivo();
                        licitacaoArq.NomeArquivo = downloadedFile;
                        licitacaoArq.NomeArquivoOriginal = Name + DateTime.Now.ToString("yyyyMMddHHmmss");
                        licitacaoArq.NomeArquivoFonte = fileName;
                        licitacaoArq.Status = 0;
                        licitacaoArq.IdLicitacao = licit.Id;

                        LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                        repoArq.Insert(licitacaoArq);

                        if (File.Exists(PathEditais + downloadedFile))
                        {
                            File.Delete(PathEditais + downloadedFile);
                        }

                        RService.Log("(GetFiles) " + Name + ": Arquivo " + fileName + " enviado com sucesso para Amazon S3" + " at {0}", Path.GetTempPath() + Name + ".txt");
                    }
                    else
                    {
                        RService.Log("Exception (GetFiles) " + Name + ": Erro ao enviar o arquivo para Amazon (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + Name + ".txt");
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetFiles) " + Name + ": " + e.Message + " / " + e.StackTrace + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            finally
            {
                if (web != null)
                    web.Close();
            }
        }

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

        private static void GetCaptchaImg(IWebElement element, Bitmap image, string tempImgCrop)
        {
            RService.Log("(GetCaptchaImg) " + GetNameRobot() + ": Achando captcha... " + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            try
            {
                Point p = element.Location;

                int eleWidth = element.Size.Width;
                int eleHeight = element.Size.Height;

                Size size = new Size(eleWidth, eleHeight);
                Rectangle r = new Rectangle(p, size);

                Image ib = image.Clone(r, image.PixelFormat);

                ib.Save(tempImgCrop);

                image.Dispose();
                ib.Dispose();
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetCaptchaImg) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            }
        }

        private static void SegmentarLicitacaoPorItens(Licitacao licit)
        {
            var itens = licit.ItensLicitacao;
            var segmentos = new List<int>();

            foreach (var item in itens)
            {
                var segmentosParaAdicionar = SegmentoRepository.ObterSegmentos(item.Descricao, licit.Id);

                foreach (var segmento in segmentosParaAdicionar)
                {
                    if (!segmentos.Contains(segmento))
                        segmentos.Add(segmento);
                }
            }

            SegmentoRepository.InserirSegmentacao(segmentos, licit.Id);
        }

        private static void SegmentarLicitacao(Licitacao licitacao)
        {
            List<Segmento> segmentos = SegmentoController.CreateListaSegmentos(licitacao);

            foreach (Segmento segmento in segmentos)
            {
                int objMatch = 0, segmentoCount = 0;
                bool itemMatch = false;
                var palavrasChave = segmento.PalavrasChave.Split(';');

                if (palavrasChave.Length > 0)
                {
                    foreach (var palavrachave in palavrasChave)
                    {
                        if (licitacao.Objeto.ToUpper().Contains(palavrachave))
                        {
                            objMatch++;
                        }

                        foreach (var item in licitacao.ItensLicitacao)
                        {
                            if (item.Descricao.ToUpper().Contains(palavrachave) || item.DescricaoDetalhada.ToUpper().Contains(palavrachave))
                            {
                                itemMatch = true;
                            }
                        }
                    }

                    if (objMatch >= 4 && itemMatch == true)
                    {
                        LicitacaoSegmento licSeg = new LicitacaoSegmento()
                        {
                            IdLicitacao = licitacao.Id,
                            IdSegmento = segmento.IdSegmento
                        };

                        segmentoCount++;
                        LicitacaoSegmentoRepository repoLS = new LicitacaoSegmentoRepository();
                        repoLS.Insert(licSeg);
                    }
                }

                if (segmentoCount > 0)
                {
                    RService.Log("(SegmentarLicitacao) " + Name + ": Licitação " + licitacao.IdLicitacaoFonte + " foi segmentada em " + segmentoCount + " segmentos at {0}", Path.GetTempPath() + Name + ".txt");
                }
                else
                {
                    RService.Log("(SegmentarLicitacao) " + Name + ": Licitação " + licitacao.IdLicitacaoFonte + " não foi segmentada at {0}", Path.GetTempPath() + Name + ".txt");
                }
            }
        }

        /*Cria uma licitação*/
        private static Licitacao CreateLicitacao(string num, string innerHtml, string linkEdital)
        {
            RService.Log("(CreateLicitacao) " + GetNameRobot() + ": Criando licitação.. " + num + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");

            Licitacao licitacao = new Licitacao();
            MatchCollection helper;

            try
            {
                licitacao.IdFonte = 508;
                helper = StringHandle.GetMatches(innerHtml, "(?i)Nº(.*?)</b>");
                licitacao.Num = helper != null ? helper[0].Groups[1].Value.Trim() : null;
                licitacao.IdLicitacaoFonte = long.Parse(num);
                licitacao.LinkEdital = linkEdital;
                licitacao.LinkSite = Constants.CN_HOST;
                licitacao.Excluido = 0;
                licitacao.SegmentoAguardandoEdital = 0;
                licitacao.DigitacaoUsuario = 43; //Robo
                licitacao.Lote = Lote;

                //licitacao.DigitacaoData = null;
                //licitacao.ProcessamentoData = null;

                helper = StringHandle.GetMatches(innerHtml, "<td.*class=\\\"td_titulo_campo\\\".*>(.*)</td.*</table");
                string estadoCidade = helper != null ? helper[0].Groups[1].Value.Trim() : null;
                licitacao.EstadoFonte = estadoCidade != null ? CidadeController.GetUFCidade(estadoCidade).First().Key : null;

                Dictionary<string, int?> ufToCidade = UfToNomeCidadeToIdCidade.ContainsKey(licitacao.EstadoFonte) ? UfToNomeCidadeToIdCidade[licitacao.EstadoFonte] : null;

                licitacao.CidadeFonte = ufToCidade != null ? StringHandle.FindKeyRegex(ufToCidade, CidadeController.GetUFCidade(estadoCidade).First().Value) : null;
                licitacao.Cidade = licitacao.CidadeFonte != null ? CidadeController.GetUFCidade(estadoCidade).First().Value : UfToCapital.ContainsKey(licitacao.EstadoFonte) ? UfToCapital[licitacao.EstadoFonte] : null;

                helper = StringHandle.GetMatches(innerHtml, "(?i)UASG:(.*?)<br>");
                licitacao.Uasg = helper != null ? helper[0].Groups[1].Value.Trim() : null;

                helper = StringHandle.GetMatches(innerHtml, "(?i)UASG:.*?<b>(.*?)Nº");
                string modalidade = helper != null ? StringHandle.RemoveAccent(helper[0].Groups[1].Value.Trim().ToUpper()) : null;
                licitacao.Modalidade = NameToModalidade.ContainsKey(modalidade) ? NameToModalidade[modalidade] : null;

                helper = StringHandle.GetMatches(innerHtml, "(?i)/b>.*Objeto:.*nico.*?-(.*?)<br><b>Edital");
                if (helper == null)
                {
                    helper = StringHandle.GetMatches(innerHtml, "(?i)/b>.*Objeto:(.*?)<br><b>Edital");
                    licitacao.Objeto = helper != null ? helper[0].Groups[1].Value.Trim() : null;
                }
                else
                {
                    licitacao.Objeto = helper[0].Groups[1].Value.Trim();
                }
                helper = StringHandle.GetMatches(innerHtml, "(?i)Endere.*:</b>(.*?)<br><b>Tele");
                licitacao.Endereco = helper != null ? helper[0].Groups[1].Value.Trim() : null;

                helper = StringHandle.GetMatches(innerHtml, "(?i)Entrega.*?Proposta:</b>(.*?)Hs");

                string abertura;
                string entrega;
                string data = entrega = helper != null ? helper[0].Groups[1].Value.Trim() : null;

                helper = StringHandle.GetMatches(data, "(\\d{2}/\\d{2}/\\d{4})");
                entrega = helper != null ? helper[0].Groups[1].Value.Trim() : "";
                helper = StringHandle.GetMatches(data, "(\\d{2}:\\d{2}?)");
                entrega += " ";
                entrega += helper != null ? helper[0].Groups[1].Value.Trim() : "";
                entrega.Trim();

                if (Regex.Match(innerHtml, @"(?i)Abertura").Success)
                {
                    helper = StringHandle.GetMatches(innerHtml, "(?i)Abertura.*?Proposta:</b>(.*?)Hs");
                    string dt;
                    dt = abertura = helper != null ? helper[0].Groups[1].Value.Trim() : null;
                    helper = StringHandle.GetMatches(dt, "(\\d{2}/\\d{2}/\\d{4})");
                    abertura = helper != null ? helper[0].Groups[1].Value.Trim() : "";
                    helper = StringHandle.GetMatches(dt, "(\\d{2}:\\d{2}?)");
                    abertura += " ";
                    abertura += helper != null ? helper[0].Groups[1].Value.Trim() : "";
                    abertura.Trim();
                }
                else
                {
                    abertura = entrega;
                }

                if (licitacao.Modalidade.Id == 29)
                    licitacao.AberturaData = DateHandle.Parse(entrega, "dd/MM/yyyy hh:mm");
                else
                    licitacao.AberturaData = DateHandle.Parse(abertura, "dd/MM/yyyy hh:mm");

                licitacao.EntregaData = DateHandle.Parse(entrega, "dd/MM/yyyy hh:mm");

                int i = 0;
                helper = StringHandle.GetMatches(innerHtml, "(?i)</table>.*<b>(.*?)<br>Código da UASG:");
                string orgaoDepartamento = helper != null ? helper[0].Groups[1].Value.Trim().Replace("<br>", "!") : null;
                if (orgaoDepartamento != null)
                {
                    int len = orgaoDepartamento.Split('!').Length;
                    foreach (string dep in orgaoDepartamento.Split('!'))
                    {
                        if (i != 0)
                        {
                            if (i == 1)
                                licitacao.Departamento = dep;

                            if (len - 1 != i)
                                licitacao.Observacoes += dep + "/";
                            else
                                licitacao.Observacoes += dep;
                        }
                        i++;
                    }

                    string orgao = orgaoDepartamento.Split('!')[0];
                    licitacao.Orgao = OrgaoController.GetOrgaoByNameAndUf(orgao.Trim().ToUpper() + ":" + licitacao.EstadoFonte.ToUpper(), NameToOrgao);
                }
                else
                {
                    licitacao.Orgao = OrgaoController.FindById(390);//NÃO ESPECIFICADO
                }
                string[] tipos = { "M", "S" };
                licitacao.ItensLicitacao = licitacao.ItensLicitacao ?? new List<ItemLicitacao>();

                helper = StringHandle.GetMatches(innerHtml, "button.*?\'(.*?)\'");
                CreateItensLicitacao(helper[0].Groups[1].Value.Trim(), licitacao, helper, tipos);
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacao) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
                if (TryReload)
                {
                    TryReload = false;

                    licitacao = CreateLicitacao(num, innerHtml, linkEdital);
                }
            }

            return LicitacaoController.IsValid(licitacao, out mensagemErro) ? licitacao : licitacao;
        }

        private static void CreateItensLicitacao(string parametros, Licitacao licitacao, MatchCollection helper, string[] tipos)
        {
            RService.Log("(CreateItensLicitacao) " + GetNameRobot() + ": Coletando itens do objeto da licitação.. " + licitacao.Num + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            try
            {
                bool proximaPagina = false;

                foreach (string tipo in tipos)
                {
                    var url = string.Format(Constants.CN_ITENS_EDITAL, parametros, tipo);

                    HtmlDocument itensDoc = WebHandle.GetHtmlDocOfPage(url, Encoding.GetEncoding("ISO-8859-1"));

                    do
                    {
                        var noItem = itensDoc.DocumentNode.Descendants("td").Where(x => x.Attributes.Contains("width") && x.Attributes["width"].Value.Equals("650"));

                        foreach (var item in noItem)
                        {
                            List<HtmlNode> detalhes = item.Descendants("span").ToList();

                            ItemLicitacao itemL = new ItemLicitacao();

                            helper = StringHandle.GetMatches(detalhes[0].InnerText, "(\\d+).*?-");

                            if (helper != null)
                            {
                                itemL.Numero = helper != null ? (int?)int.Parse(helper[0].Groups[1].Value.Trim()) : null;

                                helper = StringHandle.GetMatches(detalhes[1].InnerText, "(?i)Decreto 7174:(.*?)Aplica");
                                itemL.Decreto7174 = helper != null ? StringHandle.RemoveAccent(helper[0].Groups[1].Value.Trim()).ToUpper().Equals("NAO") ? "0" : "1" : null;
                                helper = StringHandle.GetMatches(detalhes[1].InnerText, "(?i)Unidade de fornecimento:(.*)");
                                itemL.Unidade = helper != null ? helper[0].Groups[1].Value.Trim() : null;
                                if (itemL.Unidade.Length >= 50)
                                {
                                    itemL.Unidade = itemL.Unidade.Substring(0, 46) + "...";
                                }
                                helper = StringHandle.GetMatches(detalhes[1].InnerText, "(?i)preferência.*Quantidade:(.*?)Unidade");
                                itemL.Quantidade = helper != null ? (int?)int.Parse(helper[0].Groups[1].Value.Trim()) : null;
                                helper = StringHandle.GetMatches(detalhes[1].InnerText, "(?i)Tratamento Diferenciado:(.*?)Aplicabi");
                                itemL.TratamentoDiferenciado = helper != null ? helper[0].Groups[1].Value.Trim() : null;
                                helper = StringHandle.GetMatches(detalhes[1].InnerText, "(?i)Margem de preferência:(.*?)Quantidade");
                                itemL.MargemPreferencia = helper != null ? StringHandle.RemoveAccent(helper[0].Groups[1].Value.Trim()).ToUpper().Equals("NAO") ? "0" : "1" : null;
                                itemL.Tipo = tipo;
                                helper = StringHandle.GetMatches(detalhes[0].InnerText, "-(.*)");
                                itemL.Descricao = helper[0].Groups[1].Value.Trim();

                                helper = StringHandle.GetMatches(detalhes[1].InnerText, "(?i)(.*?)Tratamento Diferenciado");
                                itemL.DescricaoDetalhada = helper != null ? helper[0].Groups[1].Value.Trim() : null;

                                licitacao.ItensLicitacao.Add(itemL);
                            }
                        }

                        proximaPagina = false;

                        //Verifica se tem mais paginas com itens da licitação
                        if (Regex.IsMatch(itensDoc.DocumentNode.InnerHtml, "Próxima( *)Página"))
                        {
                            var pagina = int.Parse(
                                Regex.Match(Regex.Match(itensDoc.DocumentNode.InnerHtml, @"Página( *)[\d]+( *)de").Value, @"\d+").Value);

                            //Inicia o navegado Phantom para selecionar a proxima pagina
                            LoadWebDriver();
                            //LoadPhantomJSDriver();

                            web.Navigate().GoToUrl(url);

                            //Clica para chamar a proxima pagina
                            for (int i = 1; i <= pagina; i++)
                            {
                                web.ExecuteScript("document.getElementsByName('proximas')[0].click();");
                                wait.Until(ExpectedConditions.ElementIsVisible(By.Name("anteriores")));
                                Thread.Sleep(2000);
                            }

                            //Coloca o conteudo na pagina no DocumentNode
                            itensDoc.DocumentNode.InnerHtml = web.PageSource;

                            proximaPagina = true;
                        }

                    }
                    while (proximaPagina);
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateItensLicitacao) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            }
            finally
            {
                if (web != null)
                    web.Close();
            }
        }

        /*Busca os históricos por licitação*/
        private static void GetHistoricos(Licitacao l, string nameRobot)
        {
            try
            {
                string num = Regex.Replace(l.Num, @"[^\d+]", "");
                string parametros = string.Format(@"coduasg={0}&modprp=5&numprp={1}", l.Uasg, num);

                HtmlDocument htmlDocument = WebHandle.HtmlParaObjeto(Constants.CN_HISTORICO_LINK + parametros, Encoding.GetEncoding("ISO-8859-1"));

                if (htmlDocument.DocumentNode.LastChild == null)
                    return;

                if (!Regex.IsMatch(htmlDocument.DocumentNode.InnerHtml, @"rico de eventos "))
                {
                    string expRegular = "<tr bgcolor=\"#ffffff\" class=\"tex3a\">(.+?)</tr>";
                    MatchCollection matches = Regex.Matches(Regex.Replace(htmlDocument.DocumentNode.InnerHtml, "\n", "").Replace("\r", ""), expRegular);

                    if (matches.Count > 0)
                    {
                        RService.Log("(GetHistoricos) " + nameRobot + ": Buscando histórico da licitação: " + l.IdLicitacaoFonte + " at {0}", Path.GetTempPath() + nameRobot + ".txt");

                        for (int i = 0; i < matches.Count; i++)
                        {
                            LicitacaoHistorico historico = new LicitacaoHistorico();
                            historico.IdLicitacao = l.Id;

                            var helper = Regex.Match(matches[i].Value, @"\d{2}/\d{2}/\d{4}( +)\d{2}\:\d{2}\:\d{2}").Value;
                            DateTime valorData = new DateTime();
                            historico.DataCadastro = DateTime.TryParse(helper, out valorData) ? valorData : new DateTime();

                            helper = Regex.Match(matches[i].Value, "<td align=\"left\">(.+?)</td>").Value;
                            helper = Regex.Replace(helper, @"&nbsp", " ");
                            helper = Regex.Replace(helper, "<td align=\"left\">", "");
                            helper = Regex.Replace(helper, "</td>", "");
                            historico.Historico = helper.Trim();

                            if (LicitacaoHistoricoController.Insert(historico))
                            {
                                RService.Log("(ConsultaAtaPregao) " + GetNameRobot() + ": Histórico registrado com sucesso" + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
                                NumHistoricos++;
                            }
                        }

                        if (matches.Count == 1)
                        {
                            RService.Log("(GetHistoricos) " + nameRobot + ": Encontrado 1 item de histórico at {0}", Path.GetTempPath() + nameRobot + ".txt");
                        }
                        else if (matches.Count > 1)
                        {
                            RService.Log("(GetHistoricos) " + nameRobot + ": Encontrados " + matches.Count + " itens de histórico at {0}", Path.GetTempPath() + nameRobot + ".txt");
                        }
                        else
                        {
                            RService.Log("(GetHistoricos) " + nameRobot + ": Não foram encontrados novos itens de histórico at {0}", Path.GetTempPath() + nameRobot + ".txt");
                        }
                    }
                    else
                    {
                        RService.Log("(GetHistoricos) " + nameRobot + ": Não foram encontrados itens de histórico at {0}", Path.GetTempPath() + nameRobot + ".txt");
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetHistoricos) " + nameRobot + " para licitação " + l.IdLicitacaoFonte + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + nameRobot + ".txt");
            }
        }

        private static void ConsultaAtaPregao(Licitacao licitacao)
        {
            RService.Log("(ConsultaAtaPregao) " + GetNameRobot() + ": Buscando itens da ata do pregão at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            try
            {
                if (!string.IsNullOrEmpty(licitacao.Uasg) && !string.IsNullOrEmpty(licitacao.NumPregao))
                {
                    List<LicitacaoHistorico> itens = new List<LicitacaoHistorico>();

                    string _num = Regex.Replace(licitacao.NumPregao, @"[^\d+]", "");
                    string _url = string.Format(Constants.CN_ATA_PREGAO, licitacao.Uasg, _num);
                    HtmlDocument htmlDoc = WebHandle.GetHtmlDocOfPage(_url, Encoding.GetEncoding("ISO-8859-1"));

                    string _valorRegex = Regex.Match(htmlDoc.DocumentNode.InnerHtml, @"exibeQuadro\(\d+").Value;
                    string _codPregao = Regex.Match(_valorRegex, @"\d+").Value;

                    if (!string.IsNullOrEmpty(_codPregao))
                    {
                        Dictionary<string, string> tipos = new Dictionary<string, string>();
                        tipos.Add("A", "Aviso");
                        tipos.Add("E", "Esclarecimento");
                        tipos.Add("I", "Impugnação");

                        foreach (var tipo in tipos)
                        {
                            RService.Log("(ConsultaAtaPregao) " + GetNameRobot() + ": Buscando itens do tipo: " + tipo.Value + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");

                            string _urlItem = string.Format(Constants.CN_PREGAO_AVISOS_DETALHE, _codPregao, tipo.Key);

                            htmlDoc = WebHandle.GetHtmlDocOfPage(_urlItem, Encoding.GetEncoding("ISO-8859-1"));

                            var listaQaCod = Regex.Matches(htmlDoc.DocumentNode.InnerHtml, @"qaCod=[\d]+&texto=T");

                            foreach (var linkQaCod in listaQaCod)
                            {
                                try
                                {
                                    //busca mensagem
                                    string _urlItemDesc = string.Format(Constants.CN_PREGAO_AVISOS_ITEM, linkQaCod.ToString());
                                    htmlDoc = WebHandle.GetHtmlDocOfPage(_urlItemDesc, Encoding.GetEncoding("ISO-8859-1"));

                                    LicitacaoHistorico historico = new LicitacaoHistorico();
                                    historico.IdLicitacao = licitacao.Id;

                                    var tds = htmlDoc.DocumentNode.Descendants("td").Where(p => !p.InnerText.Trim().Equals(""));

                                    //pega data e descrição
                                    foreach (var td in tds)
                                    {
                                        try
                                        {
                                            if (historico.DataCadastro == new DateTime() && Regex.IsMatch(td.InnerText, @"\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2}"))
                                            {
                                                string helper = Regex.Match(td.InnerText, @"\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2}").Value;
                                                DateTime valorData = new DateTime();
                                                historico.DataCadastro = DateTime.TryParse(helper, out valorData) ? valorData : new DateTime();

                                                historico.Historico = td.InnerText.Split(new string[] { helper }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                                            }
                                            else
                                            {
                                                historico.Mensagem = td.InnerText;
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            RService.Log("Exception (ConsultaAtaPregao) getData" + GetNameRobot() + " para a licitacao " + licitacao.IdLicitacaoFonte + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
                                        }
                                    }

                                    //Caso tenha resposta
                                    string _codQa = linkQaCod.ToString().Replace("&texto=T", "");
                                    _urlItemDesc = string.Format(Constants.CN_PREGAO_AVISOS_ITEM, _codQa) + "&texto=R";
                                    htmlDoc = WebHandle.GetHtmlDocOfPage(_urlItemDesc, Encoding.GetEncoding("ISO-8859-1"));
                                    tds = htmlDoc.DocumentNode.Descendants("td").Where(p => !p.InnerText.Trim().Equals(""));

                                    if (tds.Count() > 2)
                                    {
                                        foreach (var td in tds)
                                        {
                                            if (!Regex.IsMatch(td.InnerText, @"\d{2}/\d{2}/\d{4} \d{2}:\d{2}:\d{2}"))
                                            {
                                                historico.Resposta = td.InnerText;
                                            }
                                        }
                                    }

                                    if (LicitacaoHistoricoController.Insert(historico))
                                        NumHistoricos++;
                                }
                                catch (Exception e)
                                {
                                    RService.Log("Exception (ConsultaAtaPregao) getMensagem " + GetNameRobot() + " para a licitacao " + licitacao.IdLicitacaoFonte + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
                                }
                            }
                        }
                    }
                    else
                    {
                        RService.Log("(ConsultaAtaPregao) " + GetNameRobot() + ": Pregão não contém ata pois não foi encerrado at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (ConsultaAtaPregao) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");
            }
        }

        private static void test()
        {
            /********************************************************/

            int[] idsLicitacoes = new[] {
                    127046,
                    127054,
                    127066,
                    127090,
                    127098,
                    127105,
                    127106,
                    127110,
                    127160,
                    127165,
                    127168,
                    127177,
                    127205,
                    127310


                };

            List<Licitacao> licitacoesInsert = new List<Licitacao>();

            foreach (var id in idsLicitacoes)
            {
                LicitacaoRepository repol = new LicitacaoRepository();
                licitacoesInsert.Add(repol.FindById(id));
            }

            foreach (var licita in licitacoesInsert)
            {
                string parametros = string.Format("?coduasg={0}&modprp=5&numprp={1}", licita.Uasg, licita.NumPregao.Replace("/", ""));
                var url = string.Format(Constants.CN_ITENS_EDITAL, parametros, "S");

                int totalLic = licita.ItensLicitacao.Count;
                HtmlDocument html = WebHandle.GetHtmlDocOfPage(url, Encoding.GetEncoding("ISO-8859-1"));
                string qnde = Regex.Match(html.DocumentNode.InnerHtml, @"Quantidade( *)Total de Itens: \d+", RegexOptions.IgnoreCase).Value;
                if (string.IsNullOrEmpty(qnde))
                {
                    url = string.Format(Constants.CN_ITENS_EDITAL, parametros, "M");
                    html = WebHandle.GetHtmlDocOfPage(url, Encoding.GetEncoding("ISO-8859-1"));
                    qnde = Regex.Match(html.DocumentNode.InnerHtml, @"Quantidade( *)Total de Itens: \d+", RegexOptions.IgnoreCase).Value;
                }

                int numqnde = totalLic;
                if (!string.IsNullOrEmpty(qnde))
                    numqnde = int.Parse(Regex.Match(qnde, @"\d+", RegexOptions.IgnoreCase).Value);

                if (totalLic != numqnde)
                {
                    foreach (var item in licita.ItensLicitacao)
                    {
                        ItemLicitacaoRepository irepo = new ItemLicitacaoRepository();
                        irepo.Delete(item);
                    }

                    licita.ItensLicitacao = new List<ItemLicitacao>();

                    string[] tipos = { "M", "S" };
                    CreateItensLicitacao(parametros, licita, StringHandle.GetMatches("", ""), tipos);

                    int totalLicD = licita.ItensLicitacao.Count;

                    if (licita != null)
                    {
                        Repo.Update(licita);
                        NumLicitacoes++;
                    }
                }
            }
            /********************************************************/
        }
        #region OLD
        //private static void LoadWebDriver()
        //{
        //    try
        //    {
        //        if (web != null)
        //            web.Quit();

        //        var drive = PhantomJSDriverService.CreateDefaultService();
        //        drive.HideCommandPromptWindow = true;
        //        web = new PhantomJSDriver(drive);

        //        web.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(60));
        //        web.Manage().Window.Maximize();
        //        wait = new WebDriverWait(web, TimeSpan.FromSeconds(30));

        //    }
        //    catch (Exception e)
        //    {
        //        RService.Log("Exception (Reload) " + GetNameRobot() + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + GetNameRobot() + ".txt");

        //        if (web != null)
        //            web.Quit();
        //    }
        //}
        #endregion

        private static void LoadWebDriver()
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
        }
    }
}
