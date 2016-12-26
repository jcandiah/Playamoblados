using BeYourMarket.Model.Models;
using Repository.Pattern.Repositories;
using Service.Pattern;

namespace BeYourMarket.Service
{
    public interface IListingPriceService : IService<ListingPrice>
    {
    }

    public class ListingPriceService : Service<ListingPrice>, IListingPriceService
    {
        public ListingPriceService(IRepositoryAsync<ListingPrice> repository)
            : base(repository)
        {
        }
    }
}
