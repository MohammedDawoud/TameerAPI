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

    public class SuppliersController : ControllerBase
    {
        private IAcc_SuppliersService _Acc_SuppliersService;
        private readonly IFiscalyearsService _FiscalyearsService;
        public GlobalShared _globalshared;
        public SuppliersController(IAcc_SuppliersService acc_SuppliersService, IFiscalyearsService fiscalyearsService)
        {
            _Acc_SuppliersService = acc_SuppliersService;
            _FiscalyearsService = fiscalyearsService;
            HttpContext httpContext = HttpContext;_globalshared = new GlobalShared(httpContext);
        }
        [HttpGet("GetAllSuppliers")]

        public IActionResult GetAllSuppliers(string? SearchText)
        {
            return Ok(_Acc_SuppliersService.GetAllSuppliers(SearchText??"").Result);
        }
        [HttpPost("SaveSupplier")]

        public IActionResult SaveSupplier(Acc_Suppliers Supplier)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _Acc_SuppliersService.SaveSupplier(Supplier, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpPost("DeleteSupplier")]
        public IActionResult DeleteSupplier(int SupplierId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var result = _Acc_SuppliersService.DeleteSupplier(SupplierId, _globalshared.UserId_G, _globalshared.BranchId_G);
            return Ok(result);
        }
        [HttpGet("FillSuppliersSelect")]

        public IActionResult FillSuppliersSelect(string? SearchText)
        {
            return Ok(_Acc_SuppliersService.GetAllSuppliers(SearchText??"").Result.Select(s => new {
                Id = s.SupplierId,
                Name = s.NameAr
            }));
        }

        [HttpGet("FillSuppliersSelect2")]
        public IActionResult FillSuppliersSelect2()
        {
            return Ok(_Acc_SuppliersService.GetAllSuppliers("").Result.Select(s => new {
                Id = s.SupplierId,
                Name = s.NameAr
            }));
        }
        [HttpGet("FillSuppliersAllNotiSelect")]
        public IActionResult FillSuppliersAllNotiSelect2()
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            return Ok(_Acc_SuppliersService.GetAllSuppliersAllNoti("", _globalshared.Lang_G, _globalshared.BranchId_G, _globalshared.YearId_G).Select(s => new {
                Id = s.SupplierId,
                Name = s.NameAr
            }));
        }
        [HttpGet("GetTaxNoBySuppId")]

        public IActionResult GetTaxNoBySuppId(int SupplierId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var Accounts = _Acc_SuppliersService.GetTaxNoBySuppId(SupplierId, _globalshared.Lang_G, _globalshared.BranchId_G);
            return Ok(Accounts);
        }
        [HttpGet("GetAccIdBySuppId")]

        public IActionResult GetAccIdBySuppId(int SupplierId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var Accounts = _Acc_SuppliersService.GetAccIdBySuppId(SupplierId, _globalshared.Lang_G, _globalshared.BranchId_G);
            return Ok(Accounts);
        }
        [HttpGet("GetSuppIdByAccId")]

        public IActionResult GetSuppIdByAccId(int AccountId)
        {
            HttpContext httpContext = HttpContext; _globalshared = new GlobalShared(httpContext);
            var Accounts = _Acc_SuppliersService.GetSuppIdByAccId(AccountId, _globalshared.Lang_G, _globalshared.BranchId_G);
            return Ok(Accounts);
        }


    }
}
