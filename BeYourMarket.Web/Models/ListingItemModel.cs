using BeYourMarket.Model.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BeYourMarket.Web.Models
{
    public class ListingItemModel
    {
        public List<Listing> ListingsOther { get; set; }

        public Listing ListingCurrent { get; set; }

        public string UrlPicture { get; set; }

        public List<PictureModel> Pictures { get; set; }

        public List<DateTime> DatesBooked { get; set; }

        public ApplicationUser User { get; set; }

        public List<ListingReview> ListingReviews { get; set; }

        public List<Order> ListOrder { get; set; }

        public List<ApplicationUser> ListUsers { get; set; }

        public List<DetailBed> ListBeds { get; set; }
    }
}