using RSBM.Models;
using RSBM.Repository;
using RSBM.Util;
using RSBM.WebUtil;
using System;
using System.Collections.ObjectModel;
using System.IO;
using OpenQA.Selenium;
using System.Threading;

namespace RSBM.Controllers
{
    class LicitacaoArquivoController
    {
        /*Criando licitação arquivo e enviando para pasta FTP*/
        public static bool CreateLicitacaoArquivo(string nomeRobo, Licitacao licitacao, string edital, string pathEditais, string nameFile, ReadOnlyCollection<OpenQA.Selenium.Cookie> AllCookies)
        {
            try
            {
                if (!Directory.Exists(pathEditais))
                {
                    Directory.CreateDirectory(pathEditais);
                }

                string fileName = FileHandle.GetATemporaryFileName() + WebHandle.GetExtensionFile(nameFile);

                RService.Log("(CreateLicitacaoArquivo) " + nomeRobo + ": Fazendo o download do arquivo... " + fileName + " at {0}", Path.GetTempPath() + nomeRobo + ".txt");

                if (WebHandle.DownloadFileWebRequest(AllCookies, edital, pathEditais + fileName) && File.Exists(pathEditais + fileName))
                {
                    #region FTP
                    //RService.Log("(CreateLicitacaoArquivo) " + nomeRobo + ": Enviando arquivo por FTP... " + fileName + " at {0}", Path.GetTempPath() + nomeRobo + ".txt");

                    //int fileCount = Directory.GetFiles(pathEditais).Length;
                    //int wait = 0;

                    //while (fileCount == 0 && wait < 6)
                    //{
                        //Thread.Sleep(5000);
                        //wait++;
                    //}

                    //if (FTP.SendFileFtp(new FTP(pathEditais, fileName, FTP.Adrss, FTP.Pwd, FTP.UName), nomeRobo))
                    //{
                        //LicitacaoArquivo licitacaoArq = new LicitacaoArquivo();
                        //licitacaoArq.NomeArquivo = fileName;
                        //licitacaoArq.NomeArquivoOriginal = nomeRobo + DateTime.Now.ToString("yyyyMMddHHmmss");
                        //licitacaoArq.NomeArquivoFonte = nameFile;
                        //licitacaoArq.Status = 0;
                        //licitacaoArq.IdLicitacao = licitacao.Id;

                        //LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                        //repoArq.Insert(licitacaoArq);

                        //if (File.Exists(pathEditais + fileName))
                        //{
                            //File.Delete(pathEditais + fileName);
                        //}

                        //RService.Log("(CreateLicitacaoArquivo) " + nomeRobo + ": Arquivo " + fileName + " enviado com sucesso at {0}", Path.GetTempPath() + nomeRobo + ".txt");

                        //return true;
                    //}
                    //else
                    //{
                        //RService.Log("Exception (CreateLicitacaoArquivo) " + nomeRobo + ": Erro ao enviar o arquivo por FTP (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + nomeRobo + ".txt");
                    //}
                    #endregion

                    #region AWS
                    RService.Log("(CreateLicitacaoArquivo) " + nomeRobo + ": Enviando o arquivo para Amazon S3... " + fileName + " at {0}", Path.GetTempPath() + nomeRobo + ".txt");

                    if (AWS.SendObject(licitacao, pathEditais, fileName))
                    {
                        LicitacaoArquivo licitacaoArq = new LicitacaoArquivo();
                        licitacaoArq.NomeArquivo = fileName;
                        licitacaoArq.NomeArquivoOriginal = nomeRobo + DateTime.Now.ToString("yyyyMMddHHmmss");
                        licitacaoArq.NomeArquivoFonte = nameFile;
                        licitacaoArq.Status = 0;
                        licitacaoArq.IdLicitacao = licitacao.Id;

                        LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                        repoArq.Insert(licitacaoArq);

                        if (File.Exists(pathEditais + fileName))
                        {
                            File.Delete(pathEditais + fileName);
                        }

                        RService.Log("(CreateLicitacaoArquivo) " + nomeRobo + ": Arquivo " + fileName + " enviado com sucesso para Amazon S3" + " at {0}", Path.GetTempPath() + nomeRobo + ".txt");

                        return true;
                    }
                    else
                    {
                        RService.Log("Exception (CreateLicitacaoArquivo) " + nomeRobo + ": Erro ao enviar o arquivo para Amazon (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + nomeRobo + ".txt");
                    }

                    #endregion
                }
                else
                {
                    RService.Log("Exception (CreateLicitacaoArquivo) " + nomeRobo + ": Erro ao fazer o download do arquivo, no link: " + edital + " at {0}", Path.GetTempPath() + nomeRobo + ".txt");
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacaoArquivo) " + nomeRobo + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " / " + e.InnerException + " at {0}", Path.GetTempPath() + nomeRobo + ".txt");
            }
            return false;
        }

        internal static bool CreateLicitacaoArquivo(string nomeRobo, Licitacao licitacao, string pathEditais, string nameFile, ReadOnlyCollection<Cookie> allCookies)
        {
            try
            {
                if (!Directory.Exists(pathEditais))
                {
                    Directory.CreateDirectory(pathEditais);
                }

                string fileName = FileHandle.GetATemporaryFileName() + WebHandle.GetExtensionFile(nameFile);
                System.Threading.Thread.Sleep(15000);

                if (File.Exists(pathEditais + "\\" + nameFile))
                {
                    if (nomeRobo.Contains("BB") || nomeRobo.Contains("PCP") || nomeRobo.Contains("TCERS") || nomeRobo.Contains("CRJ") )
                        File.Move(pathEditais + nameFile, pathEditais + fileName);

                    #region FTP
                    //RService.Log("(CreateLicitacaoArquivo) " + nomeRobo + ": Enviando arquivo por FTP... " + fileName + " at {0}", Path.GetTempPath() + nomeRobo + ".txt");

                    //if (FTP.SendFileFtp(new FTP(pathEditais, fileName, FTP.Adrss, FTP.Pwd, FTP.UName), nomeRobo))
                    //{
                        //LicitacaoArquivo licitacaoArq = new LicitacaoArquivo();
                        //licitacaoArq.NomeArquivo = fileName;
                        //licitacaoArq.NomeArquivoOriginal = nomeRobo + DateTime.Now.ToString("yyyyMMddHHmmss");
                        //licitacaoArq.NomeArquivoFonte = nameFile;
                        //licitacaoArq.Status = 0;
                        //licitacaoArq.IdLicitacao = licitacao.Id;

                        //LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                        //repoArq.Insert(licitacaoArq);

                        //if (File.Exists(pathEditais + fileName))
                        //{
                            //File.Delete(pathEditais + fileName);
                        //}

                        //return true;
                    //}
                    //else
                    //{
                        //RService.Log("Exception (CreateLicitacaoArquivo) " + nomeRobo + ": Erro ao enviar o arquivo por FTP (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + nomeRobo + ".txt");
                    //}

                    #endregion

                    #region AWS
                    RService.Log("(CreateLicitacaoArquivo) " + nomeRobo + ": Enviando o arquivo para Amazon S3... " + fileName + " at {0}", Path.GetTempPath() + nomeRobo + ".txt");

                    if (AWS.SendObject(licitacao, pathEditais, fileName))
                    {
                        LicitacaoArquivo licitacaoArq = new LicitacaoArquivo();
                        licitacaoArq.NomeArquivo = fileName;
                        licitacaoArq.NomeArquivoOriginal = nomeRobo + DateTime.Now.ToString("yyyyMMddHHmmss");
                        licitacaoArq.NomeArquivoFonte = nameFile;
                        licitacaoArq.Status = 0;
                        licitacaoArq.IdLicitacao = licitacao.Id;

                        LicitacaoArquivoRepository repoArq = new LicitacaoArquivoRepository();
                        repoArq.Insert(licitacaoArq);

                        if (File.Exists(pathEditais + fileName))
                        {
                            File.Delete(pathEditais + fileName);
                        }

                        RService.Log("(CreateLicitacaoArquivo) " + nomeRobo + ": Arquivo " + fileName + " enviado com sucesso para Amazon S3" + " at {0}", Path.GetTempPath() + nomeRobo + ".txt");

                        return true;
                    }
                    else
                    {
                        RService.Log("Exception (CreateLicitacaoArquivo) " + nomeRobo + ": Erro ao enviar o arquivo para Amazon (CreateLicitacaoArquivo) {0}", Path.GetTempPath() + nomeRobo + ".txt");
                    }

                    #endregion
                }
            }
            catch (Exception e)
            {
                RService.Log("Exception (CreateLicitacaoArquivo) " + nomeRobo + ": " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " / " + e.InnerException + " at {0}", Path.GetTempPath() + nomeRobo + ".txt");
            }

            return false;
        }
    }
}
