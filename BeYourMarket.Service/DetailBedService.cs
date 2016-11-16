using BeYourMarket.Model.Models;
using Repository.Pattern.Repositories;
using Service.Pattern;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeYourMarket.Service
{
    public interface IDetailBedService : IService<DetailBed>
    {
    }

    public class DetailBedService : Service<DetailBed>, IDetailBedService
    {
        public DetailBedService(IRepositoryAsync<DetailBed> repository)
            : base(repository)
        {
        }
    }
}
