using HtmlAgilityPack;
using RSBM.Models;
using RSBM.Repository;
using RSBM.Util;
using RSBM.WebUtil;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;

namespace RSBM.Controllers
{
    class BECPEController
    {
        #region Declaração de variaveis
        private static HashSet<long> AlreadyColected;

        private static int NumLicitacoes;

        private static Dictionary<string, int?> Cidades;

        private static Modalidade Modalidade;
        private static Orgao Orgao;
        private static Lote Lote;
        private static ConfigRobot config;
        private static LicitacaoRepository Repo;
        private static List<Licitacao> licitacoes = new List<Licitacao>();
        private static string mensagemErro;

        public static string Name { get; } = "BECPE";
        public static string PathEditais { get; } = Path.GetTempPath() + DateTime.Now.ToString("yyyyMM") + Name + "/";
        #endregion

        /*Inicia o processamento do robot*/
        private static void Init()
        {
            RService.Log("(Init) " + Name + ": Começando o processamento.. " + "at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                AlreadyColected = new HashSet<long>();

                /*Lista das licitacoes que já existem para bec.sp.gov*/
                //AlreadyInserted = LicitacaoController.GetAlreadyInserted(Constants.BEC_SITE);

                /*Lista das cidades para o estado*/
                Cidades = CidadeController.GetNameToCidade(Constants.BEC_UF);

                Modalidade = ModalidadeController.FindById(24);
                Orgao = OrgaoController.FindById(388);
                Lote = LoteController.CreateLote(43, 507);
                Repo = new LicitacaoRepository();

                //Define os pontos de partida, uri e argumentos do post
                List<string> urls = new List<string>();

                urls.Add(Constants.BEC_LINK_MODALIDADE_71);
                urls.Add(Constants.BEC_LINK_MODALIDADE_72);

                /*Percorre cada modalidade*/
                foreach (string uri in urls)
                {
                    /*Lista dos parametros do post*/
                    NameValueCollection post = new NameValueCollection();

                    /*Percorre as naturezas de cada modalidade*/
                    foreach (var attr in WebHandle.GetHtmlDocOfPage(uri, post).DocumentNode.Descendants("span").Where(x => x.Attributes.Contains("id")
                         && x.Attributes["id"].Value.ToString().Contains(Constants.BEC_ID_NATUREZA)))
                    {

                        string urin = attr.SelectSingleNode("a").Attributes["href"].Value.ToString();

                        int page = 2, count = 20;

                        /*Percorre as páginas para cada uma das naturezas (ex: 1;2;3)*/
                        HtmlDocument pagehtml = WebHandle.GetHtmlDocOfPage(urin, post);
                        while (pagehtml != null && count == 20)
                        {
                            RService.Log("(GetOC) " + Name + ": Percorrendo os links da página.. " + (page - 1) + " at {0}", Path.GetTempPath() + Name + ".txt");

                            //Pega as licitações de cada página (OC's)
                            count = GetOC(pagehtml);
                            //Pega o html da próxima página
                            pagehtml = WebHandle.GetHtmlDocOfPage(urin, GetFormParameters(pagehtml, page));
                            //Numero da proxima página
                            page++;

                        }
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (Init) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            finally
            {
                if (NumLicitacoes <= 0)
                    LoteController.Delete(Lote);
            }
        }

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
                RService.Log("Exception (GetFormParameters) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            return formData;
        }

        /*Percorre os links de cada página*/
        private static int GetOC(HtmlDocument htmlDoc)
        {
            int count = 0;

            try
            {
                var links = htmlDoc.DocumentNode.Descendants("span").Where(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains(Constants.BEC_ID_NATUREZA_RESUMO));
                count = links.Count();

                //Pra cada OC da pagina
                foreach (var link in links)
                {
                    //Valida o status da licitação
                    string status = link.ParentNode.NextSibling.NextSibling.NextSibling.InnerText;
                    string href = link.SelectSingleNode("a").Attributes["href"].Value;
                    string ocnum = link.SelectSingleNode("a").InnerText.ToString();
                    HandleCreate(WebHandle.GetHtmlDocOfPage(href), ocnum, status);
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetOC) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
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

                /*Verifica se a oc já não esta na base de dados, cria um novo lote se for preciso*/
                //if (!string.IsNullOrEmpty(ocnum) && !AlreadyInserted.Contains(long.Parse(ocnum)) && AguardandoPropostasEditalPub(htmlDoc) && !AlreadyColected.Contains(long.Parse(ocnum)))
                if (!string.IsNullOrEmpty(ocn) && !LicitacaoController.ExistsBEC(ocn))
                {
                    //AlreadyColected.Add(long.Parse(ocnum));
                    //Preenche os dados da licitação e retorna para inserir na lista
                    Licitacao licitacao = CreateLicitacao(htmlDoc, ocnum, situacao);
                    if (licitacao != null && !string.IsNullOrEmpty(licitacao.LinkEdital))
                    {
                        licitacao.Observacoes = ocn;

                        Repo.Insert(licitacao);
                        //licitacoes.Add(licitacao);

                        HtmlDocument htmlEditais = WebHandle.GetHtmlDocOfPage(licitacao.LinkEdital);
                        int numeroArquivo = 2;
                        //Faz o download de todos os arquivos do edital
                        foreach (HtmlNode editais in htmlEditais.DocumentNode.Descendants("a").Where(x => x.Attributes.Contains("href") && x.Attributes["href"].Value.Contains("ctl00$conteudo$WUC_Documento1$dgDocumento")))
                        {
                            DownloadEditais(licitacao.LinkEdital, GetFormParametersEdital(htmlEditais, numeroArquivo));
                        }
                        CreateLicitacaoArquivo(licitacao);
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
                RService.Log("Exception (HandleCreate) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        /*Verifica o situacao da OC*/
        private static bool AguardandoPropostasEditalPub(HtmlDocument htmlDoc)
        {
            if (htmlDoc == null)
                return false;

            try
            {
                string situacao = htmlDoc.DocumentNode.Descendants("span").SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("ctl00_wucOcFicha_txtStatus")).InnerText;
                return situacao.Trim().ToUpper().Contains("EDITAL PUBLICAD") || situacao.Trim().ToUpper().Contains("AGUARDANDO RECEBIMENTO DE PROPOS");
            }
            catch (Exception e)
            {
                RService.Log("Exception (AguardandoPropostasEditalPub) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            return false;
        }

        /*Cria as Licitacaoes arquivos se conseguir envia-los por ftp*/
        private static void CreateLicitacaoArquivo(Licitacao licitacao)
        {
            RService.Log("(CreateLicitacaoArquivo) " + Name + ": Criando arquivo de edital da licitação.. " + licitacao.IdLicitacaoFonte + " at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                string fileName = FileHandle.GetATemporaryFileName();
                string zipPath = string.Empty;
                if (Directory.Exists(PathEditais))
                {
                    zipPath = @Path.GetTempPath() + fileName + ".zip";
                    ZipFile.CreateFromDirectory(PathEditais, zipPath);
                }
                Directory.Delete(PathEditais, true);
                if (!string.IsNullOrEmpty(zipPath))
                {
                    #region FTP
                    //if (FTP.SendFileFtp(new FTP(@Path.GetTempPath(), fileName + ".zip", FTP.Adrss, FTP.Pwd, FTP.UName), Name))
                    //{
                    //LicitacaoArquivo licitacaoArq = new LicitacaoArquivo();
                    //licitacaoArq.NomeArquivo = fileName + ".zip";
                    //licitacaoArq.NomeArquivoOriginal = Name + DateTime.Now.ToString("yyyyMMddHHmmss");
                    //licitacaoArq.Status = 0;
                    //licitacaoArq.IdLicitacao = licitacao.Id;

                    //LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                    //repoArq.Insert(licitacaoArq);

                    //if (File.Exists(zipPath))
                    //{
                    //File.Delete(zipPath);
                    //}
                    //}
                    //else
                    //{
                    //RService.Log("(CreateLicitacaoArquivo) " + Name + ": error sending the file by FTP (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + Name + ".txt");
                    //}
                    #endregion

                    #region AWS
                    RService.Log("(CreateLicitacaoArquivo) " + Name + ": Enviando arquivo para Amazon S3... " + fileName + " at {0}", Path.GetTempPath() + Name + ".txt");

                    if (AWS.SendObject(licitacao, Path.GetTempPath(), fileName + ".zip"))
                    {
                        LicitacaoArquivo licitacaoArq = new LicitacaoArquivo();
                        licitacaoArq.NomeArquivo = fileName + ".zip";
                        licitacaoArq.NomeArquivoOriginal = Name + DateTime.Now.ToString("yyyyMMddHHmmss");
                        licitacaoArq.Status = 0;
                        licitacaoArq.IdLicitacao = licitacao.Id;

                        LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                        repoArq.Insert(licitacaoArq);

                        if (File.Exists(zipPath))
                            File.Delete(zipPath);

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
                    RService.Log("(CreateLicitacaoArquivo) " + Name + ": error while create zip file (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + Name + ".txt");
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacaoArquivo) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        /*Pega os parametros do post para o download do arquivo do edital*/
        private static NameValueCollection GetFormParametersEdital(HtmlDocument htmlDoc, int numeroArquivo)
        {
            NameValueCollection formData = new NameValueCollection();
            try
            {
                string viewstate = htmlDoc.DocumentNode.Descendants("input").SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Equals("__VIEWSTATE")).Attributes["value"].Value.ToString();
                string eventvalidation = htmlDoc.DocumentNode.Descendants("input").SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("__EVENTVALIDATION")).Attributes["value"].Value.ToString();

                formData["__EVENTTARGET"] = string.Format("ctl00$conteudo$WUC_Documento1$dgDocumento$ctl0{0}$ctl00", numeroArquivo);
                formData["__EVENTARGUMENT"] = "";
                formData["__VIEWSTATE"] = viewstate;
                formData["__EVENTVALIDATION"] = eventvalidation;
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetFormParametersEdital)" + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            return formData;
        }

        /*Cria o objeto licitacao arquivo, com o nome do arquivo do edital e a licitacao referente*/
        private static void DownloadEditais(string linkEdital, NameValueCollection formparameters)
        {
            try
            {
                if (!Directory.Exists(PathEditais))
                {
                    Directory.CreateDirectory(PathEditais);
                }
                WebHandle.DownloadDataPost(linkEdital, PathEditais + FileHandle.GetATemporaryFileName(), formparameters);
            }
            catch (Exception e)
            {
                RService.Log("Exception (DownloadEditais) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }


        /*Cria uma nova licitacao.*/
        private static Licitacao CreateLicitacao(HtmlDocument htmDoc, string ocnum, string situacao)
        {
            RService.Log("(CreateLicitacao) " + Name + ": Criando licitação.. " + ocnum + " at {0}", Path.GetTempPath() + Name + ".txt");
            Licitacao licitacao = new Licitacao();
            try
            {
                licitacao.Lote = Lote;
                licitacao.LinkSite = Constants.BEC_SITE;
                licitacao.Modalidade = Modalidade;
                licitacao.IdFonte = 507;
                licitacao.EstadoFonte = Constants.BEC_UF;
                licitacao.CidadeFonte = 9668;
                licitacao.Orgao = Orgao;
                licitacao.Cidade = Constants.BEC_CIDADE;
                licitacao.Estado = Constants.BEC_ESTADO;
                licitacao.Excluido = 0;
                licitacao.SegmentoAguardandoEdital = 0;
                licitacao.DigitacaoUsuario = 43; //Robo
                licitacao.Situacao = situacao;

                //licitacao.DigitacaoData = null;
                //licitacao.ProcessamentoData = null;

                licitacao.Num = ocnum;
                licitacao.IdLicitacaoFonte = long.Parse(ocnum);
                licitacao.Departamento = htmDoc.DocumentNode.Descendants("span").SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("ctl00_wucOcFicha_txtNomUge")).InnerText.Trim();
                int count;

                string city = htmDoc.DocumentNode.Descendants("span").SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("ctl00_wucOcFicha_txtDescricaoEnteFederativo")).InnerText.Trim();
                if (city != "GOVERNO DO ESTADO DE SÃO PAULO")
                {
                    city = StringHandle.RemoveAccent(city);
                    foreach (var cidade in Cidades)
                    {
                        if (city.Contains(cidade.Key))
                        {
                            licitacao.Cidade = cidade.Key;
                            licitacao.CidadeFonte = cidade.Value;
                            break;
                        }
                    }
                }

                /*Percorre os links da OC para montar o objeto licitação*/
                bool findFirstEditalLink = false;
                bool findFirstOcLink = false;
                foreach (HtmlNode htmNode in htmDoc.DocumentNode.Descendants("li"))
                {
                    /*Link onde ficam os arquivos do edital*/
                    HtmlNode htmlNodeInNode = htmNode.SelectSingleNode("a");

                    if (htmlNodeInNode != null)
                    {
                        if (htmlNodeInNode.Attributes.Contains("href") && htmlNodeInNode.Attributes["href"].Value.Contains("bec_pregao_UI/Edital") && !findFirstEditalLink)
                        {
                            licitacao.LinkEdital = htmlNodeInNode.Attributes["href"].Value.Trim();
                            findFirstEditalLink = true;
                        }
                        /*Link para a pag onde ficam as datas*/
                        if (htmlNodeInNode.Attributes.Contains("href") && htmlNodeInNode.Attributes["href"].Value.Contains("bec_pregao_UI/Agendamento"))
                        {
                            /*Html da pág onde ficam as datas*/
                            HtmlDocument htmDocAgendamento = WebHandle.GetHtmlDocOfPage(htmlNodeInNode.Attributes["href"].Value);
                            /*Tabela de datas, agendamentos*/
                            HtmlNode table = htmDocAgendamento.DocumentNode.Descendants("table").SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("ctl00_conteudo_grd"));
                            /*Cada célula da tabela*/
                            List<HtmlNode> tds = table.Descendants("td").ToList();
                            count = 0;
                            foreach (HtmlNode inf in tds)
                            {
                                /*Célula com o label ENTREGA DE PROPOSTA, na célula seguinte ficam as datas*/
                                if (inf.InnerText.ToUpper().Trim().Contains("ENTREGA DE PROPOSTA"))
                                {
                                    MatchCollection matches = StringHandle.GetMatches(tds[count + 1].InnerText.Trim(), @"(\d{2}\/\d{2}\/\d{4}\s+\d{2}:\d{2})");
                                    licitacao.EntregaData = DateHandle.Parse(matches[0].Groups[1].Value, "dd/MM/yyyy hh:mm");
                                    licitacao.AberturaData = DateHandle.Parse(matches[1].Groups[1].Value, "dd/MM/yyyy hh:mm");
                                    break;
                                }
                                count++;
                            }
                        }
                        /*Link com dados da OC*/
                        if (htmlNodeInNode.Attributes.Contains("href") && htmlNodeInNode.Attributes["href"].Value.Contains("bec_pregao_UI/OC") && !findFirstOcLink)
                        {
                            HtmlDocument htmDocFasePrep = WebHandle.GetHtmlDocOfPage(htmlNodeInNode.Attributes["href"].Value);
                            licitacao.Endereco = Regex.Replace(htmDocFasePrep.DocumentNode.Descendants("span").SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("ctl00_conteudo_Wuc_OC_Ficha2_txtEndUge")).InnerText.Trim(), @"\s+", " ");
                            licitacao.Objeto = "Contratação de " + Regex.Replace(htmDocFasePrep.DocumentNode.Descendants("span").SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("ctl00_conteudo_Wuc_OC_Ficha2_txtNaturezaJuridica")).InnerText.Trim(), @"\s+", " ");
                            findFirstOcLink = true;
                        }
                    }
                }

                licitacao.ItensLicitacao = licitacao.ItensLicitacao ?? new List<ItemLicitacao>();

                CreateItensLicitacao(htmDoc, licitacao);
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacao) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }

            return LicitacaoController.IsValid(licitacao, out mensagemErro) ? licitacao : null;
        }

        private static void CreateItensLicitacao(HtmlDocument htmDoc, Licitacao licitacao)
        {
            RService.Log("(CreateItensLicitacao) " + Name + ": Criando itens da licitação .. " + licitacao.IdLicitacaoFonte + " at {0}", Path.GetTempPath() + Name + ".txt");

            int count = 0;
            /*Contador das linhas da tabela*/
            int rows = 1;
            /*Pega o html da table com o id dgItensOc pra essa OC*/
            var dadosOc = htmDoc.DocumentNode.Descendants().SingleOrDefault(x => x.Id == "ctl00_conteudo_dg");

            /*Percorre as linhas da tabela exceto o Header que tem o atributo class HeaderStyle*/
            if (dadosOc != null)
            {
                CreateItens(licitacao, ref count, ref rows, dadosOc);
            }
            else
            {
                CreateItensAlternateId(htmDoc, licitacao, ref count, ref rows);
            }
        }

        private static void CreateItensAlternateId(HtmlDocument htmDoc, Licitacao licitacao, ref int count, ref int rows)
        {
            HtmlNode dadosOc = htmDoc.DocumentNode.Descendants().SingleOrDefault(x => x.Id == "ctl00_conteudo_loteGridItens_grdLote");
            foreach (var row in dadosOc.Descendants("tr"))
            {
                if (row.Attributes.Contains("class") && row.Attributes["class"].Value.Contains("HeaderStyle"))
                    continue;

                ItemLicitacao item = new ItemLicitacao();
                /*Contador usado para as colunas da tabela*/
                count = 0;
                /*Percorre cada coluna de uma linha da tabela Intes da Oferta de Compra*/
                foreach (var data in row.Descendants("td"))
                {
                    /*A quarta coluna da tabela de Itens da Oferta de Compra contém a descrição do Item.*/
                    switch (count)
                    {
                        case 2:
                            item.Numero = int.Parse(data.InnerText.Trim());
                            break;
                        case 3:
                            string desc = data.InnerText.Trim();
                            item.Descricao = desc.Length > 50 ? desc.Substring(0, 50) + "..." : desc;
                            item.DescricaoDetalhada = desc;
                            break;
                        case 4:
                            item.Quantidade = int.Parse(data.InnerText.Trim().Replace(".", ""));
                            break;
                        case 1:
                            item.Unidade = "GRUPO";
                            break;
                    }
                    count++;
                }

                licitacao.ItensLicitacao.Add(item);
                rows++;
            }
        }

        private static void CreateItens(Licitacao licitacao, ref int count, ref int rows, HtmlNode dadosOc)
        {
            foreach (var row in dadosOc.Descendants("tr"))
            {
                if (row.Attributes.Contains("class") && row.Attributes["class"].Value.Contains("HeaderStyle"))
                    continue;

                ItemLicitacao item = new ItemLicitacao();
                /*Contador usado para as colunas da tabela*/
                count = 0;
                /*Percorre cada coluna de uma linha da tabela Intes da Oferta de Compra*/
                foreach (var data in row.Descendants("td"))
                {
                    /*A quarta coluna da tabela de Itens da Oferta de Compra contém a descrição do Item.*/
                    switch (count)
                    {
                        case 2:
                            item.Numero = int.Parse(data.InnerText.Trim());
                            break;
                        case 4:
                            string desc = data.InnerText.Trim();
                            item.Descricao = desc.Length > 50 ? desc.Substring(0, 50) + "..." : desc;
                            item.DescricaoDetalhada = desc;
                            break;
                        case 5:
                            item.Quantidade = int.Parse(data.InnerText.Trim().Replace(".", ""));
                            break;
                        case 6:
                            item.Unidade = data.InnerText.Trim();
                            break;
                    }
                    count++;
                }

                licitacao.ItensLicitacao.Add(item);
                rows++;
            }
        }
    }
}
