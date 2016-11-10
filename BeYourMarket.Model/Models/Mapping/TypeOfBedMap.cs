using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;

namespace BeYourMarket.Model.Models.Mapping
{
    public class TypeOfBedMap : EntityTypeConfiguration<TypeOfBed>
    {
        public TypeOfBedMap()
        {
            //PrimaryKey
            this.HasKey(t => t.ID);

            //Properties
            this.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(500);

            // Table & Column Mappings
            this.ToTable("TypeOfBeds");
            this.Property(t => t.ID).HasColumnName("ID");
            this.Property(t => t.Name).HasColumnName("Name");
        }
    }
}
