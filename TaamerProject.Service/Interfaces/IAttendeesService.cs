﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models;

namespace TaamerProject.Service.Interfaces
{
    public interface IAttendeesService
    {
        Task<IEnumerable<AttendeesVM>> GetAllAttendees(int BranchId);
        Task<IEnumerable<AttendeesVM>> GetAttendeesbyStatus(int Status, string Date, int BranchId);
        Task<IEnumerable<AttendeesVM>> GetAttendeeslate(bool IsLate, string Date, int BranchId);
        Task<IEnumerable<AttendeesVM>> GetAttendeesEarlyCheckOut(bool IsEarlyCheckOut, string Date, int BranchId);
        Task<IEnumerable<AttendeesVM>> GetAttendeesOut(bool IsOut, string Date, int BranchId);
        Task<IEnumerable<AttendeesVM>> GetAllEmpAbsence(int Status, string Date, int BranchId);
        Task<IEnumerable<AttendeesVM>> GetAllEmplate(bool IsLateCheckIn, string Date, int BranchId);
        Task<IEnumerable<AttendeesVM>> GetAllEmpAbsencelate(string Date, int BranchId);
        void GetEmpAttendenceDevice(List<Thing> things);
    }
}
