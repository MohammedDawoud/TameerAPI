using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaamerProject.API.Helper;
using TaamerProject.Models;
using TaamerProject.Service.Interfaces;
using TaamerProject.Service.Services;

namespace TaamerProject.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]

    public class StatisticsController : ControllerBase
    {
        private readonly IAccountsService _accountsService;
        private readonly IProjectService _projectservice;
        private readonly INotificationService _NotificationService;
        private readonly IProjectPhasesTasksService _projectPhasesTasksservice;
        private readonly IUserMailsService _userMailsservice;
        
        private IBranchesService _branchesService;
        private string? Con;
        private IConfiguration Configuration;
        private readonly IOrganizationsService _organizationsservice;
        private readonly IEmployeesService _employeeService;
        public GlobalShared _globalshared;
        public StatisticsController(IConfiguration _configuration, IAccountsService accountsService, IProjectService projectservice
            , INotificationService NotificationService, IProjectPhasesTasksService projectPhasesTasksservice
            , IBranchesService branchesService, IOrganizationsService organizationsservice, IEmployeesService employeeService
            , IUserMailsService userMailsservice)
        {
             _accountsService = accountsService;
             _projectservice = projectservice;
             _projectPhasesTasksservice = projectPhasesTasksservice;
             _userMailsservice = userMailsservice;
             _branchesService = branchesService;
            _NotificationService = NotificationService;
            
            _organizationsservice = organizationsservice;
            
            _employeeService = employeeService;
            Configuration = _configuration;
            Con = this.Configuration.GetConnectionString("DBConnection");
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
        }

        [HttpGet("GetALLOrgData")]
        public IActionResult GetALLOrgData()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            int orgId = _branchesService.GetOrganizationId(_globalshared.BranchId_G).Result;
            return Ok(_organizationsservice.GetBranchOrganizationData(orgId));
        }
        [HttpGet("GetStatisticsCount")]
        public IActionResult GetStatisticsCount()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            //var userBranchs = _branchesService.GetAllBranchesByUserId(_globalshared.Lang_G, _globalshared.UserId_G);
            var ProjectCount = 0;

            var obj = new ProjectVM();
            obj.BranchId = _globalshared.BranchId_G;
            obj.Status = 0;
            var pro = _projectservice.GetAllProjectsNew(Con ?? "", obj, _globalshared.UserId_G, 0,0, _globalshared.BranchId_G).Result.ToList();

            //var pro = _projectservice.GetAllProjects3(Con, _globalshared.Lang_G, _globalshared.BranchId_G, _globalshared.UserId_G).Result;
            if (pro !=null &&pro.Count() > 0)
            {
                ProjectCount = pro.ToList().Where(a => a.MangerId == _globalshared.UserId_G).ToList().Count();
            }
            else
            {
                ProjectCount = 0;
            }
            var emp = _employeeService.GetEmployeeByUserid(_globalshared.UserId_G).Result.FirstOrDefault();
            int VactionBalance = 0;
            var VactionBalancestr = "";
            if (emp != null)
            {
                VactionBalance = emp.VacationEndCount ?? 0;
            }

            VactionBalancestr = (VactionBalance < 30) ? VactionBalance + " يوم " : (VactionBalance == 30) ? VactionBalance / 30 + " شهر " : (VactionBalance > 30) ? ((VactionBalance / 30) + " شهر " + (VactionBalance % 30) + " يوم  ") : "";
            var Counts = new
            {
                NotificationsCount = _NotificationService.GetNotificationReceived(_globalshared.UserId_G).Result.Where(s => s.IsRead != true).Count(),
                AllertCount = _NotificationService.GetUserAlert(_globalshared.UserId_G).Result.Where(x => x.IsRead != true).Count(),
                TasksByUserCount = _projectPhasesTasksservice.GetTasksByUserId(_globalshared.UserId_G, 0, _globalshared.BranchId_G).Result.Count(),
                MyInboxCount = _userMailsservice.GetAllUserMails(_globalshared.UserId_G, _globalshared.BranchId_G).Result.Count(),
                GetUserProjects = ProjectCount,
                VacationBalance = VactionBalancestr,
                BackupAlertLoad_M = _NotificationService.GetUserBackupNotesAlert(_globalshared.UserId_G).Result
        };
            return Ok(Counts );
        }


        [HttpGet("GetLateTasksByUserId")]
        public IActionResult GetLateTasksByUserId()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _projectPhasesTasksservice.GetLateTasksByUserId(_globalshared.UserId_G, 0, _globalshared.BranchId_G).Result.Count();
            return Ok(result );
        }
        [HttpGet("GetOfficialPapersStatitecs")]
        public IActionResult GetOfficialPapersStatitecs()
        {
            var result = _NotificationService.GetOfficialDocsStatsecs(Con??"");
            return Ok(result );
        }
    }
}
