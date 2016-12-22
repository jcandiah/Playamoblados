using System.Data.Entity.ModelConfiguration;

namespace BeYourMarket.Model.Models.Mapping
{
    public class ListingPriceMap : EntityTypeConfiguration<ListingPrice>
    {
        public ListingPriceMap()
        {
            // Primary Key
            this.HasKey(t => t.ID);

            // Properties
            // Table & Column Mappings
            this.ToTable("ListingPrice");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.ListingID).HasColumnName("ListingID");
            this.Property(t => t.FromDate).HasColumnName("FromDate");
            this.Property(t => t.ToDate).HasColumnName("ToDate");
            this.Property(t => t.Price).HasColumnName("Price");
            this.Property(t => t.Active).HasColumnName("Active");


            // Relationships
            this.HasRequired(t => t.Listing)
                .WithMany(t => t.ListingPrices)
                .HasForeignKey(d => d.ListingID).WillCascadeOnDelete();

        }
    }
}
