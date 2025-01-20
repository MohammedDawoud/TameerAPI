﻿using System;

namespace TaamerProject.Models
{
    public class CarMovementsVM : Auditable
    {
        public int MovementId { get; set; }
        public int? ItemId { get; set; }
        public int? Type { get; set; }
        public int? EmpId { get; set; }
        public int? BranchId { get; set; }
        public string? Date { get; set; }
        public string? EndDate { get; set; }
        public string? HijriDate { get; set; }
        public decimal? EmpAmount { get; set; }
        public decimal? OwnerAmount { get; set; }
        public string? Notes { get; set; }
        public string? EmployeeName { get; set; }
        public string? TypeName { get; set; }
        public string? ItemName { get; set; }
        public bool? IsSearch { get; set; }
        public int? AccountId { get; set; }


    }
}
