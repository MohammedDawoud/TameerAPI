using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models.Common;
using TaamerProject.Models;
using TaamerProject.Models.DBContext;
using TaamerProject.Repository.Interfaces;
using TaamerProject.Service.IGeneric;
using System.Net;
using TaamerProject.Service.Interfaces;
using TaamerProject.Service.Generic;
using TaamerProject.Service.LocalResources;

namespace TaamerProject.Service.Services
{
    public class PayrollMarchesService :   IPayrollMarchesService
    {
        private readonly IPayrollMarchesRepository _payrollMarchesRepository;
         private readonly IEmployeesRepository _employeesRepository;
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly ISystemAction _SystemAction;
        public PayrollMarchesService(IPayrollMarchesRepository payrollMarchesRepository 
            , IEmployeesRepository employeesRepository
            , TaamerProjectContext dataContext, ISystemAction systemAction)
        {
            _payrollMarchesRepository = payrollMarchesRepository;
             _employeesRepository = employeesRepository;
            _TaamerProContext = dataContext;
            _SystemAction = systemAction;
        }
        
        public Task<PayrollMarches> GetPayrollMarches(int EmpId, int MonthId) {
            return _payrollMarchesRepository.GetPayrollMarches(EmpId, MonthId);
        }
        public Task<IEnumerable<PayrollMarchesVM>> GetPayrollMarches(int MonthId, int BranchSearch, string SearchText) {
            var poyrolls = _payrollMarchesRepository.GetPayrollMarches(MonthId, BranchSearch, SearchText);
            return poyrolls;
        }


        public Task<IEnumerable<PayrollMarchesVM>> GetPayrollMarches(int MonthId, int BranchSearch, string SearchText,int YearId)
        {
            var poyrolls = _payrollMarchesRepository.GetPayrollMarches(MonthId, BranchSearch, SearchText, YearId);
            return poyrolls;
        }
        public GeneralMessage SavePayrollMarches(PayrollMarches payroll, int UserId, int BranchId) {
            try
            {
                // var Employee = _employeesRepository.GetById(payroll.EmpId);
                Employees? Employee = _TaamerProContext.Employees.Where(s => s.EmployeeId == payroll.EmpId).FirstOrDefault();
                
                if (Employee == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ  مسير رواتب";
                    _SystemAction.SaveAction("SavePayrollMarches", "PayrollMarchesService", 1, Resources.NoEmployee, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.NoEmployee };
                }

                if (payroll.PayrollId == 0)
                {
                    //payroll.MonthNo = DateTime.Now.Month;
                    payroll.AddUser = UserId;
                    payroll.AddDate = DateTime.Now;
                    payroll.IsPostVoucher = false;

                    _TaamerProContext.PayrollMarches.Add(payroll);
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة مسير رواتب جديد";
                    _SystemAction.SaveAction("SavePayrollMarches", "PayrollMarchesService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage() {  StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.General_SavedSuccessfully };
                }
                {
                    //var UpdatedPayroll = _payrollMarchesRepository.GetById(payroll.PayrollId);
                    PayrollMarches? UpdatedPayroll = _TaamerProContext.PayrollMarches.Where(s => s.PayrollId == payroll.PayrollId).FirstOrDefault();

                    if (UpdatedPayroll == null)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote3 = "فشل في حفظ  مسير رواتب";
                        _SystemAction.SaveAction("SavePayrollMarches", "PayrollMarchesService", 2, Resources.noPayrollThisNumber, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.noPayrollThisNumber };
                    }
                    if (UpdatedPayroll.PostDate.HasValue)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote2 = "فشل في حفظ  مسير رواتب";
                        _SystemAction.SaveAction("SavePayrollMarches", "PayrollMarchesService", 2, "لا يمكن التعديل على مسير مُرحل", "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.notPossibleModifyRelayPath };
                    }
                    
                    UpdatedPayroll.MonthNo = payroll.MonthNo;
                    UpdatedPayroll.MainSalary = payroll.MainSalary;
                    UpdatedPayroll.SalaryOfThisMonth = payroll.SalaryOfThisMonth;
                    UpdatedPayroll.Bonus = payroll.Bonus;
                    UpdatedPayroll.CommunicationAllawance = payroll.CommunicationAllawance;
                    UpdatedPayroll.ProfessionAllawance = payroll.ProfessionAllawance;
                    UpdatedPayroll.TransportationAllawance = payroll.TransportationAllawance;
                    UpdatedPayroll.HousingAllowance = payroll.HousingAllowance;
                    UpdatedPayroll.MonthlyAllowances = payroll.MonthlyAllowances;
                    UpdatedPayroll.ExtraAllowances = payroll.ExtraAllowances;
                    UpdatedPayroll.TotalRewards = payroll.TotalRewards;
                    UpdatedPayroll.TotalDiscounts = payroll.TotalDiscounts;
                    UpdatedPayroll.TotalLoans = payroll.TotalLoans;
                    UpdatedPayroll.TotalSalaryOfThisMonth = payroll.TotalSalaryOfThisMonth;
                    UpdatedPayroll.UpdateUser = payroll.UpdateUser;
                    UpdatedPayroll.UpdateDate = payroll.UpdateDate;
                    UpdatedPayroll.TotalAbsDays = payroll.TotalAbsDays;
                    UpdatedPayroll.TotalVacations = payroll.TotalVacations;
                    UpdatedPayroll.Taamen = payroll.Taamen;
                    UpdatedPayroll.YearId= payroll.YearId;
                    UpdatedPayroll.TotalLateDiscount = payroll.TotalLateDiscount;
                    UpdatedPayroll.TotalAbsenceDiscount = payroll.TotalAbsenceDiscount;
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل مسير رواتب رقم " + payroll.PayrollId;
                    _SystemAction.SaveAction("SavePayrollMarches", "PayrollMarchesService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage() {  StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.General_SavedSuccessfully };
                    }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote2 = "فشل في حفظ  مسير رواتب";
                _SystemAction.SaveAction("SavePayrollMarches", "PayrollMarchesService", 1, Resources.General_SavedFailed, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage PostPayrollMarches(int PayrollId, int UserId, int BranchId)
        {
            try
            {
                //var UpdatedPayroll = _payrollMarchesRepository.GetById(PayrollId);
                PayrollMarches? UpdatedPayroll = _TaamerProContext.PayrollMarches.Where(s => s.PayrollId == PayrollId).FirstOrDefault();

                if (UpdatedPayroll == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ  مسير رواتب";
                    _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.noPayrollThisNumber, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.noPayrollThisNumber };
                }
                //
                //--------الحسابات-------
                //
                if (UpdatedPayroll.PostDate != null)
                {
                    return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.payrollPathAlreadyMigrated };
                }
                UpdatedPayroll.PostDate = DateTime.Now;

                
                UpdatedPayroll.UpdateDate = DateTime.Now;
                UpdatedPayroll.UpdateUser = UserId;
                UpdatedPayroll.IsPostVoucher = false;
                _TaamerProContext.SaveChanges();
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " ترحيل مسير رواتب رقم " + PayrollId;
                _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.PayrollProcessNo + PayrollId, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------
               
                return new GeneralMessage() {  StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.ResourceManager.GetString("PayrollProcessNo", CultureInfo.CreateSpecificCulture("ar")) + PayrollId };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote2 = Resources.ResourceManager.GetString("PayrollProcessNoFaild", CultureInfo.CreateSpecificCulture("ar")) + PayrollId;
                _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.PayrollProcessNoFaild + PayrollId, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.ResourceManager.GetString("PayrollProcessNoFaild", CultureInfo.CreateSpecificCulture("ar")) + PayrollId };
            }
        }
        public GeneralMessage PostEmpPayroll_Back(int PayrollId, int UserId, int BranchId)
        {
            try
            {
               // var UpdatedPayroll = _payrollMarchesRepository.GetById(PayrollId);
                PayrollMarches? UpdatedPayroll = _TaamerProContext.PayrollMarches.Where(s => s.PayrollId == PayrollId).FirstOrDefault();

                if (UpdatedPayroll == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ  مسير رواتب";
                    _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.noPayrollThisNumber, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.noPayrollThisNumber };
                }

                if (UpdatedPayroll.PostDate == null)
                {
                    return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = "تم فك ترحيل مسير الرواتب هذا مسبقا " };
                }
                if (UpdatedPayroll.IsPostVoucher == true)
                {
                    return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = "لا يمكن فك الترحيل لانه تم ترحيل القيود " };
                }

                if (UpdatedPayroll.IsPostPayVoucher == true)
                {
                    return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = "لا يمكن فك الترحيل لانه تم اضافة صرف للمسير " };
                }
                UpdatedPayroll.PostDate = null;
                UpdatedPayroll.UpdateDate = DateTime.Now;
                UpdatedPayroll.UpdateUser = UserId;
                UpdatedPayroll.IsPostVoucher = false;
                _TaamerProContext.SaveChanges();
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " فك ترحيل مسير رواتب رقم " + PayrollId;
                _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, "تم فك ترحيل مسير الرواتب رقم" + PayrollId, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------
              
                return new GeneralMessage() {  StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.ResourceManager.GetString("PayrollProcessNo", CultureInfo.CreateSpecificCulture("ar")) + PayrollId };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote2 = " فشل في فك ترحيل مسير رواتب رقم" + PayrollId;
                _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, " فشل في فك ترحيل مسير رواتب رقم" + PayrollId, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                //-----------------------------------------------------------------------------------------------------------------
                
                return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.ResourceManager.GetString("PayrollProcessNoFaild", CultureInfo.CreateSpecificCulture("ar")) + PayrollId };
            }
        }

        
        public GeneralMessage PostPayrollMarcheslist(List<Int32> payrollid, int UserId, int BranchId)
        {
            try
            {
                int CountNMora7l = 0;
                int CountMora7l = 0;
                for (int i = 0; i < payrollid.Count; i++)
                {
                   // var UpdatedPayroll = _payrollMarchesRepository.GetById(payrollid[i]);
                    PayrollMarches? UpdatedPayroll = _TaamerProContext.PayrollMarches.Where(s => s.PayrollId == payrollid[i]).FirstOrDefault();


                    if (UpdatedPayroll != null) {
                    //if (UpdatedPayroll == null)
                    //{
                    //    //-----------------------------------------------------------------------------------------------------------------
                    //    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    //    string ActionNote3 = "فشل في حفظ  مسير رواتب";
                    //    _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.noPayrollThisNumber, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //    //-----------------------------------------------------------------------------------------------------------------

                    //    return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.noPayrollThisNumber };
                    //}
                    //
                    //--------الحسابات-------
                    //
                    CountNMora7l += 1;
                    UpdatedPayroll.PostDate = DateTime.Now;
                    UpdatedPayroll.UpdateDate = DateTime.Now;
                    UpdatedPayroll.UpdateUser = UserId;
                    UpdatedPayroll.IsPostVoucher = false;
                    _TaamerProContext.SaveChanges();
                    }
                    else
                    {
                        CountMora7l += 1;
                    }
                }

                string Message = "";
                if (CountMora7l == 0 && CountNMora7l > 0)
                {
                    Message = "Resources.Deported";
                    //return new GeneralMessage {  StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.Deported };
                }
                else if (CountMora7l > 0 && CountNMora7l == 0)
                {
                    // _TaamerProContext.SaveChanges();
                    Message = "Resources.Restrictionspreviouslyposted";
                    //return new GeneralMessage {  StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.Restrictionspreviouslyposted };
                }
                else if (CountMora7l > 0 && CountNMora7l > 0)
                {
                    //_TaamerProContext.SaveChanges();
                    string Msg = String.Format("Resources.posted", CountNMora7l, CountMora7l);
                    Message = Msg;
                    //return new GeneralMessage {  StatusCode = HttpStatusCode.OK,ReasonPhrase = Msg };
                }

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " ترحيل مسيرات رواتب  " ;
                _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.payrollPathAlreadyMigrated, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage() {  StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.payrollPathAlreadyMigrated };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote2 = Resources.PayrollProcessNoFaild ;
                _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.PayrollProcessFaild , "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.PayrollProcessFaild };
            }
        }

        public GeneralMessage PostEmpPayrollVoucher(int PayrollId, int UserId, int BranchId)
        {
            try
            {
                // var UpdatedPayroll = _payrollMarchesRepository.GetById(PayrollId);
                PayrollMarches? UpdatedPayroll = _TaamerProContext.PayrollMarches.Where(s => s.PayrollId == PayrollId).FirstOrDefault();

                if (UpdatedPayroll == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في ترحيل قيود  مسير رواتب";
                    _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.noPayrollThisNumber, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.noPayrollThisNumber };
                }
                //
                //--------الحسابات-------
                //
                UpdatedPayroll.IsPostVoucher = true;
                _TaamerProContext.SaveChanges();
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " ترحيل مسير رواتب رقم " + PayrollId;
                _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.PayrollProcessNo + PayrollId, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage() {  StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.PayrollProcessNo + PayrollId };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote2 = Resources.PayrollProcessNoFaild + PayrollId;
                _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.PayrollProcessNoFaild + PayrollId, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.PayrollProcessNoFaild + PayrollId };
            }
        }



        public GeneralMessage PostAllEmpPayrollVoucher(List<int> PayrollId, int UserId, int BranchId)
        {
            try
            {
                foreach(var pyrol in PayrollId) {
                    // var UpdatedPayroll = _payrollMarchesRepository.GetById(pyrol);
                    PayrollMarches? UpdatedPayroll = _TaamerProContext.PayrollMarches.Where(s => s.PayrollId == pyrol).FirstOrDefault();

                    if (UpdatedPayroll == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في ترحيل قيود  مسير رواتب";
                    _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.noPayrollThisNumber, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.noPayrollThisNumber };
                }
                //
                //--------الحسابات-------
                //
                UpdatedPayroll.IsPostVoucher = true;
                _TaamerProContext.SaveChanges();
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " ترحيل مسيرات رواتب  ";
                _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.payrollPathAlreadyMigrated, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                }
                return new GeneralMessage() 
                {  StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.payrollPathAlreadyMigrated };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote2 = Resources.PayrollProcessNoFaild + PayrollId;
                _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.PayrollProcessNoFaild + PayrollId, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.PayrollProcessNoFaild + PayrollId };
            }
        }
        public GeneralMessage PostALLEmpPayrollPayVoucher(List<int> PayrollId, int UserId, int BranchId)
        {
            try
            {
                foreach (var pyrol in PayrollId)
                {
                    // var UpdatedPayroll = _payrollMarchesRepository.GetById(pyrol);
                    PayrollMarches? UpdatedPayroll = _TaamerProContext.PayrollMarches.Where(s => s.PayrollId == pyrol).FirstOrDefault();

                    if (UpdatedPayroll == null)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote3 = "فشل في اضافة صرف  بمسير رواتب";
                        _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.noPayrollThisNumber, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.noPayrollThisNumber };
                    }
                    //
                    //--------الحسابات-------
                    //
                    UpdatedPayroll.IsPostPayVoucher = true;
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " ترحيل مسيرات رواتب  " ;
                    _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.payrollPathAlreadyMigrated, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                }
                return new GeneralMessage() {  StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.payrollPathAlreadyMigrated };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote2 = " فشل في ترحيل مسيرات رواتب " ;
                _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.PayrollProcessFaild, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.PayrollProcessFaild };
            }
        }




        public GeneralMessage PostEmpPayrollPayVoucher(int PayrollId, int UserId, int BranchId)
        {
            try
            {
                // var UpdatedPayroll = _payrollMarchesRepository.GetById(PayrollId);
                PayrollMarches? UpdatedPayroll = _TaamerProContext.PayrollMarches.Where(s => s.PayrollId == PayrollId).FirstOrDefault();

                if (UpdatedPayroll == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في اضافة صرف  بمسير رواتب";
                    _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.noPayrollThisNumber, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.noPayrollThisNumber };
                }
                //
                //--------الحسابات-------
                //
                UpdatedPayroll.IsPostPayVoucher = true;
                _TaamerProContext.SaveChanges();
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " ترحيل مسير رواتب رقم " + PayrollId;
                _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.PayrollProcessNo + PayrollId, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage() {  StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.PayrollProcessNo + PayrollId };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote2 = Resources.PayrollProcessNoFaild + PayrollId;
                _SystemAction.SaveAction("PostPayrollMarches", "PayrollMarchesService", 2, Resources.PayrollProcessNoFaild + PayrollId, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage() {  StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.PayrollProcessNoFaild + PayrollId };
            }
        }


        

    }
}