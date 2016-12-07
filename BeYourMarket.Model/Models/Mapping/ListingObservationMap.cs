using System.Data.Entity.ModelConfiguration;

namespace BeYourMarket.Model.Models.Mapping
{
    public class ListingObservationMap : EntityTypeConfiguration<ListingObservation>
    {
        public ListingObservationMap()
        {
            //Primary Key
            this.HasKey(t=>t.ID);

            // Properties
            // Table & Column Mappings
            this.ToTable("ListingObservations");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.Type).HasColumnName("Type");
            this.Property(t => t.Description).HasColumnName("Description");
            this.Property(t => t.Created).HasColumnName("Created");
            this.Property(t => t.ListingID).HasColumnName("ListingID");

            // Relationships
            this.HasRequired(t => t.Listing)
                .WithMany(t => t.ListingObservations)
                .HasForeignKey(d => d.ListingID).WillCascadeOnDelete();
        }
    }
}
