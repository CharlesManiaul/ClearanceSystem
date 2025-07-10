namespace ClearanceSystem.Models
{

    public class Clear
    {
        public int CId { get; set; }
        public int RId { get; set; }
        public string depName { get; set; }
        public string EmpId { get; set; }
        public string EmpName { get; set; }
        public string Remarks { get; set; }
        public string Status { get; set; }
        public string ClearedBy { get; set; }
        public DateTime? ClearedDate { get; set; }
        public string Type { get; set; }
        public string FileNames { get; set; }
        public string attachment { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool withIssue { get; set; }
 

    }


    public class FinClear
    {
        public int ADId { get; set; }
        public int RId { get; set; }
        public string Remarks { get; set; }
        public string Status { get; set; }
        public string ClearedBy { get; set; }
        public string ClearedDate { get; set; }
        public string attachment { get; set; }
        public string FileNames { get; set; }

    }


    public class OtherClear
    {
        public int OId { get; set; }
        public int RId { get; set; }
        public string Remarks { get; set; }
        public string Status { get; set; }
        public string ClearedBy { get; set; }
        public string ClearedDate { get; set; }
        public string attachment { get; set; }

        public string FileNames { get; set; }
    }


    public class AdminClear
    {
        public int HRId { get; set; }
        public int RId { get; set; }
        public string Remarks { get; set; }
        public string Status { get; set; }
        public string ClearedBy { get; set; }
        public string ClearedDate { get; set; }
        public string attachment { get; set; }
        public string FileNames { get; set; }
    }

}
