﻿using System.ComponentModel.DataAnnotations;

namespace doan1_v1.Models
{
    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }

        public List<PurchaseReport> PurchaseReports { get; set; } // 1 supplier co nhieu purchase report
    }
}
