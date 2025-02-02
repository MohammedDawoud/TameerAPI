using TaamerProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Repository.Interfaces;
using TaamerProject.Models.DBContext;

namespace TaamerProject.Repository.Repositories
{
    public class Acc_InvoicesRequestsRepository: IAcc_InvoicesRequestsRepository
    {
        private readonly TaamerProjectContext _TaamerProContext;

        public Acc_InvoicesRequestsRepository(TaamerProjectContext dataContext)
        {
            _TaamerProContext = dataContext;

        }

        public async Task<Acc_InvoicesRequestsVM> GetInvoiceReq(int InvoiceId)
        {
            Acc_InvoicesRequestsVM req = new Acc_InvoicesRequestsVM();

            try
            {
                var InvRequest = _TaamerProContext.Acc_InvoicesRequests.Where(s => s.InvoiceId == InvoiceId).Select(x => new Acc_InvoicesRequestsVM
                {
                    InvoiceReqId = x.InvoiceReqId,
                    InvoiceId = x.InvoiceId,
                    InvoiceNoRequest = x.InvoiceNoRequest,
                    IsSent = x.IsSent,
                    StatusCode = x.StatusCode,
                    SendingStatus = x.SendingStatus,
                    warningmessage = x.warningmessage,
                    ClearedInvoice = x.ClearedInvoice,
                    errormessage = x.errormessage,
                    InvoiceHash = x.InvoiceHash,
                    SingedXML = x.SingedXML,
                    EncodedInvoice = x.EncodedInvoice,
                    ZatcaUUID = x.ZatcaUUID,
                    QRCode = x.QRCode,
                    PIH = x.PIH,
                    SingedXMLFileName = x.SingedXMLFileName,
                    BranchId = x.BranchId,

                }).FirstOrDefault();
                return InvRequest;
            }
            catch (Exception ex)
            {
                return req;

            }
        }
    }
}
