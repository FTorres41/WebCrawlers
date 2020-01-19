using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Web.Mail;

namespace RSBM.Util
{
    class EmailHandle
    {
        public static void SendMail(string fileAttachment, string nmeRobot)
        {
            try
            {
                string h1 = "<h1 style=\"font-family:Arial'; text-align:'center'; color:#c40318;\">RSBM</h1>";
                string h2 = "<h2 style=\"font-family:Arial'; text-align:'center'; color:#58595b;\">Log robô {0}</h2>";
                string hr = "<hr style=\"height: 5px; color:#c40318;\">";
                string p = "<p style=\"font-family:Arial';\">Log do dia {1} às {2}</p>";
                string corpoMensagem = string.Format(h1 + h2 + hr + p + hr, nmeRobot, DateTime.Now.ToShortDateString(), DateTime.Now.ToShortTimeString());

                #region Mail
                System.Net.Mail.MailMessage mail = new System.Net.Mail.MailMessage();
                mail.From = new MailAddress("robos.licitacoesmais@gmail.com");
                mail.To.Add(new MailAddress("programador1@licitacoes.com.br"));
                mail.To.Add(new MailAddress("programador2@licitacoes.com.br"));
                mail.To.Add(new MailAddress("fabiotorresdequadros@gmail.com"));
                mail.Subject = string.Format("[RSBM] Log robô: {0}", nmeRobot);

                mail.Body = corpoMensagem;
                mail.BodyEncoding = System.Text.Encoding.UTF8;
                mail.IsBodyHtml = true;

                Attachment anexo = new Attachment(fileAttachment, MediaTypeNames.Application.Octet);
                mail.Attachments.Add(anexo);

                /********servidor SMTP temporário*********/
                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587)
                {
                    Credentials = new NetworkCredential("robos.licitacoesmais@gmail.com", "gnp1234@"),
                    EnableSsl = true
                };
                /*****************************************/
                smtp.Send(mail);
                #endregion

                #region Web.Mail 
                /*Esta biblioteca está depreciada, mas funciona com ImplicitSSL, que o servidor da NP utiliza*/
                //MailMessage mail = new MailMessage();
                //mail.From = "alertas.robos@licitacoesmais.com.br";
                //mail.To = "programador1@licitacoes.com.br;programador2@licitacoes.com.br;programador3@licitacoes.com.br";
                //mail.Cc = "fabiotorresdequadros@gmail.com";
                //mail.Subject = string.Format("[RSBM] Log robô: {0}", nmeRobot);

                //mail.Body = corpoMensagem;
                //mail.BodyEncoding = System.Text.Encoding.UTF8;
                //mail.BodyFormat = MailFormat.Html;
                //mail.Fields.Add("http://schemas.microsoft.com/cdo/configuration/smtpusessl", true);
                //mail.Fields.Add("http://schemas.microsoft.com/cdo/configuration/smtpauthenticate", 1);
                //mail.Fields.Add("http://schemas.microsoft.com/cdo/configuration/smtpserver", "smtp.licitacoes.com.br");
                //mail.Fields.Add("http://schemas.microsoft.com/cdo/configuration/smtpserverport", 587);
                //mail.Fields.Add("http://schemas.microsoft.com/cdo/configuration/sendusername", "alertas.robos@licitacoesmais.com.br");
                //mail.Fields.Add("http://schemas.microsoft.com/cdo/configuration/sendpassword", "Inova@2018");

                //MailAttachment anexo = new MailAttachment(fileAttachment);
                //mail.Attachments.Add(anexo);

                //SmtpMail.SmtpServer = "smtp.licitacoes.com.br";
                //SmtpMail.Send(mail);
                #endregion
            }
            catch (Exception e)
            {
                RService.Log("RService Exception EmailHandle: (SendMail)" + e.Message + " / " + e.StackTrace + " / " + e.InnerException + " at {0}", Path.GetTempPath() + "RSERVICE" + ".txt");
            }
        }
    }
}
