using BeYourMarket.Model.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeYourMarket.Web.Models
{
    public class ListingUpdateModel
    {
        public ListingUpdateModel()
        {            
            Categories = new List<Category>();
            Users = new List<ApplicationUser>();
            Pictures = new List<PictureModel>();
            CustomFields = new CustomFieldListingModel();
            Roles = new List<AspNetRole>();
            TypesOfBeds = new List<TypeOfBed>();
            DetailBeds = new List<DetailBed>();
        }

        public int CategoryID { get; set; }

        public int ListingTypeID { get; set; }

        public string UserID { get; set; }

        public Listing ListingItem { get; set; }

        public List<Category> Categories { get; set; }

        public List<ListingType> ListingTypes { get; set; }

        public List<TypeOfBed> TypesOfBeds { get; set; }

        public List<DetailBed> DetailBeds { get; set; }

        public List<ApplicationUser> Users { get; set; }

        public List<AspNetRole> Roles { get; set; }

        public List<PictureModel> Pictures { get; set; }

        public CustomFieldListingModel CustomFields { get; set; }


    }
}
