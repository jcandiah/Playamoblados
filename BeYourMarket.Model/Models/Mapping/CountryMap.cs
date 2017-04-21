using System.Data.Entity.ModelConfiguration;

namespace BeYourMarket.Model.Models.Mapping
{
	public class CountryMap : EntityTypeConfiguration<Country>
	{
		public CountryMap()
		{
			//PrimaryKey
			this.HasKey(t => t.ID);

			//Properties
			this.Property(t => t.Name)
				.IsRequired()
				.HasMaxLength(50);

			// Table & Column Mappings
			this.ToTable("Country");
			this.Property(t => t.ID).HasColumnName("ID");
			this.Property(t => t.Name).HasColumnName("Name");
		}
	}
}
