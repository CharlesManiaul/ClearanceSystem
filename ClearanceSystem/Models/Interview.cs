namespace ClearanceSystem.Models
{
    public class Interview
    {
        public int IVId { get; set; }
        public int RId { get; set; }
        public string EmpId { get; set; }
        public DateTime? SchedDate { get; set; }
        public string CrtdBy {  get; set; }
        public string EmpName { get; set; }
        public string DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string Status { get; set; }

        public string Position { get; set; }
        public IEnumerable<Interview> interview { get; set; }
        public IEnumerable<Registration> reg {  get; set; }

        public IEnumerable<InterviewForm> intForm { get; set; }

        public InterviewForm Form { get; set; }

    }






    public class InterviewForm 
    {
        public int QId { get; set; }
        public string Question { get; set; }
        public int QNumber { get; set; }
        public string Type { get; set; }

        public string Answer { get; set; }


    
    }
}
