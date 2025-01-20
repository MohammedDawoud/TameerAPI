﻿using System;
using System.ComponentModel.DataAnnotations;
namespace TaamerProject.Models
{
    public class CustodyVM
    {
        public int CustodyId { get; set; }
        public int? EmployeeId { get; set; }
        public int? ItemId { get; set; }
        public string? Date { get; set; }
        public string? HijriDate { get; set; }
        public int? Quantity { get; set; }
        public int? Type { get; set; }
        public string? ItemName { get; set; }
        public decimal? ItemPrice { get; set; }
        public string? EmployeeName { get; set; }
        public int? BranchId { get; set; }
        public bool? Status { get; set; }
        public decimal? CustodyValue { get; set; }
        public bool? ConvertStatus { get; set; }
        public int? InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public bool? IsPost { get; set; }

    }
}
