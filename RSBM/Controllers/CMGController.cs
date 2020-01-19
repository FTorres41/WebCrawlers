using HtmlAgilityPack;
using RSBM.Models;
using RSBM.Repository;
using RSBM.Util;
using RSBM.WebUtil;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RSBM.Controllers
{
    class CMGController
    {
        #region Declaração de variaveis

        private static List<string> AllLinks;
        private static List<Modalidade> Modalidades;

        private static Orgao Orgao;
        private static ConfigRobot config;
        private static Lote Lote;
        private static LicitacaoRepository Repo;

        private static int CurrentPage;
        private static int NumLicitacoes;
        private static int NumCaptcha = 0;
        private static string mensagemErro;

        private static string Id;
        private static string Secretaria;
        private static string Situacao;
        public static string Name { get; } = "CMG";
        public static string PathEditais { get; } = Path.GetTempPath() + DateTime.Now.ToString("yyyyMM") + Name + "/";
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
            }

            RService.Log("Finished " + Name + " at {0}", Path.GetTempPath() + Name + ".txt");

            EmailHandle.SendMail(Path.GetTempPath() + Name + ".txt", Name);
        }

        /*Inica o processamento do robot*/
        public static void Init()
        {
            RService.Log("(Init) " + Name + ": Começando o processamento... " + "at {0}", Path.GetTempPath() + Name + ".txt");

            try
            {
                AllLinks = new List<string>();
                Orgao = OrgaoController.FindById(27);
                Modalidades = new List<Modalidade>();
                Lote = LoteController.CreateLote(43, 506);
                Repo = new LicitacaoRepository();

                Modalidades.Add(ModalidadeController.FindById(24));
                Modalidades.Add(ModalidadeController.FindById(22));

                CurrentPage = 1;
                /*Pega o html da primeira página*/
                HtmlDocument pagehtml = WebHandle.GetHtmlHandleCaptcha(Constants.CMG_SITE + Constants.CMG_LINK_PAGINATION, Encoding.GetEncoding("ISO-8859-1"), "textoConfirmacao", Constants.CMG_CAPTCHA, GetParametersPagination(string.Format(Constants.CMG_PARAMETERS_PAGINATION, CurrentPage, DateTime.Now.Year)));
      
                /*Numero de paginas encontradas*/
                int numberPages = pagehtml.DocumentNode.Descendants("a").Where(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("tabConsultaPregoes_pagina")).ToList().Count;
                /*Caso existam poucas licitações, ao ponto de só haver uma página, o robô estava retornando numberPages = 0. Com a limitação abaixo, ele sempre vai pegar as licitações independente do pouco número de licitações*/
                if (numberPages == 0)
                    numberPages = 1;

                /*Percorre todas as paginas*/
                while (pagehtml != null && CurrentPage <= numberPages)
                {
                    /*Pega todas os pregões de cada página*/
                    GetPregoes(pagehtml);
                    /*Numero da próxima pagina*/
                    CurrentPage++;
                    /*Pega o html da próxima página*/
                    pagehtml = WebHandle.GetHtmlHandleCaptcha(Constants.CMG_SITE + Constants.CMG_LINK_PAGINATION, Encoding.GetEncoding("ISO-8859-1"), "textoConfirmacao", Constants.CMG_CAPTCHA, GetParametersPagination(string.Format(Constants.CMG_PARAMETERS_PAGINATION, CurrentPage, DateTime.Now.ToString("yyyy"))));
                    NumCaptcha++;
                }

                if (Directory.Exists(PathEditais))
                    Directory.Delete(PathEditais, true);
            }
            catch (Exception e)
            {
                RService.Log("Exception (Init)" + Name + ":" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            finally
            {
                if (NumLicitacoes <= 0)
                    LoteController.Delete(Lote);
            }
        }

        private static NameValueCollection GetParametersPagination(string parameters)
        {
            NameValueCollection name = new NameValueCollection();
            foreach (string p in parameters.Split('&'))
                name[p.Split('=')[0]] = p.Split('=')[1];
            return name;
        }

        /*Percorre a lista de links dos pregoes*/
        private static void GetPregoes(HtmlDocument pagehtml)
        {
            RService.Log("(GetPregoes) " + Name + ": Percorrendo a lista de licitações da página... " + CurrentPage + " at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                /*Percorre cada um dos pregões de cada página*/
                foreach (var link in pagehtml.DocumentNode.Descendants("a").Where(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("vizualizar")))
                {
                    /*Pega o link do pregão*/
                    MatchCollection matches = StringHandle.GetMatches(link.Attributes["onclick"].Value, @"\/(.*)\'");
                    //string linkPregao = GetLinkPregaoSetId(link.Attributes["onclick"].Value);
                    string linkPregao = Constants.CMG_SITE + matches[0].Groups[1].Value;
                    /*Pega o id do pregão*/
                    Id = StringHandle.GetMatches(matches[0].Groups[1].Value, @"id=(\d+)")[0].Groups[1].Value;
                    /*Pega a secretaria antes de acessar o link*/
                    Secretaria = pagehtml.DocumentNode.Descendants("td").SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("colOrgaoEntidade_" + Id)).InnerText.Trim();
                    /*Pega a situação antes de acessar o link*/
                    Situacao = pagehtml.DocumentNode.Descendants("td").SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("colSituacao_" + Id)).InnerText.Trim();
                    /*Verifica se o pregão já não foi acessado antes*/
                    if (!AllLinks.Contains(linkPregao))
                    {
                        AllLinks.Add(linkPregao);                     
                        HandleCreate(WebHandle.GetHtmlDocOfPage(linkPregao, Encoding.GetEncoding("ISO-8859-1")));
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("RService Exception " + Name + ": (GetPregoes)" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        /*Cria os objetos Licitacao, Lote e LicitacaoArquivo*/
        private static void HandleCreate(HtmlDocument htmlDocument)
        {
            try
            {
                string linkEdital = Constants.CMG_SITE + Constants.CMG_LINK_EDITAL + htmlDocument.DocumentNode.Descendants("a").SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("arquivoDoEdital")).Attributes["href"].Value;
                /*Verifica se o pregão já não esta na base de dados*/
                if (!string.IsNullOrEmpty(Id) && !LicitacaoController.Exists(Id))
                {
                    Licitacao licitacao = CreateLicitacao(htmlDocument, linkEdital);
                    if (licitacao != null)
                    {
                        Repo.Insert(licitacao);
                        RService.Log("(HandleCreate) " + Name + ": Licitação " + licitacao.IdLicitacaoFonte + " inserida com sucesso at {0}", Path.GetTempPath() + Name + ".txt");

                        CreateLicitacaoArquivo(licitacao, linkEdital);

                        //SegmentarLicitacao(licitacao);

                        NumLicitacoes++;
                    }
                    else
                    {
                        RService.Log("Exception (HandleCreate) " + Name + ": A licitação não foi salva - Motivo(s): " + mensagemErro + " at {0}", Path.GetTempPath() + Name + ".txt");
                    }
                }
                else if (!string.IsNullOrEmpty(Id) && LicitacaoController.Exists(Id) && LicitacaoController.SituacaoAlterada(Id, Situacao))
                {
                    Licitacao licitacao = LicitacaoController.GetByIdLicitacaoFonte(Id);
                    licitacao.Situacao = Situacao;

                    LicitacaoRepository repo = new LicitacaoRepository();
                    repo.Update(licitacao);
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (HandleCreate)" + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
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

                if (matchCount >= 3)
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

        /*Cria uma licitação arquivo se o download e o envio do arquivo for efetuado com sucesso*/
        private static void CreateLicitacaoArquivo(Licitacao licitacao, string linkEdital)
        {
            RService.Log("(CreateLicitacaoArquivo) " + Name + ": Criando arquivo de edital da licitação num... " + licitacao.IdLicitacaoFonte + " at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                if (!Directory.Exists(PathEditais))
                {
                    Directory.CreateDirectory(PathEditais);
                }
                string fileName = FileHandle.GetATemporaryFileName();

                if (WebHandle.DownloadData(linkEdital, PathEditais + fileName))
                {
                    #region FTP
                    //if (FTP.SendFileFtp(new FTP(PathEditais, fileName + WebHandle.ExtensionLastFileDownloaded, FTP.Adrss, FTP.Pwd, FTP.UName), Name))
                    //{
                        //LicitacaoArquivo licitacaoArq = new LicitacaoArquivo();
                        //licitacaoArq.NomeArquivo = fileName + WebHandle.ExtensionLastFileDownloaded;
                        //licitacaoArq.NomeArquivoOriginal = Name + DateTime.Now.ToString("yyyyMMddHHmmss");
                        //licitacaoArq.Status = 0;
                        //licitacaoArq.IdLicitacao = licitacao.Id;

                        //LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                        //repoArq.Insert(licitacaoArq);

                        //RService.Log("(CreateLicitacaoArquivo) " + Name + ": Arquivo de edital da licitação " + licitacao.IdLicitacaoFonte + " inserido com sucesso at {0}", Path.GetTempPath() + Name + ".txt");

                        //if (File.Exists(PathEditais + fileName))
                        //{
                            //File.Delete(PathEditais + fileName);
                        //}
                    //}
                    //else
                    //{
                        //RService.Log("Exception (CreateLicitacaoArquivo) " + Name + ": Erro ao enviar o arquivo para o FTP (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + Name + ".txt");
                    //}
                    #endregion

                    #region AWS
                    RService.Log("(CreateLicitacaoArquivo) " + Name + ": Enviando arquivo para Amazon S3... " + fileName + " at {0}", Path.GetTempPath() + Name + ".txt");

                    if (AWS.SendObject(licitacao, PathEditais, fileName + WebHandle.ExtensionLastFileDownloaded))
                    {
                        LicitacaoArquivo licitacaoArq = new LicitacaoArquivo();
                        licitacaoArq.NomeArquivo = fileName + WebHandle.ExtensionLastFileDownloaded;
                        licitacaoArq.NomeArquivoOriginal = Name + DateTime.Now.ToString("yyyyMMddHHmmss");
                        licitacaoArq.Status = 0;
                        licitacaoArq.IdLicitacao = licitacao.Id;

                        LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                        repoArq.Insert(licitacaoArq);

                        if (File.Exists(PathEditais + fileName))
                        {
                            File.Delete(PathEditais + fileName);
                        }

                        RService.Log("(CreateLicitacaoArquivo) " + Name + ": Arquivo " + fileName + " enviado com sucesso para Amazon S3" + " at {0}", Path.GetTempPath() + Name + ".txt");
                    }
                    else
                    {
                        RService.Log("(CreateLicitacaoArquivo) " + Name + ": Erro ao enviar o arquivo para Amazon S3 (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + Name + ".txt");
                    }
                    #endregion
                }
                else
                {
                    RService.Log("(CreateLicitacaoArquivo) " + Name + ": Erro ao baixar o arquivo (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + Name + ".txt");
                }
            }
            catch (Exception e)
            {
                RService.Log("RService Exception " + Name + ": (CreateLicitacaoArquivo)" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " / " + e.InnerException + 
                    " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        /**Cria uma nova licitação*/
        private static Licitacao CreateLicitacao(HtmlDocument htmlDocument, string linkEdital)
        {
            RService.Log("(CreateLicitacao) " + Name + ": Criando licitação... " + Id + " at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                IEnumerable<HtmlNode> spanNodes = htmlDocument.DocumentNode.Descendants("span");
                Licitacao licitacao = new Licitacao();
                licitacao.Lote = Lote;
                licitacao.Num = Id;
                licitacao.IdLicitacaoFonte = long.Parse(Id);
                string modalidade = spanNodes.SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("procedimentoContratacao")).InnerText;
                licitacao.Modalidade = modalidade.Contains("eletr") ? Modalidades[0] : Modalidades[1];
                licitacao.LinkSite = Constants.CMG_HOST;
                licitacao.Orgao = Orgao;
                licitacao.IdFonte = 506;
                licitacao.Excluido = 0;
                licitacao.SegmentoAguardandoEdital = 0;
                licitacao.DigitacaoUsuario = 43; //Robo
                licitacao.LinkEdital = linkEdital;
                licitacao.CidadeFonte = 2754;
                licitacao.Cidade = Constants.CMG_CIDADE;
                licitacao.EstadoFonte = Constants.CMG_UF;
                licitacao.Estado = Constants.CMG_ESTADO;
                licitacao.Observacoes = "ENDERECO NAO DIVULGADO.";
                licitacao.Situacao = Situacao;

                //licitacao.DigitacaoData = null;
                //licitacao.ProcessamentoData = null;

                string departamento = spanNodes.SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("unidadeCompra")).InnerText;

                licitacao.Departamento = departamento.Trim() + " SECRETARIA: " + Secretaria;

                string data = spanNodes.SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("dataInicioSessaoPregao")).InnerText;
                string hora = spanNodes.SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("horaInicioSessaoPregao")).InnerText;

                DateTime? aberturaEntrega = DateHandle.Parse(data.Trim() + " " + hora.Trim(), "dd/MM/yyyy hh:mm:ss");

                licitacao.AberturaData = aberturaEntrega;
                licitacao.EntregaData = aberturaEntrega;
                licitacao.Objeto = spanNodes.SingleOrDefault(x => x.Attributes.Contains("id") && x.Attributes["id"].Value.Contains("objetoLicitacao")).InnerText;

                return LicitacaoController.IsValid(licitacao, out mensagemErro) ? licitacao : null;
            }
            catch (Exception e)
            {
                RService.Log("Exception " + Name + ": (CreateLicitacao)" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
                return null;
            }
        }
    }
}
