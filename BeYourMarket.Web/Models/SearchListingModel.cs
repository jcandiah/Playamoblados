using BeYourMarket.Model.Models;
using BeYourMarket.Web.Models.Grids;
using PagedList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BeYourMarket.Web.Models
{
    public class SearchListingModel : SortViewModel
    {
        public int CategoryID { get; set; }

        public List<int> ListingTypeID { get; set; }

        public string SearchText { get; set; }

        public string string_fromdate { get; set; }
        public string string_todate { get; set; }

        private DateTime? _fromDate;

        public DateTime? FromDate
        {
            get
            {
                if(string.IsNullOrEmpty(string_fromdate))
                {
                    return new DateTime(2000, 1, 1);
                }
                return DateTime.Parse(string_fromdate);
            }

            set
            {
                var valor = DateTime.Parse(string_fromdate);
                _fromDate = DateTime.Parse(string_fromdate);
            }
        }
        private Nullable<DateTime> _toDate;

        public DateTime? ToDate
        {
            get
            {
                if (string.IsNullOrEmpty(string_todate))
                {
                    return new DateTime(2500, 1, 1);
                }
                return DateTime.Parse(string_todate);
            }

            set
            {
                _toDate = DateTime.Parse(string_todate);
            }
        }


        public Nullable<int> Passengers { get; set; }

        public string Location { get; set; }

        public bool PhotoOnly { get; set; }

        public double? PriceFrom { get; set; }

        public double? PriceTo { get; set; }

        public Nullable<int> Rooms { get; set; }

        public Nullable<int> Beds { get; set; }

        public Nullable<int> M2From { get; set; }

        public Nullable<int> M2To { get; set; }

        public Nullable<int> Bathrooms { get; set; }

        public Nullable<int> Cellar { get; set; }

        public Nullable<int> ParkingLot { get; set; }

        public Nullable<int> FloorNumber { get; set; }

        public List<MetaCategory> MetaCategories { get; set; }

        public List<ListingItemModel> Listings { get; set; }

        public List<ListingItemModel> Listings2 { get; set; }

        public IPagedList<ListingItemModel> ListingsPageList { get; set; }

        public IPagedList<ListingItemModel> ListingsPageList2 { get; set; }

        public List<Category> Categories { get; set; }

        public List<Category> BreadCrumb { get; set; }

        public List<ListingType> ListingTypes { get; set; }

        public ListingModelGrid Grid { get; set; }

        public Nullable<int> Property { get; set; }
    }
}