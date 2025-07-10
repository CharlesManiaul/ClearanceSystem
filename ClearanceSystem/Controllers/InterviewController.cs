using ClearanceSystem.Models;
using Dapper;
using FluentFTP;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Org.BouncyCastle.Ocsp;
using System.Data;
using System.Security.Claims;

namespace ClearanceSystem.Controllers
{

    public class InterviewController : Controller
    {
        SqlConnection db, adb;
        public InterviewController(IConfiguration configuration)
        {
            db = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
            adb = new SqlConnection(configuration.GetConnectionString("AssetConnection"));

        }



        public IActionResult Index()
        {
            string sql;
            Interview interview = new Interview();

            sql = @"SELECT 
                    a.*,
                    c.eid as EmpId,
                    c.name As EmpName, 
                    c.dateHired as DateHired,
                    d.position,
                    e.depName as DepartmentName,
                    e.depID as DepartmentId 
                    FROM CLR_Interview_Master a
                    join CLR_Registration_Master b
                    on a.RId = b.RId
                    JOIN employee_master c
                    on b.EmpId = c.eid
                    JOIN position_master d
                    on c.pid = d.pid
                    JOIN department e
                    on  c.def_dept = e.depID
                     WHERE (a.IsCancel is null or a.isCancel = 0) 
                    ";

            interview.interview = db.Query<Interview>(sql);

            sql = @"SELECT a.*, a.RId ,b.eid as EmpId, b.name As EmployeeName, b.dateHired as DateHired, c.position, d.depName as Department, d.depID as DepartmentId FROM CLR_Registration_Master a
                    JOIN employee_master b
                    on a.EmpId = b.eid
                    JOIN position_master c
                    on b.pid = c.pid
                    JOIN department d
                    on  b.def_dept = d.depID
                    WHERE (IsCancel is null or isCancel = 0)";

            interview.reg = db.Query<Registration>(sql);

            return View(interview);
        }



        [Route("Interview/Details/{IVId}")]
        public IActionResult Details(int IVId)
        {

            string sql;
            Interview interview = new Interview();

            sql = @"SELECT 
                    a.*,
                    c.eid as EmpId,
                    c.name As EmpName, 
                    c.dateHired as DateHired,
                    d.position,
                    e.depName as DepartmentName,
                    e.depID as DepartmentId 
                    FROM CLR_Interview_Master a
                    join CLR_Registration_Master b
                    on a.RId = b.RId
                    JOIN employee_master c
                    on b.EmpId = c.eid
                    JOIN position_master d
                    on c.pid = d.pid
                    JOIN department e
                    on  c.def_dept = e.depID
                     WHERE (a.IsCancel is null or a.isCancel = 0)
                    and a.IVId = @IVId
                    ";

            interview = db.QueryFirstOrDefault<Interview>(sql, new { IVId });

            sql = @"
                    Select a.Number as QNumber, b.Question, a.Answer from clr_Interview_Answers a
                    LEFT JOIN CLR_Question_Master b
                    on a.Number = b.QNumber
                    WHERE a.IVId = @IVId";

            interview.intForm = db.Query<InterviewForm>(sql, new { IVId });


            return View(interview);
        }




        public IActionResult SaveInterview(Interview interview)
        {

            var response = db.ExecuteScalar("sp_clr_save_interview", new
            {
                interview.IVId,
                interview.SchedDate,
                SchedBy = HttpContext.User.FindFirstValue(ClaimTypes.Name),

            });

            if (response.ToString() == "Successful") // Assuming the query will affect only one row
            {
                TempData["Success Title"] = "Interview Registration Done";
                TempData["Success Message"] = "Clearance Registration is successful. Remind other Departments";
                return RedirectToAction("Index");
            }
            TempData["Error Title"] = "Interview Registration Failed";
            TempData["Error Message"] = response.ToString();
            return RedirectToAction("Index");

        }



        public IActionResult DeleteInterview(Interview interview)
        {

            var response = db.ExecuteScalar("sp_clr_del_interview", new
            {
                interview.IVId,
                interview.RId,
                CancelBy = HttpContext.User.FindFirstValue(ClaimTypes.Name)

            });



            if (response.ToString() == "Successful") // Assuming the query will affect only one row
            {
                TempData["Success Title"] = "Interview Cancelled";
                TempData["Success Message"] = "Cancelled the interview, interview won't procceed";
                return RedirectToAction("Index");

            }


            TempData["Error Title"] = "Interview Cancellation Failed";
            TempData["Error Message"] = response.ToString();
            return RedirectToAction("Index");


        }


        public IActionResult FinishInterview(Interview interview)
        {
            var response = db.ExecuteScalar("sp_clr_fin_interview", new
            {
                interview.IVId,
                FinishBy = HttpContext.User.FindFirstValue(ClaimTypes.Name)

            });

            if (response.ToString() == "Successful") // Assuming the query will affect only one row
            {
                TempData["Success Title"] = "Interview Finish";
                TempData["Success Message"] = "Finish the interview, Thank you.";
                return RedirectToAction("Index");

            }


            TempData["Error Title"] = "Finishing Interview Failed";
            TempData["Error Message"] = response.ToString();
            return RedirectToAction("Index");


        }



        [AllowAnonymous]
        [Route("Interview/Form/{IVId}/{EmpId}")]
        public IActionResult Form(int IVId, int EmpId)
        {
            string sql;
            Interview interview = new Interview();


            sql = @"SELECT 
                    a.*,
                    c.eid as EmpId,
                    c.name As EmpName, 
                    c.dateHired as DateHired,
                    d.position,
                    e.depName as DepartmentName,
                    e.depID as DepartmentId 
                    FROM CLR_Interview_Master a
                    join CLR_Registration_Master b
                    on a.RId = b.RId
                    JOIN employee_master c
                    on b.EmpId = c.eid
                    JOIN position_master d
                    on c.pid = d.pid
                    JOIN department e
                    on  c.def_dept = e.depID
                     WHERE (a.IsCancel is null or a.isCancel = 0)
                    and a.IVId = @IVId
                    ";

            interview = db.QueryFirstOrDefault<Interview>(sql, new { IVId });


            sql = @"SELECT * FROM CLR_Question_Master";
            interview.intForm = db.Query<InterviewForm>(sql);


            return View(interview);

        }

        [HttpPost]
        public IActionResult SubmitAnswer(int EmpId, int IVId, Dictionary<int, string> Answers)
        {
            // Create DataTable
            DataTable interviewanswer = new DataTable();
            interviewanswer.Columns.Add("QuestionNumber", typeof(int));
            interviewanswer.Columns.Add("Answer", typeof(string));

            // Process answers and add them to the DataTable
            foreach (var answer in Answers)
            {
                int questionNumber = answer.Key;
                string Answer = answer.Value;

                interviewanswer.Rows.Add(questionNumber, Answer);
            }


            var response = db.ExecuteScalar("sp_clr_interview_Answer", new
            {
                IVId,
                interviewanswer

            });



            if (response.ToString() == "Successful") // Assuming the query will affect only one row
            {
                TempData["Success Title"] = "Thank you for Answering the Exit Interview";
                TempData["Success Message"] = "Good luck on your future endeavors";
                return RedirectToAction("Index");

            }


            TempData["Error Title"] = "Answer not submitted";
            TempData["Error Message"] = response.ToString();
            return RedirectToAction("Index");



        }



        public IActionResult InterviewReport()
        {

            string sql;
            Interview interview = new Interview();

            sql = @"SELECT 
                    a.*,
                    c.eid as EmpId,
                    c.name As EmpName, 
                    c.dateHired as DateHired,
                    d.position,
                    e.depName as DepartmentName,
                    e.depID as DepartmentId 
                    FROM CLR_Interview_Master a
                    join CLR_Registration_Master b
                    on a.RId = b.RId
                    JOIN employee_master c
                    on b.EmpId = c.eid
                    JOIN position_master d
                    on c.pid = d.pid
                    JOIN department e
                    on  c.def_dept = e.depID
                     WHERE (a.IsCancel is null or a.isCancel = 0)
                    ";

            interview.interview = db.Query<Interview>(sql);

            sql = @"SELECT a.*, a.RId ,b.eid as EmpId, b.name As EmployeeName, b.dateHired as DateHired, c.position, d.depName as Department, d.depID as DepartmentId FROM CLR_Registration_Master a
                    JOIN employee_master b
                    on a.EmpId = b.eid
                    JOIN position_master c
                    on b.pid = c.pid
                    JOIN department d
                    on  b.def_dept = d.depID
                    WHERE (IsCancel is null or isCancel = 0)";

            interview.reg = db.Query<Registration>(sql);

            return View(interview);

        }


    }
}
