using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeYourMarket.Model.Models
{
    public partial class ListingPrice : Repository.Pattern.Ef6.Entity
    {
        public int ID { get; set; }
        public int ListingID { get; set; }
        public System.DateTime FromDate { get; set; }        
        public System.DateTime ToDate { get; set; }
        public int Price { get; set; }
        public bool Active { get; set; }
        public virtual Listing Listing { get; set; }
    }
}
