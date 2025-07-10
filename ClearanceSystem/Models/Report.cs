namespace ClearanceSystem.Models
{
    public class Reports
    {
        public int id { get; set; }
        public string tranid { get; set; }
        public string function { get; set; }
        public string action { get; set; }
        public string remarks { get; set; }
        public DateTime createdDate { get; set; }
        public string createdBy { get; set; }

        public string name { get; set; }



        public IEnumerable<Reports> report { get; set; }
    }
}
