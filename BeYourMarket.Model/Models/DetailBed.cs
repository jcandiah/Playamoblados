using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeYourMarket.Model.Models
{
    public partial class DetailBed : Repository.Pattern.Ef6.Entity
    {
        public int ID { get; set; }
        public int Quantity { get; set; }
        public int ListingID { get; set; }
        public int TypeOfBedID { get; set; }

        //Referencias
        public virtual TypeOfBed TypeOfBed { get; set; }
        public virtual Listing Listing { get; set; }
    }
}
