using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeYourMarket.Model.Models
{
    public partial class TypeOfBed : Repository.Pattern.Ef6.Entity
    {
        public TypeOfBed()
        {
            this.DetailBeds = new List<DetailBed>();
        }

        public int ID { get; set; }
        public string Name { get; set; } 

        //Referencias
        public virtual ICollection<DetailBed> DetailBeds { get; set; }
    }
}
