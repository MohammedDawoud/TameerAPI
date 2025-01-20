using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaamerProject.Models
{
    public class ReturnInvoiceVM
    {
        public int AccountId { get; set; }
        public decimal? Total { get; set; }
        public decimal? Credit { get; set; }
        public decimal? Depit { get; set; }
    }
}
