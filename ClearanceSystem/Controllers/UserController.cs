using ClearanceSystem.Models;
using Dapper;
using FluentFTP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;

namespace ClearanceSystem.Controllers
{
    public class UserController : Controller
    {
        SqlConnection db, adb;


        public UserController(IConfiguration configuration)
        {
            db = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
            adb = new SqlConnection(configuration.GetConnectionString("AssetConnection"));

        }
        public IActionResult Index()
        {
            string sql;
            Registration reg = new Registration();
            var username = HttpContext.User.FindFirstValue("username");
            
            var Department = HttpContext.User.FindFirstValue("DepartmentId");
            var EId = HttpContext.User.FindFirstValue("eid");





            reg.Register = db.Query<Registration>("sp_CLR_registration_user", new { EId, Department }, commandType: CommandType.StoredProcedure).ToList();

            sql = @"
                    SELECT a.*, b.EmpId, c.name As EmpName FROM CLR_Clearing_Report a
                    LEFT JOIN CLR_Registration_Master b
                    on a.RId = b.RId
                    LEFT JOIN employee_master c
                    on b.EmpId = c.eid
                    WHERE Role IN (SELECT Roles FROM user_role WHERE EId = @username and SystemName = 'Clearance') and a.Status = 'Cleared'";

            reg.clearing=db.Query<Clearing>(sql, new { username });

            return View(reg);
        }

        //Details
        [Route("User/Details/{RId}/{EmpId}")]
        public IActionResult Details(int RId, int EmpId)
        {
            string sql;
            Registration reg = new Registration();
            var user = HttpContext.User.FindFirstValue("username");
            var department = HttpContext.User.FindFirstValue("department");
            var level = HttpContext.User.FindFirstValue("level");

            sql = @"SELECT a.*, b.name, b.dateHired DateHired, c.position, d.depName DepartmentName FROM CLR_Registration_Master a
                    JOIN employee_master b
                    on a.EmpId = b.eid
                    JOIN position_master c
                    on b.pid = c.pid
                    JOIN department d
                    on  a.Department = d.depID
                    Where RId = @RId";

            reg = db.QueryFirst<Registration>(sql, new { RId });


            sql = @"
                    SELECT a.eid as EmpId, a.name as EmployeeName, b.position, c.depName as DepartmentName 
                    FROM employee_master a
                    JOIN position_master b
                    on a.pid = b.pid
                    JOIN department c
                    on  b.did = c.depId
	                Where c.depName in (SELECT depName FROM department where depName = @department)
                    ";

            reg.employees = db.Query<Employee>(sql, new { department });

            sql = @"SELECT * FROM user_role Where EId = @user and SystemName = 'Clearance'";

            reg.userRoles = db.Query<UserRoles>(sql, new { user }).ToList();



            sql = @"SELECT * FROM CLR_Clearing_Report WHERE RId = @RId ";

            reg.clearList = db.Query<Clear>(sql, new { RId, user });

            reg.attachment = db.Query<Attachments>(@"SELECT * FROM CLR_Attachment WHERE RefId IN (SELECT CID FROM CLR_Clearing_Report WHERE RId = @RId) AND RefType = 'CId'", new { RId });


            sql = @"SELECT c.DepName as DepName, a.* 
                    FROM AssetMaster a
                    join vw_CompDepartment c
                    on a.Company = c.Company and a.DepartmentId = c.DepId
                    Where (IsCancel is null or IsCancel = 0) and EmpId = @EmpId
                    ORDER BY a.AID DESC";

            reg.assetList = adb.Query<Asset>(sql, new { EmpId }).ToList();




            return View(reg);
        }




        //turnover

        [HttpPost]
        public IActionResult Turnover(Registration reg)
        {
            DataTable turnoverList = new DataTable();
            turnoverList.Columns.Add("RId", typeof(int));
            turnoverList.Columns.Add("AId", typeof(int));
            turnoverList.Columns.Add("name", typeof(string));

            foreach (var item in reg.assetList.Where(x => x.IsSelected == true))
                turnoverList.Rows.Add(reg.RId, item.AId, reg.Employee.Name); // Populate the row with the appropriate values from 'item'



            var response = db.ExecuteScalar("sp_clr_turnover", new
            {
                reg.RId,
                reg.Employee.Name,
                reg.Employee.Remarks,
                reg.Employee.EmpId,
                TransferBy = HttpContext.User.FindFirstValue(ClaimTypes.Name),
                turnoverList
            });

            if (response.ToString() == "Successful") // Assuming the query will affect only one row
            {
                TempData["Success Title"] = "Transfer Complete";
                TempData["Success Message"] = "Transfer of Asset Success.";
                return RedirectToAction("Index");

            }



            TempData["Error Title"] = "Transfer of Asset failed";
            TempData["Error Message"] = response;
            return RedirectToAction("Index");
        }

        //CLEARING
        [HttpPost]
        public IActionResult Clear(Registration reg)
        {

            var department = HttpContext.User.FindFirstValue("department");
            var user = HttpContext.User.FindFirstValue("username");


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

            var response = db.ExecuteScalar("sp_clr_clearDepartment", new
            {
                user,
                reg.UserRoles.Roles,
                
                reg.RId,
                reg.Clear.Remarks,
                ClearedBy = HttpContext.User.FindFirstValue(ClaimTypes.Name),
                attachfile

            });

            if (response.ToString() == "Successful") // Assuming the query will affect only one row
            {
                TempData["Success Title"] = "Cleared";
                TempData["Success Message"] = "Thank you for clearing";
                return RedirectToAction("Index");

            }

            TempData["Error Title"] = "Clearing failed";
            TempData["Error Message"] = response;
            return RedirectToAction("Index");


        }

        //CLEARING
        [HttpPost]
        public IActionResult ClearDepartment(Registration reg)
        {

            var department = HttpContext.User.FindFirstValue("department");
            var user = HttpContext.User.FindFirstValue("username");

            DataTable attachfile = new DataTable();
            attachfile.Columns.Add("FileName", typeof(string));
            attachfile.Columns.Add("MimeType", typeof(string)); // Add new column

            if (reg.Files is not null)
            {
                foreach (var item in reg.Files)
                {
                    string FileName = $"{Path.GetFileNameWithoutExtension(item.FileName)}{DateTime.Now:yyyyMMddhhmmss}{Path.GetExtension(item.FileName)}";

                    // Use ContentType directly from IFormFile
                    string mimeType = item.ContentType;

                    // Upload to FTP
                    var client = new FtpClient("172.16.0.12", "ftpadmin", "welcome");
                    client.Connect();
                    FtpStatus stat = client.UploadStream(item.OpenReadStream(), $"ClearSys/Attachments/{FileName}");
                    client.Disconnect();

                    attachfile.Rows.Add(FileName, mimeType);
                }
            }

            var response = db.ExecuteScalar("sp_clr_clearUserDepartment", new
            {
                user,
                reg.UserRoles.Roles,
                reg.RId,
                reg.DelEmailOthers,
                reg.Clear.Remarks,
                reg.Clear.withIssue,
                ClearedBy = HttpContext.User.FindFirstValue(ClaimTypes.Name),
                attachfile

            });

            if (response.ToString() == "Successful") // Assuming the query will affect only one row
            {
                TempData["Success Title"] = "Cleared";
                TempData["Success Message"] = "Thank you for clearing";
                return RedirectToAction("Details", "User", new { reg.RId, reg.EmpId });

            }

            TempData["Error Title"] = "Clearing failed";
            TempData["Error Message"] = response;
            return RedirectToAction("Details", "User", new { reg.RId, reg.EmpId });


        }



        public IActionResult GetDetails(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return File("~/Images/noimage.png", "image/png");
            }

            var model = db.QueryFirstOrDefault<Attachments>(
                "SELECT * FROM CLR_Attachment WHERE FileName = @fileName",
                new { fileName }
            );

            if (model == null)
            {
                return File("~/Images/noimage.png", "image/png");
            }

            var client = new FtpClient("172.16.0.12", "ftpadmin", "welcome");

            try
            {
                client.Connect();
                client.DownloadBytes(out byte[] bytes, $"ClearSys/Attachments/{model.FileName}");
                client.Disconnect();

                // Determine content type based on file extension
                string contentType = GetContentType(model.FileName);

                return File(bytes, contentType);
            }
            catch
            {
                return File("~/Images/noimage.png", "image/png");
            }
        }

        private string GetContentType(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                _ => "application/octet-stream",
            };
        }



        public IActionResult ClearReport()
        {
            string sql;
            Registration reg = new Registration();

            reg.clearList = db.Query<Clear>("sp_clr_clearing_report"); 



            return View(reg);


        }


        public IActionResult RegistrationReport()
        {
            string sql;
            Registration reg = new Registration();

            sql = @"
                    SELECT a.*, b.name, c.position, d.depName DepartmentName
                    FROM CLR_Registration_Master a
                    JOIN employee_master b
                    on a.EmpId = b.eid
                    JOIN position_master c
                    on b.pid = c.pid
                    LEFT JOIN department d 
                    ON d.depID = b.def_dept
                    WHERE (IsCancel is null or isCancel = 0)
                   ";

            reg.Register = db.Query<Registration>(sql);



            return View(reg);

        }

        public IActionResult ApprovedReport()
        {
            string sql;
            Registration reg = new Registration();

            sql = @"
                    SELECT a.*, b.name, c.position, d.depName DepartmentName
                    FROM CLR_Registration_Master a
                    JOIN employee_master b
                    on a.EmpId = b.eid
                    JOIN position_master c
                    on b.pid = c.pid
                    LEFT JOIN department d 
                    ON d.depID = b.def_dept
                    WHERE (IsCancel is null or isCancel = 0) and a.Status = 'Cleared'
                   ";

            reg.Register = db.Query<Registration>(sql);



            return View(reg);

        }
    }
}
