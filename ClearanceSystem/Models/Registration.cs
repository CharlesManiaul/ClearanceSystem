using System.Net.Mail;

namespace ClearanceSystem.Models
{
    public class Registration
    {
        public int RId { get; set; }
        public int QCId { get; set; }
        public string EmpId { get; set; }
        public string EmployeeName { get; set; }
        public string Name { get; set; }
        public string position { get; set; }
        public string Roles { get; set; }
        public string Level { get; set; }
        public string Department { get; set; }
        public string DepartmentName { get; set; }
        public string Description { get; set; }
        public string Remarks { get; set; }
        public DateTime? DateHired { get; set; }
        public DateTime? LastDay { get; set; }
        public string Company { get; set; }
        public string DepartmentId { get; set; }
        public string CrtdBy { get; set; }
        public DateTime? CrtdDate { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }


        //
        public string NoteRemarks { get; set; }
        public DateTime? LastPay { get; set; }
        public bool? DelBiometrics { get; set; }
        public bool? DelEmailOthers { get; set; }

        //
        public IEnumerable<Clearing> clearing { get; set; }

        public IEnumerable<QCAttachments> QCAttachments { get; set; }

        //
        //
        public string DepartmentStatus { get; set; }
        public string AccountingStatus { get; set; }
        public string OtherStatus { get; set; }
        public string AdminStatus { get; set; }
        public Asset Asset { get; set; }
        public IEnumerable<Asset> Assets { get; set; }
        public IEnumerable<Registration> Register { get; set; }
        public Registration Reg { get; set; }
        public IEnumerable<Attachments> attachment { get; set; }
        public IFormFileCollection? Files { get; set; }
        public IEnumerable<Department> dept { get; set; }
        public IEnumerable<Turnover> turnover { get; set; }
        public List<TurnoverList> turnoverList { get; set; }
        public List<Asset> assetList { get; set; }
        public IEnumerable<Employee> employees { get; set; }
        public Employee Employee { get; set; }
        public Clear Clear { get; set; }
        public IEnumerable<Clear> clearList { get; set; }

        public CashAdvance CashAdvance { get; set; }
        public IEnumerable<CashAdvance> cashAdvance { get; set; }
        public List<UserRoles> userRoles { get; set; }
        public UserRoles UserRoles { get; set; }

        //

        public IEnumerable<ClearChat> messages { get; set; }

    }



    public class Employee
    {
        public string EmpId { get; set; }
        public string EmployeeName { get; set; }
        public string Name { get; set; }
        public string position { get; set; }
        public string Department { get; set; }
        public DateTime? DateHired { get; set; }
        public string DepartmentId { get; set; }
        public string Remarks { get; set; }

    }

    public class Clearing
    {
        public string RId { get; set; }
        public string EmpId { get; set; }
        public string ClearedBy { get; set; }
        public string ClearedDate { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public string EmpName { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }


    }

    public class UserRoles
    {
        public int Id { get; set; }
        public string EId { get; set; }
        public string Roles { get; set; }
        public string SystemName { get; set; }
    }

    public class Department
    {
        public string depName { get; set; }
        public string depId { get; set; }
        public string Company { get; set; }

    }

    public class Attachments
    {
        public int AId { get; set; }
        public int RefId { get; set; }
        public string RefType { get; set; }
        public string FileName { get; set; }
    }

    public class QCAttachments
    {
        public int AId { get; set; }
        public int RefId { get; set; }
        public string RefType { get; set; }
        public string FileName { get; set; }
    }


    public class ClearChat
    {
        public string message { get; set; }
        public string sent_by { get; set; }
        public DateTime sent_at { get; set; }
    }

}

