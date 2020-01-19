using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using RSBM.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RSBM.Util
{
    public class AWS
    {
        public static string bucketName = "files.licitacoesmais.com.br";
        public static StoredProfileAWSCredentials credentials = new StoredProfileAWSCredentials("licitacoesmais");
        //public static AmazonSimpleEmailServiceClient sesClient;
        public static RegionEndpoint region = RegionEndpoint.SAEast1;
        public static IAmazonS3 s3Client = new AmazonS3Client(credentials, region);

        #region S3
        //Métodos que controlam o envio de arquivos para o Amazon Simple Storage Service (S3)
        internal static bool SendObject(Licitacao l, string pathEditais, string fileName)
        {
            bool segundaTentativa = false;

            try
            {
                do
                {
                    //cria o bucket no cliente caso o bucket não exista
                    if (!AmazonS3Util.DoesS3BucketExist(s3Client, bucketName))
                        CreateABucket(s3Client);

                    //Reúne os dados necessários do arquivo
                    PutObjectRequest putRequest = new PutObjectRequest
                    {
                        BucketName = bucketName + "/licitacoes",
                        Key = fileName,
                        FilePath = pathEditais + fileName,
                        Grants = new List<S3Grant>
                        {
                            new S3Grant {Grantee = new S3Grantee {URI = "http://acs.amazonaws.com/groups/global/AllUsers" }, Permission = S3Permission.READ }
                        }/*,
                        TagSet = new List<Tag>
                        {
                            new Tag {Key = "IdLicitacao", Value = l.Id.ToString() },
                            new Tag {Key = "Lote", Value = l.Lote.Id.ToString() },
                            new Tag {Key = "Fonte", Value = l.LinkSite }
                        }*/
                    };

                    //Envia o arquivo para o bucket
                    PutObjectResponse putResponse = s3Client.PutObject(putRequest);
                    return true;
                } while (segundaTentativa);

            }
            catch (Exception e)
            {
                RService.Log("RService Exception AWS (SendObject): " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " / " + e.InnerException + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");

                //Faz uma segunda tentativa de envio
                segundaTentativa = !segundaTentativa;
                if (segundaTentativa)
                {
                    RService.Log("AWS (SendObject): Segunda tentativa de envio... at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
                    Thread.Sleep(5000);
                }
                else
                    return false;
            }
            return false;
        }

        internal static void CreateABucket(IAmazonS3 client)
        {
            try
            {
                PutBucketRequest putRequest = new PutBucketRequest
                {
                    //Envia o pedido para criar o bucket (caso ele não exista), concedendo controle total para o dono
                    BucketName = bucketName,
                    UseClientRegion = true,
                    Grants = new List<S3Grant>
                    {
                        new S3Grant { Grantee = new S3Grantee { DisplayName = "contato" }, Permission = S3Permission.FULL_CONTROL }
                    }
                };

                //Cria o bucket na região especificada pelo cliente
                PutBucketResponse putResponse = client.PutBucket(putRequest);
            }
            catch (Exception e)
            {
                RService.Log("RService Exception AWS (CreateABucket): " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " / " + e.InnerException + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
            }
        }
        #endregion

        #region SES (ñ utilizado)
        /*Métodos que controlam o envio de e-mails pelo Amazon Simple Email Service (SES)
        => SendMail = envia e-mails sem anexo pois a SDK não permite
        => SendRawMail = envia e-mails com anexo aliando a biblioteca System.Net.Mail e a SDK da Amazon*/
        //public static void SendMailSES(string fileAttachment, string nmeRobot)
        //{
        //    try
        //    {
        //        sesClient = new AmazonSimpleEmailServiceClient(credentials, region);

        //        string h1 = "<h1 style=\"font-family:Arial'; text-align:'center'; color:#c40318;\">RSBM</h1>";
        //        string h2 = "<h2 style=\"font-family:Arial'; text-align:'center'; color:#58595b;\">Log robô " + nmeRobot + " {0}</h2>";
        //        string hr = "<hr style =\"height: 5px; color:#c40318;\">";
        //        string p = "<p style=\"font-family:Arial';\">Log do dia {1} às {2}</p>";
        //        string corpoMensagem = string.Format(h1 + h2 + hr + p + hr, nmeRobot, DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), DateTime.Now);

        //        const string FROM = "logrobo@agencia.red";
        //        //string TO = "logrobo@agencia.red";
        //        string TO = "fabio@agencia.red";
        //        string SUBJECT = string.Format("[RSBM] Log robô: {0}", nmeRobot);
        //        string BODY = corpoMensagem;

        //        Destination destination = new Destination();
        //        destination.ToAddresses = (new List<string> { TO });

        //        Content subject = new Content(SUBJECT);
        //        Content textBody = new Content(BODY);
        //        Body body = new Body(textBody);

        //        Message message = new Message(subject, body);

        //        try
        //        {
        //            SendEmailRequest request = new SendEmailRequest(FROM, destination, message);
        //            sesClient.SendEmail(request);
        //        }
        //        catch (Exception e)
        //        {
        //            RService.Log("RService Exception AWS (SendMailSES/SendRequest): " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        RService.Log("RService Exception AWS (SendMailSES): " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
        //    }
        //}

        //public static void SendRawMailSES(string fileAttachment, string nmeRobot)
        //{
        //    MailMessage message = (MailMessage)composeMail(fileAttachment, nmeRobot);
        //    MemoryStream stream = ConvertMailToStream(message);

        //    RawMessage rawMessage = new RawMessage();
        //    rawMessage.Data = stream;
        //    try
        //    {
        //        sesClient = new AmazonSimpleEmailServiceClient(credentials, region);
        //        SendRawEmailRequest request = new SendRawEmailRequest(rawMessage);
        //        var response = sesClient.SendRawEmail(request);
        //    }
        //    catch (AmazonSimpleEmailServiceException e)
        //    {
        //        RService.Log("RService Exception AWS (SendRawMailSES): " + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
        //    }
        //}

        //internal static MemoryStream ConvertMailToStream(MailMessage message)
        //{
        //    Assembly assembly = typeof(SmtpClient).Assembly;

        //    Type mailWriterType = assembly.GetType("System.Net.Mail.MailWriter");

        //    MemoryStream stream = new MemoryStream();

        //    ConstructorInfo mailWriterConstructor = mailWriterType.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(MemoryStream) }, null);

        //    object mailWriter = mailWriterConstructor.Invoke(new object[] { stream });

        //    MethodInfo sendMethod = typeof(MailMessage).GetMethod("Send", BindingFlags.Instance | BindingFlags.NonPublic);
        //    sendMethod.Invoke(message, BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { mailWriter, true, true }, null);

        //    MethodInfo closeMethod = mailWriter.GetType().GetMethod("Close", BindingFlags.Instance | BindingFlags.NonPublic);
        //    closeMethod.Invoke(mailWriter, BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { }, null);

        //    return stream;
        //}

        //internal static MailMessage composeMail(string fileAttachment, string nmeRobot)
        //{
        //    //Email sem formatação HTML
        //    string plainMail = string.Format("RSBM\n\nLog robô " + nmeRobot + " {0}\n\nLog do dia {1} às {2}", DateTime.Now.ToShortDateString(), DateTime.Now.ToShortDateString(), DateTime.Now);

        //    //Email com formatação HTML
        //    string h1 = "<h1 style=\"font-family:Arial'; text-align:'center'; color:#c40318;\">RSBM</h1>";
        //    string h2 = "<h2 style=\"font-family:Arial'; text-align:'center'; color:#58595b;\">Log robô " + nmeRobot + " {0}</h2>";
        //    string hr = "<hr style =\"height: 5px; color:#c40318;\">";
        //    string p = "<p style=\"font-family:Arial';\">Log do dia {1} às {2}</p>";
        //    string htmlMail = string.Format(h1 + h2 + hr + p + hr, nmeRobot, DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString(), DateTime.Now);

        //    AlternateView plainView = AlternateView.CreateAlternateViewFromString(plainMail, Encoding.UTF8, "text/plain");
        //    AlternateView htmlView = AlternateView.CreateAlternateViewFromString(htmlMail, Encoding.UTF8, "text/html");

        //    MailMessage message = new MailMessage();
        //    message.From = new MailAddress("logrobo@agencia.red");
        //    message.To.Add(new MailAddress("logrobo@agencia.red"));
        //    //message.To.Add(new MailAddress("fabio@agencia.red"));
        //    message.Subject = string.Format("[RSBM] Log robô: {0}", nmeRobot);
        //    message.SubjectEncoding = Encoding.UTF8;
        //    message.AlternateViews.Add(plainView);
        //    message.AlternateViews.Add(htmlView);

        //    Attachment attach = new Attachment(fileAttachment, MediaTypeNames.Application.Octet);
        //    message.Attachments.Add(attach);

        //    return message;
        //}


        #endregion
    }
}
