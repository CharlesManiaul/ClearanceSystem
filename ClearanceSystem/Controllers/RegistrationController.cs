using ClearanceSystem.Models;
using Dapper;
using FluentFTP;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System.Data;
using System.Security.Claims;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace ClearanceSystem.Controllers
{
    public class RegistrationController : Controller
    {
        SqlConnection db, adb;


        public RegistrationController(IConfiguration configuration)
        {
            db = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
            adb = new SqlConnection(configuration.GetConnectionString("AssetConnection"));

        }
        public IActionResult Index()
        {
            string sql;
            Registration reg = new Registration();

            sql = @"SELECT a.*, b.name, c.position,d.depName as DepartmentName FROM CLR_Registration_Master a
                    JOIN employee_master b
                    on a.EmpId = b.eid
                    JOIN position_master c
                    on b.pid = c.pid
                    JOIN department d
                    on  c.did = d.depID";

            reg.Register = db.Query<Registration>(sql);


            return View(reg);
        }

        //Details
        [Route("Registration/Details/{RId}/{EmpId}")]
        public IActionResult Details(int RId, int EmpId)
        {
            string sql;
            Registration reg = new Registration();

            sql = @"SELECT a.*, b.name, b.dateHired DateHired, c.position, d.depName DepartmentName FROM CLR_Registration_Master a
                    JOIN employee_master b
                    on a.EmpId = b.eid
                    JOIN position_master c
                    on b.pid = c.pid
                    JOIN department d
                    on  a.Department = d.depID
                    Where RId = @RId";

            reg = db.QueryFirstOrDefault<Registration>(sql, new { RId });

            sql = @"SELECT
                    a.CAid, 
                    a.Status, 
                    a.Department, 
                    c.DepName,
                    a.company,
                    a.personnel,
                    a.amount,
                    a.description,
                    b.EmpId
                    FROM EMS.dbo.vw_CashAdvance_Header a
                    JOIN CCARSYS.dbo.tbl_Users b
                    on a.createdBy = b.Username
                    Join CCARSYS.dbo.vw_CompDepartment c
                    on a.Department = c.DepId and a.company = c.Company
                    Where b.EmpId = @EmpId
                    ";

            reg.cashAdvance = db.Query<CashAdvance>(sql, new { EmpId });




            reg.assetList = adb.Query<Asset>(@"
                                        SELECT c.DepName as DepName, a.* 
                                        FROM AssetMaster a
                                        join vw_CompDepartment c
                                        on a.Company = c.Company and a.DepartmentId = c.DepId
                                        Where (IsCancel is null or IsCancel = 0) and EmpId = @EmpId
                                        ORDER BY a.AID DESC"
                                                , new { EmpId }).ToList();



            return View(reg);
        }





        //Registration

        public IActionResult Form()
        {
            string sql;
            Registration reg = new Registration();

            sql = @"SELECT a.eid as EmpId, a.name As EmployeeName, a.dateHired as DateHired, b.position, c.depName as Department, c.depID as DepartmentId  
                    FROM employee_master a
                    JOIN position_master b
                    on a.pid = b.pid
                    JOIN department c
                    on  b.did = c.depID
                    ";

            reg.Register = db.Query<Registration>(sql);



            sql = @"SELECT CONCAT('CBC', DepID) DepID, CONCAT('CBC - ', DepName) DepName, 'CBC' as Company FROM CCARSYS.dbo.tbl_Department
                    UNION ALL
                    SELECT  CONCAT('CBR', DepID) DepID,CONCAT('CBR - ', DepName) DepName, 'CBR' as Company FROM CBR_ISS.dbo.tbl_Department
                    ORDER BY DepName  ";

            reg.dept = adb.Query<Department>(sql);




            ViewBag.form = "active";
            return View(reg);
        }
        //Saving form
        [HttpPost]

        public IActionResult SaveForm(Registration reg)
        {
            DataTable attachfile = new DataTable();
            attachfile.Columns.Add("FileName", typeof(string));
            if (reg.Files is not null)
            {
                foreach (var item in reg.Files)
                {
                    string FileName = $"{Path.GetFileNameWithoutExtension(item.FileName)}{DateTime.Now.ToString("yyyyMMddhhmmss")}{Path.GetExtension(item.FileName)}";
                    var client = new FtpClient("172.16.0.12", "ftpadmin", "welcome");
                    client.Connect();
                    FtpStatus stat = client.UploadStream(item.OpenReadStream(), $"ClearSys/Attachments/{FileName}");
                    client.Disconnect();

                    attachfile.Rows.Add(FileName);
                }

            }
            reg.CrtdBy = HttpContext.User.FindFirstValue(ClaimTypes.Name);

            var response = db.ExecuteScalar("sp_clr_save_registration", new
            {
                reg.DepartmentId,
                reg.EmpId,
                reg.Remarks,
                reg.Description,
                reg.LastDay,
                CrtdBy = HttpContext.User.FindFirstValue(ClaimTypes.Name),
                attachfile
            });

            if (response == "Successful") // Assuming the query will affect only one row
            {
                TempData["Error Title"] = "Clearance Registration Failed";
                TempData["Error Message"] = "Clearance registration failed. Employee already has pending clearance";
                return RedirectToAction("Index");
            }

            TempData["Success Title"] = "Clearance Registration Done";
            TempData["Success Message"] = "Clearance Registration is successful. Remind other Departments";
            return RedirectToAction("Index", "Registration");



        }





        //For Report
        public IActionResult RegReport()
        {
            return View();
        }





        //turnover

        //[HttpPost]
        //public IActionResult Turnover(Registration reg) 
        //{


        //    return View(reg); 
        //}










    }
}
