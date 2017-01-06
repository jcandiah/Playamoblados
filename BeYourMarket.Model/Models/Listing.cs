using System;
using System.Collections.Generic;

namespace BeYourMarket.Model.Models
{
    public partial class Listing : Repository.Pattern.Ef6.Entity
    {
        public Listing()
        {
            this.ListingMetas = new List<ListingMeta>();
            this.ListingPictures = new List<ListingPicture>();
            this.ListingReviews = new List<ListingReview>();
            this.ListingStats = new List<ListingStat>();
            this.Orders = new List<Order>();
            this.MessageThreads = new List<MessageThread>();
            this.DetailBeds = new List<DetailBed>();
            this.ListingObservations = new List<ListingObservation>();
            this.ListingPrices = new List<ListingPrice>();
        }

        public int ID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int CategoryID { get; set; }
        public int ListingTypeID { get; set; }
        public string UserID { get; set; }
        public Nullable<double> Price { get; set; }
        public string Currency { get; set; }
        public string ContactName { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public bool ShowPhone { get; set; }
        public bool Active { get; set; }
        public bool Enabled { get; set; }
        public bool ShowEmail { get; set; }
        public bool Premium { get; set; }
        public System.DateTime Expiration { get; set; }
        public string IP { get; set; }
        public string Location { get; set; }
        public Nullable<double> Latitude { get; set; }
        public Nullable<double> Longitude { get; set; }
        public System.DateTime Created { get; set; }
        public System.DateTime LastUpdated { get; set; }
        public Nullable<int> Rooms { get; set; }
        public Nullable<int> Beds { get; set; }
        public Nullable<int> M2 { get; set; }
        public Nullable<int> Bathrooms { get; set; }
        public Nullable<int> Cellar { get; set; }
        public Nullable<int> ParkingLot { get; set; }
        public Nullable<int> FloorNumber { get; set; }  
        public int Max_Capacity { get; set;}
        public bool Dishwasher { get; set; }
        public bool Washer { get; set; }
        public bool Grill { get; set; }
        public bool Terrace { get; set; }
        public string NroOfParking { get; set; }
        public bool SafetyMesh { get; set; }
        public bool Wifi { get; set; }
        public bool TV_cable { get; set; }
        public Nullable<int> Tv { get; set; }
        public bool FirstLine { get; set;}
        public Nullable<int> Suite { get; set; }
        public string TypeOfProperty { get; set; }
        //Release R 2.1
        public bool Smoker { get; set; }
        public bool Pets { get; set; }
        public bool Children { get; set; }
        public string ConditionCheckOut { get; set; }
        public string DescribeCondominium{ get; set; }
        public string ConditionHouse { get; set; }
        public bool Elevator { get; set; }
        public string Stay { get; set; }
        public string ShortDescription { get; set; }
        public Nullable<int> CleanlinessPrice { get; set; }
        public Nullable<int> Warranty { get; set; }
		public string Address { get; set; }

        //Referencias
        public virtual AspNetUser AspNetUser { get; set; }
        public virtual Category Category { get; set; }
        public virtual ICollection<ListingMeta> ListingMetas { get; set; }
        public virtual ICollection<ListingPicture> ListingPictures { get; set; }
        public virtual ICollection<ListingReview> ListingReviews { get; set; }
        public virtual ListingType ListingType { get; set; }
        public virtual ICollection<ListingStat> ListingStats { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<MessageThread> MessageThreads { get; set; }
        public virtual ICollection<DetailBed> DetailBeds { get; set; }
        public virtual ICollection<ListingObservation> ListingObservations { get; set; }
        public virtual ICollection<ListingPrice> ListingPrices { get; set; }
    }
}
