﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models;

namespace TaamerProject.Service.Interfaces
{
    public interface IOfferserviceService
    {
        Task<IEnumerable<OfferServiceVM>> GetOfferservicenByid(int offerid);

    }
}
