using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BeYourMarket.Web.Models
{
    public class EmailModel
    {
        public EmailModel()
        {
            lista = new List<EmailModel>();
        }

        public string Id { get; set; }

        public string Email { get; set;}

        public string Nombre { get; set; }

        List<EmailModel> lista { get; set; }
    }
}