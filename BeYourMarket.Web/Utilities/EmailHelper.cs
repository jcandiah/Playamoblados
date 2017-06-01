using BeYourMarket.Model.Models;
using BeYourMarket.Web.App_Start;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using BeYourMarket.Service;
using BeYourMarket.Model.Enum;
using Postal;
using System.Net.Mail;
using System.Net;
using System.IO;

namespace BeYourMarket.Web.Utilities
{
	public static class EmailHelper
	{
		public static IEmailService EmailService = Postal.Email.CreateEmailService();

		public static void SendEmail(Email email, bool preMailer = true)
		{
			var httpContext = Elmah.ErrorSignal.FromCurrentContext();

			//Task.Factory.StartNew(() =>
			//{
				try
				{
					var message = EmailService.CreateMailMessage(email);
					
					SmtpClient server = new SmtpClient(CacheHelper.Settings.SmtpHost, CacheHelper.Settings.SmtpPort.Value);
					server.Credentials = new System.Net.NetworkCredential(CacheHelper.Settings.SmtpUserName, CacheHelper.Settings.SmtpPassword);
					server.EnableSsl = CacheHelper.Settings.SmtpSSL;
					MailMessage mnsj = new MailMessage();
					//mnsj.Body = "<html><title>hhhh</title><body><p>Hoa mundo prueba 3</p></body></html>";					
					mnsj.Body = message.Body;
					mnsj.IsBodyHtml = true;
					mnsj.Subject = message.Subject;
					mnsj.To.Add(new MailAddress(message.To.ToString()));
					mnsj.From = new MailAddress(message.From.ToString(), "Playamoblados");

				if (preMailer)
					mnsj.Body = PreMailer.Net.PreMailer.MoveCssInline(message.Body).Html;
					/* Enviar */
					server.Send(mnsj);
				}
				catch (Exception ex)
				{
					//http://stackoverflow.com/questions/7441062/how-to-use-elmah-to-manually-log-errors
					httpContext.Raise(ex);
				}
			//});
		}
	}
}
