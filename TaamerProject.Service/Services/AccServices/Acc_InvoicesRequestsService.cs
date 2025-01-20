using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using TaamerProject.Models;
using TaamerProject.Models.Common.FIlterModels;
using TaamerProject.Models.Common;
using TaamerProject.Service.Interfaces;
using TaamerProject.Repository.Interfaces;
using TaamerProject.Models.DBContext;
using TaamerProject.Service.Generic;
using TaamerProject.Service.IGeneric;
using Twilio.Base;
using TaamerProject.Service.LocalResources;
using TaamerProject.Repository.Repositories;

namespace TaamerProject.Service.Services
{
    public class Acc_InvoicesRequestsService: IAcc_InvoicesRequestsService
    {
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly ISystemAction _SystemAction;

        private readonly IAcc_InvoicesRequestsRepository _Acc_InvoicesRequestsRepository;
        public Acc_InvoicesRequestsService(IAcc_InvoicesRequestsRepository acc_InvoicesRequestsRepository
    , TaamerProjectContext dataContext
    , ISystemAction systemAction)
        {
            _TaamerProContext = dataContext;
            _SystemAction = systemAction;
            _Acc_InvoicesRequestsRepository = acc_InvoicesRequestsRepository;
        }
        public Task<Acc_InvoicesRequestsVM> GetInvoiceReq(int InvoiceId)
        {
            var Request = _Acc_InvoicesRequestsRepository.GetInvoiceReq(InvoiceId);
            return Request;
        }


        public GeneralMessage SaveInvoicesRequest(int InvoiceReqId, int InvoiceId, string InvoiceHash, string SingedXML, string EncodedInvoice
            , string ZatcaUUID, string QRCode, string PIH, string SingedXMLFileName, int InvoiceNoRequest
            , bool IsSent,int? StatusCode, string? SendingStatus, string? warningmessage, string? ClearedInvoice, string? errormessage)
        {
            try
            {
                Acc_InvoicesRequests InvoicesRequest = new Acc_InvoicesRequests();
                InvoicesRequest.InvoiceReqId = InvoiceReqId;
                if(InvoiceId!=0){InvoicesRequest.InvoiceId = InvoiceId;}             
                InvoicesRequest.InvoiceHash = InvoiceHash;
                InvoicesRequest.SingedXML = SingedXML;
                InvoicesRequest.EncodedInvoice = EncodedInvoice;
                InvoicesRequest.ZatcaUUID = ZatcaUUID;
                if (QRCode != null){InvoicesRequest.QRCode = QRCode;}
                InvoicesRequest.PIH = PIH;
                InvoicesRequest.SingedXMLFileName = SingedXMLFileName;
                if (InvoiceNoRequest != 0) { InvoicesRequest.InvoiceNoRequest = InvoiceNoRequest; }
                InvoicesRequest.IsSent = IsSent;
                InvoicesRequest.StatusCode = StatusCode;
                InvoicesRequest.SendingStatus = SendingStatus;
                InvoicesRequest.warningmessage = warningmessage;
                InvoicesRequest.ClearedInvoice = ClearedInvoice;
                InvoicesRequest.errormessage = errormessage;



                if (InvoicesRequest.InvoiceReqId == 0)
                {
                    _TaamerProContext.Acc_InvoicesRequests.Add(InvoicesRequest);
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "حفظ بيانات الفاتورة";
                    _SystemAction.SaveAction("SaveInvoicesRequest", "Acc_InvoicesRequestsService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, 1, 1, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully,ReturnedParm= InvoicesRequest.InvoiceReqId };

                }
                else
                {
                    var reqUpdated = _TaamerProContext.Acc_InvoicesRequests.Where(s => s.InvoiceReqId == InvoicesRequest.InvoiceReqId).FirstOrDefault();

                    if (reqUpdated != null)
                    {
                        reqUpdated.IsSent = InvoicesRequest.IsSent;
                        reqUpdated.StatusCode = InvoicesRequest.StatusCode;
                        reqUpdated.SendingStatus = InvoicesRequest.SendingStatus;
                        reqUpdated.warningmessage = InvoicesRequest.warningmessage;
                        reqUpdated.ClearedInvoice = InvoicesRequest.ClearedInvoice;
                        reqUpdated.errormessage = InvoicesRequest.errormessage;
                        if (InvoicesRequest.QRCode != null)
                        {
                            reqUpdated.QRCode = InvoicesRequest.QRCode;
                        }
                    }

                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل بيانات الفاتورة رقم " + InvoicesRequest.InvoiceReqId;
                    _SystemAction.SaveAction("SaveInvoicesRequest", "Acc_InvoicesRequestsService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, 1, 1, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = InvoicesRequest.InvoiceReqId };
                }

            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ بيانات الفاتورة";
                _SystemAction.SaveAction("SaveInvoicesRequest", "Acc_InvoicesRequestsService", 1, Resources.General_SavedFailed, "", "", ActionDate, 1, 1, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed, ReturnedParm = 0};
            }
        }
    }
}
