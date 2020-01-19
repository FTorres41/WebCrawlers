using OpenQA.Selenium;
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
using System.Threading;

namespace RSBM.Controllers
{
    class GenericElementController
    {
        #region Declaração de variáveis
        private static ChromeDriver web;
        private static WebDriverWait wait;

        public static string Name { get; } = "GenericElement";
        private static int NumAlteracoes;
        private static string content;

        private static ConfigRobot config;
        private static FontePesquisaRepository repo;
        private static FontePesquisaRobot fpr;
        private static List<FontePesquisa> fontePesquisa;

        private static List<string> meses = new List<string>() { "janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro" };
        #endregion

        enum StatusAlerta
        {
            Warning = 0,
            Processado = 1,
            Sem_Acao = 2
        }

        #region Métodos
        //Método pelo qual inicia a execução do robô a partir do timer agendado
        internal static void InitCallBack(object state)
        {
            try
            {
                config = ConfigRobotController.FindByName(Name);

                if (config.Active == 'Y')
                {
                    if (File.Exists(Path.GetTempPath() + Name + ".txt"))
                        File.Delete(Path.GetTempPath() + Name + ".txt");

                    config.Status = 'R';
                    ConfigRobotController.Update(config);
                    using (FileStream fs = File.Create(Path.GetTempPath() + Guid.NewGuid().ToString() + ".ire")) { }
                    Init();

                    config = ConfigRobotController.FindByName(Name);

                    config.NumLicitLast = NumAlteracoes;
                    RService.Log(Name + " find " + NumAlteracoes + " novas alterações at {0}", Path.GetTempPath() + Name + ".txt");
                    NumAlteracoes = 0;

                    config.LastDate = DateTime.Now;
                }

                RService.ScheduleMe(config);

                config.Status = 'W';
                ConfigRobotController.Update(config);

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
            RService.Log("(Init) " + Name + ": Começando o processamento... at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                NumAlteracoes = 0;
                repo = new FontePesquisaRepository();
                fontePesquisa = repo.FindByElement();

                //FontePesquisa f = repo.FindById(1750);
                //fontePesquisa = new List<FontePesquisa>();
                //fontePesquisa.Add(f);

                foreach (var fp in fontePesquisa)
                {
                    RService.Log("(Init) " + Name + ": Consultando fonte: " + fp.Nome + " at {0}", Path.GetTempPath() + Name + ".txt");

                    try
                    {
                        LoadChromeDriver();

                        RegistrarConsulta(fp);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        if (web != null)
                            web.Quit();
                    }
                }

            }
            catch (Exception e)
            {
                RService.Log("Exception (Init) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }
     
        private static void RegistrarConsulta(FontePesquisa fp)
        {
            RService.Log("(RegistrarConsulta) " + Name + ": Verificando registros... " + "at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                //Analisa a Regex para definir qual caminho seguir, o que exige mais de um clique ou o que ele consegue capturar o conteúdo de primeira
                content = SitesComMaisProcedimentos(fp);            

                if (fp.Regex.Contains(';'))
                {
                    string[] element = fp.Regex.Split(';');
                    int last = element.Count() - 1;

                    web.Navigate().GoToUrl(fp.Link);
                    Thread.Sleep(3000);

                    for (int i = 0; i < last; i++)
                    {
                        if (element[i].Contains("frame"))
                        {
                            web.SwitchTo().Frame(web.FindElement(By.XPath(element[i])));
                        }
                        else
                        {
                            web.FindElement(By.XPath(element[i])).Click();
                        }
                    }

                    content = web.FindElement(By.XPath(element[last])).GetAttribute("innerHTML").ToString();
                }
                else
                {
                    web.Navigate().GoToUrl(fp.Link);
                    Thread.Sleep(3000);

                    content = web.FindElement(By.XPath(fp.Regex)).GetAttribute("innerHTML").ToString();
                }
                if (content.Contains("�") || content.Contains("\""))
                {
                    content = content.Replace("�", "");
                    content = content.Replace("\"", "");
                }

                fpr = new FontePesquisaRobot(fp.Id);
                fpr.DataHoraPesquisa = DateTime.Now;
                fpr.Conteudo = content;

                //Usando o valor obtido no if-else anterior, é registrada a alteração no banco
                if (!content.Equals(fp.UltimoConteudo))
                {
                    if (fp.UltimoConteudo != null)
                    {
                        RService.Log("(RegistrarConsulta) " + Name + ": Houve alteração na fonte de pesquisa, gerado Warning... at {0}", Path.GetTempPath() + Name + ".txt");

                        fpr.Status = (byte)StatusAlerta.Warning;
                        FontePesquisaRobotController.Criar(fpr);
                        NumAlteracoes++;
                    }

                    fp.UltimoConteudo = content;
                    FontePesquisaController.Atualizar(fp);
                }
                else
                {
                    RService.Log("(RegistrarConsulta) " + Name + ": Não houve alteração na fonte de pesquisa at {0}", Path.GetTempPath() + Name + ".txt");
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (RegistrarConsulta) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        private static string SitesComMaisProcedimentos(FontePesquisa fp)
        {
            switch (fp.Nome)
            {
                case "Diário Oficial do Maranhão":
                    content = GetContentDOMaranhao(fp);
                    break;
                case "Imprensa Oficial do Estado do Rio de Janeiro":
                    content = GetContentIORJ(fp);
                    break;
                case "Diário Oficial de Canoas - RS":
                    content = GetContentDOCanoas(fp);
                    break;
                case "Diário Oficial de Candeal - BA":
                case "Diário Oficial de Santaluz - BA":
                    content = GetContentDOCandeal(fp);
                    break;
                case "Diário Oficial de Pocinhos - PB":
                    content = getContentDOPocinhos(fp);
                    break;
                case "Diário Oficial de Ariranha do Ivaí  - PR":
                case "Diário Oficial de Lidianópolis - PR":
                    content = getContentDOAriranhaAvai(fp);
                    break;
                case "Diário Oficial de Guarujá - SP":
                    content = getContentDOGuaruja(fp);
                    break;                                 
            }

            return content;
        }

        //Método específico para o Diário Oficial de Guarujá
        private static string getContentDOGuaruja(FontePesquisa fp)
        {
            web.Navigate().GoToUrl(fp.Link + string.Format("index.php/{0}{1}", meses[DateTime.Now.Month - 1], DateTime.Now.Year));
            Thread.Sleep(2000);

            return web.FindElement(By.XPath(fp.Regex)).GetAttribute("innerHTML");
        }

        //Método específico para o Diário Oficial de Ariranha do Avaí e Lidianópolis
        private static string getContentDOAriranhaAvai(FontePesquisa fp)
        {
            if (fp.Nome == "Diário Oficial de Lidianópolis - PR")
            {
                web.Navigate().GoToUrl(fp.Link + string.Format("publicMes15.php?public={0}&ano={1}", meses[DateTime.Now.Month - 1], DateTime.Now.Year));
                Thread.Sleep(1000);

                return web.FindElement(By.XPath("//*[@id='conteudo_direito_meio']/table/tbody")).GetAttribute("innerHTML");
            }
            else
            {
                web.Navigate().GoToUrl(fp.Link + string.Format("publicMes.php?public={0}&ano={1}", meses[DateTime.Now.Month - 1], DateTime.Now.Year));
                Thread.Sleep(1000);

                return web.FindElement(By.XPath("//*[@id='tabela1']/tbody")).GetAttribute("innerHTML");
            }
        }      

        //Método específico para o Diário Oficial de Pocinhos
        private static string getContentDOPocinhos(FontePesquisa fp)
        {
            string[] element = fp.Regex.Split(';');
            string format = string.Empty;

            web.Navigate().GoToUrl(fp.Link);
            Thread.Sleep(1000);

            var data = DateTime.Now;
            int mes = data.Month;
            int ano = data.Year;

            web.FindElement(By.XPath(element[0])).Click();
            web.FindElement(By.XPath("//*[@id='table23']/tbody/tr/td/form/select[1]/option[2]")).Click();

            web.FindElement(By.XPath(element[1])).Click();
            web.FindElement(By.XPath(string.Format("//*[@id='table23']/tbody/tr/td/form/select[2]/option[{0}]", mes + 1))).Click();

            web.FindElement(By.XPath(element[2])).Click();

            web.Navigate().GoToUrl(string.Format("view-source:http://pocinhos.pb.gov.br/portaldatransparencia/data{0}.htm", mes < 10 ? ano + "0" + mes : ano + mes.ToString()));
            Thread.Sleep(1000);

            return web.PageSource;
        }

        //Método específico para o Diário Oficial de Itabaianinha
        private static string getContentDOItabaianinha(FontePesquisa fp)
        {
            string content = string.Empty;
            string[] element = fp.Regex.Split(';');

            web.Navigate().GoToUrl(fp.Link);
            string year = DateTime.Today.Year.ToString("d4");
            new SelectElement(web.FindElement(By.XPath(element[0]))).SelectByText(year);
            Thread.Sleep(2000);
            string month = DateTime.Today.Month.ToString("d2");
            new SelectElement(web.FindElement(By.XPath(element[1]))).SelectByValue(month);
            Thread.Sleep(2000);
            var options = web.FindElement(By.XPath("//*[@id=\"body_ddlEdicao\"]")).FindElements(By.TagName("option"));
            int index = options.Count() - 1;
            content = options[index].GetAttribute("innerHTML").ToString();

            return content;
        }

        //Método específico para o Diário Oficial de Candeal
        private static string GetContentDOCandeal(FontePesquisa fp)
        {
            int cont = 0;
            IWebElement mesSelecionado = null;
            IWebElement diaSelecionado = null;
            List<IWebElement> addMes = new List<IWebElement>();
            List<IWebElement> addDia = new List<IWebElement>();

            web.Navigate().GoToUrl(fp.Link);
            Thread.Sleep(500);

            web.FindElement(By.XPath(fp.Regex)).Click();
            Thread.Sleep(1000);

            var blocoMeses = web.FindElement(By.XPath("//*[@id='ui-id-2']"));
            var lstMeses = blocoMeses.FindElements(By.TagName("h5"));

            foreach (var mes in lstMeses)
            {
                if (meses.Contains(mes.Text))
                    addMes.Add(mes);
            }

            foreach (var mes in addMes)
            {
                cont++;
                if (cont == addMes.Count)
                {
                    mes.Click();
                    mesSelecionado = mes;
                    cont = 0;
                }

            }
            Thread.Sleep(1000);

            string getAttributeMes = mesSelecionado.GetAttribute("aria-controls");
            var getDias = blocoMeses.FindElement(By.XPath("//div[@id='" + getAttributeMes + "']"));
            var lstDias = getDias.FindElements(By.TagName("h5"));

            foreach (var diaLic in lstDias)
            {
                cont++;
                if (cont == lstDias.Count)
                {
                    diaLic.Click();
                    diaSelecionado = diaLic;
                }
            }
            Thread.Sleep(1000);

            string getAttributeDia = diaSelecionado.GetAttribute("aria-controls");
            var getEdicao = getDias.FindElement(By.XPath("//div[@id='" + getAttributeDia + "']"));

            return getEdicao.GetAttribute("innerText");
        }

        //Método específico para o Diário Oficial de Canoas
        private static string GetContentDOCanoas(FontePesquisa fp)
        {
            string content = string.Empty;
            string[] element = fp.Regex.Split(';');

            web.Navigate().GoToUrl(fp.Link);
            web.FindElement(By.XPath(element[0])).Click();
            web.FindElement(By.Id("j_idt31:dataPublicacao1_input")).SendKeys(DateTime.Today.ToString("dd/MM/yyyy"));
            web.FindElement(By.Id("j_idt31:dataPublicacao1_input")).SendKeys(Keys.Escape);
            web.FindElement(By.Id("j_idt31:dataPublicacao1_input")).SendKeys(Keys.Enter);
            web.FindElement(By.XPath(element[1])).Click();
            content = web.FindElement(By.XPath(element[2])).GetAttribute("innerHTML").ToString();

            return content;
        }

        //Método específico para o Diário Oficial de Lavras
        private static string GetContentDOLavras(FontePesquisa fp)
        {
            string content = string.Empty;

            web.Navigate().GoToUrl(fp.Link);
            web.ExecuteScript("document.getElementsByClassName('doclink')[0].setAttribute('target', '_self')");
            web.FindElement(By.XPath(fp.Regex)).Click();
            Thread.Sleep(30000);
            var d = web.FindElements(By.ClassName("diarioOficial-titulo"));
            content = d[0].GetAttribute("innerHTML").ToString();

            return content;
        }

        //Método específico para a Imprensa Oficial do Rio de Janeiro
        private static string GetContentIORJ(FontePesquisa fp)
        {
            string content = string.Empty;

            string[] element = fp.Regex.Split(';');

            web.Navigate().GoToUrl(fp.Link);
            web.FindElement(By.Name("uname")).SendKeys("fabiored");
            web.FindElement(By.Name("pass")).SendKeys("bingo66");
            web.FindElement(By.XPath(element[0])).Click();
            Thread.Sleep(5000);
            web.Navigate().GoToUrl("http://www.ioerj.com.br/portal/modules/conteudoonline/do_ultima_edicao.php");
            content = web.FindElement(By.XPath(element[1])).GetAttribute("innerHTML").ToString();

            return content;
        }

        //Método específico para o Diário Oficial do Governo do Maranhão
        private static string GetContentDOMaranhao(FontePesquisa fp)
        {
            string content = string.Empty;

            web.Navigate().GoToUrl(fp.Link);
            web.FindElement(By.Name("formPesq:calendarInic_input")).SendKeys(DateTime.Today.AddDays(-1).ToString("dd/MM/yyyy"));
            web.FindElement(By.Name("formPesq:calendarFim_input")).SendKeys(DateTime.Today.ToString("dd/MM/yyyy"));

            new SelectElement(web.FindElement(By.Name("formPesq:comboCaderno"))).SelectByText("Executivo");
            web.FindElement(By.Name("formPesq:j_idt98")).Click();
            Thread.Sleep(1000);
            if (!web.PageSource.Contains("Não há resultados para essa consulta."))
                content = content + "\n\n" + web.FindElement(By.XPath(fp.Regex)).GetAttribute("innerHTML").ToString();

            new SelectElement(web.FindElement(By.Name("formPesq:comboCaderno"))).SelectByText("Judiciário");
            web.FindElement(By.Name("formPesq:j_idt98")).Click();
            Thread.Sleep(1000);
            if (!web.PageSource.Contains("Não há resultados para essa consulta."))
                content = content + "\n\n" + web.FindElement(By.XPath(fp.Regex)).GetAttribute("innerHTML").ToString();

            new SelectElement(web.FindElement(By.Name("formPesq:comboCaderno"))).SelectByText("Terceiros");
            web.FindElement(By.Name("formPesq:j_idt98")).Click();
            Thread.Sleep(1000);
            if (!web.PageSource.Contains("Não há resultados para essa consulta."))
                content = content + "\n\n" + web.FindElement(By.XPath(fp.Regex)).GetAttribute("innerHTML").ToString();

            new SelectElement(web.FindElement(By.Name("formPesq:comboCaderno"))).SelectByText("Suplemento Executivo");
            web.FindElement(By.Name("formPesq:j_idt98")).Click();
            Thread.Sleep(1000);
            if (!web.PageSource.Contains("Não há resultados para essa consulta."))
                content = content + "\n\n" + web.FindElement(By.XPath(fp.Regex)).GetAttribute("innerHTML").ToString();

            new SelectElement(web.FindElement(By.Name("formPesq:comboCaderno"))).SelectByText("Suplemento Judiciário");
            web.FindElement(By.Name("formPesq:j_idt98")).Click();
            Thread.Sleep(3000);
            if (!web.PageSource.Contains("Não há resultados para essa consulta."))
                content = content + "\n\n" + web.FindElement(By.XPath(fp.Regex)).GetAttribute("innerHTML").ToString();

            new SelectElement(web.FindElement(By.Name("formPesq:comboCaderno"))).SelectByText("Suplemento de Terceiros");
            web.FindElement(By.Name("formPesq:j_idt98")).Click();
            Thread.Sleep(3000);
            if (!web.PageSource.Contains("Não há resultados para essa consulta."))
                content = content + "\n\n" + web.FindElement(By.XPath(fp.Regex)).GetAttribute("innerHTML").ToString();

            return content;
        }

        //Método que inicializa o navegador Chrome
        #region OLD
        //private static void LoadChromeDriver()
        //{
        //    try
        //    {
        //        if (web != null)
        //            web.Quit();

        //        var driver = ChromeDriverService.CreateDefaultService();
        //        driver.HideCommandPromptWindow = true;

        //        ChromeOptions options = new ChromeOptions();

        //        web = new ChromeDriver(driver, options, TimeSpan.FromSeconds(180));
        //        web.Manage().Window.Maximize();
        //        web.Manage().Timeouts().SetPageLoadTimeout(TimeSpan.FromSeconds(180));
        //        web.Manage().Timeouts().ImplicitlyWait(TimeSpan.FromSeconds(180));

        //        wait = new WebDriverWait(web, TimeSpan.FromSeconds(180));
        //    }
        //    catch (Exception e)
        //    {
        //        RService.Log("Exception (LoadWebDriver) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");

        //        if (web != null)
        //            web.Quit();
        //    }
        //}
        #endregion

        private static void LoadChromeDriver()
        {
            Tuple<ChromeDriver, WebDriverWait> loadDriver = WebDriverChrome.LoadWebDriver(Name);
            web = loadDriver.Item1;
            wait = loadDriver.Item2;
        }

        #endregion
    }
}
