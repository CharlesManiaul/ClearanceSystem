using ClearanceSystem.Models;
using Dapper;
using FastReport;
using FluentFTP;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Ocsp;
using System.Data;
using System.Security.Claims;
using FastReport.Export.PdfSimple;
using FastReport.Utils;
using Microsoft.IdentityModel.Tokens;


namespace ClearanceSystem.Controllers
{
    public class AdminController : Controller
    {
        SqlConnection db, adb;
        private string _webHostEnvironment;

        public AdminController(IConfiguration configuration, IWebHostEnvironment env)
        {
            db = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
            adb = new SqlConnection(configuration.GetConnectionString("AssetConnection"));
            _webHostEnvironment = env.WebRootPath;

        }
        public IActionResult Index()
        {
            string sql;
            Registration reg = new Registration();
            var username = HttpContext.User.FindFirstValue("username");

            sql = @"
                    SELECT a.*, b.name, c.position, d.depName DepartmentName
                    FROM CLR_Registration_Master a
                    JOIN employee_master b
                    on a.EmpId = b.eid
                    JOIN position_master c
                    on b.pid = c.pid
                    LEFT JOIN department d 
                    ON d.depID = b.def_dept
                    WHERE (IsCancel is null or isCancel = 0) AND a.Status != 'Cleared'
                   ";

            reg.Register = db.Query<Registration>(sql);

            sql = @"
                    SELECT a.*, b.EmpId, c.name As EmpName FROM CLR_Clearing_Report a
                    LEFT JOIN CLR_Registration_Master b
                    on a.RId = b.RId
                    LEFT JOIN employee_master c
                    on b.EmpId = c.eid
                    WHERE Role IN (SELECT Roles FROM user_role WHERE EId = @username and SystemName = 'Clearance') and a.Status = 'Cleared'
                    ";

            reg.clearing = db.Query<Clearing>(sql, new { username });

            return View(reg);
        }

        //Details
        [Route("Admin/Details/{RId}/{EmpId}")]
        public IActionResult Details(int RId, int EmpId)
        {
            string sql;
            Registration reg = new Registration();
            var user = HttpContext.User.FindFirstValue("username");
            var department = HttpContext.User.FindFirstValue("department");
            var level = HttpContext.User.FindFirstValue("level");

            sql = @"SELECT a.*, b.name, b.dateHired DateHired, c.position, d.depName DepartmentName, f.code as Company FROM CLR_Registration_Master a
                    JOIN employee_master b
                    on a.EmpId = b.eid
                    JOIN position_master c
                    on b.pid = c.pid
                    JOIN department d
                    on  a.Department = d.depID
                    Join compDept e
                    on d.depID = e.depID
                    JOIN company f
                    ON e.cid = f.cid
                    Where a.RId = @RId and f.isActive = 1";

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



            sql = @"SELECT * FROM CLR_Clearing_Report WHERE RId = @RId";

            reg.clearList = db.Query<Clear>(sql, new { RId });

            reg.attachment = db.Query<Attachments>(@"SELECT * FROM CLR_Attachment WHERE (RefId = @RId AND RefType = 'RId') or (RefId in (SELECT CID FROM CLR_Clearing_Report WHERE RId = @RId)  AND RefType = 'CId') ", new { RId });



            sql = @"SELECT c.DepName as DepName, a.* 
                    FROM AssetMaster a
                    join vw_CompDepartment c
                    on a.Company = c.Company and a.DepartmentId = c.DepId
                    Where (IsCancel is null or IsCancel = 0) and EmpId = @EmpId
                    ORDER BY a.AID DESC";

            reg.assetList = adb.Query<Asset>(sql, new { EmpId }).ToList();

            return View(reg);
        }





        //Registration
        public IActionResult Form()
        {
            string sql;
            Registration reg = new Registration();

            sql = @"
                    SELECT a.eid as EmpId, a.name As EmployeeName, a.dateHired as DateHired, b.position, c.depName as Department, c.depID as DepartmentId, comp.code as Company
                    FROM employee_master a
                    JOIN position_master b
                    on a.pid = b.pid
                    JOIN department c
                    on  a.def_dept = c.depID
                    LEFT JOIN compDept d 
                    ON d.depID = a.def_dept
                    JOIN company comp ON d.cid = comp.cid


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
                reg.Company,
                CrtdBy = HttpContext.User.FindFirstValue(ClaimTypes.Name),
                attachfile
            });

            if (response.ToString() == "Successful") // Assuming the query will affect only one row
            {
                TempData["Success Title"] = "Clearance Registration Done";
                TempData["Success Message"] = "Clearance Registration is successful. Remind other Departments";
                return RedirectToAction("Index", "Admin");

            }

            TempData["Error Title"] = "Clearance Registration Failed";
            TempData["Error Message"] = response.ToString();
            return RedirectToAction("Index", "Admin");

        }


        //clearing

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

            var response = db.ExecuteScalar("sp_clr_clearAdminDepartment", new
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
                return RedirectToAction("Details", "Admin", new { reg.RId, reg.EmpId });

            }

            TempData["Error Title"] = "Clearing failed";
            TempData["Error Message"] = response.ToString();
            return RedirectToAction("Details", "Admin", new { reg.RId, reg.EmpId });


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


        [HttpPost]
        public IActionResult DeleteClearance(int RId)
        {

            var response = db.ExecuteScalar("sp_clr_delete_clearance", new
            {
                RId,
                CancelBy = HttpContext.User.FindFirstValue(ClaimTypes.Name),
            });

            if (response.ToString() == "Successful") // Assuming the query will affect only one row
            {
                TempData["Success Title"] = "Deleted Successfully";
                TempData["Success Message"] = "Deletion complete.";
                return RedirectToAction("Index");

            }



            TempData["Error Title"] = "Deleted Failed";
            TempData["Error Message"] = response;
            return RedirectToAction("Index");



        }



        [HttpPost]
        public IActionResult FinishClearance(Registration reg)
        {

            var response = db.ExecuteScalar("sp_clr_finish_clearance", new
            {
                reg.RId,
                reg.NoteRemarks,
                reg.LastPay,
                reg.DelBiometrics,
                FinishedBy = HttpContext.User.FindFirstValue(ClaimTypes.Name),
            });

            if (response.ToString() == "Successful") // Assuming the query will affect only one row
            {
                TempData["Success Title"] = "Finish Clearance";
                TempData["Success Message"] = "Clearance now needs to be approved.";
                return RedirectToAction("Index");
            }



            TempData["Error Title"] = "Failed to finish clearance";
            TempData["Error Message"] = response;
            return RedirectToAction("Index");



        }


        [HttpPost]
        public IActionResult ApproveClearance(Registration reg)
        {

            var response = db.ExecuteScalar("sp_clr_approve_clearance", new
            {
                reg.RId,
                ApprovedBy = HttpContext.User.FindFirstValue(ClaimTypes.Name),
            });

            if (response.ToString() == "Successful") // Assuming the query will affect only one row
            {
                TempData["Success Title"] = "Finish Clearance";
                TempData["Success Message"] = "Clearance Approved";
                return RedirectToAction("Index");
            }

            TempData["Error Title"] = "Failed to approve clearance";
            TempData["Error Message"] = response;
            return RedirectToAction("Index");

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


        //print
        public IActionResult Print(Registration reg)
        {
            string reportPath = string.Empty;

            if (reg.Company == "CBC")
            {
                reportPath = Path.Combine(_webHostEnvironment, "Reports", "CBCreport.frx");
            }
            else if (reg.Company == "CBR")
            {
                reportPath = Path.Combine(_webHostEnvironment, "Reports", "CBRreport.frx");
            }  

            if (string.IsNullOrEmpty(reportPath) || !System.IO.File.Exists(reportPath))
            {
                throw new FileNotFoundException("Report file not found.", reportPath);
            }

            // Load the report
            Report report = new Report();
            report.Load(reportPath);
            report.SetParameterValue("RId", reg.RId);

            DataTable Logo = new DataTable();
            Logo.Columns.Add("Logo", typeof(Byte[]));
            Logo.Rows.Add();

            // Prepare the report
            report.Prepare();

            using (MemoryStream ms = new MemoryStream())
            {
                PDFSimpleExport pdfExport = new PDFSimpleExport();  
                pdfExport.Export(report.Report, ms);
                ms.Flush();

                return File(ms.ToArray(), "application/pdf");
            }
              
        }







        public IActionResult QuitClaim()
        {
            string sql;
            Registration reg = new Registration();

            reg.Register = db.Query<Registration>("sp_CLR_QuitClaim_list");


            sql = @"SELECT * FROM CLR_QuitClaim_Attachment a
                    JOIN CLR_Registration_Master b
                    on a.RId = b.RId
                    where (b.IsCancel IS NULL OR IsCancel = 0) 
                    AND b.Status = 'Cleared'
                    ";
            reg.QCAttachments = db.Query<QCAttachments>(sql);


            return View(reg);
        }


        public IActionResult UploadClaim(Registration reg)
        {
            DataTable attachfile = new DataTable();
            attachfile.Columns.Add("FileName", typeof(string));
            attachfile.Columns.Add("MimeType", typeof(string)); // Add new column
            if (reg.Files is not null)
            {
                foreach (var item in reg.Files)
                {
                    string FileName = $"{Path.GetFileNameWithoutExtension(item.FileName)}{DateTime.Now.ToString("yyyyMMddhhmmss")}{Path.GetExtension(item.FileName)}";
                    var client = new FtpClient("172.16.0.12", "ftpadmin", "welcome");

                    // Use ContentType directly from IFormFile
                    string mimeType = item.ContentType;

                    client.Connect();
                    FtpStatus stat = client.UploadStream(item.OpenReadStream(), $"ClearSys/Attachments/{FileName}");
                    client.Disconnect();

                    attachfile.Rows.Add(FileName, mimeType);
                }

            }


            var response = db.ExecuteScalar("sp_clr_QuitClaim_Upload", new
            {
                reg.RId,
                reg.QCId,
                attachfile
            });

            if (response.ToString() == "Successful") // Assuming the query will affect only one row
            {
                TempData["Success Title"] = "Clearance Registration Done";
                TempData["Success Message"] = "Clearance Registration is successful. Remind other Departments";
                return RedirectToAction("Index", "Admin");

            }

            TempData["Error Title"] = "Clearance Registration Failed";
            TempData["Error Message"] = response.ToString();
            return RedirectToAction("Index", "Admin");

        }

        public IActionResult PrintClaim(Registration reg)
        {
            string reportPath = string.Empty;

            if (reg.Company.StartsWith("CBC"))
            {
                reportPath = Path.Combine(_webHostEnvironment, "Reports", "QuitCBCReport.frx");
            }
            else if (reg.Company.StartsWith("CBR"))
            {
                reportPath = Path.Combine(_webHostEnvironment, "Reports", "QuitCBRReport.frx");
            }

            if (string.IsNullOrEmpty(reportPath) || !System.IO.File.Exists(reportPath))
            {
                throw new FileNotFoundException("Report file not found.", reportPath);
            }

            // Load the report
            Report report = new Report();
            report.Load(reportPath);

            //DataTable Logo = new DataTable();
            //Logo.Columns.Add("Logo", typeof(Byte[]));
            //Logo.Rows.Add();

            // Prepare the report
            report.Prepare();

            using (MemoryStream ms = new MemoryStream())
            {
                PDFSimpleExport pdfExport = new PDFSimpleExport();
                pdfExport.Export(report.Report, ms);
                ms.Flush();

                return File(ms.ToArray(), "application/pdf");
            }
        }



        public IActionResult EditClaim(Registration reg)
        {


            var response = db.ExecuteScalar("sp_CLR_QuitClaim_Claim", new
            {
                reg.QCId
            });

            if (response.ToString() == "Successful") // Assuming the query will affect only one row
            {
                TempData["Success Title"] = "Claimed Successfully";
                TempData["Success Message"] = "Claiming complete.";
                return RedirectToAction("Index");

            }



            TempData["Error Title"] = "Claiming Failed";
            TempData["Error Message"] = response;
            return RedirectToAction("Index");
        }


            [Route("/Admin/DownloadClaim/{RId}")]
        public IActionResult DownloadClaim(int RId)
        {
            if (RId == 0)
            {
                TempData["NoFileMessage"] = "⚠️ There is no file for this record.";
                return RedirectToAction("QuitClaim");
            }

            var model = db.QueryFirstOrDefault<Attachments>(
                "SELECT FileName FROM CLR_QuitClaim_Attachment WHERE RId = @RId",
                new { RId }
            );

            if (model == null || string.IsNullOrEmpty(model.FileName))
            {
                TempData["NoFileMessage"] = "⚠️ There is no file for this record.";
                return RedirectToAction("QuitClaim");
            }

            try
            {
                using var client = new FtpClient("172.16.0.12", "ftpadmin", "welcome");
                client.Connect();
                client.DownloadBytes(out byte[] bytes, $"ClearSys/Attachments/{model.FileName}");
                client.Disconnect();

                if (bytes == null || bytes.Length == 0)
                {
                    TempData["NoFileMessage"] = "⚠️ There is no file for this record.";
                    return RedirectToAction("QuitClaim");
                }

                // Determine content type based on file extension
                string contentType = GetContentTypeClaim(model.FileName);
                return File(bytes, contentType, model.FileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file: {ex.Message}");
                TempData["NoFileMessage"] = "⚠️ An error occurred while downloading the file.";
                return RedirectToAction("QuitClaim");
            }
        }

        private string GetContentTypeClaim(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".txt" => "text/plain",
                ".doc" or ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };
        }



    }
}
