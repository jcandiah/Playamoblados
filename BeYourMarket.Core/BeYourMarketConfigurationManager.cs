﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeYourMarket.Core
{
    public class BeYourMarketConfigurationManager
    {
        public static string[] Cultures
        {
            get
            {
                // first culture is the DEFAULT
                return ConfigurationManager.AppSettings["Cultures"].Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
            }
        }

        public static string TwilioSid
        {
            get
            {
                return ConfigurationManager.AppSettings["TwilioSid"].ToString();
            }
        }

        public static string TwilioToken
        {
            get
            {
                return ConfigurationManager.AppSettings["TwilioToken"].ToString();
            }
        }

		public static string TwilioPhoneNumber
		{
			get
			{
				return ConfigurationManager.AppSettings["TwilioPhoneNumber"].ToString();
			}
		}

		public static string SpreadSheetClientId
		{
			get
			{
				return ConfigurationManager.AppSettings["SpreadSheetClientId"].ToString();
			}
		}
		
		public static string SpreadSheetClientSecret
		{
			get
			{
				return ConfigurationManager.AppSettings["SpreadSheetClientSecret"].ToString();
			}
		}

		public static string SpreadSheetId
		{
			get
			{
				return ConfigurationManager.AppSettings["SpreadSheetId"].ToString();
            }
		}

		public static bool AutomaticMigrationDataLossAllowed
        {
            get
            {
                return string.IsNullOrEmpty(ConfigurationManager.AppSettings["AutomaticMigrationDataLossAllowed"]) ? 
                    false : Convert.ToBoolean(ConfigurationManager.AppSettings["AutomaticMigrationDataLossAllowed"]);
            }
        }

        public static bool MigrateDatabaseToLatestVersion
        {
            get
            {
                return string.IsNullOrEmpty(ConfigurationManager.AppSettings["MigrateDatabaseToLatestVersion"]) ?
                    false : Convert.ToBoolean(ConfigurationManager.AppSettings["MigrateDatabaseToLatestVersion"]);
            }
        }
    }
}
