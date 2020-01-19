using HtmlAgilityPack;
using OpenQA.Selenium.Chrome;
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
using System.Threading.Tasks;

namespace RSBM.Controllers
{
    public class CNETPrecosController
    {
        #region Declaração de variáveis

        public static string Name { get; } = Constants.CNETPRECOS_NAME;
        private static ConfigRobot Config;
        private static int NumPrecos;
        private static string LogPath { get; } = Path.GetTempPath() + Name + ".txt";
        private static PrecoRepository repo;
        #endregion

        #region Métodos
        internal static void InitCallBack(object state)
        {
            try
            {
                Config = ConfigRobotController.FindByName(Name);

                if (Config.Active == 'Y')
                {
                    // Deleta o último arquivo de log.
                    if (File.Exists(LogPath))
                        File.Delete(LogPath);

                    Config.Status = 'R';
                    ConfigRobotController.Update(Config);
                    using (FileStream fs = File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire")) { }
                    Init();

                    //Verifica se teve atualização
                    Config = ConfigRobotController.FindByName(Name);

                    Config.NumLicitLast = NumPrecos;
                    RService.Log(Name + " find " + NumPrecos + " novos preços at {0}", LogPath);
                    NumPrecos = 0;

                    Config.LastDate = DateTime.Now;
                }


                //Reprogamando a próxima execução do robô.
                RService.ScheduleMe(Config);

                //Atualiza as informações desse robô.
                Config.Status = 'W';
                ConfigRobotController.Update(Config);

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
            RService.Log("(Init) " + Name + ": Buscando licitações homologadas..." + " at {0}", LogPath);
            try
            {
                DateTime dataLimite = DateTime.Today.AddDays(-90);
                repo = new PrecoRepository();

                /*Busca licitações com a data de abertura anterior a 90 dias, ou que ainda não aconteceu*/
                List<Licitacao> licitacoes = LicitacaoController.FindByRangeHistoric(Constants.CN_HOST, dataLimite);

                RService.Log("(Init) " + Name + ": " + licitacoes.Count + " licitacões encontradas at {0}", LogPath);

                foreach (Licitacao licitacao in licitacoes)
                {
                    RService.Log("(Init) " + Name + ": Consultando ata e histórico da licitação " + licitacao.IdLicitacaoFonte + " at {0}", LogPath);
                    ConsultaAtaPregao(licitacao);
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (Init) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
            }

        }

        private static void ConsultaAtaPregao(Licitacao licitacao)
        {
            RService.Log("(ConsultaAtaPregao) " + Name + ": Buscando ata do pregão at {0}", LogPath);

            try
            {
                if (!string.IsNullOrEmpty(licitacao.Uasg) && !string.IsNullOrEmpty(licitacao.NumPregao))
                {
                    List<LicitacaoHistorico> itens = new List<LicitacaoHistorico>();

                    string _num = Regex.Replace(licitacao.NumPregao, @"[^\d+]", "");
                    string _url = string.Format(Constants.CN_ATA_PREGAO, licitacao.Uasg, _num);
                    HtmlDocument htmlDoc = WebHandle.GetHtmlDocOfPage(_url, Encoding.GetEncoding("ISO-8859-1"));
                    var licitInfo = FindLicitInfo(htmlDoc.DocumentNode.InnerHtml);

                    if (!string.IsNullOrEmpty(licitInfo))
                    {
                        HandleTermoHomologacao(licitInfo, licitacao);
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (ConsultaAtaPregao) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
            }
        }

        private static void HandleTermoHomologacao(string licitInfo, Licitacao licitacao)
        {
            try
            {
                var nums = HandleLicitInfo(licitInfo);

                var termoHomologacaoUrl = string.Format(Constants.CN_TERMOHOMOLOGACAO, nums[0], nums[1], nums[2]);
                HtmlDocument homologDoc = WebHandle.GetHtmlDocOfPage(termoHomologacaoUrl, Encoding.GetEncoding("ISO-8859-1"));

                var grupos = homologDoc.DocumentNode.ChildNodes[0].ChildNodes[3].ChildNodes.Where(g => g.InnerText.Contains("GRUPO")).ToList();

                foreach (var grupo in grupos)
                {
                    GetPrecos(grupo, licitacao);
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (HandleTermoHomologacao) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
            }
        }

        private static void GetPrecos(HtmlNode grupo, Licitacao licitacao)
        {
            try
            {
                var itens = grupo.ChildNodes.Where(i => i.InnerText.Contains("Item")).ToList();

                for (int i = 0; i < itens.Count; i += 2)
                {
                    int j = i + 1;

                    if (itens[i].InnerText.Contains("Homologado"))
                    {
                        Preco preco = CreatePreco(itens[i], itens[j], licitacao.Id);

                        if (preco != null && !repo.Exists(preco))
                        {
                            repo.Insert(preco);
                            NumPrecos++;
                            RService.Log("(GetPrecos) " + Name + ": Preço obtido com sucesso para licitacao " + licitacao.Id + " at {0}", LogPath);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetPrecos) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
            }
        }

        private static Preco CreatePreco(HtmlNode itemInfo, HtmlNode itemEvents, int id)
        {
            Preco preco = new Preco();

            var infoNodes = itemInfo.ChildNodes[1].ChildNodes[3];
            var priceNode = itemInfo.ChildNodes[1].ChildNodes[5];
            var dateNode = itemEvents.ChildNodes[1].ChildNodes[3].ChildNodes[9];

            try
            {
                preco.IdLicitacao = id;
                preco.Item = GetNomeItem(infoNodes.ChildNodes[3]);
                preco.Descricao = GetDescricaoItem(infoNodes.ChildNodes[3]);
                preco.Quantidade = GetQuantidadeItem(infoNodes.ChildNodes[3]);
                preco.ValorEstimado = GetValorEstimadoItem(infoNodes.ChildNodes[3]);
                preco.ValorHomologado = GetValorHomologado(priceNode);
                preco.DataHomologacao = GetDataHomologacao(dateNode);
                preco.DataInsercao = DateTime.Now;
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreatePreco) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", LogPath);
            }

            return preco;
        }

        private static string GetNomeItem(HtmlNode htmlNode)
        {
            Regex rxNome = new Regex("Descrição\\:(.*?)\\<\\/td");

            if (rxNome.IsMatch(htmlNode.InnerHtml))
            {
                var matches = rxNome.Matches(htmlNode.InnerHtml);
                if (matches.Count > 0)
                {
                    string nome = matches[0].Value
                                            .Replace("Descrição:</b>", "")
                                            .Replace("</td", "");

                    return nome;
                }
            }

            return null;
        }

        private static string GetDescricaoItem(HtmlNode htmlNode)
        {
            Regex rxDescricao = new Regex("Descrição Complementar\\:(.*?)\\<\\/td");

            if (rxDescricao.IsMatch(htmlNode.InnerHtml))
            {
                var matches = rxDescricao.Matches(htmlNode.InnerHtml);
                if (matches.Count > 0)
                {
                    string descricao = matches[0].Value
                                            .Replace("Descrição Complementar:</b>", "")
                                            .Replace("</td", "");

                    return descricao;
                }
            }

            return null;
        }

        private static double GetQuantidadeItem(HtmlNode htmlNode)
        {
            Regex rxQtde = new Regex("Quantidade\\:(.*?)\\<\\/td");

            if (rxQtde.IsMatch(htmlNode.InnerHtml))
            {
                var matches = rxQtde.Matches(htmlNode.InnerHtml);
                if (matches.Count > 0)
                {
                    string quantidade = matches[0].Value
                                            .Replace("Quantidade:</b>", "")
                                            .Replace("</td", "");

                    return double.Parse(quantidade.Trim());
                }
            }

            return 0;
        }

        private static double GetValorEstimadoItem(HtmlNode htmlNode)
        {
            Regex rxEstimado = new Regex("Valor estimado\\:(.*?)\\<\\/td");

            if (rxEstimado.IsMatch(htmlNode.InnerHtml))
            {
                var matches = rxEstimado.Matches(htmlNode.InnerHtml);
                if (matches.Count > 0)
                {
                    string valorEstimado = matches[0].Value
                                            .Replace("Valor estimado: R$</b> ", "")
                                            .Replace("</td", "");

                    return double.Parse(valorEstimado.Trim());
                }
            }

            return 0;
        }

        private static double GetValorHomologado(HtmlNode priceNode)
        {
            var info = Regex.Replace(priceNode.InnerText, "\t", "");
            info = Regex.Replace(info, "\n", "");

            Regex rxValorHomolog = new Regex("lance de (.*)");

            if (rxValorHomolog.IsMatch(info))
            {
                var matches = rxValorHomolog.Matches(info);
                if (matches.Count > 0)
                {
                    string valorHomolog = matches[0].Value
                                                    .Replace("lance de R$ ", "")
                                                    .Replace("  .     ", "");

                    return double.Parse(valorHomolog.Trim());
                }
            }

            return 0;
        }

        private static DateTime GetDataHomologacao(HtmlNode dateNode)
        {
            string info = Regex.Replace(dateNode.InnerText, "\t", "");
            info = Regex.Replace(info, "\n", "");

            Regex rxDataHomolog = new Regex(@"\d+/\d+/\d+ \d+\:\d+:\d+");

            if (rxDataHomolog.IsMatch(info))
            {
                var matches = rxDataHomolog.Matches(info);
                if (matches.Count > 0)
                {
                    string dataHomolog = matches[0].Value;

                    return DateTime.Parse(dataHomolog);
                }
            }

            return new DateTime();
        }

        private static List<string> HandleLicitInfo(string licitInfo)
        {
            List<string> nums = new List<string>();

            var data = licitInfo.Split('(')[1].Split(')')[0].Split(',');

            foreach (var d in data)
            {
                nums.Add(d);
            }

            return nums;
        }

        private static string FindLicitInfo(string innerHtml)
        {
            Regex rxInfo = new Regex("termoHomologacao\\(\\d+\\,\\d+\\,\\d+\\)");

            if (rxInfo.IsMatch(innerHtml))
            {
                var matches = rxInfo.Matches(innerHtml);
                if (matches.Count > 0)
                {
                    return matches[0].Value;
                }
            }

            return null;
        }
        #endregion
    }
}
