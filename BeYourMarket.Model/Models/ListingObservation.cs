using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeYourMarket.Model.Models
{
    public partial class ListingObservation : Repository.Pattern.Ef6.Entity
    {
        public int ID { get; set; }
        public string Type { get; set; }
        public string Description { get; set; }
        public System.DateTime Created { get; set; }
        public int ListingID { get; set; }
        public virtual Listing Listing { get; set; }
    }
}
