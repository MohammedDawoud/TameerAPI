﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models;
using TaamerProject.Models.Common;
using TaamerProject.Models.DBContext;
using TaamerProject.Repository.Interfaces;
using TaamerProject.Service.IGeneric;
using TaamerProject.Service.Interfaces;
using TaamerProject.Service.LocalResources;

namespace TaamerProject.Service.Services
{
    public class DiscountRewardService :   IDiscountRewardService
    {
        private readonly IDiscountRewardRepository _DiscountRewardRepository;
        private readonly IBranchesRepository _BranchesRepository;
        private readonly ITransactionsRepository _TransactionsRepository;
        private readonly IEmployeesRepository _EmployeesRepository;
        private readonly IAccountsRepository _AccountsRepository;
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly ISystemAction _SystemAction;
        private readonly IPayrollMarchesRepository _payrollMarchesRepository;

        public DiscountRewardService(TaamerProjectContext dataContext
            , ISystemAction systemAction, IDiscountRewardRepository DiscountRewardRepository,
            IBranchesRepository BranchesRepository, ITransactionsRepository TransactionsRepository,
            IEmployeesRepository EmployeesRepository, IAccountsRepository AccountsRepository, IPayrollMarchesRepository payrollMarchesRepository
            )
        {
            _DiscountRewardRepository = DiscountRewardRepository;
            _BranchesRepository = BranchesRepository;
            _TransactionsRepository = TransactionsRepository;
            _EmployeesRepository = EmployeesRepository;
            _AccountsRepository = AccountsRepository;
            _TaamerProContext = dataContext;
            _SystemAction = systemAction;
            _payrollMarchesRepository =payrollMarchesRepository  ;
        }
       
        public Task<IEnumerable<DiscountRewardVM>> GetAllDiscountRewards(int? EmpId, string SearchText)
        {
            var DiscountRewards = _DiscountRewardRepository.GetAllDiscountRewards(EmpId, SearchText);
            return DiscountRewards;
        }
        
        public Task<decimal> GetDiscountRewordSumForPayroll(int EmpId, DateTime StartDate, DateTime EndDate, int Type) {
            return _DiscountRewardRepository.GetDiscountRewordSumForPayroll(EmpId, StartDate, EndDate, Type);
        }

        public GeneralMessage SaveDiscountReward(DiscountReward discountReward,int UserId, int BranchId, int? yearid)
        {
            try
            {
                var Branch = _BranchesRepository.GetById(BranchId);
                var Payroll = _payrollMarchesRepository.GetPayrollMarches(discountReward.EmployeeId.Value, DateTime.Now.Month);

                if (Payroll.Result != null && Payroll.Result.PostDate.HasValue &&
                    (discountReward.StartDate.HasValue && discountReward.StartDate <= DateTime.Now) && 
                    (!discountReward.EndDate.HasValue || ((discountReward.EndDate.HasValue && discountReward.EndDate >= DateTime.Now))))
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في تعديل مسير الرواتب (المكافآت و الخصومات)) ";
                     _SystemAction.SaveAction("UpdatePayrollWithDiscountRewards", "DiscountRewardService", 2, Resources.Posted_payroll_cannot_be_modified, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage() { StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.Posted_payroll_cannot_be_modified };
                }

                //if (discountReward.Type==1)
                //{
                //    if (Branch == null || Branch.PurchaseDiscAccId == null)
                //    {
                //        //-----------------------------------------------------------------------------------------------------------------
                //        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                //        string ActionNote2 = "فشل في حفظ خصم للموظف";
                //         _SystemAction.SaveAction("SaveDiscountReward", "DiscountRewardService", 1, "تأكد من حساب الخصم للموظفين في حسابات الفرع", "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                //        //-----------------------------------------------------------------------------------------------------------------

                //        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = "تأكد من حساب الخصم للموظفين في حسابات الفرع" };

                //    }
                //}
                //else
                //{
                //    if (Branch == null || Branch.PurchaseApprovalAccId == null)
                //    {
                //        //-----------------------------------------------------------------------------------------------------------------
                //        string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                //        string ActionNote3 = "فشل في حفظ خصم للموظف";
                //         _SystemAction.SaveAction("SaveDiscountReward", "DiscountRewardService", 1, "تأكد من حساب المكافئات للموظفين في حسابات الفرع", "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                //        //-----------------------------------------------------------------------------------------------------------------

                //        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = "تأكد من حساب المكافئات للموظفين في حسابات الفرع" };

                //    }
                //}

                //discountReward.TransactionDetails = new List<Transactions>();

                //decimal? AccDepit = 0;
                //decimal? AccCredit = 0;
                decimal? AccAmount = 0;
                //mokf2at msrof yb2a mden hsab l mkaf2at w da2n hsab l mwzf    Employee.AccountIDs_Bouns   20
                //5sm eradate yb2a mden hsab l mwzf w da2n hsab l 5somat  Employee.AccountIDs_Discount     21
                int TypeDis = discountReward.Type == 1 ? 21 : 20;
                //var Employee = _EmployeesRepository.GetById(discountReward.EmployeeId);
                Employees? Employee = _TaamerProContext.Employees.Where(s => s.EmployeeId == discountReward.EmployeeId).FirstOrDefault();

                string vouchertypename = discountReward.Type == 1 ? "خصم" : "مكافأة";
                //int? AccountTypeDis = discountReward.Type == 1 ? _AccountsRepository.GetById(Employee.AccountIDs_Discount).Type : _AccountsRepository.GetById(Employee.AccountIDs_Bouns).Type;
                //int? AccountIdDis = discountReward.Type == 1 ? Employee.AccountIDs_Discount : Employee.AccountIDs_Bouns;

                AccAmount = discountReward.Amount;

                //AccDepit = discountReward.Type == 1 ? discountReward.Amount : 0;
                //AccCredit = discountReward.Type == 1 ? 0 : discountReward.Amount;

                if (discountReward.DiscountRewardId == 0)
                {
                    discountReward.AddUser = UserId;
                    discountReward.AddDate = DateTime.Now;
                    _TaamerProContext.DiscountReward.Add(discountReward);

                    _TaamerProContext.SaveChanges();

                    //UpdatePayrollWithDiscountRewards(discountReward.EmployeeId.Value, UserId, BranchId);

                    //if (discountReward.Type == 1)
                    //{
                    //    discountReward.TransactionDetails.Add(new Transactions
                    //    {

                    //        TransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                    //        TransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                    //        AccountId = Branch.PurchaseDiscAccId,
                    //        CostCenterId = null,
                    //        AccountType = AccountTypeDis,
                    //        Type = TypeDis,
                    //        LineNumber = 1,
                    //        Depit = 0,
                    //        Credit = AccAmount,
                    //        YearId = yearid,
                    //        Notes = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                    //        Details = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                    //        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                    //        InvoiceReference = "",

                    //        InvoiceId = 0,
                    //        DiscountRewardId = discountReward.DiscountRewardId,
                    //        IsPost = true,
                    //        BranchId = BranchId,
                    //        AddDate = DateTime.Now,
                    //        AddUser = UserId,
                    //        IsDeleted = false,
                    //    });
                    //    discountReward.TransactionDetails.Add(new Transactions
                    //    {

                    //        TransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                    //        TransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                    //        AccountId = AccountIdDis,
                    //        CostCenterId = null,
                    //        AccountType = AccountTypeDis,
                    //        Type = TypeDis,
                    //        LineNumber = 2,
                    //        Depit = AccAmount,
                    //        Credit = 0,
                    //        YearId = yearid,
                    //        Notes = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                    //        Details = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                    //        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                    //        InvoiceReference = "",

                    //        InvoiceId = 0,
                    //        DiscountRewardId = discountReward.DiscountRewardId,
                    //        IsPost = true,
                    //        BranchId = BranchId,
                    //        AddDate = DateTime.Now,
                    //        AddUser = UserId,
                    //        IsDeleted = false,
                    //    });

                    //}
                    //else
                    //{
                    //    discountReward.TransactionDetails.Add(new Transactions
                    //    {

                    //        TransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                    //        TransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                    //        AccountId = Branch.PurchaseApprovalAccId,
                    //        CostCenterId = null,
                    //        AccountType =  AccountTypeDis,
                    //        Type = TypeDis,
                    //        LineNumber = 1,
                    //        Depit = AccAmount,
                    //        Credit = 0,
                    //        YearId = yearid,
                    //        Notes = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                    //        Details = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                    //        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                    //        InvoiceReference = "",

                    //        InvoiceId = 0,
                    //        DiscountRewardId = discountReward.DiscountRewardId,
                    //        IsPost = true,
                    //        BranchId = BranchId,
                    //        AddDate = DateTime.Now,
                    //        AddUser = UserId,
                    //        IsDeleted = false,
                    //    });
                    //    discountReward.TransactionDetails.Add(new Transactions
                    //    {

                    //        TransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                    //        TransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                    //        AccountId = AccountIdDis,
                    //        CostCenterId = null,
                    //        AccountType = AccountTypeDis,
                    //        Type = TypeDis,
                    //        LineNumber = 2,
                    //        Depit = 0,
                    //        Credit = AccAmount,
                    //        YearId = yearid,
                    //        Notes = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                    //        Details = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                    //        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                    //        InvoiceReference = "",

                    //        InvoiceId = 0,
                    //        DiscountRewardId = discountReward.DiscountRewardId,
                    //        IsPost = true,
                    //        BranchId = BranchId,
                    //        AddDate = DateTime.Now,
                    //        AddUser = UserId,
                    //        IsDeleted = false,
                    //    });
                    //}

                    //_TransactionsRepository.AddRange(discountReward.TransactionDetails.ToList());



                    var ms = "";
                    if (discountReward.Type == 1)
                    {
                        ms = "خصم";
                    }
                    else
                    {
                        ms = "مكافأة";
                    }

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " اضافة "+ ms ;
                     _SystemAction.SaveAction("SaveDiscountReward", "DiscountRewardService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.General_SavedSuccessfully };

                }
                else
                {



                    //var DiscountRewardUpdated = _DiscountRewardRepository.GetById(discountReward.DiscountRewardId);
                    DiscountReward? DiscountRewardUpdated = _TaamerProContext.DiscountReward.Where(s => s.DiscountRewardId == discountReward.DiscountRewardId).FirstOrDefault();

                    if (DiscountRewardUpdated != null)
                    {
                        DiscountRewardUpdated.EmployeeId = discountReward.EmployeeId;
                        DiscountRewardUpdated.Amount = discountReward.Amount;
                        DiscountRewardUpdated.Date = discountReward.Date;
                        //DiscountRewardUpdated.StartDate = discountReward.StartDate;
                        //DiscountRewardUpdated.EndDate = discountReward.EndDate;
                        DiscountRewardUpdated.MonthNo = discountReward.MonthNo;
                        DiscountRewardUpdated.HijriDate = discountReward.HijriDate;
                        DiscountRewardUpdated.Type = discountReward.Type;
                        DiscountRewardUpdated.Notes = discountReward.Notes;
                        DiscountRewardUpdated.UpdateUser = UserId;
                        //DiscountRewardUpdated.UpdatedDate = DateTime.Now;

                        //_TransactionsRepository.RemoveRange(DiscountRewardUpdated.TransactionDetails.ToList());

                        //if (discountReward.Type == 1)
                        //{
                        //    discountReward.TransactionDetails.Add(new Transactions
                        //    {

                        //        TransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                        //        TransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                        //        AccountId = Branch.PurchaseDiscAccId,
                        //        CostCenterId = null,
                        //        AccountType =  AccountTypeDis,
                        //        Type = TypeDis,
                        //        LineNumber = 1,
                        //        Depit = 0,
                        //        Credit = AccAmount,
                        //        YearId = yearid,
                        //        Notes = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                        //        Details = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                        //        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        //        InvoiceReference = "",

                        //        InvoiceId = 0,
                        //        DiscountRewardId = discountReward.DiscountRewardId,
                        //        IsPost = true,
                        //        BranchId = BranchId,
                        //        AddDate = DateTime.Now,
                        //        AddUser = UserId,
                        //        IsDeleted = false,
                        //    });
                        //    discountReward.TransactionDetails.Add(new Transactions
                        //    {

                        //        TransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                        //        TransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                        //        AccountId = AccountIdDis,
                        //        CostCenterId = null,
                        //        AccountType = AccountTypeDis,
                        //        Type = TypeDis,
                        //        LineNumber = 2,
                        //        Depit = AccAmount,
                        //        Credit = 0,
                        //        YearId = yearid,
                        //        Notes = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                        //        Details = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                        //        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        //        InvoiceReference = "",

                        //        InvoiceId = 0,
                        //        DiscountRewardId = discountReward.DiscountRewardId,
                        //        IsPost = true,
                        //        BranchId = BranchId,
                        //        AddDate = DateTime.Now,
                        //        AddUser = UserId,
                        //        IsDeleted = false,
                        //    });

                        //}
                        //else
                        //{
                        //    discountReward.TransactionDetails.Add(new Transactions
                        //    {

                        //        TransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                        //        TransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                        //        AccountId = Branch.PurchaseApprovalAccId,
                        //        CostCenterId = null,
                        //        AccountType =  AccountTypeDis,
                        //        Type = TypeDis,
                        //        LineNumber = 1,
                        //        Depit = AccAmount,
                        //        Credit = 0,
                        //        YearId = yearid,
                        //        Notes = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                        //        Details = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                        //        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        //        InvoiceReference = "",

                        //        InvoiceId = 0,
                        //        DiscountRewardId = discountReward.DiscountRewardId,
                        //        IsPost = true,
                        //        BranchId = BranchId,
                        //        AddDate = DateTime.Now,
                        //        AddUser = UserId,
                        //        IsDeleted = false,
                        //    });
                        //    discountReward.TransactionDetails.Add(new Transactions
                        //    {

                        //        TransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                        //        TransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                        //        AccountId = AccountIdDis,
                        //        CostCenterId = null,
                        //        AccountType = AccountTypeDis,
                        //        Type = TypeDis,
                        //        LineNumber = 2,
                        //        Depit = 0,
                        //        Credit = AccAmount,
                        //        YearId = yearid,
                        //        Notes = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                        //        Details = "" + vouchertypename + " " + Resources.number + " " + discountReward.DiscountRewardId + "",
                        //        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        //        InvoiceReference = "",

                        //        InvoiceId = 0,
                        //        DiscountRewardId = discountReward.DiscountRewardId,
                        //        IsPost = true,
                        //        BranchId = BranchId,
                        //        AddDate = DateTime.Now,
                        //        AddUser = UserId,
                        //        IsDeleted = false,
                        //    });
                        //}

                        //_TransactionsRepository.AddRange(discountReward.TransactionDetails.ToList());

                       
                    }
                    _TaamerProContext.SaveChanges();


                    //UpdatePayrollWithDiscountRewards(discountReward.EmployeeId.Value, UserId, BranchId);

                    var ms = "";
                    if (discountReward.Type == 1)
                    {
                        ms = "خصم";
                    }
                    else
                    {
                        ms = "مكافأة";
                    }
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل " + ms + " رقم " + discountReward.DiscountRewardId;
                     _SystemAction.SaveAction("SaveDiscountReward", "DiscountRewardService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.General_EditedSuccessfully };
                }
                
            }
            catch (Exception ex)
            {
                var ms = "";
                if (discountReward.Type == 1)
                {
                    ms = "خصم";
                }
                else
                {
                    ms = "مكافأة";
                }
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ "+ ms;
                 _SystemAction.SaveAction("SaveDiscountReward", "DiscountRewardService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        //private GeneralMessage UpdatePayrollWithDiscountRewards(int EmpId, int UserId, int BranchId) {
        //    var Emp = _EmployeesRepository.GetById(EmpId);
        //    var Payroll = _payrollMarchesRepository.GetPayrollMarches(EmpId, DateTime.Now.Month);

        //    if (Payroll != null && Payroll.PostDate.HasValue)
        //    { 
        //        //-----------------------------------------------------------------------------------------------------------------
        //        string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
        //        string ActionNote = "فشل في تعديل مسير الرواتب (المكافآت و الخصومات)) ";
        //         _SystemAction.SaveAction("UpdatePayrollWithDiscountRewards", "DiscountRewardService", 2, Resources.Posted_payroll_cannot_be_modified, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
        //        //-----------------------------------------------------------------------------------------------------------------
        //        return new GeneralMessage() { StatusCode = HttpStatusCode.BadRequest, Message= Resources.Posted_payroll_cannot_be_modified };
        //    }

        //    if (Emp != null && Payroll != null)
        //    {
        //        Payroll.TotalDiscounts = Emp.DiscountRewards.Where(s => s.IsDeleted == false && s.Type == 1 && s.AddDate.Value.Month == DateTime.Now.Month).Sum(s => s.Amount) ?? 0;
        //        Payroll.TotalRewards = Emp.DiscountRewards.Where(s => s.IsDeleted == false && s.Type == 2 && s.AddDate.Value.Month == DateTime.Now.Month).Sum(s => s.Amount) ?? 0;

        //        Payroll.UpdatedDate = DateTime.Now;
        //        Payroll.UpdateUser = UserId;

        //    }
        //    _TaamerProContext.SaveChanges();
        //    //-----------------------------------------------------------------------------------------------------------------
        //    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
        //    string ActionNote2 = "تعديل مسير الرواتب (المكافآت و الخصومات) ";
        //     _SystemAction.SaveAction("UpdatePayrollWithDiscountRewards", "DiscountRewardService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
        //    //-----------------------------------------------------------------------------------------------------------------

        //    return new GeneralMessage() { StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.General_SavedSuccessfully };
        //}
        public GeneralMessage DeleteDiscountReward(int DiscountRewardId,int UserId,int BranchId)
        {
            var ms = "";
            try
            {
                // DiscountReward discountReward = _DiscountRewardRepository.GetById(DiscountRewardId);
                DiscountReward? discountReward = _TaamerProContext.DiscountReward.Where(s => s.DiscountRewardId == DiscountRewardId).FirstOrDefault();


                var Payroll = _payrollMarchesRepository.GetPayrollMarches(discountReward.EmployeeId.Value, DateTime.Now.Month);

                if (Payroll != null &&
                    (discountReward.StartDate.HasValue && discountReward.StartDate <= DateTime.Now) &&
                    (!discountReward.EndDate.HasValue || ((discountReward.EndDate.HasValue && discountReward.EndDate >= DateTime.Now))))
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote2 = "فشل في تعديل مسير الرواتب (المكافآت و الخصومات)) ";
                     _SystemAction.SaveAction("UpdatePayrollWithDiscountRewards", "DiscountRewardService", 2, Resources.Posted_payroll_cannot_be_modified, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage() { StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.Posted_payroll_cannot_be_modified };
                }

                if (discountReward.Type == 1)
                {
                    ms = "خصم";
                }
                else
                {
                    ms = "مكافأة";
                }
                discountReward.IsDeleted = true;
                discountReward.DeleteDate = DateTime.Now;
                discountReward.DeleteUser = UserId;
                _TaamerProContext.SaveChanges();
               
               
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " حذف " + ms + " رقم " + DiscountRewardId;

                 _SystemAction.SaveAction("DeleteDiscountReward", "DiscountRewardService", 3, Resources.General_DeletedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK,ReasonPhrase = Resources.General_DeletedSuccessfully };
            }
            catch (Exception)
            {
                
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " فشل في حذف " + ms + " رقم " + DiscountRewardId;

                 _SystemAction.SaveAction("DeleteDiscountReward", "DiscountRewardService", 3, Resources.General_DeletedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest,ReasonPhrase = Resources.General_DeletedFailed };
            }
        }

    

    }
}
