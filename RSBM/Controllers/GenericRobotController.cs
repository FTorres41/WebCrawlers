using HtmlAgilityPack;
using OpenQA.Selenium.Chrome;
using RSBM.Models;
using RSBM.Repository;
using RSBM.Util;
using RSBM.WebUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace RSBM.Controllers
{
    enum StatusAlerta
    {
        Warning = 0,
        Processado = 1,
        Sem_Acao = 2
    }

    class GenericRobotController
    {
        #region
        private static FontePesquisaRepository repo;
        private static List<FontePesquisa> fontePesquisa;
        private static ConfigRobot config;

        private static int NumAlteracoes;

        public static string Name { get; } = "GenericRobot";
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
                    config.NumLicitLast = NumAlteracoes;
                    RService.Log(Name + " find " + NumAlteracoes + " novas alterações at {0}", Path.GetTempPath() + Name + ".txt");
                    NumAlteracoes = 0;

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
            RService.Log("(Init) " + Name + ": Começando o processamento..." + " at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                NumAlteracoes = 0;
                repo = new FontePesquisaRepository();
                //fontePesquisa = repo.FindByActiveRobot();
                fontePesquisa = repo.FindByRegex();


                //Para debug descomentar o código abaixo
                /*FontePesquisa fp = repo.FindById(1750);
                fontePesquisa = new List<FontePesquisa>();
                fontePesquisa.Add(fp);*/

                foreach (FontePesquisa f in fontePesquisa)
                {
                    RService.Log("(Init) " + Name + ": Consultando fonte: " + f.Nome + " at {0}", Path.GetTempPath() + Name + ".txt");
                    try
                    {
                        HtmlDocument html = new HtmlDocument();

                        html = SitesComCodigoFrame(f);

                        if (string.IsNullOrEmpty(html.DocumentNode.InnerText))
                        {
                            html = WebHandle.HtmlParaObjeto(f.Link, Encoding.UTF8);
                            if (html.DocumentNode.InnerHtml.Contains("�"))
                            {
                                html = WebHandle.HtmlParaObjeto(f.Link, Encoding.GetEncoding("ISO-8859-1"));
                            }
                        }

                        RegistrarConsulta(html, f);
                    }
                    catch (Exception e)
                    {
                        RService.Log("Exception (Init)" + Name + ":" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (Init)" + Name + ":" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        private static HtmlDocument SitesComCodigoFrame(FontePesquisa f)
        {
            List<string> lstViewSourcePage = new List<string>();
            HtmlDocument html = new HtmlDocument();

            if (f.Nome.Equals("Diário Oficial de São Félix do Coribe"))
                lstViewSourcePage.Add("view-source:http://procedebahia.com.br/ba/saofelixdocoribe/io/");
            if (f.Nome.Equals("Diário Oficial de Rio Brilhante - MS"))
                lstViewSourcePage.Add("view-source:http://www.diariooficial.inf.br/arquivos.asp");

            if (lstViewSourcePage.Count != 0)
            {
                ChromeDriver web = new ChromeDriver();

                web.Navigate().GoToUrl(f.Link);
                Thread.Sleep(2000);

                web.Navigate().GoToUrl(lstViewSourcePage[0]);
                Thread.Sleep(5000);

                string codigoFonte = web.PageSource;
                html.DocumentNode.InnerHtml = codigoFonte;
                web.Quit();
            }

            return html;
        }

        /*Registra a consulta no banco*/
        private static void RegistrarConsulta(HtmlDocument htmlDocument, FontePesquisa fp)
        {
            RService.Log("(RegistrarConsulta) " + Name + ": Verificando registros... " + "at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                string htmlTratado = Regex.Replace(htmlDocument.DocumentNode.InnerHtml, @"\s*\n*", "");
                string valorRegex = string.Format(@"{0}", fp.Regex);

                MatchCollection mt = StringHandle.GetMatches(htmlTratado, valorRegex);
                string conteudo = mt != null ? mt[0].Value : string.Empty;

                FontePesquisaRobot fpr = new FontePesquisaRobot(fp.Id);
                fpr.DataHoraPesquisa = DateTime.Now;
                fpr.Conteudo = conteudo;

                /*caso tenha alteração no site*/
                if (!conteudo.Equals(fp.UltimoConteudo))
                {
                    if (fp.UltimoConteudo != null)
                    {
                        RService.Log("(RegistrarConsulta) " + Name + ": Houve alteração na fonte de pesquisa, gerado Warning... at {0}", Path.GetTempPath() + Name + ".txt");
                        fpr.Status = (byte)StatusAlerta.Warning;
                        FontePesquisaRobotController.Criar(fpr);

                        NumAlteracoes++;
                    }

                    fp.UltimoConteudo = conteudo;
                    FontePesquisaController.Atualizar(fp);
                }
                /*caso não tenha alteração no site*/
                else
                {
                    RService.Log("(RegistrarConsulta) " + Name + ": Não houve alteração na fonte de pesquisa... at {0}", Path.GetTempPath() + Name + ".txt");
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (RegistrarConsulta)" + Name + ":" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }
    }
}
