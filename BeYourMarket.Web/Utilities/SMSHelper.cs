using System;
using System.Threading.Tasks;
using BeYourMarket.Core;
using Twilio;
namespace BeYourMarket.Web.Utilities
{
	public static class SMSHelper
	{
		public static void SendSMS(string to, string body)
		{
			Task.Factory.StartNew(() =>
			{
				try
				{
					string AccountSid = BeYourMarketConfigurationManager.TwilioSid;
					string AuthToken = BeYourMarketConfigurationManager.TwilioToken;
					var twilio = new TwilioRestClient(AccountSid, AuthToken);
					var message = twilio.SendMessage(BeYourMarketConfigurationManager.TwilioPhoneNumber, to, body);
				}
				catch (Exception ex)
				{
					throw ex;
				}
			});
		}
	}
}