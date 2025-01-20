﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models;
using TaamerProject.Models.Common;

namespace TaamerProject.Service.Interfaces
{
    public interface IPrivilegesService  
    {
        Task<IEnumerable<UserPrivilegesVM>> GetUsersByPrivilegesIds(int? PrivId);
    }
}
