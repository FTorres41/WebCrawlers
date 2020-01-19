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
using System.Threading.Tasks;

namespace RSBM.Controllers
{
    public class BBRMController
    {
        #region Declaracao de variveis
        private static ChromeDriver web;
        private static WebDriverWait wait;

        private static List<string> numUFs = new List<string>();
        private static List<string> numUFsSalved = new List<string>();
        private static List<string> licitacoesAlteradas = new List<string>();
        private static int NumArquivosLicitacao;
        private static int NumCaptcha = 0;
        private static int IdFonte { get; } = 509;

        private static ConfigRobot config;

        private static bool TryReload;

        public static string Name { get; } = "BBRM";
        public static string PathEditais { get; } = Path.GetTempPath() + DateTime.Now.ToString("yyyyMM") + Name + "/";
        #endregion

        /*Método pelo qual o serviço inicia o robô no Timer agendado.*/
        internal static void RemainingCallBack(object state)
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
                    RemainingFiles();

                    //Verifica se teve atualização
                    config = ConfigRobotController.FindByName(Name);

                    //Verifica quantas licitações foram coletadas nessa execução, grava em log.
                    config.NumLicitLast = NumArquivosLicitacao;
                    RService.Log(Name + " find " + NumArquivosLicitacao + " novos arquivos de licitações at {0}", Path.GetTempPath() + Name + ".txt");
                    RService.Log(Name + " consumiu " + NumCaptcha + " captchas at {0}", Path.GetTempPath() + Name + ".txt");
                    NumArquivosLicitacao = 0;

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
                RService.Log("Exception (RemainingCallBack) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }

            RService.Log("Finished " + Name + " at {0}", Path.GetTempPath() + Name + ".txt");

            EmailHandle.SendMail(Path.GetTempPath() + Name + ".txt", Name);
        }

        /*Busca arquivos de edital para licitações antigas que tenham data de abertura maior do que o dia corrente.*/
        private static void RemainingFiles()
        {
            RService.Log("(RemainingFiles) " + Name + ": Buscando arquivos de licitações antigas..." + " at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                /*Busca licitações com a data de abertura maior do que o dia corrente*/
                List<Licitacao> licitacoes = LicitacaoController.GetAberturaGratherThan(DateTime.Today, Constants.BB_HOST);

                TryReload = true;

                foreach (Licitacao lic in licitacoes)
                {
                    //if (lic.DigitacaoData.HasValue)
                    //{
                        /*Verifica e baixa os arquivos que ainda não foram coletados*/
                        DownloadEdAndCreatLicArq(lic.IdLicitacaoFonte.ToString(), lic);
                    //}
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (RemainingFiles) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
            finally
            {
                if (web != null)
                    web.Close();
            }
        }

        /*Acessa página dos arquivos de edital passando pelo captcha*/
        private static void DownloadEdAndCreatLicArq(string num, Licitacao licitacao)
        {
            RService.Log("(DownloadEdAndCreatLicArq) " + Name + ": Visualizando arquivos de edital, licitação... " + num + " at {0}", Path.GetTempPath() + Name + ".txt");

            try
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


                web.Navigate().GoToUrl(licitacao.LinkEdital);

                if (TryReload)
                {
                    foreach (var a in web.FindElements(By.TagName("a")))
                    {
                        if (!string.IsNullOrEmpty(a.GetAttribute("onclick")) && a.GetAttribute("onclick").Contains("Listar documentos"))
                        {
                            web.ExecuteScript(a.GetAttribute("onclick"));
                            wait.Until(ExpectedConditions.ElementIsVisible(By.Id("idImgListarAnexo")));

                            web.ExecuteScript(GetScriptFillCaptcha("idImgListarAnexo", "inputCaptchaAnexosLicitacao"));
                            Thread.Sleep(1000);

                            web.ExecuteScript("document.getElementById('botao_continuar').click()");
                            Thread.Sleep(2000);

                            var select = web.FindElement(By.TagName("select"));
                            new SelectElement(select).SelectByValue("-1"); //mostrar todos

                            MatchCollection linkForm = Regex.Matches(web.PageSource, "numeroLicitacao=(.+?)&amp;sem-reg=true");

                            if (linkForm.Count > 0)
                                DownloadEditais(FindDocLinks(num), licitacao, linkForm);

                            if (Directory.Exists(PathEditais))
                                Directory.Delete(PathEditais, true);

                            break;
                        }
                    }
                }
                else
                {
                    web.ExecuteScript(GetScriptFillCaptcha("idImgListarAnexo", "inputCaptchaAnexosLicitacao"));
                    Thread.Sleep(1000);

                    web.ExecuteScript("document.getElementById('botao_continuar').click()");
                    Thread.Sleep(2000);

                    var select = web.FindElement(By.XPath("//*[@id=\"tDocumento_length\"]/label/select"));
                    new SelectElement(select).SelectByText("Todos");

                    MatchCollection linkForm = Regex.Matches(web.PageSource, "numeroLicitacao=(.+?)&amp;sem-reg=true");

                    DownloadEditais(FindDocLinks(num), licitacao, linkForm);

                    if (Directory.Exists(PathEditais))
                        Directory.Delete(PathEditais, true);
                }

            }
            catch (Exception e)
            {
                if (TryReload)
                {
                    RService.Log("Exception (DownloadEdAndCreatLicArq) " + Name + ": Falha na visualização, tentando novamente... " + num + " at {0}", Path.GetTempPath() + Name + ".txt");

                    TryReload = false;
                    DownloadEdAndCreatLicArq(num, licitacao);
                }
                else
                {
                    RService.Log("Exception (DownloadEdAndCreatLicArq) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
                }

                TryReload = true;
            }
        }

        private static List<string> FindDocLinks(string num)
        {
            RService.Log("(FindDocLinks) " + Name + ": Buscando editais da licitação.. " + num + " at {0}", Path.GetTempPath() + Name + ".txt");

            List<string> links = new List<string>();
            try
            {
                foreach (var ai in web.FindElements(By.TagName("a")))
                {
                    if (!string.IsNullOrEmpty(ai.GetAttribute("onclick")) && ai.GetAttribute("onclick").Contains("Download de Documento"))
                    {
                        links.Add(string.Format(Constants.BB_LINK_EDITAL, num, Regex.Replace(ai.Text, @"\u001a", "%1A")));
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (FindDocLinks) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }

            return links;
        }

        /*Baixa os arquivos de edital*/
        private static void DownloadEditais(List<string> editais, Licitacao licitacao, MatchCollection linkForm)
        {
            try
            {
                RService.Log("(DownloadEditais) " + Name + ": Consultando arquivos de edital, licitação... " + licitacao.IdLicitacaoFonte + " at {0}", Path.GetTempPath() + Name + ".txt");

                if (!Directory.Exists(PathEditais))
                {
                    Directory.CreateDirectory(PathEditais);
                }

                for (int i = 0; i < editais.Count; i++)
                {
                    //Busca os arquivos da licitação
                    LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                    List<LicitacaoArquivo> arquivosAntigos = repoArq.FindByLicitacao(licitacao.Id);

                    var edital = editais[i];
                    bool isExist = false;

                    string nameFile = Regex.Replace(edital.Split('/')[edital.Split('/').Length - 1],
                                      string.Format("[{0}]", Regex.Escape(
                                      new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()))), "");

                    while (nameFile.Contains("%1A"))
                        nameFile = Regex.Replace(nameFile, "%1A", "_");

                    foreach (var arquivoAntigo in arquivosAntigos)
                    {
                        /*Verifica se o arquivo já existe*/
                        if (nameFile.Equals(arquivoAntigo.NomeArquivoFonte))
                        {
                            isExist = true;
                            break;
                        }
                    }
                    if (!isExist)
                    {
                        Thread.Sleep(5000);
                        FillForm(linkForm[i].Value);
                        if (LicitacaoArquivoController.CreateLicitacaoArquivo(Name, licitacao, edital, PathEditais, nameFile, web.Manage().Cookies.AllCookies))
                            NumArquivosLicitacao++;
                    }
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (DownloadEditais) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        /*Resolve o captcha e preenche campo com a string obtida*/
        private static string GetScriptFillCaptcha(string imageId, string inputId)
        {
            RService.Log("(GetScriptFillCaptcha) " + Name + ": Resolvendo captcha... " + " at {0}", Path.GetTempPath() + Name + ".txt");

            try
            {
                string tempImg = Path.GetTempPath() + DateTime.Now.ToString("yyyyMMddfff") + ".jpg";
                string tempImgCrop = Path.GetTempPath() + DateTime.Now.ToString("ddMMyyyyfff") + ".jpg";

                web.GetScreenshot().SaveAsFile(tempImg, ScreenshotImageFormat.Jpeg);
                Bitmap image = (Bitmap)Image.FromFile(tempImg);
                GetCaptchaImg(web.FindElement(By.Id(imageId)), image, tempImgCrop);

                string script = string.Format("document.getElementById('{0}').value = '{1}'", inputId,
                    WebHandle.ResolveCaptcha(tempImgCrop));

                if (File.Exists(tempImg))
                    File.Delete(tempImg);
                if (File.Exists(tempImgCrop))
                    File.Delete(tempImgCrop);
                    
                NumCaptcha++;
                
                return script;
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetScriptFillCaptcha) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
                return null;
            }
        }


        /*Recorta imagem do captcha da img da pag para enviar pro decodificador*/
        private static void GetCaptchaImg(IWebElement element, Bitmap screenShot, string cutCaptchaFile)
        {
            RService.Log("(GetCaptchaImg) " + Name + ": Achando captcha... " + " at {0}", Path.GetTempPath() + Name + ".txt");
            try
            {
                Point p = element.Location;

                int eleWidth = element.Size.Width;
                int eleHeight = element.Size.Height;

                Size size = new Size(eleWidth, eleHeight);
                Rectangle r = new Rectangle(p, size);

                Image ib = screenShot.Clone(r, screenShot.PixelFormat);

                ib.Save(cutCaptchaFile);

                screenShot.Dispose();
                ib.Dispose();
            }
            catch (Exception e)
            {
                RService.Log("Exception (GetCaptchaImg) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }

        private static void FillForm(string parametros)
        {
            try
            {
                string url = string.Format("http://www.licitacoes-e.com.br/aop/lct/licitacao/consulta/CadastrarInteressedital.jsp?{0}", parametros);
                web.Navigate().GoToUrl(url);

                web.ExecuteScript(string.Format("document.getElementById('nomeEmpresa').value = '{0}'", "Otavio"));
                web.ExecuteScript(string.Format("document.getElementById('endereco').value = '{0}'", "Rua das Aguias"));
                web.ExecuteScript(string.Format("document.getElementById('bairro').value = '{0}'", "Curitiba"));
                web.ExecuteScript(string.Format("document.getElementById('cidade').value = '{0}'", "Diadema"));
                web.ExecuteScript(string.Format("document.getElementById('cep').value = '{0}'", "81000-000"));
                web.ExecuteScript(string.Format("document.getElementById('uf').value = '{0}'", "AM"));
                web.ExecuteScript(string.Format("document.getElementById('codigoTipoDocumentoCnpj').checked = '{0}'", "true"));
                web.ExecuteScript(string.Format("document.getElementById('textoDocumentoPessoaJuridica').value = '{0}'", "46824418000114"));

                web.ExecuteScript(string.Format("document.getElementById('contato').value = '{0}'", "Keller Ches"));
                web.ExecuteScript(string.Format("document.getElementById('ddd').value = '{0}'", "31"));
                web.ExecuteScript(string.Format("document.getElementById('telefone').value = '{0}'", "98879528"));
                web.ExecuteScript(string.Format("document.getElementById('email').value = '{0}'", "aloalo@gmail.com"));

                web.ExecuteScript("document.getElementsByClassName('button clear col')[0].click();");

            }
            catch (Exception e)
            {
                RService.Log("Exception (FillForm) " + Name + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + Name + ".txt");
            }
        }
    }
}
