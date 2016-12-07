using BeYourMarket.Model.Models;
using Repository.Pattern.Repositories;
using Service.Pattern;

namespace BeYourMarket.Service
{
    public interface IListingObservationService : IService<ListingObservation>
    {
    }

    public class ListingObservationService : Service<ListingObservation>, IListingObservationService
    {
        public ListingObservationService(IRepositoryAsync<ListingObservation> repository)
            : base(repository)
        {
        }
    }
}
