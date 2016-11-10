using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace BeYourMarket.Model.Models.Mapping
{
    public class DetailBedMap : EntityTypeConfiguration<DetailBed>
    {
        public DetailBedMap()
        {
            //PrimaryKey
            this.HasKey(t => t.ID);

            //Properties

            // Table & Column Mappings
            this.ToTable("DetailBeds");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.Quantity).HasColumnName("Quantity");
            this.Property(t => t.ListingID).HasColumnName("ListingID");
            this.Property(t => t.TypeOfBedID).HasColumnName("TypeOfBedID");

            // Relationships
            this.HasRequired(t => t.Listing)
                .WithMany(t => t.DetailBeds)
                .HasForeignKey(d => d.ListingID).WillCascadeOnDelete();
            this.HasRequired(t => t.TypeOfBed)
                .WithMany(t => t.DetailBeds)
                .HasForeignKey(d => d.TypeOfBedID).WillCascadeOnDelete();
        }
    }
}
