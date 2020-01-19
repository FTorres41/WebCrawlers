using HtmlAgilityPack;
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
using System.Threading.Tasks;

namespace RSBM.Controllers
{
    class CNETCotacaoController
    {
        #region Declaração de variáveis
        public static string Name { get; } = "CNETCotacao";

        private static string mensagemErro;
        private static int numCotacoes;
        private static bool icms = false;

        private static Dictionary<string, Modalidade> nameToModalidade;
        private static Dictionary<string, Orgao> nameToOrgao;
        private static Dictionary<string, string> ufToCapital;
        private static Dictionary<string, Dictionary<string, int?>> ufToNomeCidadeToIdCidade;

        private static ConfigRobot config;
        private static Lote lote;
        private static LicitacaoRepository repo;
        #endregion

        #region Métodos
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

                    config.NumLicitLast = numCotacoes;
                    RService.Log(Name + " find " + numCotacoes + " novas licitações at {0}", Path.GetTempPath() + Name + ".txt");
                    numCotacoes = 0;

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

        private static void Init()
        {
            RService.Log("(Init) " + Name + ": Começando o processamento... " + "at {0}", Path.GetTempPath() + Name + ".txt");

            try
            {
                nameToModalidade = ModalidadeController.GetNameToModalidade();
                nameToOrgao = OrgaoController.GetNomeUfToOrgao();
                ufToCapital = CityUtil.GetUfToCapital();
                ufToNomeCidadeToIdCidade = CidadeController.GetUfToNameCidadeToIdCidade();
                lote = LoteController.CreateLote(43, 508);
                repo = new LicitacaoRepository();

                HtmlDocument htmlDoc = WebHandle.GetHtmlDocOfPage(Constants.CN_COTACOES, Encoding.GetEncoding("ISO-8859-1"));

                RService.Log("(Init) " + Name + ": Percorrendo as cotações do dia " + DateTime.Today.ToShortDateString() + " at {0}", Path.GetTempPath() + Name + ".txt");

                foreach (var row in htmlDoc.DocumentNode.Descendants("tr").Where(x => x.Attributes.Contains("class") && x.Attributes["class"].Value.Contains("estiloLinhaTabela")).ToList())
                {
                    if (row.ChildNodes[5].InnerText == "Sim")
                        icms = true;
                    else
                        icms = false;

                    HandleCreate(htmlDoc, row);
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (Init) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        private static void HandleCreate(HtmlDocument htmlDoc, HtmlNode row)
        {
            try
            {
                if (row.InnerHtml.Contains("href"))
                {
                    string situacao;
                    string quoteLink = row.ChildNodes[3].ChildNodes["a"].Attributes["href"].Value.ToString();
                    HtmlDocument htmlQuote = WebHandle.GetHtmlDocOfPage(string.Format(Constants.CN_COTACAO_LINK, quoteLink), Encoding.GetEncoding("ISO-8859-1"));
                    Licitacao l = CreateQuote(htmlQuote, quoteLink, out situacao);
                    //RandomSleep();
                    if (l != null && !repo.Exists(l.IdLicitacaoFonte.ToString()))
                    {
                        repo.Insert(l);
                        numCotacoes++;
                        RService.Log("Cotação " + l.IdLicitacaoFonte + " inserida com sucesso" + " at {0}", Path.GetTempPath() + Name + ".txt");

                        //SegmentarCotacao(l);
                    }
                    else if (l != null && repo.Exists(l.IdLicitacaoFonte.ToString()) && LicitacaoController.SituacaoAlterada(l.IdLicitacaoFonte.ToString(), situacao))
                    {
                        l = repo.GetByIdLicitacaoFonte(l.IdLicitacaoFonte.ToString());
                        l.Situacao = situacao;

                        repo.Update(l);
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (HandleCreate) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        private static void RandomSleep()
        {
            Random rnd = new Random(5000);
            System.Threading.Thread.Sleep(rnd.Next());
        }

        private static Licitacao CreateQuote(HtmlDocument htmlQuote, string quoteLink, out string situacao)
        {
            Licitacao l = new Licitacao();
            int count = 0;
            situacao = null;

            try
            {
                l.IdFonte = 508;
                l.LinkEdital = string.Format(Constants.CN_COTACAO_LINK, quoteLink);
                l.LinkSite = Constants.CN_HOST;
                l.Excluido = 0;
                l.SegmentoAguardandoEdital = 0;
                l.DigitacaoUsuario = 43; //Robo
                l.Lote = lote;
                l.Modalidade = nameToModalidade.ContainsKey("COTACAO ELETRONICA") ? nameToModalidade["COTACAO ELETRONICA"] : null;
                l.ItensLicitacao = l.ItensLicitacao ?? new List<ItemLicitacao>();

                foreach (var row in htmlQuote.DocumentNode.Descendants("tr").Where(x => x.Attributes.Contains("height")))
                {
                    switch (count)
                    {
                        case 0:
                            string uasg = row.InnerText.Split('-')[0].TrimEnd().Replace("UASG: ", "");
                            string departamento = string.Empty;
                            if (row.InnerText.Split('-').Count() > 2)
                            {
                                for (int i = 1; i < row.InnerText.Split('-').Count(); i++)
                                {
                                    if (i != 1)
                                        departamento = departamento + "-" + row.InnerText.Split('-')[i].TrimStart();
                                    else
                                        departamento = row.InnerText.Split('-')[i].TrimStart();
                                }
                            }
                            else
                            {
                                departamento = row.InnerText.Split('-')[1].TrimStart();
                            }
                            l.Uasg = uasg;
                            l.Departamento = departamento;
                            break;

                        case 1:
                            string numero = row.InnerText.Split(':')[1].TrimStart();
                            l.Num = numero;
                            break;

                        case 2:
                            string objeto = row.InnerText.Replace("Objeto: ", string.Empty);
                            l.Objeto = objeto;
                            break;

                        case 3:
                            string dataEntrega = row.InnerText.Split(':')[1].TrimStart();
                            l.EntregaData = Convert.ToDateTime(dataEntrega);
                            break;

                        case 4:
                            string obsLink = row.ChildNodes[0].ChildNodes[1].Attributes["href"].Value.ToString().Remove(0, 8);
                            HtmlDocument htmlObs = WebHandle.GetHtmlDocOfPage(string.Format(Constants.CN_COTACAO_LINK, obsLink), Encoding.GetEncoding("ISO-8859-1"));
                            string obs = Regex.Replace(htmlObs.DocumentNode.InnerHtml.ToString(), "<.*?>", string.Empty)
                                .Replace("\n\n\n\n  \tCOMPRASNET - O Portal de Compras do Governo Federal :: Observações Gerais da Cotação Eletrônica.\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\n\n    function erroLogin(){\n        window.opener.erroLogin();\n        window.close();\n    }\n\n\n\n    function popup(ev, elA, features) {\n        var url, target;\n        url = elA.getAttribute(\"href\");\n        target = elA.getAttribute(\"target\");\n        window.open(url, target, features);\n\n        if (ev.cancelBubble != undefined) { //IE\n            ev.cancelBubble = true; \n            ev.returnValue = false;\n        }\n        if (ev.preventDefault) ev.preventDefault(); //Outros\n    }\n\n\n\n\n\n\nAguarde!\n\n\nObservações Gerais da Cotação Eletrônica\n\r\n              ", string.Empty)
                                .Replace("  ", string.Empty);
                            if (icms)
                                l.Observacoes = "ICMS: Sim\n\n" + obs;
                            else
                                l.Observacoes = "ICMS: Não\n\n" + obs;
                            break;

                        case 5:
                            situacao = Regex.Replace(StringHandle.GetMatches(row.InnerHtml, @"Situação:( *)</b><span(.*?)>(.*?)</span")[0].ToString(), @"Situação:|</b><span(.*?)>|</span", "").Trim();
                            l.Situacao = situacao;
                            break;

                        case 6:
                            string dataAbertura = row.InnerText.Split(':')[1].TrimStart().Split('(')[0].Replace('h', ':');
                            l.AberturaData = DateHandle.Parse(dataAbertura, "dd/MM/yyyy hh:mm");
                            break;

                        case 7:
                            string valor = row.InnerText.Split(':')[1].TrimStart();
                            l.ValorMax = valor;
                            break;
                    }
                    count++;
                }

                l.IdLicitacaoFonte = Convert.ToInt64(l.Uasg + l.Num.ToString());

                Licitacao oldLic = LicitacaoRepository.FindByUASG(l.Uasg);

                if (oldLic != null)
                {
                    l.Orgao = oldLic.Orgao;
                    l.EstadoFonte = oldLic.EstadoFonte;
                    l.CidadeFonte = oldLic.CidadeFonte;
                    l.Endereco = oldLic.Endereco;
                    l.Cidade = oldLic.Cidade;
                    l.Estado = oldLic.Estado;
                }
                else
                {
                    l.Orgao = OrgaoRepository.FindOrgao(l.Departamento);
                    if (l.Orgao == null)
                    {
                        Orgao org = OrgaoRepository.CreateOrgao(l.Departamento, l.Observacoes);
                        OrgaoRepository repo = new OrgaoRepository();
                        repo.Insert(org);
                        l.Orgao = org;
                    }
                    l.Estado = l.Orgao.Estado;
                    l.EstadoFonte = l.Orgao.Estado;
                    l.Cidade = ufToCapital.ContainsKey(l.EstadoFonte) ? ufToCapital[l.EstadoFonte] : null;
                    Dictionary<string, int?> ufToCidade = ufToNomeCidadeToIdCidade.ContainsKey(l.EstadoFonte) ? ufToNomeCidadeToIdCidade[l.EstadoFonte] : null;
                    l.CidadeFonte = ufToCidade != null ? ufToCidade.ContainsKey(StringHandle.RemoveAccent(l.Cidade.ToUpper())) ? ufToCidade[StringHandle.RemoveAccent(l.Cidade.ToUpper())] : CityUtil.GetCidadeFonte(l.Cidade, ufToCidade) : CityUtil.GetCidadeFonte(l.Cidade, ufToCidade);
                    l.Endereco = null;
                }

                GetItens(htmlQuote, l);

                return LicitacaoController.IsValid(l, out mensagemErro) ? l : null;
            }
            catch (Exception e)
            {
                if (l.Orgao == null)
                    RService.Log("Exception (CreateQuote) " + Name + ": Órgão não foi localizado - ver log do serviço RService" + " at {0}", Path.GetTempPath() + Name + ".txt");
                else
                    RService.Log("Exception (CreateQuote) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");

                return null;
            }
        }

        private static void GetItens(HtmlDocument htmlQuote, Licitacao l)
        {
            try
            {
                foreach (var row in htmlQuote.DocumentNode.Descendants("tr").Where(x => !x.Attributes.Contains("height") && x.Attributes.Contains("class") && x.Attributes["class"].Value.Equals("tex3")))
                {
                    ItemLicitacao item = new ItemLicitacao();
                    int count = 0;

                    foreach (var cell in row.ChildNodes.Where(x => x.Name.Equals("td")))
                    {
                        switch (count)
                        {
                            case 0:
                                item.Numero = Convert.ToInt32(cell.InnerText);
                                break;

                            case 1:
                                item.Descricao = cell.InnerText.Replace("\");\r\n      \">", "");
                                string itemLink = cell.ChildNodes[0].Attributes["href"].Value.ToString().Split('/')[2];
                                HtmlDocument itemDetail = WebHandle.GetHtmlDocOfPage(string.Format(Constants.CN_COTACAO_LINK, itemLink), Encoding.GetEncoding("ISO-8859-1"));
                                string descDetail = Regex.Replace(itemDetail.DocumentNode.InnerHtml.ToString().Replace("<br>", "\n"), "<.*?>", String.Empty)
                                    .Replace("\n\n\n\n  \tCOMPRASNET - O Portal de Compras do Governo Federal :: DESCRIÇÃO COMPLEMENTAR.\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\t\n\n\n    function erroLogin(){\n        window.opener.erroLogin();\n        window.close();\n    }\n\n\n\n    function popup(ev, elA, features) {\n        var url, target;\n        url = elA.getAttribute(\"href\");\n        target = elA.getAttribute(\"target\");\n        window.open(url, target, features);\n\n        if (ev.cancelBubble != undefined) { //IE\n            ev.cancelBubble = true; \n            ev.returnValue = false;\n        }\n        if (ev.preventDefault) ev.preventDefault(); //Outros\n    }\n\n\n\n\n\n\nAguarde!\n\n\nDESCRIÇÃO COMPLEMENTAR\n\r\n\r\n", "")
                                    .Replace("\r\n\r\n\r\n", "\n")
                                    .Replace("  ", "");
                                item.DescricaoDetalhada = descDetail;
                                break;

                            case 2:
                                item.Quantidade = Convert.ToInt32(cell.InnerText);
                                break;

                            case 3:
                                item.Unidade = cell.InnerText;
                                break;

                        }
                        count++;
                    }
                    item.Decreto7174 = "0";
                    item.MargemPreferencia = "0";

                    l.ItensLicitacao.Add(item);
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetItens) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        private static void SegmentarCotacao(Licitacao l)
        {
            List<Segmento> segmentos = SegmentoController.CreateListaSegmentos(l);

            foreach (Segmento segmento in segmentos)
            {
                int objMatch = 0, segmentoCount = 0;
                bool itemMatch = false;
                var palavrasChave = segmento.PalavrasChave.Split(';');

                if (palavrasChave.Length > 0)
                {
                    foreach (var palavrachave in palavrasChave)
                    {
                        if (l.Objeto.ToUpper().Contains(palavrachave))
                        {
                            objMatch++;
                        }

                        foreach (var item in l.ItensLicitacao)
                        {
                            if (item.Descricao.ToUpper().Contains(palavrachave) || item.DescricaoDetalhada.ToUpper().Contains(palavrachave))
                            {
                                itemMatch = true;
                            }
                        }
                    }

                    if (objMatch >= 2 && itemMatch == true)
                    {
                        LicitacaoSegmento licSeg = new LicitacaoSegmento()
                        {
                            IdLicitacao = l.Id,
                            IdSegmento = segmento.IdSegmento
                        };

                        segmentoCount++;
                        LicitacaoSegmentoRepository repoLS = new LicitacaoSegmentoRepository();
                        repoLS.Insert(licSeg);
                    }
                }

                if (segmentoCount > 0)
                {
                    RService.Log("(SegmentarLicitacao) " + Name + ": Licitação " + l.IdLicitacaoFonte + " foi segmentada em " + segmentoCount + " segmentos at {0}", Path.GetTempPath() + Name + ".txt");
                }
                else
                {
                    RService.Log("(SegmentarLicitacao) " + Name + ": Licitação " + l.IdLicitacaoFonte + " não foi segmentada at {0}", Path.GetTempPath() + Name + ".txt");
                }
            }
        }
        #endregion
    }
}
