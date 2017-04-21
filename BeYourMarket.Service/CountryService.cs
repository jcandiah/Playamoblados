using BeYourMarket.Model.Models;
using Repository.Pattern.Repositories;
using Service.Pattern;

namespace BeYourMarket.Service
{
	public interface ICountryService : IService<Country>
	{
	}

	public class CountryService : Service<Country>, ICountryService
	{
		public CountryService(IRepositoryAsync<Country> repository)
			: base(repository)
		{
		}
	}
}
