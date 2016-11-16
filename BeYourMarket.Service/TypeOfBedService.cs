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
   public interface ITypeOfBedService : IService<TypeOfBed>
    {
    }

    public class TypeOfbedService : Service<TypeOfBed>, ITypeOfBedService
    {
        public TypeOfbedService(IRepositoryAsync<TypeOfBed> repository)
            : base(repository)
        {
        }
    }
}
