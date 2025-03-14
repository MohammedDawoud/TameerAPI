﻿using System;
using System.ComponentModel.DataAnnotations;
namespace TaamerProject.Models
{
    public class Attendees : Auditable
    {
        public long AttendeesId { get; set; }
        public int? EmpId { get; set; }
        public string? Date { get; set; }
        public string? DayOfWeek { get; set; }
        public int? Day { get; set; }
        public int Status { get; set; }
        public int WorkMinutes { get; set; }
        public int ActualWorkMinutes { get; set; }
        public bool IsLate { get; set; }
        public int LateMinutes { get; set; }
        public bool IsOverTime { get; set; }
        public int OverTimeMinutes { get; set; }
        public decimal Discount { get; set; }
        public decimal Bonus { get; set; }
        public bool IsVacancy { get; set; }
        public bool IsLateCheckIn { get; set; }
        public int LateCheckInMinutes { get; set; }

        public bool IsEarlyCheckOut { get; set; }
        public int EarlyCheckOutMin { get; set; }
        public bool IsEntry { get; set; }
        public bool IsOut { get; set; }
        public bool IsRealVacancy { get; set; }
        public bool IsVacancyEmp { get; set; }
        public bool IsDone { get; set; }
        public int? AttTimeId { get; set; }
        public int BranchId { get; set; }
        public Employees? Employees { get; set; }
    }
}
