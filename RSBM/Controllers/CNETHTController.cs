using HtmlAgilityPack;
using RSBM.Models;
using RSBM.Util;
using RSBM.WebUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RSBM.Controllers
{
    public class CNETHTController
    {
        #region Declaracao de variaveis

        public static string Historic { get; } = "CNETHT";
        private static int NumHistoricos;
        private static ConfigRobot config;

        private static List<string> licitacoesHistorico;
        #endregion

        #region Métodos
        /*Método pelo qual o serviço inicia o robô no Timer agendado.*/
        internal static void HistoricCallBack(object state)
        {
            try
            {
                //Busca as informações do robô no banco de dados.
                config = ConfigRobotController.FindByName(Historic);

                //Se o robô estiver ativo inicia o processamento.
                if (config.Active == 'Y')
                {
                    // Deleta o último arquivo de log.
                    if (File.Exists(Path.GetTempPath() + Historic + ".txt"))
                        File.Delete(Path.GetTempPath() + Historic + ".txt");

                    config.Status = 'R';
                    ConfigRobotController.Update(config);
                    using (FileStream fs = File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire")) { }
                    HistoricFiles();

                    //Verifica se teve atualização
                    config = ConfigRobotController.FindByName(Historic);

                    //Verifica quantas licitações foram coletadas nessa execução, grava em log.
                    config.NumLicitLast = NumHistoricos;
                    RService.Log(Historic + " find " + NumHistoricos + " novos itens de histórico de licitações at {0}", Path.GetTempPath() + Historic + ".txt");
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
                RService.Log("Exception (HistoricCallBack) " + Historic + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Historic + ".txt");
            }

            RService.Log("Finished " + Historic + " at {0}", Path.GetTempPath() + Historic + ".txt");

            EmailHandle.SendMail(Path.GetTempPath() + Historic + ".txt", Historic);
        }

        /*Busca arquivos de edital para licitações antigas que tenham data de abertura maior do que o dia corrente.*/
        private static void HistoricFiles()
        {
            RService.Log("(HistoricFiles) " + Historic + ": Buscando licitações..." + " at {0}", Path.GetTempPath() + Historic + ".txt");
            try
            {
                DateTime dataLimite = DateTime.Today.AddDays(-90);

                /*Busca licitações com a data de abertura anterior a 90 dias, ou que ainda não aconteceu*/
                List<Licitacao> licitacoes = LicitacaoController.FindByRangeHistoric(Constants.CN_HOST, dataLimite);

                RService.Log("(HistoricFiles) " + Historic + ": " + licitacoes.Count + " licitacões encontradas at {0}", Path.GetTempPath() + Historic + ".txt");

                licitacoesHistorico = new List<string>();

                foreach (Licitacao licitacao in licitacoes)
                {
                    RService.Log("(HistoricFiles) " + Historic + ": Consultando ata e histórico da licitação " + licitacao.IdLicitacaoFonte + " at {0}", Path.GetTempPath() + Historic + ".txt");
                    ConsultaAtaPregao(licitacao);
                    GetHistoricos(licitacao, Historic);
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (HistoricFiles) " + Historic + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Historic + ".txt");
            }
        }

        private static void ConsultaAtaPregao(Licitacao licitacao)
        {
            RService.Log("(ConsultaAtaPregao) " + Historic + ": Buscando itens da ata do pregão at {0}", Path.GetTempPath() + Historic + ".txt");
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
                            RService.Log("(ConsultaAtaPregao) " + Historic + ": Buscando itens do tipo: " + tipo.Value + " at {0}", Path.GetTempPath() + Historic + ".txt");

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
                                            RService.Log("Exception (ConsultaAtaPregao) getData" + Historic + " para a licitacao " + licitacao.IdLicitacaoFonte + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Historic + ".txt");
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
                                    RService.Log("Exception (ConsultaAtaPregao) getMensagem " + Historic + " para a licitacao " + licitacao.IdLicitacaoFonte + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Historic + ".txt");
                                }
                            }
                        }
                    }
                    else
                    {
                        RService.Log("(ConsultaAtaPregao) " + Historic + ": Pregão não contém ata pois não foi encerrado at {0}", Path.GetTempPath() + Historic + ".txt");
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (ConsultaAtaPregao) " + Historic + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Historic + ".txt");
            }
        }

        private static void GetHistoricos(Licitacao l, string Historic)
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
                        RService.Log("(GetHistoricos) " + Historic + ": Buscando histórico da licitação: " + l.IdLicitacaoFonte + " at {0}", Path.GetTempPath() + Historic + ".txt");

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
                                RService.Log("(ConsultaAtaPregao) " + Historic + ": Histórico registrado com sucesso" + " at {0}", Path.GetTempPath() + Historic + ".txt");
                                NumHistoricos++;
                            }
                        }

                        if (matches.Count == 1)
                        {
                            RService.Log("(GetHistoricos) " + Historic + ": Encontrado 1 item de histórico at {0}", Path.GetTempPath() + Historic + ".txt");
                        }
                        else if (matches.Count > 1)
                        {
                            RService.Log("(GetHistoricos) " + Historic + ": Encontrados " + matches.Count + " itens de histórico at {0}", Path.GetTempPath() + Historic + ".txt");
                        }
                        else
                        {
                            RService.Log("(GetHistoricos) " + Historic + ": Não foram encontrados novos itens de histórico at {0}", Path.GetTempPath() + Historic + ".txt");
                        }
                    }
                    else
                    {
                        RService.Log("(GetHistoricos) " + Historic + ": Não foram encontrados itens de histórico at {0}", Path.GetTempPath() + Historic + ".txt");
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetHistoricos) " + Historic + " para licitação " + l.IdLicitacaoFonte + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Historic + ".txt");
            }
        }

        #endregion
    }
}
