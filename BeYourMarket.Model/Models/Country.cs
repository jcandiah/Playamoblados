using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeYourMarket.Model.Models
{
	public class Country : Repository.Pattern.Ef6.Entity
	{
		public Country()
		{
			this.AspNetUser = new List<AspNetUser>();
		}
		public int ID { get; set; }
		public string Name { get; set; }

		//Referencias
		public virtual ICollection<AspNetUser> AspNetUser { get; set; }
	}
}
