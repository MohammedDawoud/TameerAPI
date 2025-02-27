﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models;
using TaamerProject.Models.Common.FIlterModels;
using TaamerProject.Models.Common;
using TaamerProject.Service.Interfaces;
using TaamerProject.Repository.Interfaces;
using TaamerProject.Models.DBContext;
using TaamerProject.Service.Generic;
using TaamerProject.Repository.Repositories;
using System.Net;
using TaamerProject.Service.IGeneric;
using System.Runtime.CompilerServices;
using System.Resources;
using System.Data;
using System.Data.SqlClient;
using TaamerProject.Service.LocalResources;
using static TaamerProject.Models.ReportGridVM;
using System.Text.RegularExpressions;
using System.Collections;
using System.Runtime.InteropServices.JavaScript;
using Twilio.Types;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.TwiML.Voice;
using System.Text.Json;
using System.Net.Http.Headers;
using RestSharp;
using static System.Net.WebRequestMethods;
using Google.Apis.Auth.OAuth2;
using static Dropbox.Api.TeamLog.LoginMethod;
using Newtonsoft.Json.Linq;

namespace TaamerProject.Service.Services
{
    public class VoucherService : IVoucherService
    {
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly ISystemAction _SystemAction;
        private readonly IVoucherDetailsRepository _VoucherDetailsRepository;
        private readonly IInvoicesRepository _InvoicesRepository;
        private readonly ITransactionsRepository _TransactionsRepository;
        private readonly IAccountsRepository _AccountsRepository;
        private readonly IJournalsRepository _JournalsRepository;
        private readonly IFiscalyearsRepository _fiscalyearsRepository;
        private readonly ICostCenterRepository _CostCenterRepository;
        private readonly ICustomerRepository _CustomerRepository;
        private readonly ICustomerPaymentsRepository _CustomerPaymentsRepository;
        private readonly IBranchesRepository _BranchesRepository;
        private readonly IServicesPriceServiceRepository _ServicesPriceService;
        private readonly ICustodyRepository _CustodyRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IAcc_CategoriesRepository _Acc_CategoriesRepository;
        private readonly ISMSSettingsRepository _sMSSettingsRepository;




        public VoucherService(TaamerProjectContext dataContext, ISystemAction systemAction, IVoucherDetailsRepository voucherDetailsRepository,
            IInvoicesRepository invoicesRepository , ITransactionsRepository transactionsRepository, IAccountsRepository accountsRepository,
            IJournalsRepository journalsRepository, IFiscalyearsRepository fiscalyearsRepository ,ICostCenterRepository costCenterRepository,
            ICustomerRepository customerRepository ,ICustomerPaymentsRepository customerPaymentsRepository, IBranchesRepository branchesRepository,
            IServicesPriceServiceRepository servicesPriceService, ICustodyRepository custodyRepository ,IProjectRepository projectRepository,
             IAcc_CategoriesRepository acc_CategoriesRepository, ISMSSettingsRepository SMSSettingsRepository)
        {
            _InvoicesRepository = invoicesRepository;
            _VoucherDetailsRepository = voucherDetailsRepository;
            _TransactionsRepository = transactionsRepository;
            _AccountsRepository = accountsRepository;
            _JournalsRepository = journalsRepository;
            _fiscalyearsRepository = fiscalyearsRepository;
            _CostCenterRepository = costCenterRepository;
            _CustomerRepository = customerRepository;
            _CustomerPaymentsRepository = customerPaymentsRepository;
            _BranchesRepository = branchesRepository;
            _SystemAction = systemAction;
            _ServicesPriceService = servicesPriceService;
            _CustodyRepository = custodyRepository;
            _projectRepository = projectRepository;
            _Acc_CategoriesRepository = acc_CategoriesRepository;
            _TaamerProContext = dataContext;
            _SystemAction = systemAction;
            _sMSSettingsRepository = SMSSettingsRepository;

        }


        public static string ConvertToWord_NEW(string Num)
        {
            CurrencyInfo _currencyInfo = new CurrencyInfo(CurrencyInfo.Currencies.SaudiArabia);
            ToWord toWord = new ToWord(Convert.ToDecimal(Num), _currencyInfo);
            return toWord.ConvertToArabic();
        }
        public Task<IEnumerable<InvoicesVM>> GetAllVouchersback()
        {      
            return _InvoicesRepository.GetAllVoucherstoback();         //voucher
        }

        public async Task<IEnumerable<InvoicesVM>> GetAllVouchers(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                if (voucherFilterVM.IsSearch??false)
                {
                    return await _InvoicesRepository.GetAllVouchersBySearch(voucherFilterVM, yearid ?? default(int), BranchId);
                }
               return await _InvoicesRepository.GetAllVouchers(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            
           return new List<InvoicesVM>();
        }

        public GeneralMessage SendWInvoice(int InvoiceId, int UserId, int BranchId, string AttachmentFile, string environmentURL,string fileTypeUpload)
        {
            try
            {
                //2 ynf3o
                var Invoice = _InvoicesRepository.GetInvById(InvoiceId).Result; 
                //var Invoice = _InvoicesRepository.GetVoucherById(InvoiceId).Result;

               
                bool Check = false;

                if (Invoice.CustomerPhone != "" && Invoice.CustomerPhone != null)
                {
                    Check = SendWhatsAppInvoice(Invoice, Invoice.CustomerPhone, BranchId, UserId, AttachmentFile, environmentURL, fileTypeUpload);
                    //Check = SendWhatsAppInvoice3(Invoice, Invoice.CustomerPhone, BranchId, UserId, AttachmentFile, environmentURL, fileTypeUpload);
                }
                else
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " فشل في ارسال فاتورة رقم " + InvoiceId; ;
                    _SystemAction.SaveAction("SendWInvoice", "VoucherService", 1, "فشل في الارسال لا يوجد رقم واتس اب للعميل", "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.WhatsAppNumberCustomer };
                }
                if (Check == true)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " ارسال فاتورة رقم " + InvoiceId;
                    _SystemAction.SaveAction("SendWInvoice", "VoucherService", 1, Resources.sent_succesfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.sent_succesfully };
                }
                else
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " فشل في ارسال فاتورة رقم " + InvoiceId; ;
                    _SystemAction.SaveAction("SendWInvoice", "VoucherService", 1, Resources.FailedSendCheckSettings, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.FailedSendCheckSettings };
                }


            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " فشل في ارسال فاتورة رقم " + InvoiceId; ;
                _SystemAction.SaveAction("SendWInvoice", "VoucherService", 1, Resources.FailedSendCheckSettings, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.FailedSendCheckSettings };
            }

        }


        public bool SendWhatsAppInvoice(InvoicesVM InvoicesVM, string ToPhone, int BranchId, int UserId, string AttachmentFile,string environmentURL,string fileTypeUpload)
        {
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; //TLS 1.2
            try
            {
                var accountSid = "AC21b75a6b7a6020096317985cb7fc0781";
                var authToken = "51083bb163926b234a2e39cd1cca4229";
                //string accountSid = Environment.GetEnvironmentVariable(apiLink);
                //string authToken = Environment.GetEnvironmentVariable(apiKey);
                TwilioClient.Init(accountSid, authToken);

                var messageOptions = new CreateMessageOptions(
                new PhoneNumber("whatsapp:+201020412606"));


                //new PhoneNumber("whatsapp:+966503326610"));
                //new PhoneNumber("whatsapp:" + "+966" + ToPhone));

                messageOptions.From = new PhoneNumber("whatsapp:+14155238886");
                //messageOptions.From = new PhoneNumber("whatsapp:" + "+" + FromPhone);

                messageOptions.Body = "Message From " + "dawoud";
                //Uri uri = new Uri("https://tameercloud.com/Uploads/Attachment/EmpIdentity_637658432120598472.pdf");
                //var Attachment = System.IO.Path.Combine(AttachmentFile);
                //Uri uri = new Uri("/TempFiles/PDFFile_638333565529630966.pdf");

                //Uri uri = new Uri("https://api.tameercloud.com/TempFiles/NewInvoice.pdf");
                Uri uri = new Uri("https://api.tameercloud.com/TempFiles/OldInvoice.pdf");

                //Uri uri = new Uri(AttachmentFile);

                //Uri uri = new Uri(AttachmentFile);
                messageOptions.MediaUrl.Add(uri);

                var message = MessageResource.Create(messageOptions);
                Console.WriteLine(message.Body);
                GeneralMessage msg = new GeneralMessage();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool SendWhatsAppInvoice4(InvoicesVM InvoicesVM, string ToPhone, int BranchId, int UserId, string AttachmentFile, string environmentURL, string fileTypeUpload)
        {
            try
            {
                    string INSTANCE_ID = "YOUR_INSTANCE_ID";
                    string CLIENT_ID = "YOUR_CLIENT_ID_HERE";
                    string CLIENT_SECRET = "YOUR_CLIENT_SECRET_HERE";
                    string DOCUMENT_SINGLE_API_URL = "https://api.whatsmate.net/v3/whatsapp/single/document/message/" + INSTANCE_ID;
                    //var File= "https://api.tameercloud.com/TempFiles/OldInvoice.pdf";
                    var File = "https://api.tameercloud.com/TempFiles/NewInvoice.pdf";
                    // TODO: Put down your recipient's number (e.g. your own cell phone number)
                    string recipient = "12025550105";
                    // TODO: Remember to copy the PDF from ..\assets to the TEMP directory!
                    string base64Content = convertFileToBase64(File);
                    string fn = "anyname.pdf";
                    string caption = "You should find the map handy.";  // optional; can be empty

                    sendDocument(recipient, base64Content, fn, caption, DOCUMENT_SINGLE_API_URL, CLIENT_ID, CLIENT_SECRET);

                    Console.WriteLine("Press Enter to exit.");
                    Console.ReadLine();





                ////var File= "https://api.tameercloud.com/TempFiles/OldInvoice.pdf";
                //var File = "https://api.tameercloud.com/TempFiles/NewInvoice.pdf";
                //var File2 = environmentURL + "/" + AttachmentFile;

                //string instanceId = "instance85270"; // your instanceId
                //string token = "749v41rrvo6cs27f";         //instance Token
                //string mobile = "+20" + "1020412606";
                ////string mobile = "+20" + "1012325454";
                //string mobile2 = "+20" + ToPhone;

                //var url = "https://api.ultramsg.com/" + instanceId + "/messages/document";
                //var client = new RestClient(url);
                //var request = new RestRequest(url, Method.Post);
                //request.AddHeader("content-type", "application/x-www-form-urlencoded");
                //request.AddParameter("token", token);
                //request.AddParameter("to", mobile);
                //request.AddParameter("filename", "test.pdf");
                //request.AddParameter("document", File);
                //request.AddParameter("caption", "فاتورة ضريبية \r\n شكرا لك");


                //RestResponse response = client.ExecuteAsync(request).Result;
                //var output = response.Content;
                //Console.WriteLine(output);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        static public string convertFileToBase64(string fullPathToDoc)
        {
            Byte[] bytes = System.IO.File.ReadAllBytes(fullPathToDoc);
            String base64Encoded = Convert.ToBase64String(bytes);
            return base64Encoded;
        }

        public bool sendDocument(string number, string base64Content, string fn, string caption,string DOCUMENT_SINGLE_API_URL,string CLIENT_ID,string CLIENT_SECRET)
        {
            bool success = true;

            try
            {
                HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(DOCUMENT_SINGLE_API_URL);
                httpRequest.Method = "POST";
                httpRequest.Accept = "application/json";
                httpRequest.ContentType = "application/json";
                httpRequest.Headers["X-WM-CLIENT-ID"] = CLIENT_ID;
                httpRequest.Headers["X-WM-CLIENT-SECRET"] = CLIENT_SECRET;

                SingleDocPayload payloadObj = new SingleDocPayload() { number = number, caption = caption, document = base64Content, filename = fn };
                string postData = JsonSerializer.Serialize(payloadObj);

                using (var streamWriter = new StreamWriter(httpRequest.GetRequestStream()))
                {
                    streamWriter.Write(postData);
                }

                var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    Console.WriteLine(result);
                }
            }
            catch (WebException webExcp)
            {
                Console.WriteLine("A WebException has been caught.");
                Console.WriteLine(webExcp.ToString());
                WebExceptionStatus status = webExcp.Status;
                if (status == WebExceptionStatus.ProtocolError)
                {
                    Console.Write("The REST API server returned a protocol error: ");
                    HttpWebResponse? httpResponse = webExcp.Response as HttpWebResponse;
                    System.IO.Stream stream = httpResponse.GetResponseStream();
                    StreamReader reader = new StreamReader(stream);
                    String body = reader.ReadToEnd();
                    Console.WriteLine((int)httpResponse.StatusCode + " - " + body);
                    success = false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("A general exception has been caught.");
                Console.WriteLine(e.ToString());
                success = false;
            }


            return success;
        }

        public class SingleDocPayload
        {
            public string number { get; set; }
            public string caption { get; set; }
            public string document { get; set; }
            public string filename { get; set; }
        }

        public bool SendWhatsAppInvoice3(InvoicesVM InvoicesVM, string ToPhone, int BranchId, int UserId, string AttachmentFile, string environmentURL, string fileTypeUpload)
        {
            try
            {
                //var File= "https://api.tameercloud.com/TempFiles/OldInvoice.pdf";
                var File = "https://api.tameercloud.com/TempFiles/NewInvoice.pdf";
                var File2 = environmentURL+"/" + AttachmentFile;

                string instanceId = "instance85270"; // your instanceId
                string token = "749v41rrvo6cs27f";         //instance Token
                string mobile = "+20" + "1020412606";
                //string mobile = "+20" + "1012325454";
                string mobile2 = "+20" + ToPhone;

                var url = "https://api.ultramsg.com/" + instanceId + "/messages/document";
                var client = new RestClient(url);
                var request = new RestRequest(url, Method.Post);
                request.AddHeader("content-type", "application/x-www-form-urlencoded");
                request.AddParameter("token", token);
                request.AddParameter("to", mobile);
                request.AddParameter("filename", "test.pdf");
                request.AddParameter("document", File);
                request.AddParameter("caption", "فاتورة ضريبية \r\n شكرا لك");


                RestResponse response = client.ExecuteAsync(request).Result;
                var output = response.Content;
                Console.WriteLine(output);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
           
        }



        public bool SendWhatsAppInvoice2(InvoicesVM InvoicesVM, string ToPhone, int BranchId, int UserId, string AttachmentFile, string environmentURL, string fileTypeUpload)
        {
            var documentUrl = "https://api.tameercloud.com/TempFiles/OldInvoice.pdf";
            //Uri documentUrl = new Uri("https://api.tameercloud.com/TempFiles/OldInvoice.pdf");
            var ToPhone2 = "+201020412606";
            var requestBody = new
            {
                messaging_product = "whatsapp",
                to = ToPhone2,
                type = "document",
                document = new
                {
                    link = documentUrl,
                    filename = "yourfile.pdf"
                }
            };
            //ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; //TLS 1.2
            System.Net.ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");


            //var formContent = new FormUrlEncodedContent(new[]
            //{
            //new KeyValuePair<string, string>("mobile_numbers", ToPhone2),
            //new KeyValuePair<string, string>("message", "Whoo hahahahah")
            //});


            HttpClient client = new HttpClient();
            //client.BaseAddress = new Uri("https://tameercloud.com");

            client.BaseAddress = new Uri("https://graph.facebook.com/v13.0/{your-whatsapp-business-account-id}/messages");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "134134134");
            //HttpResponseMessage response = client.PostAsync("iwin/api/v1/messages", formContent).Result;


            HttpResponseMessage response = client.PostAsync("/v1/messages", content).Result;

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage =  response.Content.ReadAsStringAsync();
                Console.WriteLine($"Failed to send message: {errorMessage}");
                return false;
            }
            else
            {
                Console.WriteLine("Message sent successfully!");
                return true;

            }
        }

        public async Task<IEnumerable<InvoicesVM>> GetAllVouchersLastMonth(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                if (voucherFilterVM.IsSearch??false)
                {
                    return await _InvoicesRepository.GetAllVouchersBySearch(voucherFilterVM, yearid ?? default(int), BranchId);
                }
                return await _InvoicesRepository.GetAllVouchersLastMonth(voucherFilterVM, yearid ?? default(int), BranchId);

            }

            return new List<InvoicesVM>();
        }
        public async Task<IEnumerable<InvoicesVM>> GetAllVouchersSearch(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetAllVouchersSearch(voucherFilterVM, yearid ?? default(int), BranchId);
            }

            return new List<InvoicesVM>();
        }
        public async Task<IEnumerable<InvoicesVM>> GetAllVouchersSearchCustomer(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await  _InvoicesRepository.GetAllVouchersSearchCustomer(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }
        public async Task<InvoicesVM> GetVouchersSearchInvoiceByID(int InvoiceId, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetVouchersSearchInvoiceByID(InvoiceId, yearid ?? default(int), BranchId);
            }

            return new InvoicesVM();
        }
        public async Task<InvoicesVM> GetVouchersSearchInvoicePurchaseByID(int InvoiceId, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetVouchersSearchInvoicePurchaseByID(InvoiceId, yearid ?? default(int), BranchId);
            }

            return new InvoicesVM();
        }
        public async Task<IEnumerable<InvoicesVM>> GetAllVouchersPurchase(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                if (voucherFilterVM.IsSearch??false)
                {
                    return await _InvoicesRepository.GetAllVouchersBySearchPurchase(voucherFilterVM, yearid ?? default(int), BranchId);
                }
                return await _InvoicesRepository.GetAllVouchersPurchase(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }

        public async Task<IEnumerable<InvoicesVM>> GetAllAlarmVoucher(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetAllAlarmVoucher(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }
        public async Task<IEnumerable<InvoicesVM>> GetAllNotioucher(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetAllNotioucher(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }

        public async Task<IEnumerable<InvoicesVM>> GetAllVouchersRetSearch(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetAllVouchersRetSearch(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }

        public async Task<IEnumerable<InvoicesVM>> GetAllVouchersfromcontractSearch(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetAllVouchersfromcontractSearch(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }


        public async Task<IEnumerable<InvoicesVM>> GetAllVouchersRetSearchPurchase(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetAllVouchersRetSearchPurchase(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }

        public async Task<IEnumerable<InvoicesVM>> GetAllVouchersQR(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            //var year = _fiscalyearsRepository.GetCurrentYear();
            if (yearid != null)
            {
                if (voucherFilterVM.IsSearch??false)
                {
                    return await _InvoicesRepository.GetAllVouchersBySearchQR(voucherFilterVM, yearid ?? default(int), BranchId);
                }
                return await _InvoicesRepository.GetAllVouchers(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }
        public async Task<IEnumerable<InvoicesVM>> GetAllVouchersProject( int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetAllVouchersProject( yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }
        public async Task<IEnumerable<InvoicesVM>> GetAllVouchersRet(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetAllVouchersRet(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }
        public async Task<IEnumerable<InvoicesVM>> GetAllVouchersRetPurchase(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetAllVouchersRetPurchase(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }
        public async Task<IEnumerable<InvoicesVM>> GetAllCreditDepitNotiReport(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                if(voucherFilterVM.Type==2)
                {
                    voucherFilterVM.SupplierId = null;
                    return await _InvoicesRepository.GetAllCreditDepitNotiReport(voucherFilterVM, yearid ?? default(int), BranchId);
                }
                else
                {
                    voucherFilterVM.CustomerId = null;
                    voucherFilterVM.ProjectId = null;
                    return await _InvoicesRepository.GetAllCreditDepitNotiReport_Pur(voucherFilterVM, yearid ?? default(int), BranchId);
                }
            }
            return new List<InvoicesVM>();
        }
        public async Task<IEnumerable<InvoicesVM>> GetAllVouchersRetReport(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetAllVouchersRetReport(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }
        public async Task<IEnumerable<InvoicesVM>> GetAllVouchersRetReport_Pur(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetAllVouchersRetReport_Pur(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }
        public async Task<IEnumerable<InvoicesVM>> GetAllPayVouchersRet(VoucherFilterVM voucherFilterVM, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetAllPayVouchersRet(voucherFilterVM, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }

        public async Task<IEnumerable<InvoicesVM>> GetVoucherRpt(int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetVoucherRpt(yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }
        public async Task<IEnumerable<InvoicesVM>> GetCustRevenueExpensesDetails(string FromDate, string ToDate, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _InvoicesRepository.GetCustRevenueExpensesDetails(FromDate, ToDate, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }

        public Task<IEnumerable<VoucherDetailsVM>> GetAllDetailsByVoucherId(int? voucherId)
        {
            return _VoucherDetailsRepository.GetAllDetailsByVoucherId(voucherId);
        }
        public Task<VoucherDetailsVM> GetInvoiceIDByProjectID(int? ProjectId)
        {
            return _VoucherDetailsRepository.GetInvoiceIDByProjectID(ProjectId);
        }
        public Task<IEnumerable<VoucherDetailsVM>> GetAllDetailsByInvoiceId(int? voucherId)
        {
            return _VoucherDetailsRepository.GetAllDetailsByInvoiceId(voucherId);
        }
        public Task<VoucherDetailsVM> GetAllDetailsByVoucherDetailsId(int? VoucherDetailsId)
        {
            return _VoucherDetailsRepository.GetAllDetailsByVoucherDetailsId(VoucherDetailsId);
        }
        public Task<IEnumerable<VoucherDetailsVM>> GetAllDetailsByInvoiceIdFirstOrDef(int? voucherId)
        {
            return _VoucherDetailsRepository.GetAllDetailsByInvoiceIdFirstOrDef(voucherId);
        }
        public Task<IEnumerable<VoucherDetailsVM>> GetAllDetailsByInvoiceIdPurchase(int? voucherId)
        {
            return _VoucherDetailsRepository.GetAllDetailsByInvoiceIdPurchase(voucherId);
        }
        public Task<Invoices> MaxVoucherP(int BranchId, int? yearid)
        {
            return _InvoicesRepository.MaxVoucherP(BranchId, yearid);
        }
        public Task<IEnumerable<VoucherDetailsVM>> GetAllTransByLineNo(int LineNo)
        {
            return _VoucherDetailsRepository.GetAllTransByLineNo(LineNo);
        }

        public Task<IEnumerable<VoucherDetailsVM>> GetAllTrans(int VouDetailsID)
        {
            return _VoucherDetailsRepository.GetAllTrans(VouDetailsID);
        }

        //public GeneralMessage UpdateInvoiceWithZatcaData(Invoices voucher, int UserId, int BranchId)
        //{
        //    try
        //    {
        //        // var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
        //        Invoices? VoucherUpdated = _TaamerProContext.Invoices.Where(s => s.InvoiceId == voucher.InvoiceId).FirstOrDefault();
        //        if (VoucherUpdated != null)
        //        {
        //            VoucherUpdated.InvoiceHash = voucher.InvoiceHash;
        //            //VoucherUpdated.SingedXML = voucher.SingedXML;
        //            VoucherUpdated.EncodedInvoice = voucher.EncodedInvoice;
        //            VoucherUpdated.ZatcaUUID = voucher.ZatcaUUID;
        //            VoucherUpdated.QRCode = voucher.QRCode;
        //            VoucherUpdated.PIH = voucher.PIH;
        //            VoucherUpdated.SingedXMLFileName = voucher.SingedXMLFileName;

        //            _TaamerProContext.SaveChanges();

        //            //-----------------------------------------------------------------------------------------------------------------
        //            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
        //            string ActionNote2 = " تم تعديل الفاتورة";
        //            _SystemAction.SaveAction("UpdateInvoiceWithZatcaData", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
        //            //-----------------------------------------------------------------------------------------------------------------

        //        }

        //        return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
        //    }
        //    catch (Exception ex)
        //    {
        //        //-----------------------------------------------------------------------------------------------------------------
        //        string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
        //        string ActionNote = "فشل في تعديل الفاتورة";
        //        _SystemAction.SaveAction("UpdateInvoiceWithZatcaData", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
        //        //-----------------------------------------------------------------------------------------------------------------

        //        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
        //    }

        //}
        public GeneralMessage SaveVoucher(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                voucher.TransactionDetails = new List<Transactions>();
                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("SaveVoucher", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }

                voucher.InvoiceValueText=ConvertToWord_NEW(voucher?.InvoiceValue?.ToString()??"");


                if (voucher?.InvoiceId == 0)
                {

                    voucher.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = voucher?.VoucherDetails?.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                    voucher.YearId = yearid;
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.AddUser = UserId;
                    voucher.BranchId = BranchId;
                    voucher.AddDate = DateTime.Now;
                    voucher.StoreId = voucher.StoreId;
                    voucher.DunCalc = false;
                    voucher.printBankAccount = false;


                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();      
                    }

                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();
                    foreach (var item in voucher?.VoucherDetails?.ToList())
                    {
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AddUser = UserId;
                        item.PayType = voucher.PayType;
                        item.AddDate = DateTime.Now;
                        decimal? Depit = item.TotalAmount;
                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        item.TFK = ConvertToWord_NEW(Depit?.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        _TaamerProContext.VoucherDetails.Add(item);
                        
                        string vouchertypename = voucher.Type == 6 ? "سند قبض" : "سند صرف";
                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 6)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucher", "VoucherService", 1, Resources.ChooseReceiptaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.ChooseReceiptaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 5)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucher", "VoucherService", 1, Resources.Chooseexchangeaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.Chooseexchangeaccount };
                        }

                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 2)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucher", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount};
                        }


                        //item.CostCenterId = Convert.ToInt32(voucher.CostCenterId) > 0 ? voucher.CostCenterId : item.CostCenterId;

                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }


                        //depit 
                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = voucher.Type == 6 ? item.ToAccountId : item.AccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== (voucher.Type == 6 ?  item.ToAccountId : item.AccountId))?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = Depit,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename +" "+ "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        //credit 
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = voucher.Type == 6 ? item.AccountId : item.ToAccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (voucher.Type == 6 ? item.AccountId : item.ToAccountId))?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = Depit,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                    }
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);
                    
                    _TaamerProContext.SaveChanges();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 6 && s.YearId == yearid && s.InvoiceNumber == voucher.InvoiceNumber && s.InvoiceValue == voucher.InvoiceValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransaDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucDetails);
                        if (TransaDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransaDetails);
                    }
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة سند جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveVoucher", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId };
                }
                else
                {
                    //var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();

                   
                    if (VoucherUpdated != null)
                    {
                        if (VoucherUpdated.IsPost == true)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucher", "VoucherService", 2, Resources.canteditevoucher, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.canteditevoucher };
                        }

                        VoucherUpdated.Notes = voucher.Notes??"";
                        VoucherUpdated.InvoiceNotes = voucher.InvoiceNotes;
                        VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        VoucherUpdated.RecevierTxt = voucher.RecevierTxt;
                        VoucherUpdated.Date = voucher.Date;
                        VoucherUpdated.HijriDate = voucher.HijriDate;
                        VoucherUpdated.PayType = voucher.PayType;
                        VoucherUpdated.InvoiceReference = voucher.InvoiceReference;
                        VoucherUpdated.CustomerId = voucher.CustomerId;
                        VoucherUpdated.JournalNumber = voucher.JournalNumber;
                        VoucherUpdated.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                        VoucherUpdated.TaxAmount = voucher.TaxAmount;
                        VoucherUpdated.InvoiceValueText = voucher.InvoiceValueText;
                        VoucherUpdated.IsTax = voucher?.VoucherDetails?.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                        VoucherUpdated.BranchId = BranchId;
                        VoucherUpdated.UpdateDate = DateTime.Now;
                        VoucherUpdated.UpdateUser = UserId;
                        VoucherUpdated.ToAccountId = voucher.ToAccountId;
                    }
                    //delete existing details 
                    var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                    _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                    _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    
                    // add new details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.VoucherDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تكرار الحساب مع مركز التكلفة" };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.InvoiceId = voucher.InvoiceId;
                        item.PayType = voucher.PayType;
                        item.AddUser = UserId;
                        item.AddDate = DateTime.Now;
                        decimal? Depit = item.TotalAmount;
                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        //item.TFK = utilDetails.GetNumberAr();
                        // decimal Credit = item.TotalAmount;
                        _TaamerProContext.VoucherDetails.Add(item);
                        //// add transaction



                        string vouchertypename = voucher.Type == 6 ? "سند قبض" : "سند صرف";
                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 6)
                        { 
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucher", "VoucherService", 1, Resources.ChooseReceiptaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.ChooseReceiptaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 5)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucher", "VoucherService", 1, Resources.Chooseexchangeaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.Chooseexchangeaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 2)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucher", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }
                        //  _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                        //depit 


                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }


                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = VoucherUpdated.Type == 6 ? item.ToAccountId : item.AccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (VoucherUpdated.Type == 6 ? item.ToAccountId : item.AccountId))?.FirstOrDefault()?.Type,
                            Type = VoucherUpdated.Type,
                            LineNumber = 1,
                            Depit = Depit,
                            Credit = 0,


                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = item.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        //credit 
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = VoucherUpdated.Type == 6 ? item.AccountId : item.ToAccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (VoucherUpdated.Type == 6 ? item.ToAccountId : item.AccountId))?.FirstOrDefault()?.Type,
                            Type = VoucherUpdated.Type,
                            LineNumber = 2,
                            Credit = Depit,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = item.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                    }
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                    _TaamerProContext.SaveChanges();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 6 && s.YearId == yearid && s.InvoiceNumber == (VoucherUpdated!.InvoiceNumber ?? "0") && s.InvoiceValue == voucher.InvoiceValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransaDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucDetails);
                        if (TransaDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransaDetails);
                    }
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل سند  " +GetVoucherType(voucher.Type)  + voucher.InvoiceNumber;
                    _SystemAction.SaveAction("SaveVoucher", "VoucherService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId };
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ السند";
                _SystemAction.SaveAction("SaveVoucher", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage SaveandPostVoucher(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                voucher.TransactionDetails = new List<Transactions>();
                //var year = _fiscalyearsRepository.GetCurrentYear();
                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("SaveandPostVoucher", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                voucher.InvoiceValueText = ConvertToWord_NEW(voucher.InvoiceValue.ToString());

                if (voucher.InvoiceId == 0)
                {
                    voucher.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                    voucher.YearId = yearid;
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.AddUser = UserId;
                    voucher.BranchId = BranchId;
                    voucher.AddDate = DateTime.Now;
                    voucher.StoreId = voucher.StoreId;
                    voucher.DunCalc = false;

                    voucher.printBankAccount = false;


                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {

                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;

                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }

                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.VoucherDetails.ToList())
                    {

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AddUser = UserId;
                        item.PayType = voucher.PayType;
                        item.AddDate = DateTime.Now;
                        decimal? Depit = item.TotalAmount;
                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        _TaamerProContext.VoucherDetails.Add(item);

                        string vouchertypename = voucher.Type == 6 ? "سند قبض" : "سند صرف";
                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 6)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucher", "VoucherService", 1, Resources.ChooseReceiptaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.ChooseReceiptaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 5)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucher", "VoucherService", 1, Resources.Chooseexchangeaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.Chooseexchangeaccount };
                        }

                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 2)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucher", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }

                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }

                        //depit 
                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = voucher.Type == 6 ? item.ToAccountId : item.AccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (voucher.Type == 6 ? item.ToAccountId : item.AccountId))?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = Depit,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        //credit 
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = voucher.Type == 6 ? item.AccountId : item.ToAccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (voucher.Type == 6 ? item.AccountId : item.ToAccountId))?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = Depit,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                    }
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                    _TaamerProContext.SaveChanges();

                    if (voucher.IsPost != true)
                    {
                        voucher.IsPost = true;
                        voucher.PostDate = voucher.Date;
                        voucher.PostHijriDate = voucher.HijriDate;
                        //voucher.PostDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        //voucher.PostHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));

                        var newJournal = new Journals();
                        var JNo = _JournalsRepository.GenerateNextJournalNumber(yearid ?? default(int), BranchId).Result;
                        if (voucher.Type == 10)
                        {
                            JNo = 1;
                        }
                        else
                        {
                            JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                        }


                        newJournal.JournalNo = JNo;
                        newJournal.YearMalia = yearid ?? default(int);
                        newJournal.VoucherId = voucher.InvoiceId;
                        newJournal.VoucherType = voucher.Type;
                        newJournal.BranchId = voucher.BranchId ?? 0;
                        newJournal.AddDate = DateTime.Now;
                        newJournal.AddUser = newJournal.UserId = UserId;
                        _TaamerProContext.Journals.Add(newJournal);
                        
                        foreach (var trans in voucher.TransactionDetails.ToList())
                        {
                            trans.IsPost = true;
                            trans.JournalNo = newJournal.JournalNo;
                        }
                        voucher.JournalNumber = newJournal.JournalNo;
                        //_TaamerProContext.SaveChanges();
                    }
                    else
                    {
                        ////-----------------------------------------------------------------------------------------------------------------
                        //string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        //string ActionNote3 = "فشل في حفظ السند";
                        //_SystemAction.SaveAction("SaveandPostVoucher", "VoucherService", 1, "مرحلة مسبقا", "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        ////-----------------------------------------------------------------------------------------------------------------

                        //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "مرحلة مسبقا" };
                    }

                    _TaamerProContext.SaveChanges();

                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == voucher.Type && s.YearId == yearid && s.InvoiceNumber == voucher.InvoiceNumber && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransaDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucDetails);
                        if (TransaDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransaDetails);
                    }
                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة سند جديد" +GetVoucherType(voucher.Type)+ " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveandPostVoucher", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
                else
                {
                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                    if (!String.IsNullOrEmpty(Convert.ToString(VoucherUpdated?.JournalNumber)) && VoucherUpdated.JournalNumber > 0)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote2 = "فشل في حفظ السند";
                        _SystemAction.SaveAction("SaveandPostVoucher", "VoucherService", 2, Resources.canteditevoucher, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                        //-----------------------------------------------------------------------------------------------------------------
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.canteditevoucher };
                    }
                    if (VoucherUpdated != null)
                    {
                        VoucherUpdated.Notes = voucher.Notes;
                        VoucherUpdated.InvoiceNotes = voucher.InvoiceNotes;
                        VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        VoucherUpdated.RecevierTxt = voucher.RecevierTxt;
                        VoucherUpdated.Date = voucher.Date;
                        VoucherUpdated.HijriDate = voucher.HijriDate;
                        VoucherUpdated.PayType = voucher.PayType;
                        VoucherUpdated.InvoiceReference = voucher.InvoiceReference;
                        VoucherUpdated.CustomerId = voucher.CustomerId;
                        VoucherUpdated.JournalNumber = voucher.JournalNumber;
                        VoucherUpdated.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                        VoucherUpdated.TaxAmount = voucher.TaxAmount;
                        VoucherUpdated.InvoiceValueText = voucher.InvoiceValueText;
                        VoucherUpdated.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                        VoucherUpdated.BranchId = BranchId;
                        VoucherUpdated.UpdateDate = DateTime.Now;
                        VoucherUpdated.UpdateUser = UserId;
                        VoucherUpdated.ToAccountId = voucher.ToAccountId;
                    }
                    //delete existing details 
                    var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                    _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                    _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    // add new details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.VoucherDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تكرار الحساب مع مركز التكلفة" };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.InvoiceId = voucher.InvoiceId;
                        item.PayType = voucher.PayType;
                        item.AddUser = UserId;
                        item.AddDate = DateTime.Now;
                        decimal? Depit = item.TotalAmount;
                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());
                        
                        // decimal Credit = item.TotalAmount;
                        _TaamerProContext.VoucherDetails.Add(item);
                        //// add transaction



                        string vouchertypename = voucher.Type == 6 ? "سند قبض" : "سند صرف";
                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 6)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveandPostVoucher", "VoucherService", 1, Resources.ChooseReceiptaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.ChooseReceiptaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 5)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveandPostVoucher", "VoucherService", 1, Resources.Chooseexchangeaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.Chooseexchangeaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 2)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveandPostVoucher", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }
                        //  _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                        //depit 
                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }

                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = VoucherUpdated.Type == 6 ? item.ToAccountId : item.AccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (VoucherUpdated.Type == 6 ? item.ToAccountId : item.AccountId))?.FirstOrDefault()?.Type,
                            Type = VoucherUpdated.Type,
                            LineNumber = 1,
                            Depit = Depit,
                            Credit = 0,


                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = item.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        //credit 
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = VoucherUpdated.Type == 6 ? item.AccountId : item.ToAccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (VoucherUpdated.Type == 6 ? item.ToAccountId : item.AccountId))?.FirstOrDefault()?.Type,
                            Type = VoucherUpdated.Type,
                            LineNumber = 2,
                            Credit = Depit,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = item.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                    }
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                    _TaamerProContext.SaveChanges();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 6 && s.YearId == yearid && s.InvoiceNumber == (VoucherUpdated!.InvoiceNumber ?? "0") && s.InvoiceValue == voucher.InvoiceValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransaDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucDetails);
                        if (TransaDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransaDetails);
                    }
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل سند  " + GetVoucherType(voucher.Type)+"  رقم  " + voucher.InvoiceId;
                    _SystemAction.SaveAction("SaveandPostVoucher", "VoucherService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ السند";
                _SystemAction.SaveAction("SaveandPostVoucher", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage SaveVoucherP(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.ContractsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.EnsureRevenueAccountedBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureRevenueAccountedBranchAccounts };
                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };
                }
                else if (Branch == null || Branch.TaxsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.EnsureTheVAT, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                }
                else if (Branch == null || Branch.SuspendedFundAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.EnsureVATcalculatedExpensesBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureVATcalculatedExpensesBranchAccounts };
                }

                voucher.TransactionDetails = new List<Transactions>();
                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                voucher.InvoiceValueText = ConvertToWord_NEW(voucher.InvoiceValue.ToString());
                if (voucher.InvoiceId == 0)
                {
                    //voucher.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                    voucher.YearId = yearid;
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.AddUser = UserId;
                    voucher.BranchId = BranchId;
                    voucher.AddDate = DateTime.Now;
                    voucher.StoreId = voucher.StoreId;

                    voucher.printBankAccount = false;

                    

                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;

                        //var NewNextInv = string.Format("{0:000000}", NextInv);
                        voucher.InvoiceNumber = NewNextInv.ToString();                     
                    }

                    _TaamerProContext.Invoices.Add(voucher);
                    //var AccOVAT = _TaamerProContext.Accounts.Where(w => w.Classification == 18).Select(s => s.AccountId).FirstOrDefault();
                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.SuspendedFundAccId).Select(s => s.AccountId).FirstOrDefault();

                    
                    //add details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.VoucherDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تكرار الحساب مع مركز التكلفة" };
                        //}

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AddUser = UserId;
                        item.PayType = voucher.PayType;
                        item.AddDate = DateTime.Now;
                        decimal? Depit = item.TotalAmount;
                        decimal? Credit = item.TotalAmount - item.TaxAmount;
                        decimal? SVAT = (item.TaxAmount);
                        if (item.TaxType == 2) //غير شامل الضريبة
                        {
                            Depit = voucher.InvoiceValue + item.TaxAmount;
                        }
                        else //شامل الضريبة
                        {
                            Depit = voucher.InvoiceValue;
                        }



                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        _TaamerProContext.VoucherDetails.Add(item);

                        string vouchertypename = voucher.Type == 6 ? "سند قبض" : "سند صرف";
                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 6)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.ChooseReceiptaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.ChooseReceiptaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 5)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.Chooseexchangeaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.Chooseexchangeaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 2)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }
                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }
                        //depit 
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = voucher.Type == 6 ? item.AccountId : item.ToAccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (voucher.Type == 6 ? item.ToAccountId : item.AccountId))?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = 0,
                            Credit = Depit,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        //credit 
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = voucher.Type == 6 ? item.ToAccountId : item.AccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (voucher.Type == 6 ? item.AccountId : item.ToAccountId))?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = 0,
                            Depit = Credit,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });

                        if (Convert.ToDecimal(item.TaxAmount) > 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {

                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                //AccountId = AccOVAT,// الضريبة
                                AccountId = Branch.SuspendedFundAccId,// الضريبة

                                CostCenterId = item.CostCenterId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== (voucher.Type == 2 ? AccOVAT : AccOVAT))?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 2,
                                Credit = 0,
                                Depit = SVAT,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                InvoiceReference = "",

                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchCost,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });

                        }

                    }
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                    _TaamerProContext.SaveChanges();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == voucher.Type && s.YearId == yearid && s.InvoiceNumber == voucher.InvoiceNumber && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransaDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucDetails);
                        if (TransaDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransaDetails);
                    }
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة سند جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId };

                }
                else
                {
                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                    if (VoucherUpdated != null)
                    {

                        if (VoucherUpdated.IsPost == true)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.canteditevoucher, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.canteditevoucher };
                        }

                        VoucherUpdated.Date = voucher.Date;
                        VoucherUpdated.HijriDate = voucher.HijriDate;


                        VoucherUpdated.Notes = voucher.Notes;
                        VoucherUpdated.InvoiceNotes = voucher.InvoiceNotes;
                        VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        VoucherUpdated.SupplierInvoiceNo = voucher.SupplierInvoiceNo;
                        VoucherUpdated.RecevierTxt = voucher.RecevierTxt;
                        VoucherUpdated.SupplierId = voucher.SupplierId;
                        VoucherUpdated.ClauseId = voucher.ClauseId;

                        VoucherUpdated.JournalNumber = voucher.JournalNumber;
                        //VoucherUpdated.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                        VoucherUpdated.TotalValue = voucher.TotalValue;

                        VoucherUpdated.TaxAmount = voucher.TaxAmount;
                        VoucherUpdated.InvoiceValueText = voucher.InvoiceValueText;
                        VoucherUpdated.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                        VoucherUpdated.BranchId = BranchId;
                        VoucherUpdated.UpdateDate = DateTime.Now;
                        VoucherUpdated.UpdateUser = UserId;
                        VoucherUpdated.ToAccountId = voucher.ToAccountId;
                        VoucherUpdated.DunCalc = voucher.DunCalc;
                        VoucherUpdated.PayType = voucher.PayType;

                    }

                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.SuspendedFundAccId).Select(s => s.AccountId).FirstOrDefault();


                    //delete existing details 
                    var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                    _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                    _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    // add new details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.VoucherDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تكرار الحساب مع مركز التكلفة" };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.InvoiceId = voucher.InvoiceId;
                        item.PayType = voucher.PayType;
                        item.AddUser = UserId;
                        item.AddDate = DateTime.Now;
                        decimal? Depit = item.TotalAmount;
                        decimal? Credit = item.TotalAmount - item.TaxAmount;
                        decimal? SVAT = (item.TaxAmount);
                        if (item.TaxType == 2) //غير شامل الضريبة
                        {
                            Depit = voucher.InvoiceValue + item.TaxAmount;

                        }
                        else //شامل الضريبة
                        {
                            Depit = voucher.InvoiceValue;

                        }


                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        // decimal Credit = item.TotalAmount;
                        _TaamerProContext.VoucherDetails.Add(item);
                        //// add transaction



                        string vouchertypename = voucher.Type == 6 ? "سند قبض" : "سند صرف";
                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 6)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.ChooseReceiptaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.ChooseReceiptaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 5)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.Chooseexchangeaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.Chooseexchangeaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 2)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }
                        //  _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                        //depit 

                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }

                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = VoucherUpdated.Type == 6 ? item.AccountId : item.ToAccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (VoucherUpdated.Type == 6 ? item.ToAccountId : item.AccountId))?.FirstOrDefault()?.Type,
                            Type = VoucherUpdated.Type,
                            LineNumber = 1,
                            Depit = 0,
                            Credit = Depit,


                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = item.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        //credit 
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = VoucherUpdated.Type == 6 ? item.ToAccountId : item.AccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (VoucherUpdated.Type == 6 ? item.ToAccountId : item.AccountId))?.FirstOrDefault()?.Type,
                            Type = VoucherUpdated.Type,
                            LineNumber = 2,
                            Credit = 0,
                            Depit = Credit,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = item.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        if (Convert.ToDecimal(item.TaxAmount) > 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                //AccountId = AccOVAT,// الضريبة
                                AccountId = Branch.SuspendedFundAccId,// الضريبة

                                CostCenterId = item.CostCenterId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== (voucher.Type == 2 ? AccOVAT : AccOVAT))?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 2,
                                Credit = 0,
                                Depit = SVAT,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                InvoiceReference = "",

                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchCost,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });

                        }
                    }
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);
                    _TaamerProContext.SaveChanges(); 
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == voucher.Type && s.YearId == yearid && s.InvoiceNumber == (VoucherUpdated!.InvoiceNumber ?? "0") && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransaDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucDetails);
                        if (TransaDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransaDetails);
                    }
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل سند رقم " + voucher.InvoiceId;
                    _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId };

                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ السند";
                _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveVoucherPUpdateImage(int InvoiceId, int UserId, int BranchId, int? yearid, string FileName, string FileUrl)
        {
            if(InvoiceId > 0)
            {
                if (FileName != "")
                {
                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId== InvoiceId).FirstOrDefault();
                    if(VoucherUpdated!=null)
                    {
                        VoucherUpdated.FileName = FileName;
                        VoucherUpdated.FileUrl = FileUrl;
                        _TaamerProContext.SaveChanges();
                    }

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
                else
                {
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
                }
            }
            else
            {
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage SaveandPostVoucherP(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.ContractsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("SaveandPostVoucherP", "VoucherService", 1, Resources.EnsureRevenueAccountedBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureRevenueAccountedBranchAccounts };
                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("SaveandPostVoucherP", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };
                }
                else if (Branch == null || Branch.TaxsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("SaveandPostVoucherP", "VoucherService", 1, Resources.EnsureTheVAT, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                }
                else if (Branch == null || Branch.SuspendedFundAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("SaveandPostVoucherP", "VoucherService", 1, Resources.EnsureVATcalculatedExpensesBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureVATcalculatedExpensesBranchAccounts };
                }

                voucher.TransactionDetails = new List<Transactions>();
                if (yearid == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("SaveandPostVoucherP", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }

                voucher.InvoiceValueText = ConvertToWord_NEW(voucher.InvoiceValue.ToString());
                if (voucher.InvoiceId == 0)
                {
                    //voucher.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                    voucher.YearId = yearid;
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.AddUser = UserId;
                    voucher.BranchId = BranchId;
                    voucher.AddDate = DateTime.Now;
                    voucher.StoreId = voucher.StoreId;

                    voucher.printBankAccount = false;


                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;

                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }

                    _TaamerProContext.Invoices.Add(voucher);
                    //var AccOVAT = _TaamerProContext.Accounts.Where(w => w.Classification == 18).Select(s => s.AccountId).FirstOrDefault();
                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.SuspendedFundAccId).Select(s => s.AccountId).FirstOrDefault();


                    //add details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.VoucherDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تكرار الحساب مع مركز التكلفة" };
                        //}

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AddUser = UserId;
                        item.PayType = voucher.PayType;
                        item.AddDate = DateTime.Now;
                        decimal? Depit = item.TotalAmount;
                        decimal? Credit = item.TotalAmount - item.TaxAmount;
                        decimal? SVAT = (item.TaxAmount);

                        //if(item.TaxType==2) //غير شامل الضريبة
                        //{
                        //    Depit = voucher.InvoiceValue;

                        //}
                        //else //شامل الضريبة
                        //{
                        //    Depit = voucher.InvoiceValue - item.TaxAmount;

                        //}


                        if (item.TaxType == 2) //غير شامل الضريبة
                        {
                            Depit = voucher.InvoiceValue + item.TaxAmount;

                        }
                        else //شامل الضريبة
                        {
                            Depit = voucher.InvoiceValue;

                        }



                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        _TaamerProContext.VoucherDetails.Add(item);

                        string vouchertypename = voucher.Type == 6 ? "سند قبض" : "سند صرف";
                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 6)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.ChooseReceiptaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.ChooseReceiptaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 5)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.Chooseexchangeaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.Chooseexchangeaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 2)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveVoucherP", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }
                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }
                        //depit 
                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = voucher.Type == 6 ? item.AccountId : item.ToAccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (voucher.Type == 6 ? item.ToAccountId : item.AccountId))?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = 0,
                            Credit = Depit,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        //credit 
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = voucher.Type == 6 ? item.ToAccountId : item.AccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (voucher.Type == 6 ? item.AccountId : item.ToAccountId))?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = 0,
                            Depit = Credit,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });

                        if (Convert.ToDecimal(item.TaxAmount) > 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {

                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                //AccountId = AccOVAT,// الضريبة
                                AccountId = Branch.SuspendedFundAccId,// الضريبة

                                CostCenterId = item.CostCenterId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== (voucher.Type == 2 ? AccOVAT : AccOVAT))?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 2,
                                Credit = 0,
                                Depit = SVAT,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                InvoiceReference = "",

                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchCost,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });

                        }

                    }
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                    _TaamerProContext.SaveChanges();

                    if (voucher.IsPost != true)
                    {
                        voucher.IsPost = true;
                        //voucher.PostDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        //voucher.PostHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));
                        voucher.PostDate = voucher.Date;
                        voucher.PostHijriDate = voucher.HijriDate;

                        var newJournal = new Journals();
                        var JNo = _JournalsRepository.GenerateNextJournalNumber(yearid ?? default(int), BranchId).Result;
                        if (voucher.Type == 10)
                        {
                            JNo = 1;
                        }
                        else
                        {
                            JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                        }


                        newJournal.JournalNo = JNo;
                        newJournal.YearMalia = yearid ?? default(int);
                        newJournal.VoucherId = voucher.InvoiceId;
                        newJournal.VoucherType = voucher.Type;
                        newJournal.BranchId = voucher.BranchId ?? 0;
                        newJournal.AddDate = DateTime.Now;
                        newJournal.AddUser = newJournal.UserId = UserId;
                        _TaamerProContext.Journals.Add(newJournal);
                        foreach (var trans in voucher.TransactionDetails.ToList())
                        {
                            trans.IsPost = true;
                            trans.JournalNo = newJournal.JournalNo;
                        }
                        voucher.JournalNumber = newJournal.JournalNo;
                        //_TaamerProContext.SaveChanges();
                    }
                    else
                    {
                        ////-----------------------------------------------------------------------------------------------------------------
                        //string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        //string ActionNote3 = "فشل في حفظ السند";
                        //_SystemAction.SaveAction("SaveandPostVoucherP", "VoucherService", 1, "مرحلة مسبقا", "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        ////-----------------------------------------------------------------------------------------------------------------

                        //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "مرحلة مسبقا" };
                    }

                    _TaamerProContext.SaveChanges();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == voucher.Type && s.YearId == yearid && s.InvoiceNumber == voucher.InvoiceNumber && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransaDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucDetails);
                        if (TransaDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransaDetails);
                    }
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة سند جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveandPostVoucherP", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId };

                }
                else
                {
                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                    if (!String.IsNullOrEmpty(Convert.ToString(VoucherUpdated.JournalNumber)) && VoucherUpdated.JournalNumber > 0)
                    {

                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote2 = "فشل في حفظ السند";
                        _SystemAction.SaveAction("SaveandPostVoucherP", "VoucherService", 1, Resources.canteditevoucher, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.canteditevoucher };
                    }
                    if (VoucherUpdated != null)
                    {
                        VoucherUpdated.Notes = voucher.Notes;
                        VoucherUpdated.InvoiceNotes = voucher.InvoiceNotes;
                        VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        VoucherUpdated.SupplierInvoiceNo = voucher.SupplierInvoiceNo;
                        VoucherUpdated.RecevierTxt = voucher.RecevierTxt;
                        VoucherUpdated.SupplierId = voucher.SupplierId;
                        VoucherUpdated.ClauseId = voucher.ClauseId;

                        VoucherUpdated.JournalNumber = voucher.JournalNumber;
                        //VoucherUpdated.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                        VoucherUpdated.TotalValue = voucher.TotalValue;

                        VoucherUpdated.TaxAmount = voucher.TaxAmount;
                        VoucherUpdated.InvoiceValueText = voucher.InvoiceValueText;
                        VoucherUpdated.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                        VoucherUpdated.BranchId = BranchId;
                        VoucherUpdated.UpdateDate = DateTime.Now;
                        VoucherUpdated.UpdateUser = UserId;
                        VoucherUpdated.ToAccountId = voucher.ToAccountId;
                    }

                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.SuspendedFundAccId).Select(s => s.AccountId).FirstOrDefault();


                    //delete existing details 
                    var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                    _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                    _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    // add new details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.VoucherDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تكرار الحساب مع مركز التكلفة" };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.InvoiceId = voucher.InvoiceId;
                        item.PayType = voucher.PayType;
                        item.AddUser = UserId;
                        item.AddDate = DateTime.Now;
                        decimal? Depit = item.TotalAmount;
                        decimal? Credit = item.TotalAmount - item.TaxAmount;
                        decimal? SVAT = (item.TaxAmount);

                        if (item.TaxType == 2) //غير شامل الضريبة
                        {
                            Depit = voucher.InvoiceValue + item.TaxAmount;

                        }
                        else //شامل الضريبة
                        {
                            Depit = voucher.InvoiceValue;

                        }


                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        // decimal Credit = item.TotalAmount;
                        _TaamerProContext.VoucherDetails.Add(item);
                        //// add transaction



                        string vouchertypename = voucher.Type == 6 ? "سند قبض" : "سند صرف";
                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 6)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveandPostVoucherP", "VoucherService", 1, Resources.ChooseReceiptaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.ChooseReceiptaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 5)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveandPostVoucherP", "VoucherService", 1, Resources.Chooseexchangeaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.Chooseexchangeaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 2)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("SaveandPostVoucherP", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }
                        //  _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                        //depit 

                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }


                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = VoucherUpdated.Type == 6 ? item.AccountId : item.ToAccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (VoucherUpdated.Type == 6 ? item.ToAccountId : item.AccountId))?.FirstOrDefault()?.Type,
                            Type = VoucherUpdated.Type,
                            LineNumber = 1,
                            Depit = 0,
                            Credit = Depit,


                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = item.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        //credit 
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = VoucherUpdated.Type == 6 ? item.ToAccountId : item.AccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == (VoucherUpdated.Type == 6 ? item.ToAccountId : item.AccountId))?.FirstOrDefault()?.Type,
                            Type = VoucherUpdated.Type,
                            LineNumber = 2,
                            Credit = 0,
                            Depit = Credit,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = item.InvoiceId,
                            IsPost = false,
                            BranchId = BranchCost,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        if (Convert.ToDecimal(item.TaxAmount) > 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {

                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                //AccountId = AccOVAT,// الضريبة
                                AccountId = Branch.SuspendedFundAccId,// الضريبة

                                CostCenterId = item.CostCenterId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== (voucher.Type == 2 ? AccOVAT : AccOVAT))?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 2,
                                Credit = 0,
                                Depit = SVAT,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                InvoiceReference = "",

                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchCost,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });

                        }
                    }
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل سند رقم " + voucher.InvoiceId;
                    _SystemAction.SaveAction("SaveandPostVoucherP", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId };

                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ السند";
                _SystemAction.SaveAction("SaveandPostVoucherP", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage UpdateVoucher(int InvoiceId, int UserId, int BranchId)
        {

            var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId== InvoiceId).FirstOrDefault();
            if (VoucherUpdated != null)
            {
                decimal sumValue = 0;
                var VoucherUpdated_Re = _TaamerProContext.Invoices.Where(s=>s.IsDeleted==false && s.Type==6 
                && s.YearId== VoucherUpdated.YearId && s.ToInvoiceId== VoucherUpdated.InvoiceNumber).ToList();

                foreach(var item in VoucherUpdated_Re)
                {
                    sumValue += Convert.ToDecimal(Math.Round(double.Parse((item.TotalValue).ToString() ?? "0"), 2));
                }

                if (VoucherUpdated.TotalValue <= (VoucherUpdated.PaidValue+ sumValue))
                {
                    VoucherUpdated.StoreId = 1;
                }
                else
                {
                    VoucherUpdated.StoreId = 0;
                }

            }
            _TaamerProContext.SaveChanges();
            //-----------------------------------------------------------------------------------------------------------------
            string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
            string ActionNote = " تعديل   " + GetVoucherType(VoucherUpdated.Type) + " " + VoucherUpdated.InvoiceNumber;
            _SystemAction.SaveAction("UpdateVoucher", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
            //-----------------------------------------------------------------------------------------------------------------
            return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
        }
        //public GeneralMessage UpdateVoucherZatca(int InvoiceId,string InvoiceHash, string SingedXML, string EncodedInvoice, string ZatcaUUID, string QRCode, string PIH, string SingedXMLFileName,int zatcaVoucherNumber, int UserId, int BranchId)
        //{

        //    var VoucherUpdated = _TaamerProContext.Invoices.Where(s => s.InvoiceId == InvoiceId).FirstOrDefault();
        //    if (VoucherUpdated != null)
        //    {
        //        VoucherUpdated.InvoiceHash = InvoiceHash;
        //        VoucherUpdated.SingedXML = SingedXML;
        //        VoucherUpdated.EncodedInvoice = EncodedInvoice;
        //        VoucherUpdated.ZatcaUUID = ZatcaUUID;
        //        VoucherUpdated.QRCode = QRCode;
        //        VoucherUpdated.PIH = PIH;
        //        VoucherUpdated.SingedXMLFileName = SingedXMLFileName;
        //        VoucherUpdated.InvoiceNoRequest = zatcaVoucherNumber;
        //    }
        //    _TaamerProContext.SaveChanges();
        //    //-----------------------------------------------------------------------------------------------------------------
        //    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
        //    string ActionNote = " تعديل   " + GetVoucherType(VoucherUpdated.Type) + " " + VoucherUpdated.InvoiceNumber;
        //    _SystemAction.SaveAction("UpdateVoucher", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
        //    //-----------------------------------------------------------------------------------------------------------------
        //    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
        //}

        public GeneralMessage UpdateVoucherDraft(int InvoiceId, int UserId, int BranchId, int yearid, string Con)
        {

            var VoucherUpdated = _TaamerProContext.Invoices.Where(s => s.InvoiceId == InvoiceId).FirstOrDefault();
            if (VoucherUpdated != null)
            {

                VoucherUpdated.Type = 2;

                //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(2, yearid, BranchId).Result;
                var NewNextInv = GenerateVoucherNumberNewPro(2, BranchId, yearid, 2, Con).Result;

                //var NewNextInv = string.Format("{0:000000}", NextInv);

                VoucherUpdated.InvoiceNumber = NewNextInv.ToString();
                var TransactionDetailsV = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                foreach (var item in TransactionDetailsV)
                {
                    item.Type = 2;
                }
            }
            _TaamerProContext.SaveChanges();
            var voucherDet = _TaamerProContext.VoucherDetails.Where(s => s.IsDeleted == false && s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
            List<int> voDetIds = new List<int>();
            foreach (var itemV in voucherDet)
            {
                voDetIds.Add(itemV.VoucherDetailsId);
            }
            //-----------------------------------------------------------------------------------------------------------------
            string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
            string ActionNote = " تعديل   " + GetVoucherType(VoucherUpdated.Type) + " " + VoucherUpdated.InvoiceNumber;
            _SystemAction.SaveAction("UpdateVoucher", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
            //-----------------------------------------------------------------------------------------------------------------
            return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully,voucherDetObj= voDetIds };
        }

        public GeneralMessage UpdateVoucherRecepient(string InvoiceId, int UserId, int BranchId,int YearId)
        {

            var VoucherUpdated = _TaamerProContext.Invoices.Where(s => s.IsDeleted==false && s.InvoiceNumber== InvoiceId &&s.Type==2 &&s.YearId== YearId).FirstOrDefault();
            if (VoucherUpdated != null)
            {
                decimal sumValue = 0;
                var VoucherUpdated_Re = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 6
                && s.YearId == VoucherUpdated.YearId && s.ToInvoiceId == VoucherUpdated.InvoiceNumber).ToList();

                foreach (var item in VoucherUpdated_Re)
                {
                    sumValue += Convert.ToDecimal(Math.Round(double.Parse((item.TotalValue).ToString() ?? "0"), 2));
                }

                if (VoucherUpdated.TotalValue <= (VoucherUpdated.PaidValue + sumValue))
                {
                    VoucherUpdated.StoreId = 1;
                }
                else
                {
                    VoucherUpdated.StoreId = 0;
                }

            }
            _TaamerProContext.SaveChanges();
            //-----------------------------------------------------------------------------------------------------------------
            string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
            string ActionNote = " تعديل   " + GetVoucherType(VoucherUpdated.Type) + " " + VoucherUpdated.InvoiceNumber;
            _SystemAction.SaveAction("UpdateVoucher", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
            //-----------------------------------------------------------------------------------------------------------------
            return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
        }

        public GeneralMessage UpdateVoucher_payed(int InvoiceId, int UserId, int BranchId)
        {

            var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId== InvoiceId).FirstOrDefault();
            if (VoucherUpdated != null)
            {
                decimal sumValue = 0;
                var VoucherUpdated_Re = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 5
                && s.YearId == VoucherUpdated.YearId && s.ToInvoiceId == VoucherUpdated.InvoiceNumber).ToList();

                foreach (var item in VoucherUpdated_Re)
                {
                    sumValue += Convert.ToDecimal(Math.Round(double.Parse((item.TotalValue).ToString() ?? "0"), 2));
                }

                if (VoucherUpdated.TotalValue <= (VoucherUpdated.PaidValue + sumValue))
                {
                    VoucherUpdated.StoreId = 1;
                }
                else
                {
                    VoucherUpdated.StoreId = 0;
                }

            }
            _TaamerProContext.SaveChanges();
            //-----------------------------------------------------------------------------------------------------------------
            string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
            string ActionNote = " تعديل   " + GetVoucherType(VoucherUpdated.Type) + " " + VoucherUpdated.InvoiceNumber;
            _SystemAction.SaveAction("UpdateVoucher", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
            //-----------------------------------------------------------------------------------------------------------------
            return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
        }

        public GeneralMessage UpdateVoucher_payed_by(string SupplierInvoiceNo, int UserId, int BranchId,int YearId)
        {

            var VoucherUpdated = _TaamerProContext.Invoices.Where(s => s.IsDeleted==false && s.InvoiceNumber == SupplierInvoiceNo && s.Type==1 && s.YearId == YearId).FirstOrDefault();
            if (VoucherUpdated != null)
            {
                decimal sumValue = 0;
                var VoucherUpdated_Re = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 5
                && s.YearId == VoucherUpdated.YearId && s.ToInvoiceId == VoucherUpdated.InvoiceNumber).ToList();

                foreach (var item in VoucherUpdated_Re)
                {
                    sumValue += Convert.ToDecimal(Math.Round(double.Parse((item.TotalValue).ToString() ?? "0"), 2));
                }

                if (VoucherUpdated.TotalValue <= (VoucherUpdated.PaidValue + sumValue))
                {
                    VoucherUpdated.StoreId = 1;
                }
                else
                {
                    VoucherUpdated.StoreId = 0;
                }

            }
            _TaamerProContext.SaveChanges();
            //-----------------------------------------------------------------------------------------------------------------
            string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
            string ActionNote = "تعديل سند";
            if (VoucherUpdated != null)
            {
                ActionNote = " تعديل   " + GetVoucherType(VoucherUpdated.Type) + " " + VoucherUpdated.InvoiceNumber;

            }
            _SystemAction.SaveAction("UpdateVoucher", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
            //-----------------------------------------------------------------------------------------------------------------
            return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
        }

        public decimal? VousherRe_Sum(int InvoiceId)
        {
            try
            {
                var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId== InvoiceId).FirstOrDefault();
                if (VoucherUpdated != null)
                {
                    decimal sumValue = 0;
                    var VoucherUpdated_Re = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 6
                    && s.YearId == VoucherUpdated.YearId && s.ToInvoiceId == VoucherUpdated.InvoiceNumber);

                    foreach (var item in VoucherUpdated_Re)
                    {
                        sumValue += Convert.ToDecimal(Math.Round(double.Parse((item.TotalValue).ToString() ?? "0"), 2));

                    }
                    return sumValue;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception)
            {
                return 0;
            }
           
        }



        public decimal? PayVousher_Sum(int InvoiceId)
        {
            try
            {
                var VoucherUpdated = _TaamerProContext.Invoices.Where(s => s.InvoiceId == InvoiceId).FirstOrDefault();
                if (VoucherUpdated != null)
                {
                    decimal sumValue = 0;
                    var VoucherUpdated_Re = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 5
                    && s.YearId == VoucherUpdated.YearId && s.ToInvoiceId == VoucherUpdated.InvoiceNumber);

                    foreach (var item in VoucherUpdated_Re)
                    {
                        sumValue += Convert.ToDecimal(Math.Round(double.Parse((item.TotalValue).ToString() ?? "0"), 2));

                    }
                    return sumValue;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception)
            {
                return 0;
            }

        }

        public GeneralMessage Issuing_invoice(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            { 
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.ContractsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("Issuing_invoice", "VoucherService", 1, Resources.EnsureRevenueAccountedBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase =Resources.EnsureRevenueAccountedBranchAccounts};

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("Issuing_invoice", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.SaleDiscountAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("Issuing_invoice", "VoucherService", 1, Resources.EnsureThatTheSales, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatTheSales };

                }
                else if (Branch == null || Branch.TaxsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("Issuing_invoice", "VoucherService", 1, Resources.EnsureTheVAT, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                }
                int? CostId;
                int payId = Convert.ToInt32(voucher.Paid);
                var CustomerPaymentsUpdated = _TaamerProContext.CustomerPayments.Where(s=>s.PaymentId== payId).FirstOrDefault();
                voucher.TransactionDetails = new List<Transactions>();
                //  var year = _fiscalyearsRepository.GetCurrentYear();
                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ السند";
                    _SystemAction.SaveAction("Issuing_invoice", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }

                voucher.InvoiceValueText = ConvertToWord_NEW(voucher.TotalValue.ToString());
                int? ToAccID = _CustomerRepository.GetCustomersByCustId(voucher.CustomerId).Result.Select(s => s.AccountId).FirstOrDefault();
                decimal Disc = 0;
                if (voucher.DiscountValue != null || voucher.DiscountValue == 0)
                {
                    Disc = voucher.DiscountValue ?? 0;
                }
                else
                {
                    Disc = 0;
                }
                if (voucher.InvoiceId == 0)
                {

                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                    voucher.YearId = yearid;
                    //voucher.ToAccountId = ToAccID;
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.PayType = voucher.PayType;

                    voucher.AddUser = UserId;
                    voucher.BranchId = BranchId;
                    voucher.AddDate = DateTime.Now;
                    voucher.ProjectId = voucher.ProjectId;
                    voucher.printBankAccount = false;

                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId==BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;

                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }
                    try
                    {
                        CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId).Select(s => s.CostCenterId).FirstOrDefault();
                    }
                    catch
                    {
                        CostId = null;
                    }
                    //var AccOVAT = _TaamerProContext.Accounts.Where(w => w.Classification == 18).Select(s => s.AccountId).FirstOrDefault();
                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                    _TaamerProContext.Invoices.Add(voucher);


                    //add details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.VoucherDetails.ToList())
                    {

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AddUser = UserId;
                        item.PayType = voucher.PayType;
                        item.AddDate = DateTime.Now;
                        //item.ToAccountId = ToAccID;
                        //item.ToAccountId = 41;   //مبيعات
                        item.ToAccountId = Branch.ContractsAccId;


                        decimal? TotalWithoutVAT = (item.Amount * 1);
                        decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                        decimal? SVAT = (item.TaxAmount);
                        decimal? Depit = item.TotalAmount;
                        decimal? VAT = item.TaxAmount;
                        item.TotalAmount = Depit;
                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        _TaamerProContext.VoucherDetails.Add(item);
                        string vouchertypename = voucher.Type == 2 ? "فاتورة عقد" : "فاتورة";
                        string vouchertypename2 = " خصم فاتورة ";

                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 2)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ السند";
                            _SystemAction.SaveAction("Issuing_invoice", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }

                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            //AccountId = ToAccID,
                            AccountId = item.AccountId, //الصندوق العام

                            CostCenterId = CostId,
                            AccountType = 2,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = TotalWithVAT,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        if (voucher.DiscountValue != null || voucher.DiscountValue == 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = Branch.SaleDiscountAccId,
                                CostCenterId = CostId,
                                AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 1,
                                Depit = voucher.DiscountValue,
                                Credit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                InvoiceReference = "",

                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });

                        }


                        //credit 
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            //AccountId = 41, //مبيعات
                            AccountId = Branch.ContractsAccId, //مبيعات

                            CostCenterId = CostId,
                            AccountType = 2,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = TotalWithoutVAT,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            //AccountId = AccOVAT,// الضريبة
                            AccountId = Branch.TaxsAccId,// الضريبة

                            CostCenterId = CostId,//item.CostCenterId,
                            AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = SVAT,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });



                    }
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);
                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة سند جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("Issuing_invoice", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
                else
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote2 = "فشل في حفظ السند";
                    _SystemAction.SaveAction("Issuing_invoice", "VoucherService", 1, Resources.MAcc_ContNotChanged, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.MAcc_ContNotChanged };

                }
            }
            catch (Exception ex)
            {
                var x = ex.Message;

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote2 = "فشل في حفظ السند";
                _SystemAction.SaveAction("Issuing_invoice", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveInvoice(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {

            try
            {

                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.ContractsAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoice", "VoucherService", 1, Resources.EnsureRevenueAccountedBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureRevenueAccountedBranchAccounts };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoice", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.SaleDiscountAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoice", "VoucherService", 1, Resources.EnsureThatTheSales, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatTheSales };

                }
                else if (Branch == null || Branch.TaxsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoice", "VoucherService", 1, Resources.EnsureTheVAT, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                }



                int? CostId;
                voucher.TransactionDetails = new List<Transactions>();
                //var year = _fiscalyearsRepository.GetCurrentYear();

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoice", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }

                if (voucher.InvoiceId == 0)
                {
                    //voucher.TotalValue = voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0);
                    voucher.TotalValue = voucher.TotalValue;

                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                    //voucher.YearId = year.YearId;
                    voucher.YearId = yearid;
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.AddUser = UserId;
                    voucher.BranchId = BranchId;
                    voucher.AddDate = DateTime.Now;
                    voucher.ProjectId = voucher.ProjectId;

                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }

                    try
                    {
                        CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId).Select(s => s.CostCenterId).FirstOrDefault();
                    }
                    catch
                    {
                        CostId = null;
                    }
                    //Utilities utilVoucher = new Utilities((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());
                    voucher.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                    //voucher.InvoiceValueText = utilVoucher.GetNumberAr();

                    //var AccOVAT = _TaamerProContext.Accounts.Where(w => w.Classification == 18).Select(s => s.AccountId).FirstOrDefault();
                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.VoucherDetails.ToList())
                    {


                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AddUser = UserId;
                        item.PayType = voucher.PayType;

                        item.AddDate = DateTime.Now;

                        decimal? TotalWithoutVAT = (item.Amount * item.Qty);
                        decimal? TotalWithVAT = ((item.Amount * item.Qty) + item.TaxAmount);
                        decimal? SVAT = (item.TaxAmount);


                        decimal? Depit = (item.Amount * item.Qty) + item.TaxAmount;
                        decimal? VAT = item.TaxAmount;
                        item.TotalAmount = item.TotalAmount;
                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        _TaamerProContext.VoucherDetails.Add(item);
                        string vouchertypename = voucher.Type == 2 ? "فاتورة مبيعات" : "فاتورة";
                        string vouchertypename2 = " خصم فاتورة ";

                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 2)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote3 = "فشل في حفظ فاتورة";
                            _SystemAction.SaveAction("SaveInvoice", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }

                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = item.ToAccountId,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== item.ToAccountId)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = TotalWithVAT,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        if (voucher.DiscountValue != null || voucher.DiscountValue == 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = Branch.SaleDiscountAccId,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 1,
                                Depit = voucher.DiscountValue,
                                Credit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                InvoiceReference = "",

                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });

                        }

                        //credit 
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = item.AccountId,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== item.AccountId)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = TotalWithoutVAT,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            //AccountId = AccOVAT,// الضريبة
                            AccountId = Branch.TaxsAccId,// الضريبة

                            CostCenterId = CostId,//item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = SVAT,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                    }

                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);


                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة فاتورة جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveInvoice", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully,ReturnedParm= voucher.InvoiceId };
                }
                else
                {


                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                    if (!String.IsNullOrEmpty(Convert.ToString(VoucherUpdated.JournalNumber)) && VoucherUpdated.JournalNumber > 0)
                    {

                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote3 = "فشل في حفظ فاتورة";
                        _SystemAction.SaveAction("SaveInvoice", "VoucherService", 1, Resources.canteditevoucher, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.canteditevoucher };
                    }
                    if (VoucherUpdated != null)
                    {
                        VoucherUpdated.Notes = voucher.Notes;
                        VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        VoucherUpdated.JournalNumber = voucher.JournalNumber;
                        //VoucherUpdated.TotalValue = voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0);
                        VoucherUpdated.TotalValue = voucher.TotalValue;

                        VoucherUpdated.TaxAmount = voucher.TaxAmount;
                        VoucherUpdated.ProjectId = voucher.ProjectId;
                        try
                        {
                            CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId).Select(s => s.CostCenterId).FirstOrDefault();
                        }
                        catch
                        {
                            CostId = null;
                        }
                        //Utilities util = new Utilities((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());
                        voucher.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                        //VoucherUpdated.InvoiceValueText = util.GetNumberAr();

                        //var AccOVAT = _TaamerProContext.Accounts.Where(w => w.Classification == 18).Select(s => s.AccountId).FirstOrDefault();
                        var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();


                        VoucherUpdated.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                        VoucherUpdated.BranchId = BranchId;
                        VoucherUpdated.UpdateDate = DateTime.Now;
                        VoucherUpdated.UpdateUser = UserId;
                        VoucherUpdated.ToAccountId = voucher.ToAccountId;

                        //delete existing details 
                        var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                        var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                        _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                        _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                        // add new details
                        var ObjList = new List<object>();
                        foreach (var item in voucher.VoucherDetails.ToList())
                        {
                            //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                            //{
                            //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تكرار الحساب مع مركز التكلفة" };
                            //}
                            ObjList.Add(new { item.AccountId, item.CostCenterId });
                            item.InvoiceId = voucher.InvoiceId;
                            item.PayType = voucher.PayType;
                            item.AddUser = 1;
                            item.AddDate = DateTime.Now;
                            decimal? TotalWithoutVAT = (item.Amount * item.Qty);
                            decimal? TotalWithVAT = ((item.Amount * item.Qty) + item.TaxAmount);
                            decimal? SVAT = (item.TaxAmount);

                            decimal? Depit = (item.Amount * item.Qty) + item.TaxAmount;
                            item.TotalAmount = item.TotalAmount;
                            //Utilities utilDetails = new Utilities(Depit.ToString());
                            //item.TFK = utilDetails.GetNumberAr();
                            item.TFK = ConvertToWord_NEW(Depit.ToString());

                            // decimal Credit = item.TotalAmount;
                            _TaamerProContext.VoucherDetails.Add(item);
                            //// add transaction



                            string vouchertypename = voucher.Type == 2 ? "فاتورة مبيعات" : "فاتورة";
                            string vouchertypename2 = " خصم فاتورة ";

                            if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 2)
                            {

                                //-----------------------------------------------------------------------------------------------------------------
                                string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                                string ActionNote3 = "فشل في حفظ فاتورة";
                                _SystemAction.SaveAction("SaveInvoice", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                                //-----------------------------------------------------------------------------------------------------------------

                                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                            }
                            //depit 

                            voucher.TransactionDetails.Add(new Transactions
                            {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                                AccountId = item.ToAccountId,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == item.ToAccountId)?.FirstOrDefault()?.Type,
                                Type = VoucherUpdated.Type,
                                LineNumber = 1,
                                Depit = TotalWithVAT,
                                Credit = 0,


                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                                InvoiceReference = "",

                                InvoiceId = item.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });
                            if (voucher.DiscountValue != null || voucher.DiscountValue == 0)
                            {
                                voucher.TransactionDetails.Add(new Transactions
                                {
                                    AccTransactionDate = voucher.Date,
                                    AccTransactionHijriDate = voucher.HijriDate,
                                    TransactionDate = voucher.Date,
                                    TransactionHijriDate = voucher.HijriDate,
                                    AccountId = Branch.SaleDiscountAccId,
                                    CostCenterId = CostId,
                                    AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                                    Type = voucher.Type,
                                    LineNumber = 1,
                                    Depit = voucher.DiscountValue,
                                    Credit = 0,
                                    YearId = yearid,
                                    Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                    Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                    //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                    InvoiceReference = "",

                                    InvoiceId = voucher.InvoiceId,
                                    IsPost = false,
                                    BranchId = BranchId,
                                    AddDate = DateTime.Now,
                                    AddUser = UserId,
                                    IsDeleted = false,
                                });

                            }

                            //credit 
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = item.AccountId,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == item.AccountId)?.FirstOrDefault()?.Type,
                                Type = VoucherUpdated.Type,
                                LineNumber = 2,
                                Credit = TotalWithoutVAT,
                                Depit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                                InvoiceReference = "",

                                InvoiceId = item.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                //AccountId = AccOVAT,// الضريبة
                                AccountId = Branch.TaxsAccId,// الضريبة

                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 2,
                                Credit = SVAT,
                                Depit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                InvoiceReference = "",

                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });
                        }
                        _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);
                    }

                _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل فاتورة رقم " + voucher.InvoiceId;
                    _SystemAction.SaveAction("SaveInvoice", "VoucherService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId };
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ الفاتورة";
                _SystemAction.SaveAction("SaveInvoice", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveInvoiceForServices(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        { 
            try
            {

                var ToAccount=0;
                var CustomerAccountPaid = 0;
                var BoxBankAccountPaid = 0;
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.ContractsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureRevenueAccountedBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureRevenueAccountedBranchAccounts };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                { 
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.SaleDiscountAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureThatTheSales, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatTheSales };

                }
                else if (Branch == null || Branch.TaxsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureTheVAT, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                }

                foreach (var item in voucher.VoucherDetails.ToList())
                {
                     var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                    int? AccountEradat = 0;
                    if (ServicesUpdated != null)
                    {
                        AccountEradat = ServicesUpdated.AccountId ?? 0;
                        if (AccountEradat == 0)
                        {
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                        }
                    }
                    else
                    {
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                    }
                }



                int? CostId;
                voucher.TransactionDetails = new List<Transactions>(); 

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                decimal Disc = 0;
                //if (voucher.DiscountValue != null)
                //{
                //    if (Convert.ToInt32(voucher.DiscountValue) != 0)
                //    {

                //        Disc = voucher.DiscountValue ?? 0;
                //    }
                //    else
                //    {
                //        Disc = 0;

                //    }
                //}
                //else
                //{
                //    Disc = 0;
                //}
                if (voucher.InvoiceId == 0)
                {
                    if (String.IsNullOrWhiteSpace(Convert.ToString(voucher.ToAccountId)) && voucher.Type == 2)
                    {

                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote3 = "فشل في حفظ فاتورة";
                        _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                    }
                    //   voucher.TotalValue = voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0);
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                    voucher.YearId = yearid;
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.AddUser = UserId;
                    voucher.BranchId = BranchId;
                    voucher.AddDate = DateTime.Now;
                    voucher.ProjectId = voucher.ProjectId;
                    voucher.DunCalc = false;

                    voucher.InvUUID = GET_UUID();

                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId==yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if(vouchercheck.Count()>0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }

                    if(voucher.ProjectId!=null)
                    {

                        var project = _TaamerProContext.Project.Where(s=>s.ProjectId== voucher.ProjectId).FirstOrDefault();
                        if (project != null)
                        {
                            project.MotionProject = 1;
                            project.MotionProjectDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            project.MotionProjectNote = "أضافة فاتورة علي مشروع";
                        }
                        if (voucher.CostCenterId > 0)
                        {
                            CostId = voucher.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }
                        }
                       

                    }
                    else
                    {
                        if (voucher.CostCenterId > 0)
                        {
                            CostId = voucher.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }

                    }
                    if(CostId==0)
                    {
                        try
                        {CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault();}
                        catch
                        {CostId = null;}
                    }
                    voucher.CostCenterId = CostId;
                    //Utilities utilVoucher = new Utilities((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());
                    //voucher.InvoiceValueText = utilVoucher.GetNumberAr();
                    voucher.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();

                    int? itemToAccountId = 0;
                    int? itemAccountId = 0;
                    var vouchertypename = "";
                    var vouchertypename2 = "";
                    int? itemInvoiceId = 0;
                    decimal? AcPaid = 0;
                    decimal? AcPaid2 = 0;
                    decimal? AcPaid3 = 0;

                    decimal? SVAT = 0;
                    int? itemTaxType = 0;

                    foreach (var item in voucher.VoucherDetails.ToList())
                    {


                        //ToAccount = item.AccountId??0;
                        ToAccount = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;

                        // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                        //if(ServicesUpdated!=null)
                        //{
                        //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                        //}

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AddUser = UserId;
                        item.PayType = voucher.PayType;

                        item.AddDate = DateTime.Now;

                        decimal? TotalWithoutVAT = (item.Amount);
                        decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                        SVAT = (voucher.TaxAmount);
                        //SVAT = SVAT + (item.TaxAmount * item.Qty);

                        item.CostCenterId = CostId;
                        decimal? Depit = item.Amount + item.TaxAmount;

                        //decimal? x =Convert.ToDecimal(voucher.PaidValue * Convert.ToDecimal(0.15));

                        //decimal? Depit = Convert.ToDecimal((voucher.PaidValue * Convert.ToDecimal(0.15)));

                        decimal? VAT = item.TaxAmount;
                        item.TotalAmount = item.TotalAmount;
                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        _TaamerProContext.VoucherDetails.Add(item);
                        vouchertypename = voucher.Type == 2 ? " فاتورة" + " مبيعات" : "فاتورة";
                        vouchertypename2 = " خصم فاتورة ";

                        itemToAccountId = item.ToAccountId;
                        itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                        itemInvoiceId = item.InvoiceId;
                        itemTaxType = item.TaxType;

                    }
                    CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                    BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                    if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                    {
                        if (itemTaxType == 2)
                        {
                            AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                            AcPaid3 = 0;

                        }
                        else
                        {
                            AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                            AcPaid3 = 0;

                        }
                    }
                    else
                    {
                        if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                        {
                            AcPaid = Convert.ToDecimal(voucher.PaidValue);
                            AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;

                            //Edit 21-01-2024 Ostaze Mohammed Req
                            //AcPaid3 = 0;
                            AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                            var CustomerData = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                            CustomerAccountPaid = CustomerData.AccountId ?? 0;
                            BoxBankAccountPaid = itemToAccountId ?? 0;

                        }
                        else
                        {
                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);

                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);

                            }
                        }

                    }

                    if (Convert.ToInt32(voucher.PaidValue??0) < Convert.ToInt32(voucher.TotalValue??0))
                    {
                        var CustomerDataV = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                        CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                    }


                    _TaamerProContext.SaveChanges();
                    voucher.TransactionDetails.Add(new Transactions
                    {

                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        AccountId = CustomerAccountPaid,
                        CostCenterId = CostId,
                        AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 1,
                        Depit = AcPaid,
                        Credit = 0,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });

                    if (Disc > 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = Branch.SaleDiscountAccId,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = voucher.DiscountValue,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });

                    }

                    //credit 


                    foreach (var item in voucher.VoucherDetails.ToList())
                    {


                         var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                        int? AccountEradat = 0;
                        if (ServicesUpdated != null)
                        {
                            AccountEradat = ServicesUpdated.AccountId ?? 0;
                            if (AccountEradat == 0)
                            {
                                //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                            }
                        }
                        else
                        {
                            //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                        }
                        item.AccountId = AccountEradat;
                        item.Qty = item.Qty ?? 1;
                        item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                        if (itemTaxType == 2)
                        {
                            item.Amount = (item.Amount * item.Qty) - (item.DiscountValue_Det);
                        }
                        else
                        {
                            item.Amount = item.TotalAmount - (item.TaxAmount);
                        }
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = AccountEradat, 


                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== AccountEradat)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = item.Amount,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            VoucherDetailsId = item.VoucherDetailsId,

                        });


                    }

                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        //AccountId = AccOVAT,// الضريبة
                        AccountId = Branch.TaxsAccId,// الضريبة

                        CostCenterId = CostId,//item.CostCenterId,
                        AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 3,
                        Credit = SVAT,
                        Depit = 0,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });

                    if(AcPaid3!=0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = BoxBankAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 4,
                            Depit = AcPaid3,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 5,
                            Depit = 0,
                            Credit = AcPaid3,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });


                    }


                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);


                    if (voucher.ServicesPriceOffer != null && voucher.ServicesPriceOffer.Count > 0)
                    {
                        foreach (var item in voucher.ServicesPriceOffer)
                        {
                            item.AddUser = UserId;
                            item.AddDate = DateTime.Now;
                            item.InvoiceId = voucher.InvoiceId;
                            _TaamerProContext.Acc_Services_PriceOffer.Add(item);
                        }
                    }


                    _TaamerProContext.SaveChanges();
                    voucher.QRCodeNum = "200010001000" + voucher.InvoiceId.ToString();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 2 && s.YearId == yearid && s.InvoiceNumber == voucher.InvoiceNumber && s.TotalValue == voucher.TotalValue && s.BranchId==BranchId).ToList();
                    if(ExistInvoice.Count()>1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if(VoucherDetails.Count()>0) _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                        if (TransactionDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    }
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة فاتورة جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    List<int> voDetIds = new List<int>();
                    foreach (var itemV in voucher.VoucherDetails)
                    {
                        voDetIds.Add(itemV.VoucherDetailsId);
                    }
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully,ReturnedParm= voucher.InvoiceId,InvoiceIsDeleted=voucher.IsDeleted, voucherDetObj = voDetIds };
                }
                else
                {
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnterNewInvoice };
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ الفاتورة";
                _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage SaveInvoiceForServicesDraft(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {

                var ToAccount = 0;
                var CustomerAccountPaid = 0;
                var BoxBankAccountPaid = 0;
                var Branch = _TaamerProContext.Branch.Where(s => s.BranchId == BranchId).FirstOrDefault();
                if (Branch == null || Branch.ContractsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ مسودة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureRevenueAccountedBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureRevenueAccountedBranchAccounts };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ مسودة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.SaleDiscountAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ مسودة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureThatTheSales, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatTheSales };

                }
                else if (Branch == null || Branch.TaxsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ مسودة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureTheVAT, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                }

                foreach (var item in voucher.VoucherDetails.ToList())
                {
                    var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s => s.ServicesId == item.ServicesPriceId).FirstOrDefault();
                    int? AccountEradat = 0;
                    if (ServicesUpdated != null)
                    {
                        AccountEradat = ServicesUpdated.AccountId ?? 0;
                        if (AccountEradat == 0)
                        {
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                        }
                    }
                    else
                    {
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                    }
                }



                int? CostId;
                voucher.TransactionDetails = new List<Transactions>();

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ مسودة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                decimal Disc = 0;
                //if (voucher.DiscountValue != null)
                //{
                //    if (Convert.ToInt32(voucher.DiscountValue) != 0)
                //    {

                //        Disc = voucher.DiscountValue ?? 0;
                //    }
                //    else
                //    {
                //        Disc = 0;

                //    }
                //}
                //else
                //{
                //    Disc = 0;
                //}
                if (voucher.InvoiceId == 0)
                {
                    if (String.IsNullOrWhiteSpace(Convert.ToString(voucher.ToAccountId)) && voucher.Type == 2)
                    {

                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote3 = "فشل في حفظ مسودة";
                        _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                    }
                    //   voucher.TotalValue = voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0);
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                    voucher.YearId = yearid;
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.AddUser = UserId;
                    voucher.BranchId = BranchId;
                    voucher.AddDate = DateTime.Now;
                    voucher.ProjectId = voucher.ProjectId;
                    voucher.DunCalc = false;

                    voucher.InvUUID = GET_UUID();

                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }

                    //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                    //var NewNextInv = string.Format("{0:000000}", NextInv);

                    //voucher.InvoiceNumber = NewNextInv.ToString();

                    if (voucher.ProjectId != null)
                    {

                        var project = _TaamerProContext.Project.Where(s => s.ProjectId == voucher.ProjectId).FirstOrDefault();
                        if (project != null)
                        {
                            project.MotionProject = 1;
                            project.MotionProjectDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            project.MotionProjectNote = "أضافة مسودة علي مشروع";
                        }
                        if (voucher.CostCenterId > 0)
                        {
                            CostId = voucher.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }
                        }


                    }
                    else
                    {
                        if (voucher.CostCenterId > 0)
                        {
                            CostId = voucher.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }

                    }
                    if (CostId == 0)
                    {
                        try
                        { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                        catch
                        { CostId = null; }
                    }
                    voucher.CostCenterId = CostId;
                    //Utilities utilVoucher = new Utilities((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());
                    //voucher.InvoiceValueText = utilVoucher.GetNumberAr();
                    voucher.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();

                    int? itemToAccountId = 0;
                    int? itemAccountId = 0;
                    var vouchertypename = "";
                    var vouchertypename2 = "";
                    int? itemInvoiceId = 0;
                    decimal? AcPaid = 0;
                    decimal? AcPaid2 = 0;
                    decimal? AcPaid3 = 0;

                    decimal? SVAT = 0;
                    int? itemTaxType = 0;

                    foreach (var item in voucher.VoucherDetails.ToList())
                    {


                        //ToAccount = item.AccountId??0;
                        ToAccount = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;

                        // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                        //if(ServicesUpdated!=null)
                        //{
                        //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                        //}

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AddUser = UserId;
                        item.PayType = voucher.PayType;

                        item.AddDate = DateTime.Now;

                        decimal? TotalWithoutVAT = (item.Amount);
                        decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                        SVAT = (voucher.TaxAmount);
                        //SVAT = SVAT + (item.TaxAmount * item.Qty);

                        item.CostCenterId = CostId;
                        decimal? Depit = item.Amount + item.TaxAmount;

                        //decimal? x =Convert.ToDecimal(voucher.PaidValue * Convert.ToDecimal(0.15));

                        //decimal? Depit = Convert.ToDecimal((voucher.PaidValue * Convert.ToDecimal(0.15)));

                        decimal? VAT = item.TaxAmount;
                        item.TotalAmount = item.TotalAmount;
                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        _TaamerProContext.VoucherDetails.Add(item);
                        vouchertypename = voucher.Type == 2 ? " فاتورة" + " مبيعات" : "فاتورة";
                        vouchertypename2 = " خصم فاتورة ";

                        itemToAccountId = item.ToAccountId;
                        itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                        itemInvoiceId = item.InvoiceId;
                        itemTaxType = item.TaxType;

                    }
                    CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                    BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                    if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                    {
                        if (itemTaxType == 2)
                        {
                            AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                            AcPaid3 = 0;

                        }
                        else
                        {
                            AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                            AcPaid3 = 0;

                        }
                    }
                    else
                    {
                        if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                        {
                            AcPaid = Convert.ToDecimal(voucher.PaidValue);
                            AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;

                            //Edit 21-01-2024 Ostaze Mohammed Req
                            //AcPaid3 = 0;
                            AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                            var CustomerData = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                            CustomerAccountPaid = CustomerData.AccountId ?? 0;
                            BoxBankAccountPaid = itemToAccountId ?? 0;

                        }
                        else
                        {
                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);

                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);

                            }
                        }

                    }

                    if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                    {
                        var CustomerDataV = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                        CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                    }


                    _TaamerProContext.SaveChanges();
                    voucher.TransactionDetails.Add(new Transactions
                    {

                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        AccountId = CustomerAccountPaid,
                        CostCenterId = CostId,
                        AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 1,
                        Depit = AcPaid,
                        Credit = 0,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });

                    if (Disc > 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = Branch.SaleDiscountAccId,
                            CostCenterId = CostId,
                            AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = voucher.DiscountValue,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });

                    }

                    //credit 


                    foreach (var item in voucher.VoucherDetails.ToList())
                    {


                        var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s => s.ServicesId == item.ServicesPriceId).FirstOrDefault();
                        int? AccountEradat = 0;
                        if (ServicesUpdated != null)
                        {
                            AccountEradat = ServicesUpdated.AccountId ?? 0;
                            if (AccountEradat == 0)
                            {
                                //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                            }
                        }
                        else
                        {
                            //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                        }
                        item.AccountId = AccountEradat;
                        item.Qty = item.Qty ?? 1;
                        item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                        if (itemTaxType == 2)
                        {
                            item.Amount = (item.Amount * item.Qty) - (item.DiscountValue_Det);
                        }
                        else
                        {
                            item.Amount = item.TotalAmount - (item.TaxAmount);
                        }
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = AccountEradat,


                            CostCenterId = CostId,
                            AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == AccountEradat)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = item.Amount,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            VoucherDetailsId = item.VoucherDetailsId,

                        });


                    }

                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        //AccountId = AccOVAT,// الضريبة
                        AccountId = Branch.TaxsAccId,// الضريبة

                        CostCenterId = CostId,//item.CostCenterId,
                        AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 3,
                        Credit = SVAT,
                        Depit = 0,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });

                    if (AcPaid3 != 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = BoxBankAccountPaid,
                            CostCenterId = CostId,
                            AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 4,
                            Depit = AcPaid3,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 5,
                            Depit = 0,
                            Credit = AcPaid3,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });


                    }


                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);


                    if (voucher.ServicesPriceOffer != null && voucher.ServicesPriceOffer.Count > 0)
                    {
                        foreach (var item in voucher.ServicesPriceOffer)
                        {
                            item.AddUser = UserId;
                            item.AddDate = DateTime.Now;
                            item.InvoiceId = voucher.InvoiceId;
                            _TaamerProContext.Acc_Services_PriceOffer.Add(item);
                        }
                    }


                    _TaamerProContext.SaveChanges();
                    voucher.QRCodeNum = "200010001000" + voucher.InvoiceId.ToString();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 2 && s.YearId == yearid && s.InvoiceNumber == voucher.InvoiceNumber && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucherDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                        if (TransactionDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    }
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة مسودة جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    List<int> voDetIds = new List<int>();
                    foreach (var itemV in voucher.VoucherDetails)
                    {
                        voDetIds.Add(itemV.VoucherDetailsId);
                    }
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId, InvoiceIsDeleted = voucher.IsDeleted, voucherDetObj = voDetIds };
                }
                else
                {

                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s => s.InvoiceId == voucher.InvoiceId)?.FirstOrDefault();
                    if (VoucherUpdated != null)
                    {
                        if (String.IsNullOrWhiteSpace(Convert.ToString(voucher.ToAccountId)) && voucher.Type == 2)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote3 = "فشل في حفظ مسودة";
                            _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }

                        VoucherUpdated.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                        VoucherUpdated.YearId = yearid;
                        VoucherUpdated.ToAccountId = voucher.ToAccountId;
                        VoucherUpdated.UpdateUser = UserId;
                        VoucherUpdated.BranchId = BranchId;
                        VoucherUpdated.UpdateDate = DateTime.Now;
                        VoucherUpdated.ProjectId = voucher.ProjectId;

                        VoucherUpdated.CustomerId = voucher.CustomerId;
                        VoucherUpdated.Date = voucher.Date;
                        VoucherUpdated.HijriDate = voucher.HijriDate;
                        VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        VoucherUpdated.DiscountPercentage = voucher.DiscountPercentage;
                        VoucherUpdated.DiscountValue = voucher.DiscountValue;
                        VoucherUpdated.TotalValue = voucher.TotalValue;
                        VoucherUpdated.InvoiceValueText = voucher.InvoiceValueText;
                        VoucherUpdated.Paid = voucher.Paid;
                        VoucherUpdated.TaxAmount = voucher.TaxAmount;
                        VoucherUpdated.PayType = voucher.PayType;
                        VoucherUpdated.printBankAccount = voucher.printBankAccount;
                        VoucherUpdated.PaidValue = voucher.PaidValue;
                        VoucherUpdated.InvoiceNotes = voucher.InvoiceNotes;
                        VoucherUpdated.Notes = voucher.Notes;

                        VoucherUpdated.DunCalc = false;

                        if (voucher.ProjectId != null)
                        {

                            var project = _TaamerProContext.Project.Where(s => s.ProjectId == voucher.ProjectId).FirstOrDefault();
                            if (project != null)
                            {
                                project.MotionProject = 1;
                                project.MotionProjectDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                                project.MotionProjectNote = "أضافة مسودة علي مشروع";
                            }
                            if (voucher.CostCenterId > 0)
                            {
                                CostId = voucher.CostCenterId;
                            }
                            else
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                {
                                    try
                                    { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                    catch
                                    { CostId = null; }
                                }
                            }


                        }
                        else
                        {
                            if (voucher.CostCenterId > 0)
                            {
                                CostId = voucher.CostCenterId;
                            }
                            else
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }

                        }
                        if (CostId == 0)
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }
                        VoucherUpdated.CostCenterId = CostId;
                        //Utilities utilVoucher = new Utilities((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());
                        //voucher.InvoiceValueText = utilVoucher.GetNumberAr();
                        VoucherUpdated.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                        var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                        var ObjList = new List<object>();

                        int? itemToAccountId = 0;
                        int? itemAccountId = 0;
                        var vouchertypename = "";
                        var vouchertypename2 = "";
                        int? itemInvoiceId = 0;
                        decimal? AcPaid = 0;
                        decimal? AcPaid2 = 0;
                        decimal? AcPaid3 = 0;

                        decimal? SVAT = 0;
                        int? itemTaxType = 0;

                        foreach (var item in voucher.VoucherDetails.ToList())
                        {
                            item.InvoiceId = VoucherUpdated.InvoiceId;
                            //item.VoucherDetailsId = 0;
                            //ToAccount = item.AccountId??0;
                            ToAccount = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;

                            // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                            //if(ServicesUpdated!=null)
                            //{
                            //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                            //}

                            ObjList.Add(new { item.AccountId, item.CostCenterId });
                            item.AddUser = UserId;
                            item.PayType = voucher.PayType;

                            item.AddDate = DateTime.Now;

                            decimal? TotalWithoutVAT = (item.Amount);
                            decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                            SVAT = (voucher.TaxAmount);
                            //SVAT = SVAT + (item.TaxAmount * item.Qty);

                            item.CostCenterId = CostId;
                            decimal? Depit = item.Amount + item.TaxAmount;

                            //decimal? x =Convert.ToDecimal(voucher.PaidValue * Convert.ToDecimal(0.15));

                            //decimal? Depit = Convert.ToDecimal((voucher.PaidValue * Convert.ToDecimal(0.15)));

                            decimal? VAT = item.TaxAmount;
                            item.TotalAmount = item.TotalAmount;
                            //Utilities utilDetails = new Utilities(Depit.ToString());
                            //item.TFK = utilDetails.GetNumberAr();
                            item.TFK = ConvertToWord_NEW(Depit.ToString());

                            _TaamerProContext.VoucherDetails.Add(item);
                            vouchertypename = voucher.Type == 4 ? " فاتورة" + " مبيعات" : "فاتورة";
                            vouchertypename2 = " خصم فاتورة ";

                            itemToAccountId = item.ToAccountId;
                            itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                            itemInvoiceId = item.InvoiceId;
                            itemTaxType = item.TaxType;

                        }
                        CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                        BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                        if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                        {
                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = 0;

                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = 0;

                            }
                        }
                        else
                        {
                            if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                            {
                                AcPaid = Convert.ToDecimal(voucher.PaidValue);
                                AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;

                                //Edit 21-01-2024 Ostaze Mohammed Req
                                //AcPaid3 = 0;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                var CustomerData = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                                CustomerAccountPaid = CustomerData.AccountId ?? 0;
                                BoxBankAccountPaid = itemToAccountId ?? 0;

                            }
                            else
                            {
                                if (itemTaxType == 2)
                                {
                                    AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);

                                }
                                else
                                {
                                    AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);

                                }
                            }

                        }

                        if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                        {
                            var CustomerDataV = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                            CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                        }

                        var VoucherDetailsV = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                        var TransactionDetailsV = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                        if (VoucherDetailsV.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetailsV);
                        if (TransactionDetailsV.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransactionDetailsV);
                        _TaamerProContext.VoucherDetails.AddRange(voucher.VoucherDetails);

                        _TaamerProContext.SaveChanges();



                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = AcPaid,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });

                        if (Disc > 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {

                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = Branch.SaleDiscountAccId,
                                CostCenterId = CostId,
                                AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 1,
                                Depit = voucher.DiscountValue,
                                Credit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                InvoiceReference = "",

                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });

                        }

                        //credit 


                        foreach (var item in voucher.VoucherDetails.ToList())
                        {


                            var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s => s.ServicesId == item.ServicesPriceId).FirstOrDefault();
                            int? AccountEradat = 0;
                            if (ServicesUpdated != null)
                            {
                                AccountEradat = ServicesUpdated.AccountId ?? 0;
                                if (AccountEradat == 0)
                                {
                                    //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                                }
                            }
                            else
                            {
                                //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                            }
                            item.AccountId = AccountEradat;
                            item.Qty = item.Qty ?? 1;
                            item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                            if (itemTaxType == 2)
                            {
                                item.Amount = (item.Amount * item.Qty) - (item.DiscountValue_Det);
                            }
                            else
                            {
                                item.Amount = item.TotalAmount - (item.TaxAmount);
                            }
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = AccountEradat,


                                CostCenterId = CostId,
                                AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == AccountEradat)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 2,
                                Credit = item.Amount,
                                Depit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",

                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                VoucherDetailsId = item.VoucherDetailsId,

                            });


                        }

                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            //AccountId = AccOVAT,// الضريبة
                            AccountId = Branch.TaxsAccId,// الضريبة

                            CostCenterId = CostId,//item.CostCenterId,
                            AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 3,
                            Credit = SVAT,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });

                        if (AcPaid3 != 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = BoxBankAccountPaid,
                                CostCenterId = CostId,
                                AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 4,
                                Depit = AcPaid3,
                                Credit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = CustomerAccountPaid,
                                CostCenterId = CostId,
                                AccountType = _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 5,
                                Depit = 0,
                                Credit = AcPaid3,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });


                        }


                        _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);


                        if (voucher.ServicesPriceOffer != null && voucher.ServicesPriceOffer.Count > 0)
                        {
                            var ServicesPriceOffer = _TaamerProContext.Acc_Services_PriceOffer.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                            if (ServicesPriceOffer.Count() > 0) _TaamerProContext.Acc_Services_PriceOffer.RemoveRange(ServicesPriceOffer);
                            foreach (var item in voucher.ServicesPriceOffer)
                            {
                                item.AddUser = UserId;
                                item.AddDate = DateTime.Now;
                                item.InvoiceId = VoucherUpdated.InvoiceId;
                                _TaamerProContext.Acc_Services_PriceOffer.Add(item);
                            }
                        }


                        _TaamerProContext.SaveChanges();
                        VoucherUpdated.QRCodeNum = "200010001000" + voucher.InvoiceId.ToString();
                        var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 2 && s.YearId == yearid && s.InvoiceNumber == VoucherUpdated.InvoiceNumber && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                        if (ExistInvoice.Count() > 1)
                        {
                            var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                            var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                            ExistInvoice.FirstOrDefault().IsDeleted = true;
                            ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                            var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                            var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                            if (VoucherDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                            if (TransactionDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                        }
                        _TaamerProContext.SaveChanges();
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote = "اضافة مسودة جديد" + " برقم " + voucher.InvoiceNumber; ;
                        _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                        //-----------------------------------------------------------------------------------------------------------------
                        List<int> voDetIds = new List<int>();
                        foreach (var itemV in voucher.VoucherDetails)
                        {
                            voDetIds.Add(itemV.VoucherDetailsId);
                        }
                        return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId, InvoiceIsDeleted = voucher.IsDeleted, voucherDetObj = voDetIds };
                    }
                    else
                    {
                       return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لم يمكنك التعديل الان" };

                    }
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ مسودة";
                _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveInvoiceForServicesNoti(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                var InvoiceReturn=0;
                var ToAccount = 0;
                var CustomerAccountPaid = 0;
                var BoxBankAccountPaid = 0;
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.ContractsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ اشعار دائن لفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureRevenueAccountedBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureRevenueAccountedBranchAccounts };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ اشعار دائن لفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.SaleDiscountAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ اشعار دائن لفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureThatTheSales, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatTheSales };

                }
                else if (Branch == null || Branch.TaxsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ اشعار دائن لفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureTheVAT, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                }


                foreach (var item in voucher.VoucherDetails.ToList())
                {


                     var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                    int? AccountEradat = 0;
                    if (ServicesUpdated != null)
                    {
                        AccountEradat = ServicesUpdated.AccountId ?? 0;
                        if (AccountEradat == 0)
                        {
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                        }
                    }
                    else
                    {
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                    }
                }


                int? CostId;
                voucher.TransactionDetails = new List<Transactions>();

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ اشعار دائن لفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                var VoucherDetailsUpdated = _VoucherDetailsRepository.GetAllDetailsByInvoiceId(voucher.InvoiceId).Result;


                decimal Disc = 0;
                //if (voucher.DiscountValue != null)
                //{
                //    if (Convert.ToInt32(voucher.DiscountValue) != 0)
                //    {

                //        Disc = voucher.DiscountValue ?? 0;
                //    }
                //    else
                //    {
                //        Disc = 0;

                //    }
                //}
                //else
                //{
                //    Disc = 0;
                //}
                if (voucher.InvoiceId > 0)
                {
                    if (String.IsNullOrWhiteSpace(Convert.ToString(voucher.ToAccountId)) && voucher.Type == 2)
                    {

                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote3 = "فشل في حفظ اشعار دائن لفاتورة";
                        _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                    }

                    var VoucherUpdatedCredit = _TaamerProContext.Invoices.Where(s=> s.IsDeleted == false && s.CreditNotiId==voucher.InvoiceId).ToList();

                    if (VoucherUpdatedCredit.Count()==0)
                    {
                        voucher.IsPost = false;
                        voucher.Rad = false;
                        voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                        voucher.YearId = yearid;
                        voucher.ToAccountId = voucher.ToAccountId;
                        voucher.AddUser = UserId;
                        voucher.BranchId = BranchId;
                        voucher.AddDate = DateTime.Now;
                        voucher.ProjectId = voucher.ProjectId;
                        voucher.DunCalc = false;
                        voucher.CreditNotiId = voucher.InvoiceId;
                        voucher.DepitNotiId = null;
                        voucher.InvoiceId = 0;
                        voucher.InvUUID = GET_UUID();


                        var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                        if (vouchercheck.Count() > 0)
                        {
                            //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumberNotiCredit(voucher.Type, yearid, BranchId).Result;
                            var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                            //var NewNextInv = string.Format("{0:000000}", NextInv);

                            voucher.InvoiceNumber = NewNextInv.ToString();
                        }

                        if (voucher.ProjectId != null)
                        {

                            if (voucher.CostCenterId > 0)
                            {
                                CostId = voucher.CostCenterId;
                            }
                            else
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                {
                                    try
                                    { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                    catch
                                    { CostId = null; }
                                }
                            }


                        }
                        else
                        {
                            if (voucher.CostCenterId > 0)
                            {
                                CostId = voucher.CostCenterId;
                            }
                            else
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }
                        }
                        if (CostId == 0)
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }
                        voucher.CostCenterId = CostId;

                        voucher.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                        var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                        _TaamerProContext.Invoices.Add(voucher);
                        var ObjList = new List<object>();

                        int? itemToAccountId = 0;
                        int? itemAccountId = 0;
                        var vouchertypename = "";
                        var vouchertypename2 = "";
                        int? itemInvoiceId = 0;
                        decimal? AcPaid = 0;
                        decimal? AcPaid2 = 0;
                        decimal? AcPaid3 = 0;

                        decimal? SVAT = 0;
                        int? itemTaxType = 0;
                        //int? AccountEradat = 0;

                        foreach (var item in voucher.VoucherDetails.ToList())
                        {


                            ToAccount = item.AccountId ?? 0;
                            // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                            //if (ServicesUpdated != null)
                            //{
                            //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                            //}

                            if (VoucherDetailsUpdated.Count() > 0)
                            {

                                foreach (var Det in VoucherDetailsUpdated.ToList())
                                {
                                    if (item.ServicesPriceId == Det.ServicesPriceId)
                                    {
                                        var QtyDiff = Det.Qty - item.Qty;
                                        if (QtyDiff <= 0)
                                        {
                                            Det.IsRetrieve = 1;

                                        }
                                    }
                                }

                            }

                            ObjList.Add(new { item.AccountId, item.CostCenterId });
                            item.AddUser = UserId;
                            item.PayType = voucher.PayType;

                            item.AddDate = DateTime.Now;

                            decimal? TotalWithoutVAT = (item.Amount);
                            decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                            SVAT = (voucher.TaxAmount);
                            //SVAT = SVAT + (item.TaxAmount * item.Qty);

                            item.CostCenterId = CostId;
                            decimal? Depit = item.Amount + item.TaxAmount;
                            decimal? VAT = item.TaxAmount;
                            item.TotalAmount = item.TotalAmount;
                            item.TFK = ConvertToWord_NEW(Depit.ToString());
                            _TaamerProContext.VoucherDetails.Add(item);
                            vouchertypename = "اشعار دائن لفاتورة";
                            vouchertypename2 = " خصم فاتورة ";

                            itemToAccountId = item.ToAccountId;
                            itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                            itemInvoiceId = item.InvoiceId;
                            itemTaxType = item.TaxType;

                        }
                        CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                        BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                        if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                        {


                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = 0;
                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = 0;

                            }
                        }
                        else
                        {
                            if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                            {
                                AcPaid = Convert.ToDecimal(voucher.PaidValue);
                                AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;
                                //Edit 21-01-2024 Ostaze Mohammed Req
                                //AcPaid3 = 0;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                var CustomerData = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                                CustomerAccountPaid = CustomerData.AccountId ?? 0;
                                BoxBankAccountPaid = itemToAccountId ?? 0;
                            }
                            else
                            {
                                if (itemTaxType == 2)
                                {
                                    AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                }
                                else
                                {
                                    AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                }

                            }

                        }
                        if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                        {
                            var CustomerDataV = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                            CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                        }
                        _TaamerProContext.SaveChanges();
                        //credit 

                        foreach (var item in voucher.VoucherDetails.ToList())
                        {


                             var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                            int? AccountEradat = 0;
                            if (ServicesUpdated != null)
                            {
                                AccountEradat = ServicesUpdated.AccountId ?? 0;
                                if (AccountEradat == 0)
                                {
                                    //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                                }
                            }
                            else
                            {
                                //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                            }
                            item.AccountId = AccountEradat;
                            item.Qty = item.Qty ?? 1;
                            item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                            if (itemTaxType == 2)
                            {
                                item.Amount = (item.Amount * item.Qty) - (item.DiscountValue_Det);
                            }
                            else
                            {
                                item.Amount = item.TotalAmount - (item.TaxAmount);
                            }
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                //AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                                //AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = AccountEradat,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == AccountEradat)?.FirstOrDefault().Type,
                                Type = 29,
                                LineNumber = 1,
                                Depit = item.Amount,
                                Credit = 0,


                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                                InvoiceReference = "",
                                InvoiceId = itemInvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdated.JournalNumber,
                                VoucherDetailsId = item.VoucherDetailsId,

                            });


                        }



                        voucher.TransactionDetails.Add(new Transactions
                        {
                            //AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                            //AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = 29,
                            LineNumber = 2,
                            Credit = AcPaid,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = itemInvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdated.JournalNumber

                        });


                        voucher.TransactionDetails.Add(new Transactions
                        {
                            //AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                            //AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            //AccountId = AccOVAT,// الضريبة
                            AccountId = Branch.TaxsAccId,// الضريبة

                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                            Type = 29,
                            LineNumber = 2,
                            Credit = 0,
                            Depit = SVAT,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdated.JournalNumber

                        });
                        if (Disc > 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                //AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                                //AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                //AccountId = AccOVAT,// الضريبة
                                AccountId = Branch.SaleDiscountAccId,// الضريبة

                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                                Type = 29,
                                LineNumber = 2,
                                Credit = Disc,
                                Depit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename2 + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename2 + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                InvoiceReference = "",
                                InvoiceId = voucher.InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdated.JournalNumber

                            });

                        }



                        if (AcPaid3 != 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                //AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                                //AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = BoxBankAccountPaid,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                                Type = 29,
                                LineNumber = 4,
                                Depit = 0,
                                Credit = AcPaid3,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = VoucherUpdated.InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdated.JournalNumber

                            });
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                //AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                                //AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = CustomerAccountPaid,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                                Type = 29,
                                LineNumber = 5,
                                Depit = AcPaid3,
                                Credit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = VoucherUpdated.InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdated.JournalNumber

                            });


                        }

                        _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                        _TaamerProContext.SaveChanges();

                        InvoiceReturn = voucher.InvoiceId;



                        if (VoucherUpdated != null)
                        {
                            VoucherUpdated.CreditNotiId = -1;

                        }
                        var VoucherCredit = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.CreditNotiId == VoucherUpdated.InvoiceId);
                        if (VoucherCredit.Count() > 0)
                        {
                            if (VoucherCredit.FirstOrDefault().TotalValue == VoucherUpdated.TotalValue)
                            {
                                VoucherUpdated.Rad = true;
                                VoucherCredit.FirstOrDefault().Rad = true;

                                var CustomerPayment = _TaamerProContext.CustomerPayments.Where(s => s.IsDeleted == false && s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                                if(CustomerPayment.Count()>0)
                                {
                                    CustomerPayment.FirstOrDefault().IsCanceled = true;
                                }
                            }
                        }
                        if (voucher.ServicesPriceOffer != null && voucher.ServicesPriceOffer.Count > 0)
                        {
                            foreach (var item in voucher.ServicesPriceOffer)
                            {
                                item.AddUser = UserId;
                                item.AddDate = DateTime.Now;
                                item.InvoiceId = VoucherCredit.FirstOrDefault().InvoiceId;
                                _TaamerProContext.Acc_Services_PriceOffer.Add(item);
                            }
                        }

                        if (voucher.IsPost != true)
                        {
                            voucher.IsPost = true;
                            voucher.PostDate = voucher.Date;
                            voucher.PostHijriDate = voucher.HijriDate;

                            var newJournal = new Journals();
                            var JNo =  _JournalsRepository.GenerateNextJournalNumber(yearid ?? default(int), BranchId).Result;
                            if (voucher.Type == 10)
                            {
                                JNo = 1;
                            }
                            else
                            {
                                JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                            }


                            newJournal.JournalNo = JNo;
                            newJournal.YearMalia = yearid ?? default(int);
                            newJournal.VoucherId = voucher.InvoiceId;
                            newJournal.VoucherType = voucher.Type;
                            newJournal.BranchId = voucher.BranchId ?? 0;
                            newJournal.AddDate = DateTime.Now;
                            newJournal.AddUser = newJournal.UserId = UserId;
                            _TaamerProContext.Journals.Add(newJournal);
                            foreach (var trans in voucher.TransactionDetails.ToList())
                            {
                                trans.IsPost = true;
                                trans.JournalNo = newJournal.JournalNo;
                            }
                            voucher.JournalNumber = newJournal.JournalNo;
                            //_TaamerProContext.SaveChanges();
                        }


                    }
                    else
                    {
                        if (voucher.ProjectId != null)
                        {

                            if (voucher.CostCenterId > 0)
                            {
                                CostId = voucher.CostCenterId;
                            }
                            else
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                {
                                    try
                                    { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                    catch
                                    { CostId = null; }
                                }
                            }


                        }
                        else
                        {
                            if (voucher.CostCenterId > 0)
                            {
                                CostId = voucher.CostCenterId;
                            }
                            else
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }
                        }
                        if (CostId == 0)
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }
                        voucher.CostCenterId = CostId;

                        var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                        var ObjList = new List<object>();

                        VoucherUpdatedCredit.FirstOrDefault().InvoiceValue = VoucherUpdatedCredit.FirstOrDefault().InvoiceValue + voucher.InvoiceValue;
                        VoucherUpdatedCredit.FirstOrDefault().TaxAmount = VoucherUpdatedCredit.FirstOrDefault().TaxAmount + voucher.TaxAmount;
                        VoucherUpdatedCredit.FirstOrDefault().TotalValue = VoucherUpdatedCredit.FirstOrDefault().TotalValue + voucher.TotalValue;

                        VoucherUpdatedCredit.FirstOrDefault().InvoiceValueText = ConvertToWord_NEW(VoucherUpdatedCredit.FirstOrDefault().TotalValue.ToString());


                        int? itemToAccountId = 0;
                        int? itemAccountId = 0;
                        var vouchertypename = "";
                        var vouchertypename2 = "";
                        int? itemInvoiceId = 0;
                        decimal? AcPaid = 0;
                        decimal? AcPaid2 = 0;
                        decimal? AcPaid3 = 0;

                        decimal? SVAT = 0;
                        int? itemTaxType = 0;
                        //int? AccountEradat = 0;

                        foreach (var item in voucher.VoucherDetails.ToList())
                        {

                            ToAccount = item.AccountId ?? 0;
                            // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                            //if (ServicesUpdated != null)
                            //{
                            //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                            //}

                            if (VoucherDetailsUpdated.Count() > 0)
                            {

                                foreach (var Det in VoucherDetailsUpdated.ToList())
                                {
                                    if (item.ServicesPriceId == Det.ServicesPriceId)
                                    {
                                        var QtyDiff = Det.Qty - item.Qty;
                                        if (QtyDiff <= 0)
                                        {
                                            Det.IsRetrieve = 1;

                                        }
                                    }
                                }

                            }

                            ObjList.Add(new { item.AccountId, item.CostCenterId });
                            item.AddUser = UserId;
                            item.PayType = voucher.PayType;

                            item.AddDate = DateTime.Now;

                            decimal? TotalWithoutVAT = (item.Amount);
                            decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                            SVAT = (voucher.TaxAmount);
                            //SVAT = SVAT + (item.TaxAmount * item.Qty);

                            item.CostCenterId = CostId;
                            decimal? Depit = item.Amount + item.TaxAmount;
                            decimal? VAT = item.TaxAmount;
                            item.TotalAmount = item.TotalAmount;
                            item.TFK = ConvertToWord_NEW(Depit.ToString());
                            _TaamerProContext.VoucherDetails.Add(item);
                            vouchertypename = "اشعار دائن لفاتورة";
                            vouchertypename2 = " خصم فاتورة ";

                            itemToAccountId = item.ToAccountId;
                            itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                            itemInvoiceId = item.InvoiceId;
                            itemTaxType = item.TaxType;
                            item.InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId;

                        }
                        CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                        BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                        if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                        {


                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = 0;
                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = 0;

                            }
                        }
                        else
                        {
                            if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                            {
                                AcPaid = Convert.ToDecimal(voucher.PaidValue);
                                AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;
                                //Edit 21-01-2024 Ostaze Mohammed Req
                                //AcPaid3 = 0;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                var CustomerData = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                                CustomerAccountPaid = CustomerData.AccountId ?? 0;
                                BoxBankAccountPaid = itemToAccountId ?? 0;
                            }
                            else
                            {
                                if (itemTaxType == 2)
                                {
                                    AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                }
                                else
                                {
                                    AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                }

                            }

                        }
                        if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                        {
                            var CustomerDataV = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                            CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                        }
                        _TaamerProContext.SaveChanges();

                        //credit 

                        foreach (var item in voucher.VoucherDetails.ToList())
                        {


                             var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                            int? AccountEradat = 0;
                            if (ServicesUpdated != null)
                            {
                                AccountEradat = ServicesUpdated.AccountId ?? 0;
                                if (AccountEradat == 0)
                                {
                                    //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                                }
                            }
                            else
                            {
                                //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                            }
                            item.AccountId = AccountEradat;
                            item.Qty = item.Qty ?? 1;
                            item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                            if (itemTaxType == 2)
                            {
                                item.Amount = (item.Amount * item.Qty) - (item.DiscountValue_Det);
                            }
                            else
                            {
                                item.Amount = item.TotalAmount - (item.TaxAmount);
                            }
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                //AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                                //AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = AccountEradat,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == AccountEradat)?.FirstOrDefault()?.Type,
                                Type = 29,
                                LineNumber = 1,
                                Depit = item.Amount,
                                Credit = 0,


                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                                InvoiceReference = "",
                                InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdatedCredit.FirstOrDefault().JournalNumber,
                                VoucherDetailsId = item.VoucherDetailsId,

                            });


                        }


                        voucher.TransactionDetails.Add(new Transactions
                        {
                            //AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                            //AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = 29,
                            LineNumber = 2,
                            Credit = AcPaid,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdatedCredit.FirstOrDefault().JournalNumber,

                        });


                        voucher.TransactionDetails.Add(new Transactions
                        {
                            //AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                            //AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            //AccountId = AccOVAT,// الضريبة
                            AccountId = Branch.TaxsAccId,// الضريبة

                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                            Type = 29,
                            LineNumber = 2,
                            Credit = 0,
                            Depit = SVAT,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",
                            InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdatedCredit.FirstOrDefault().JournalNumber,

                        });
                        if (Disc > 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                //AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                                //AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                //AccountId = AccOVAT,// الضريبة
                                AccountId = Branch.SaleDiscountAccId,// الضريبة

                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                                Type = 29,
                                LineNumber = 2,
                                Credit = Disc,
                                Depit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename2 + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename2 + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                InvoiceReference = "",
                                InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdatedCredit.FirstOrDefault().JournalNumber,

                            });

                        }



                        if (AcPaid3 != 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                //AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                                //AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = BoxBankAccountPaid,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                                Type = 29,
                                LineNumber = 4,
                                Depit = 0,
                                Credit = AcPaid3,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdatedCredit.FirstOrDefault().JournalNumber,

                            });
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                //AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                                //AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = CustomerAccountPaid,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault()?.Type,
                                Type = 29,
                                LineNumber = 5,
                                Depit = AcPaid3,
                                Credit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdatedCredit.FirstOrDefault().JournalNumber,

                            });


                        }

                        _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                        _TaamerProContext.SaveChanges();


                        InvoiceReturn = VoucherUpdatedCredit.FirstOrDefault().InvoiceId;


                        if (VoucherUpdated != null)
                        {
                            VoucherUpdated.CreditNotiId = -1;

                        }
                        var VoucherCredit = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.CreditNotiId == VoucherUpdated.InvoiceId);
                        if (VoucherCredit.Count() > 0)
                        {
                            if (VoucherCredit.FirstOrDefault().TotalValue == VoucherUpdated.TotalValue)
                            {
                                VoucherUpdated.Rad = true;
                                VoucherCredit.FirstOrDefault().Rad = true;

                                var CustomerPayment = _TaamerProContext.CustomerPayments.Where(s => s.IsDeleted == false && s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                                if (CustomerPayment.Count() > 0)
                                {
                                    CustomerPayment.FirstOrDefault().IsCanceled = true;
                                }

                            }
                        }

                        if (voucher.ServicesPriceOffer != null && voucher.ServicesPriceOffer.Count > 0)
                        {
                            var ServicesPriceOffer = _TaamerProContext.Acc_Services_PriceOffer.Where(s => s.InvoiceId == VoucherCredit.FirstOrDefault().InvoiceId).ToList();
                            if (ServicesPriceOffer.Count() > 0) _TaamerProContext.Acc_Services_PriceOffer.RemoveRange(ServicesPriceOffer);

                            foreach (var item in voucher.ServicesPriceOffer)
                            {
                                item.AddUser = UserId;
                                item.AddDate = DateTime.Now;
                                item.InvoiceId = VoucherCredit.FirstOrDefault().InvoiceId;
                                _TaamerProContext.Acc_Services_PriceOffer.Add(item);
                            }
                        }

                    }



                    //voucher.QRCodeNum = "200010001000" + voucher.InvoiceId.ToString();
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة اشعار دائن لفاتورة " + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    List<int> voDetIds = new List<int>();
                    foreach (var itemV in voucher.VoucherDetails)
                    {
                        voDetIds.Add(itemV.VoucherDetailsId);
                    }
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully , ReturnedParm = InvoiceReturn, voucherDetObj = voDetIds };
                }
                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };

            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ اشعار دائن";
                _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveInvoiceForServicesNotiDepit(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {

                var ToAccount = 0;
                var CustomerAccountPaid = 0;
                var BoxBankAccountPaid = 0;
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.ContractsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ اشعار مدين لفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureRevenueAccountedBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureRevenueAccountedBranchAccounts };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ اشعار مدين لفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.SaleDiscountAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ اشعار مدين لفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureThatTheSales, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatTheSales };

                }
                else if (Branch == null || Branch.TaxsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ اشعار مدين لفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureTheVAT, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                }

                foreach (var item in voucher.VoucherDetails.ToList())
                {

                     var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                    int? AccountEradat = 0;
                    if (ServicesUpdated != null)
                    {
                        AccountEradat = ServicesUpdated.AccountId ?? 0;
                        if (AccountEradat == 0)
                        {
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                        }
                    }
                    else
                    {
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                    }
                }



                int? CostId;
                voucher.TransactionDetails = new List<Transactions>();

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ اشعار مدين لفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                var VoucherDetailsUpdated = _VoucherDetailsRepository.GetAllDetailsByInvoiceId(voucher.InvoiceId).Result;


                decimal Disc = 0;
                //if (voucher.DiscountValue != null)
                //{
                //    if (Convert.ToInt32(voucher.DiscountValue) != 0)
                //    {

                //        Disc = voucher.DiscountValue ?? 0;
                //    }
                //    else
                //    {
                //        Disc = 0;

                //    }
                //}
                //else
                //{
                //    Disc = 0;
                //}
                if (voucher.InvoiceId > 0)
                {
                    if (String.IsNullOrWhiteSpace(Convert.ToString(voucher.ToAccountId)) && voucher.Type == 2)
                    {

                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote3 = "فشل في حفظ اشعار مدين لفاتورة";
                        _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                    }

                    var VoucherUpdatedCredit = _TaamerProContext.Invoices.Where(s => s.IsDeleted==false && s.DepitNotiId == voucher.InvoiceId).ToList();

                    if (VoucherUpdatedCredit.Count() == 0)
                    {
                        voucher.IsPost = false;
                        voucher.Rad = false;
                        voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                        voucher.YearId = yearid;
                        voucher.ToAccountId = voucher.ToAccountId;
                        voucher.AddUser = UserId;
                        voucher.BranchId = BranchId;
                        voucher.AddDate = DateTime.Now;
                        voucher.ProjectId = voucher.ProjectId;
                        voucher.DunCalc = false;
                        voucher.CreditNotiId = null;
                        voucher.DepitNotiId = voucher.InvoiceId;
                        voucher.InvoiceId = 0;
                        voucher.InvUUID = GET_UUID();


                        var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                        if (vouchercheck.Count() > 0)
                        {
                            //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumberNotiDepit(voucher.Type, yearid, BranchId).Result;
                            var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                            //var NewNextInv = string.Format("{0:000000}", NextInv);

                            voucher.InvoiceNumber = NewNextInv.ToString();
                        }

                        if (voucher.ProjectId != null)
                        {

                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }

                        }
                        else
                        {
                            if (voucher.CostCenterId > 0)
                            {
                                CostId = voucher.CostCenterId;
                            }
                            else
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }
                        }
                        if (CostId == 0)
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }
                        voucher.CostCenterId = CostId;

                        voucher.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                        var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                        _TaamerProContext.Invoices.Add(voucher);
                        var ObjList = new List<object>();

                        int? itemToAccountId = 0;
                        int? itemAccountId = 0;
                        var vouchertypename = "";
                        var vouchertypename2 = "";
                        int? itemInvoiceId = 0;
                        decimal? AcPaid = 0;
                        decimal? AcPaid2 = 0;
                        decimal? AcPaid3 = 0;

                        decimal? SVAT = 0;
                        int? itemTaxType = 0;
                        //int? AccountEradat = 0;

                        foreach (var item in voucher.VoucherDetails.ToList())
                        {


                            ToAccount = item.AccountId ?? 0;
                            // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                            //if (ServicesUpdated != null)
                            //{
                            //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                            //}

                            if (VoucherDetailsUpdated.Count() > 0)
                            {

                                foreach (var Det in VoucherDetailsUpdated.ToList())
                                {
                                    if (item.ServicesPriceId == Det.ServicesPriceId)
                                    {
                                        var QtyDiff = Det.Qty - item.Qty;
                                        if (QtyDiff <= 0)
                                        {
                                            Det.IsRetrieve = 1;

                                        }
                                    }
                                }

                            }

                            ObjList.Add(new { item.AccountId, item.CostCenterId });
                            item.AddUser = UserId;
                            item.PayType = voucher.PayType;

                            item.AddDate = DateTime.Now;

                            decimal? TotalWithoutVAT = (item.Amount);
                            decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                            SVAT = (voucher.TaxAmount);
                            //SVAT = SVAT + (item.TaxAmount * item.Qty);

                            item.CostCenterId = CostId;
                            decimal? Depit = item.Amount + item.TaxAmount;
                            decimal? VAT = item.TaxAmount;
                            item.TotalAmount = item.TotalAmount;
                            item.TFK = ConvertToWord_NEW(Depit.ToString());
                            _TaamerProContext.VoucherDetails.Add(item);
                            vouchertypename = "اشعار مدين لفاتورة";
                            vouchertypename2 = " خصم فاتورة ";

                            itemToAccountId = item.ToAccountId;
                            itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0; 
                            itemInvoiceId = item.InvoiceId;
                            itemTaxType = item.TaxType;

                        }
                        CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                        BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                        if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                        {


                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = 0;
                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = 0;

                            }
                        }
                        else
                        {
                            if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                            {
                                AcPaid = Convert.ToDecimal(voucher.PaidValue);
                                AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;
                                //Edit 21-01-2024 Ostaze Mohammed Req
                                //AcPaid3 = 0;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                var CustomerData = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                                CustomerAccountPaid = CustomerData.AccountId ?? 0;
                                BoxBankAccountPaid = itemToAccountId ?? 0;
                            }
                            else
                            {
                                if (itemTaxType == 2)
                                {
                                    AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                }
                                else
                                {
                                    AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                }

                            }

                        }
                        if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                        {
                            var CustomerDataV = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                            CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                        }
                        _TaamerProContext.SaveChanges();

                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = 30,
                            LineNumber = 1,
                            Depit = AcPaid,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });

                        if (Disc > 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {

                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = Branch.SaleDiscountAccId,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                                Type = 30,
                                LineNumber = 1,
                                Depit = voucher.DiscountValue,
                                Credit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",

                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });

                        }

                        //credit 

                        foreach (var item in voucher.VoucherDetails.ToList())
                        {


                             var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                            int? AccountEradat = 0;
                            if (ServicesUpdated != null)
                            {
                                AccountEradat = ServicesUpdated.AccountId ?? 0;
                                if (AccountEradat == 0)
                                {
                                    //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                                }
                            }
                            else
                            {
                                //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                            }
                            item.AccountId = AccountEradat;
                            item.Qty = item.Qty ?? 1;
                            item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                            if (itemTaxType == 2)
                            {
                                item.Amount = (item.Amount * item.Qty) - (item.DiscountValue_Det);
                            }
                            else
                            {
                                item.Amount = item.TotalAmount - (item.TaxAmount);
                            }
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = AccountEradat,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== AccountEradat)?.FirstOrDefault()?.Type,
                                Type = 30,
                                LineNumber = 2,
                                Credit = item.Amount,
                                Depit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                VoucherDetailsId = item.VoucherDetailsId,


                            });


                        }



                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = Branch.TaxsAccId,

                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                            Type = 30,
                            LineNumber = 3,
                            Credit = SVAT,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });

                        if (AcPaid3 != 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = BoxBankAccountPaid,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                                Type = 30,
                                LineNumber = 4,
                                Depit = AcPaid3,
                                Credit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = CustomerAccountPaid,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                                Type = 30,
                                LineNumber = 5,
                                Depit = 0,
                                Credit = AcPaid3,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });


                        }

                        _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                        _TaamerProContext.SaveChanges();


                        if (VoucherUpdated != null)
                        {
                            VoucherUpdated.DepitNotiId = -1;

                        }

                        if (voucher.IsPost != true)
                        {
                            voucher.IsPost = true;
                            voucher.PostDate = voucher.Date;
                            voucher.PostHijriDate = voucher.HijriDate;

                            var newJournal = new Journals();
                            var JNo =  _JournalsRepository.GenerateNextJournalNumber(yearid ?? default(int), BranchId).Result;
                            if (voucher.Type == 10)
                            {
                                JNo = 1;
                            }
                            else
                            {
                                JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                            }


                            newJournal.JournalNo = JNo;
                            newJournal.YearMalia = yearid ?? default(int);
                            newJournal.VoucherId = voucher.InvoiceId;
                            newJournal.VoucherType = voucher.Type;
                            newJournal.BranchId = voucher.BranchId ?? 0;
                            newJournal.AddDate = DateTime.Now;
                            newJournal.AddUser = newJournal.UserId = UserId;
                            _TaamerProContext.Journals.Add(newJournal);
                            foreach (var trans in voucher.TransactionDetails.ToList())
                            {
                                trans.IsPost = true;
                                trans.JournalNo = newJournal.JournalNo;
                            }
                            voucher.JournalNumber = newJournal.JournalNo;
                            //_TaamerProContext.SaveChanges();
                        }



                    }
                    else
                    {

                        if (voucher.ProjectId != null)
                        {

                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }

                        }
                        else
                        {
                            if (voucher.CostCenterId > 0)
                            {
                                CostId = voucher.CostCenterId;
                            }
                            else
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }
                        }
                        if (CostId == 0)
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }

                        var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                        var ObjList = new List<object>();


                        VoucherUpdatedCredit.FirstOrDefault().InvoiceValue = VoucherUpdatedCredit.FirstOrDefault().InvoiceValue + voucher.InvoiceValue;
                        VoucherUpdatedCredit.FirstOrDefault().TaxAmount = VoucherUpdatedCredit.FirstOrDefault().TaxAmount + voucher.TaxAmount;
                        VoucherUpdatedCredit.FirstOrDefault().TotalValue = VoucherUpdatedCredit.FirstOrDefault().TotalValue + voucher.TotalValue;

                        VoucherUpdatedCredit.FirstOrDefault().InvoiceValueText = ConvertToWord_NEW(VoucherUpdatedCredit.FirstOrDefault().TotalValue.ToString());


                        int? itemToAccountId = 0;
                        int? itemAccountId = 0;
                        var vouchertypename = "";
                        var vouchertypename2 = "";
                        int? itemInvoiceId = 0;
                        decimal? AcPaid = 0;
                        decimal? AcPaid2 = 0;
                        decimal? AcPaid3 = 0;

                        decimal? SVAT = 0;
                        int? itemTaxType = 0;
                        //int? AccountEradat = 0;

                        foreach (var item in voucher.VoucherDetails.ToList())
                        {


                            ToAccount = item.AccountId ?? 0;
                            // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                            //if (ServicesUpdated != null)
                            //{
                            //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                            //}

                            if (VoucherDetailsUpdated.Count() > 0)
                            {

                                foreach (var Det in VoucherDetailsUpdated.ToList())
                                {
                                    if (item.ServicesPriceId == Det.ServicesPriceId)
                                    {
                                        var QtyDiff = Det.Qty - item.Qty;
                                        if (QtyDiff <= 0)
                                        {
                                            Det.IsRetrieve = 1;

                                        }
                                    }
                                }

                            }

                            ObjList.Add(new { item.AccountId, item.CostCenterId });
                            item.AddUser = UserId;
                            item.PayType = voucher.PayType;

                            item.AddDate = DateTime.Now;

                            decimal? TotalWithoutVAT = (item.Amount);
                            decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                            SVAT = (voucher.TaxAmount);
                            //SVAT = SVAT + (item.TaxAmount * item.Qty);

                            item.CostCenterId = CostId;
                            decimal? Depit = item.Amount + item.TaxAmount;
                            decimal? VAT = item.TaxAmount;
                            item.TotalAmount = item.TotalAmount;
                            item.TFK = ConvertToWord_NEW(Depit.ToString());
                            _TaamerProContext.VoucherDetails.Add(item);
                            vouchertypename = "اشعار مدين لفاتورة";
                            vouchertypename2 = " خصم فاتورة ";

                            itemToAccountId = item.ToAccountId;
                            itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                            itemInvoiceId = item.InvoiceId;
                            itemTaxType = item.TaxType;
                            item.InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId;
                        }
                        CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                        BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                        if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                        {


                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = 0;
                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = 0;

                            }
                        }
                        else
                        {
                            if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                            {
                                AcPaid = Convert.ToDecimal(voucher.PaidValue);
                                AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;
                                //Edit 21-01-2024 Ostaze Mohammed Req
                                //AcPaid3 = 0;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                var CustomerData = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                                CustomerAccountPaid = CustomerData.AccountId ?? 0;
                                BoxBankAccountPaid = itemToAccountId ?? 0;
                            }
                            else
                            {
                                if (itemTaxType == 2)
                                {
                                    AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                }
                                else
                                {
                                    AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                }

                            }

                        }
                        if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                        {
                            var CustomerDataV = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                            CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                        }
                        _TaamerProContext.SaveChanges();
                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = 30,
                            LineNumber = 1,
                            Depit = AcPaid,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdatedCredit.FirstOrDefault().JournalNumber,
                        });

                        if (Disc > 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {

                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = Branch.SaleDiscountAccId,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                                Type = 30,
                                LineNumber = 1,
                                Depit = voucher.DiscountValue,
                                Credit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",

                                InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdatedCredit.FirstOrDefault().JournalNumber,

                            });

                        }

                        //credit 

                        foreach (var item in voucher.VoucherDetails.ToList())
                        {


                             var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                            int? AccountEradat = 0;
                            if (ServicesUpdated != null)
                            {
                                AccountEradat = ServicesUpdated.AccountId ?? 0;
                                if (AccountEradat == 0)
                                {
                                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                                }
                            }
                            else
                            {
                                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                            }
                            item.AccountId = AccountEradat;
                            item.Qty = item.Qty ?? 1;
                            item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                            if (itemTaxType == 2)
                            {
                                item.Amount = (item.Amount * item.Qty) - (item.DiscountValue_Det);
                            }
                            else
                            {
                                item.Amount = item.TotalAmount - (item.TaxAmount);
                            }
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = AccountEradat,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == AccountEradat)?.FirstOrDefault()?.Type,
                                Type = 30,
                                LineNumber = 2,
                                Credit = item.Amount,
                                Depit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdatedCredit.FirstOrDefault().JournalNumber,
                                VoucherDetailsId = item.VoucherDetailsId,


                            });


                        }

                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = Branch.TaxsAccId,

                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                            Type = 30,
                            LineNumber = 3,
                            Credit = SVAT,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdatedCredit.FirstOrDefault().JournalNumber,

                        });

                        if (AcPaid3 != 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = BoxBankAccountPaid,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                                Type = 30,
                                LineNumber = 4,
                                Depit = AcPaid3,
                                Credit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdatedCredit.FirstOrDefault().JournalNumber,

                            });
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = CustomerAccountPaid,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                                Type = 30,
                                LineNumber = 5,
                                Depit = 0,
                                Credit = AcPaid3,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = VoucherUpdatedCredit.FirstOrDefault().InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdatedCredit.FirstOrDefault().JournalNumber,

                            });


                        }

                        _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                        _TaamerProContext.SaveChanges();


                        if (VoucherUpdated != null)
                        {
                            VoucherUpdated.DepitNotiId = -1;

                        }
                    }



                    //voucher.QRCodeNum = "200010001000" + voucher.InvoiceId.ToString();
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة اشعار مدين لفاتورة " + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully , ReturnedParm = voucher.InvoiceId };
                }
                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };

            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ اشعار مدين";
                _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveandPostInvoiceForServices(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {

                var ToAccount = 0;
                var CustomerAccountPaid = 0;
                var BoxBankAccountPaid = 0;
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.ContractsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ وترحيل الفاتورة";
                    _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.EnsureRevenueAccountedBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureRevenueAccountedBranchAccounts };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ وترحيل الفاتورة";
                    _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.SaleDiscountAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ وترحيل الفاتورة";
                    _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.EnsureThatTheSales, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatTheSales };

                }
                else if (Branch == null || Branch.TaxsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ وترحيل الفاتورة";
                    _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.EnsureTheVAT, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                }


                foreach (var item in voucher.VoucherDetails.ToList())
                {
                     var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                    int? AccountEradat = 0;
                    if (ServicesUpdated != null)
                    {
                        AccountEradat = ServicesUpdated.AccountId ?? 0;
                        if (AccountEradat == 0)
                        {
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                        }
                    }
                    else
                    {
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                    }
                }


                int? CostId;
                voucher.TransactionDetails = new List<Transactions>();

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ وترحيل الفاتورة";
                    _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                decimal Disc = 0;
                //if (voucher.DiscountValue != null)
                //{
                //    if (Convert.ToInt32(voucher.DiscountValue) != 0)
                //    {

                //        Disc = voucher.DiscountValue ?? 0;
                //    }
                //    else
                //    {
                //        Disc = 0;

                //    }
                //}
                //else
                //{
                //    Disc = 0;
                //}
                if (voucher.InvoiceId == 0)
                {

                    if (String.IsNullOrWhiteSpace(Convert.ToString(voucher.ToAccountId)) && voucher.Type == 2)
                    {

                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote3 = "فشل في حفظ فاتورة";
                        _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                    }
                    //   voucher.TotalValue = voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0);
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                    voucher.YearId = yearid;
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.AddUser = UserId;
                    voucher.BranchId = BranchId;
                    voucher.AddDate = DateTime.Now;
                    voucher.ProjectId = voucher.ProjectId;
                    voucher.DunCalc = false;
                    voucher.InvUUID = GET_UUID();


                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid &&  s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }

                    if (voucher.ProjectId != null)
                    {
                        var project = _TaamerProContext.Project.Where(s=>s.ProjectId== voucher.ProjectId).FirstOrDefault();
                        if (project != null)
                        {
                            project.MotionProject = 1;
                            project.MotionProjectDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            project.MotionProjectNote = "أضافة فاتورة علي مشروع";
                        }

                        if(voucher.CostCenterId>0)
                        {
                            CostId = voucher.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }
                        }
                        

                    }
                    else
                    {
                        if (voucher.CostCenterId > 0)
                        {
                            CostId = voucher.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }
                    }
                    if (CostId == 0)
                    {
                        try
                        { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                        catch
                        { CostId = null; }
                    }
                    voucher.CostCenterId = CostId;
                    //Utilities utilVoucher = new Utilities((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());
                    voucher.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                    //voucher.InvoiceValueText = utilVoucher.GetNumberAr();

                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();

                    int? itemToAccountId = 0;
                    int? itemAccountId = 0;
                    var vouchertypename = "";
                    var vouchertypename2 = "";
                    int? itemInvoiceId = 0;
                    decimal? AcPaid = 0;
                    decimal? AcPaid2 = 0;
                    decimal? AcPaid3 = 0;

                    decimal? SVAT = 0;
                    int? itemTaxType = 0;

                    foreach (var item in voucher.VoucherDetails.ToList())
                    {


                        //ToAccount = item.AccountId ?? 0;
                        ToAccount = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;

                        // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                        //if (ServicesUpdated != null)
                        //{
                        //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                        //}

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AddUser = UserId;
                        item.PayType = voucher.PayType;

                        item.AddDate = DateTime.Now;

                        decimal? TotalWithoutVAT = (item.Amount);
                        decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                        SVAT = (voucher.TaxAmount);
                        //SVAT = SVAT + (item.TaxAmount * item.Qty);

                        item.CostCenterId = CostId;
                        decimal? Depit = item.Amount + item.TaxAmount;

                        //decimal? x =Convert.ToDecimal(voucher.PaidValue * Convert.ToDecimal(0.15));

                        //decimal? Depit = Convert.ToDecimal((voucher.PaidValue * Convert.ToDecimal(0.15)));

                        decimal? VAT = item.TaxAmount;
                        item.TotalAmount = item.TotalAmount;
                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        _TaamerProContext.VoucherDetails.Add(item);
                        vouchertypename = voucher.Type == 2 ? " فاتورة" + " مبيعات" : "فاتورة";
                        vouchertypename2 = " خصم فاتورة ";

                        itemToAccountId = item.ToAccountId;
                        itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                        itemInvoiceId = item.InvoiceId;
                        itemTaxType = item.TaxType;
                         
                    }

                    CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                    BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue

                    if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                    {


                        if (itemTaxType == 2)
                        {
                            AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                            AcPaid3 = 0;

                        }
                        else
                        {
                            AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                            AcPaid3 = 0;

                        }
                    }
                    else
                    {


                        if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                        {

                            AcPaid = Convert.ToDecimal(voucher.PaidValue);
                            AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;
                            //Edit 21-01-2024 Ostaze Mohammed Req
                            //AcPaid3 = 0;
                            AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                            var CustomerData=_TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                            CustomerAccountPaid = CustomerData.AccountId??0;
                            BoxBankAccountPaid = itemToAccountId??0;
                        }
                        else
                        {
                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);

                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);

                            }

                        }

                    }

                    if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                    {
                        var CustomerDataV = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                        CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                    }


                    _TaamerProContext.SaveChanges();

                    voucher.TransactionDetails.Add(new Transactions
                    {

                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        AccountId = CustomerAccountPaid,
                        CostCenterId = CostId,
                        AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 1,
                        Depit = AcPaid,
                        Credit = 0,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });

                    if (Disc > 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = Branch.SaleDiscountAccId,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = voucher.DiscountValue,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });

                    }
                    //credit 

                    foreach (var item in voucher.VoucherDetails.ToList())
                    {
                         var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                        int? AccountEradat = 0;
                        if (ServicesUpdated != null)
                        {
                            AccountEradat = ServicesUpdated.AccountId ?? 0;
                            if (AccountEradat == 0)
                            {
                                //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                            }
                        }
                        else
                        {
                            //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                        }

                        item.AccountId = AccountEradat;
                        item.Qty = item.Qty ?? 1;
                        item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                        if (itemTaxType == 2)
                        {
                            item.Amount = (item.Amount * item.Qty)-(item.DiscountValue_Det);
                        }
                        else
                        {
                            item.Amount = item.TotalAmount - (item.TaxAmount);
                        }
                        //item.TaxAmount = item.TaxAmount / item.Qty;
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = AccountEradat,

                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== AccountEradat)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = item.Amount,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            VoucherDetailsId = item.VoucherDetailsId,

                        });


                    }

                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        //AccountId = AccOVAT,// الضريبة
                        AccountId = Branch.TaxsAccId,// الضريبة

                        CostCenterId = CostId,//item.CostCenterId,
                        AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 3,
                        Credit = SVAT,
                        Depit = 0,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });

                    if (AcPaid3 != 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = BoxBankAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 4,
                            Depit = AcPaid3,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 5,
                            Depit = 0,
                            Credit = AcPaid3,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });


                    }

                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);





                    if (voucher.ServicesPriceOffer != null && voucher.ServicesPriceOffer.Count > 0)
                    {
                        foreach (var item in voucher.ServicesPriceOffer)
                        {
                            item.AddUser = UserId;
                            item.AddDate = DateTime.Now;
                            item.InvoiceId = voucher.InvoiceId;
                            _TaamerProContext.Acc_Services_PriceOffer.Add(item);
                        }
                    }



                    _TaamerProContext.SaveChanges();
                    voucher.QRCodeNum = "200010001000" + voucher.InvoiceId.ToString();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 2 && s.YearId == yearid && s.InvoiceNumber == voucher.InvoiceNumber && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucherDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                        if (TransactionDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    }

                    if (voucher.IsPost != true)
                    {
                        voucher.IsPost = true;
                        voucher.PostDate = voucher.Date;
                        voucher.PostHijriDate = voucher.HijriDate;
                        //voucher.PostDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        //voucher.PostHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));

                        var newJournal = new Journals();
                        var JNo =  _JournalsRepository.GenerateNextJournalNumber(yearid ?? default(int), BranchId).Result;
                        if (voucher.Type == 10)
                        {
                            JNo = 1;
                        }
                        else
                        {
                            JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                        }


                        newJournal.JournalNo = JNo;
                        newJournal.YearMalia = yearid ?? default(int);
                        newJournal.VoucherId = voucher.InvoiceId;
                        newJournal.VoucherType = voucher.Type;
                        newJournal.BranchId = voucher.BranchId ?? 0;
                        newJournal.AddDate = DateTime.Now;
                        newJournal.AddUser = newJournal.UserId = UserId;
                        _TaamerProContext.Journals.Add(newJournal);
                        foreach (var trans in voucher.TransactionDetails.ToList())
                        {
                            trans.IsPost = true;
                            trans.JournalNo = newJournal.JournalNo;
                        }
                        voucher.JournalNumber = newJournal.JournalNo;
                        //_TaamerProContext.SaveChanges();
                    }
                    else
                    {
                        ////-----------------------------------------------------------------------------------------------------------------
                        //string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        //string ActionNote3 = "فشل في حفظ وترحيل الفاتورة";
                        //_SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, "مرحلة مسبقا", "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        ////-----------------------------------------------------------------------------------------------------------------

                        //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "مرحلة مسبقا" };
                    }




                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة فاتورة جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    List<int> voDetIds = new List<int>();
                    foreach (var itemV in voucher.VoucherDetails)
                    {
                        voDetIds.Add(itemV.VoucherDetailsId);
                    }
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId, InvoiceIsDeleted = voucher.IsDeleted, voucherDetObj = voDetIds };
                }
                else
                {
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnterNewInvoice };

                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ وترحيل الفاتورة";
                _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveInvoiceForServicesRet(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {

                var ToAccount = 0;
                var CustomerAccountPaid = 0;
                var BoxBankAccountPaid = 0;
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.PurchaseReturnCashAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureRevenueAccountedBranchAccounts, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureRevenueAccountedBranchAccounts };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.SaleDiscountAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureThatTheSales, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatTheSales };

                }
                else if (Branch == null || Branch.TaxsAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureTheVAT, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                }


                int? CostId;
                voucher.TransactionDetails = new List<Transactions>();

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }


                var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                decimal Disc = 0;
                //if (VoucherUpdated.DiscountValue != null)
                //{
                //    if (Convert.ToInt32(VoucherUpdated.DiscountValue) != 0)
                //    {

                //        Disc = VoucherUpdated.DiscountValue ?? 0;
                //    }
                //    else
                //    {
                //        Disc = 0;

                //    }
                //}
                //else
                //{
                //    Disc = 0;
                //}
                if (VoucherUpdated != null)
                {

                    VoucherUpdated.Rad = true;

                    decimal TotalCredit = 0;
                    decimal TotalDepit = 0;

                    TotalCredit = GetAllCreditNotiTotalValue(voucher.InvoiceId).Result.Item1;
                    TotalDepit = GetAllCreditNotiTotalValue(voucher.InvoiceId).Result.Item2;

                    var InvoiceValueBefore = VoucherUpdated.InvoiceValue;
                    
                    var TaxAmountBefore = VoucherUpdated.TaxAmount;
                    var TotalValueBefore = VoucherUpdated.TotalValue+ Disc;
                    var TotalAfter = TotalValueBefore - TotalCredit + TotalDepit;
                    var taxAfter= Convert.ToDecimal((TotalAfter * TaxAmountBefore) / TotalValueBefore).ToString("F");
                    var InvoiceValueAfter= Convert.ToDecimal(TotalAfter) - Convert.ToDecimal(taxAfter);

                    if(TotalAfter==0)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate33 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote33 = "لا يمكن رد فاتورة تم عمل اشعار دائن لها بكامل المبلغ";
                        _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate33, UserId, BranchId, ActionNote33, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = ActionNote33 };
                    }

                    //VoucherUpdated.InvoiceValue = InvoiceValueAfter;
                    //VoucherUpdated.TaxAmount = taxAfter;
                    //VoucherUpdated.TotalValue = TotalAfter;

                    //int nextnumber = _InvoicesRepository.GenerateNextInvoiceNumberRet(VoucherUpdated.Type, yearid, BranchId).Result??0;
                    var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                    //var NewNextInv = string.Format("{0:000000}", nextnumber);

                    VoucherUpdated.InvoiceRetId = NewNextInv.ToString();
                    if (voucher.ProjectId != null)
                    {

                        if (VoucherUpdated.CostCenterId > 0)
                        {
                            CostId = VoucherUpdated.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }

                        }
                       

                    }
                    else
                    {
                        if (VoucherUpdated.CostCenterId > 0)
                        {
                            CostId = VoucherUpdated.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }

                    }
                    if (CostId == 0)
                    {
                        try
                        { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                        catch
                        { CostId = null; }
                    }
                    voucher.CostCenterId = CostId;
                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();



                    int? itemToAccountId = 0;
                    int? itemAccountId = 0;
                    var vouchertypename = "";
                    var vouchertypename2 = "";
                    int? itemInvoiceId = 0;
                    decimal? AcPaid = 0;
                    decimal? AcPaid2 = 0;
                    decimal? AcPaid3 = 0;
                    decimal? SVAT = 0;
                    int? itemTaxType = 0;
                    // add new details
                    var ObjList = new List<object>();
                    foreach (var item in VoucherUpdated.VoucherDetails.ToList())
                    {

                        ToAccount = item.AccountId ?? 0;

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.InvoiceId = VoucherUpdated.InvoiceId;
                        item.PayType = VoucherUpdated.PayType;
                        item.AddUser = 1;
                        item.AddDate = DateTime.Now;
                        decimal? TotalWithoutVAT = (item.Amount);
                        decimal? TotalWithVAT = ((item.Amount) + item.TaxAmount);
                        SVAT = (Convert.ToDecimal(taxAfter));

                        item.CostCenterId = CostId;
                        var VouchDa = VoucherUpdated.Date;

                        CultureInfo culture = new CultureInfo("en-US");
                        DateTime tempDate = Convert.ToDateTime(VouchDa, culture);
                        DateTime Today = DateTime.Now;
                        var diff = (Today - tempDate).TotalDays;



                        decimal? Depit = item.Amount + item.TaxAmount;
                        item.TotalAmount = item.TotalAmount;

                        item.TFK = ConvertToWord_NEW(Depit.ToString());


                        //if (Convert.ToInt32(diff)>90)
                        //{
                        //    SVAT = 0;
                        //}

                        //// add transaction
                        vouchertypename = "مردود مبيعات";
                        vouchertypename2 = "مردود خصم فاتورة";

                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && VoucherUpdated.Type == 2)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote3 = "فشل في حفظ فاتورة";
                            _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                            //-----------------------------------------------------------------------------------------------------------------
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }
                        //depit 

                        itemToAccountId = item.ToAccountId;
                        itemAccountId = item.AccountId;
                        itemInvoiceId = item.InvoiceId;
                        itemTaxType = item.TaxType;

                    }
                    CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                    BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                    if (VoucherUpdated.PaidValue == 0 || VoucherUpdated.PaidValue == null)
                    {
                        if (itemTaxType == 2)
                        {
                            AcPaid = Convert.ToDecimal(InvoiceValueAfter) + SVAT - Disc;
                            AcPaid2 = Convert.ToDecimal(InvoiceValueAfter);
                            AcPaid3 = 0;
                        }
                        else
                        {
                            AcPaid = Convert.ToDecimal(TotalAfter) - Disc;
                            AcPaid2 = Convert.ToDecimal(TotalAfter) - SVAT ;
                            AcPaid3 = 0;
                        }

                    }
                    else
                    {


                        if (Convert.ToInt32(VoucherUpdated.PaidValue) == Convert.ToInt32(VoucherUpdated.TotalValue))
                        {
                            AcPaid = Convert.ToDecimal(VoucherUpdated.PaidValue- TotalCredit+ TotalDepit);
                            AcPaid2 = Convert.ToDecimal(VoucherUpdated.PaidValue - TotalCredit+ TotalDepit) + Disc - SVAT;
                            //Edit 21-01-2024 Ostaze Mohammed Req
                            //AcPaid3 = 0;
                            AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                            var CustomerData = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                            CustomerAccountPaid = CustomerData.AccountId ?? 0;
                            BoxBankAccountPaid = itemToAccountId ?? 0;
                        }
                        else
                        {
                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(InvoiceValueAfter) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(InvoiceValueAfter);
                                AcPaid3 = Convert.ToDecimal(VoucherUpdated.PaidValue);
                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(TotalAfter) - Disc;
                                AcPaid2 = Convert.ToDecimal(TotalAfter) - SVAT;
                                AcPaid3 = Convert.ToDecimal(VoucherUpdated.PaidValue);
                            }
                        }
                    }
                    if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                    {
                        var CustomerDataV = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                        CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                    }
                    //check Dates of Transaction

                    var AcualTransactionDate = "";

                    DateTime First_Quarter_S = DateTime.ParseExact(yearid + "-01-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime First_Quarter_E = DateTime.ParseExact(yearid + "-03-31", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime First_Quarter_E_AndExtraMonth = DateTime.ParseExact(yearid + "-04-30", "yyyy-MM-dd", CultureInfo.InvariantCulture);

                    DateTime Second_Quarter_S = DateTime.ParseExact(yearid + "-04-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime Second_Quarter_E = DateTime.ParseExact(yearid + "-06-30", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime Second_Quarter_E_AndExtraMonth = DateTime.ParseExact(yearid + "-07-31", "yyyy-MM-dd", CultureInfo.InvariantCulture);

                    DateTime Third_Quarter_S = DateTime.ParseExact(yearid + "-07-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime Third_Quarter_E = DateTime.ParseExact(yearid + "-09-30", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime Third_Quarter_E_AndExtraMonth = DateTime.ParseExact(yearid + "-10-31", "yyyy-MM-dd", CultureInfo.InvariantCulture);

                    DateTime Fourth_Quarter_S = DateTime.ParseExact(yearid + "-10-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime Fourth_Quarter_E = DateTime.ParseExact(yearid + "-12-31", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime Fourth_Quarter_E_AndExtraMonth = DateTime.ParseExact((yearid+1) + "-01-31", "yyyy-MM-dd", CultureInfo.InvariantCulture);


                    var OldInvDate = VoucherUpdated.Date;
                    DateTime oDate = DateTime.ParseExact(OldInvDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var OldInvDate_Month = oDate.Month;


                    if ((oDate >= First_Quarter_S && oDate <= First_Quarter_E) && DateTime.Now <= First_Quarter_E_AndExtraMonth)//First Quarter and have 1 month permetion
                    {
                        AcualTransactionDate = VoucherUpdated.Date;
                    }
                    else if ((oDate >= Second_Quarter_S && oDate <= Second_Quarter_E) && DateTime.Now <= Second_Quarter_E_AndExtraMonth)//Second Quarter and have 1 month permetion
                    {
                        AcualTransactionDate = VoucherUpdated.Date;
                    }
                    else if ((oDate >= Third_Quarter_S && oDate <= Third_Quarter_E) && DateTime.Now <= Third_Quarter_E_AndExtraMonth)//Third Quarter and have 1 month permetion
                    {
                        AcualTransactionDate = VoucherUpdated.Date;
                    }
                    else if ((oDate >= Fourth_Quarter_S && oDate <= Fourth_Quarter_E) && DateTime.Now <= Fourth_Quarter_E_AndExtraMonth)//Fourth Quarter and have 1 month permetion
                    {
                        AcualTransactionDate = VoucherUpdated.Date;
                    }
                    else
                    {
                        AcualTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    }

                    // Convert Date to Hijri
                    DateTime DateTemp= DateTime.ParseExact(AcualTransactionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var AcualDateTransactionHijriDate = ConvertDateCalendar(DateTemp, "Hijri", "en-US");
                    //End Of Check Dates

                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                        AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                        TransactionDate = AcualTransactionDate,
                        TransactionHijriDate = AcualDateTransactionHijriDate,
                        AccountId = Branch.PurchaseReturnCashAccId,
                        CostCenterId = CostId,
                        AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.PurchaseReturnCashAccId)?.FirstOrDefault()?.Type,
                        Type = 4,
                        LineNumber = 1,
                        Depit = AcPaid2,
                        Credit = 0,


                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                        //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                        InvoiceReference = "",
                        InvoiceId = itemInvoiceId,
                        IsPost = true,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                        JournalNo = VoucherUpdated.JournalNumber
                    });
                    //credit 
                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                        AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                        TransactionDate = AcualTransactionDate,
                        TransactionHijriDate = AcualDateTransactionHijriDate,
                        AccountId = CustomerAccountPaid,
                        CostCenterId = CostId,
                        AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault()?.Type,
                        Type = 4,
                        LineNumber = 2,
                        Credit = AcPaid,
                        Depit = 0,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                        //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        InvoiceId = itemInvoiceId,
                        IsPost = true,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                        JournalNo = VoucherUpdated.JournalNumber

                    });
                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                        AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                        TransactionDate = AcualTransactionDate,
                        TransactionHijriDate = AcualDateTransactionHijriDate,
                        //AccountId = AccOVAT,// الضريبة
                        AccountId = Branch.TaxsAccId,// الضريبة

                        CostCenterId = CostId,
                        AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                        Type = 4,
                        LineNumber = 2,
                        Credit = 0,
                        Depit = SVAT,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",
                        InvoiceId = voucher.InvoiceId,
                        IsPost = true,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                        JournalNo = VoucherUpdated.JournalNumber

                    });
                    if (Disc > 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                            AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                            TransactionDate = AcualTransactionDate,
                            TransactionHijriDate = AcualDateTransactionHijriDate,
                            //AccountId = AccOVAT,// الضريبة
                            AccountId = Branch.SaleDiscountAccId,// الضريبة

                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                            Type = 4,
                            LineNumber = 2,
                            Credit = VoucherUpdated.DiscountValue,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename2 + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename2 + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdated.JournalNumber

                        });

                    }



                    if (AcPaid3 != 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                            AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                            TransactionDate = AcualTransactionDate,
                            TransactionHijriDate = AcualDateTransactionHijriDate,
                            AccountId = BoxBankAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == BoxBankAccountPaid).FirstOrDefault()?.Type,
                            Type = 4,
                            LineNumber = 4,
                            Depit = 0,
                            Credit = AcPaid3,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = VoucherUpdated.InvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdated.JournalNumber

                        });
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                            AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                            TransactionDate = AcualTransactionDate,
                            TransactionHijriDate = AcualDateTransactionHijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = 4,
                            LineNumber = 5,
                            Depit = AcPaid3,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = VoucherUpdated.InvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdated.JournalNumber

                        });


                    }
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);                 
                }
                _TaamerProContext.SaveChanges();

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " تعديل فاتورة رقم " + voucher.InvoiceId;
                _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في تعديل الفاتورة";
                _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveInvoiceForServicesRetNEW_func(Invoices voucher, int UserId, int BranchId, int? yearid, string lang, string Con)
        {
            try
            {
                var VoucherDetCredit = new List<VoucherDetails>();
                var VoucherDetails = new List<VoucherDetails>();
                var ToAccount = 0;
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.PurchaseReturnCashAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureRevenueAccountedBranchAccounts, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureRevenueAccountedBranchAccounts };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.SaleDiscountAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureThatTheSales, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatTheSales };

                }
                else if (Branch == null || Branch.TaxsAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureTheVAT, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                }

                int? CostId;
                voucher.TransactionDetails = new List<Transactions>();
                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                if (VoucherUpdated != null)
                {
                    VoucherUpdated.Rad = true;
                    int TotalAfter = 1;
                    if (TotalAfter == 0)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate33 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote33 = "لا يمكن رد فاتورة تم عمل اشعار دائن لها بكامل المبلغ";
                        _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate33, UserId, BranchId, ActionNote33, 0);
                        //-----------------------------------------------------------------------------------------------------------------
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = ActionNote33 };
                    }
                    //int nextnumber = _InvoicesRepository.GenerateNextInvoiceNumberRet(VoucherUpdated.Type, yearid, BranchId).Result ?? 0;
                    var NewNextInv = GenerateVoucherNumberNewPro(VoucherUpdated.Type, BranchId, yearid,4,Con).Result ?? "1";

                    //var NewNextInv = string.Format("{0:000000}", nextnumber);

                    VoucherUpdated.InvoiceRetId = NewNextInv.ToString();
                    if (voucher.ProjectId != null)
                    {
                        if (VoucherUpdated.CostCenterId > 0)
                        {
                            CostId = VoucherUpdated.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }

                        }
                    }
                    else
                    {
                        if (VoucherUpdated.CostCenterId > 0)
                        {
                            CostId = VoucherUpdated.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }

                    }
                    if (CostId == 0)
                    {
                        try
                        { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                        catch
                        { CostId = null; }
                    }
                    voucher.CostCenterId = CostId;
                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                    int? itemToAccountId = 0;
                    int? itemAccountId = 0;
                    var vouchertypename = "";
                    int? itemInvoiceId = 0;

                    int? itemTaxType = 0;
                    // add new details
                    var ObjList = new List<object>();
                    VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                    foreach (var item in VoucherDetails)
                    {

                        ToAccount = item.AccountId ?? 0;

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.InvoiceId = VoucherUpdated.InvoiceId;
                        item.PayType = VoucherUpdated.PayType;
                        item.AddUser = 1;
                        item.AddDate = DateTime.Now;
                        decimal? TotalWithoutVAT = (item.Amount);
                        decimal? TotalWithVAT = ((item.Amount) + item.TaxAmount);

                        item.CostCenterId = CostId;
                        var VouchDa = VoucherUpdated.Date;

                        CultureInfo culture = new CultureInfo("en-US");
                        DateTime tempDate = Convert.ToDateTime(VouchDa, culture);
                        DateTime Today = DateTime.Now;
                        var diff = (Today - tempDate).TotalDays;



                        decimal? Depit = item.Amount + item.TaxAmount;
                        item.TotalAmount = item.TotalAmount;

                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        vouchertypename = "مردود مبيعات";

                        itemToAccountId = item.ToAccountId;
                        itemAccountId = item.AccountId;
                        itemInvoiceId = item.InvoiceId;
                        itemTaxType = item.TaxType;

                    }

                    //check Dates of Transaction

                    var AcualTransactionDate = "";

                    DateTime First_Quarter_S = DateTime.ParseExact(yearid + "-01-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime First_Quarter_E = DateTime.ParseExact(yearid + "-03-31", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime First_Quarter_E_AndExtraMonth = DateTime.ParseExact(yearid + "-04-30", "yyyy-MM-dd", CultureInfo.InvariantCulture);

                    DateTime Second_Quarter_S = DateTime.ParseExact(yearid + "-04-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime Second_Quarter_E = DateTime.ParseExact(yearid + "-06-30", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime Second_Quarter_E_AndExtraMonth = DateTime.ParseExact(yearid + "-07-31", "yyyy-MM-dd", CultureInfo.InvariantCulture);

                    DateTime Third_Quarter_S = DateTime.ParseExact(yearid + "-07-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime Third_Quarter_E = DateTime.ParseExact(yearid + "-09-30", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime Third_Quarter_E_AndExtraMonth = DateTime.ParseExact(yearid + "-10-31", "yyyy-MM-dd", CultureInfo.InvariantCulture);

                    DateTime Fourth_Quarter_S = DateTime.ParseExact(yearid + "-10-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime Fourth_Quarter_E = DateTime.ParseExact(yearid + "-12-31", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    DateTime Fourth_Quarter_E_AndExtraMonth = DateTime.ParseExact((yearid + 1) + "-01-31", "yyyy-MM-dd", CultureInfo.InvariantCulture);


                    var OldInvDate = VoucherUpdated.Date;
                    DateTime oDate = DateTime.ParseExact(OldInvDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var OldInvDate_Month = oDate.Month;


                    if ((oDate >= First_Quarter_S && oDate <= First_Quarter_E) && DateTime.Now <= First_Quarter_E_AndExtraMonth)//First Quarter and have 1 month permetion
                    {
                        AcualTransactionDate = VoucherUpdated.Date;
                    }
                    else if ((oDate >= Second_Quarter_S && oDate <= Second_Quarter_E) && DateTime.Now <= Second_Quarter_E_AndExtraMonth)//Second Quarter and have 1 month permetion
                    {
                        AcualTransactionDate = VoucherUpdated.Date;
                    }
                    else if ((oDate >= Third_Quarter_S && oDate <= Third_Quarter_E) && DateTime.Now <= Third_Quarter_E_AndExtraMonth)//Third Quarter and have 1 month permetion
                    {
                        AcualTransactionDate = VoucherUpdated.Date;
                    }
                    else if ((oDate >= Fourth_Quarter_S && oDate <= Fourth_Quarter_E) && DateTime.Now <= Fourth_Quarter_E_AndExtraMonth)//Fourth Quarter and have 1 month permetion
                    {
                        AcualTransactionDate = VoucherUpdated.Date;
                    }
                    else
                    {
                        AcualTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    }

                    // Convert Date to Hijri
                    DateTime DateTemp = DateTime.ParseExact(AcualTransactionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var AcualDateTransactionHijriDate = ConvertDateCalendar(DateTemp, "Hijri", "en-US");
                    //End Of Check Dates

                    var DataOF_InvoiceReturn= _InvoicesRepository.GetInvoiceReturnData_func(itemInvoiceId??0, yearid ?? default(int), BranchId, lang, Con).Result;

                    int counter = 1;

                    foreach(var item_Ret in DataOF_InvoiceReturn)
                    {       
                        if(item_Ret.Total!=0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en")),
                                AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar")),
                                TransactionDate = AcualTransactionDate,
                                TransactionHijriDate = AcualDateTransactionHijriDate,
                                AccountId = item_Ret.AccountId,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== item_Ret.AccountId)?.FirstOrDefault()?.Type,
                                Type = 4,
                                LineNumber = counter,
                                Depit = item_Ret.Credit,
                                Credit = item_Ret.Depit,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = itemInvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdated.JournalNumber
                            });
                            counter += 1;
                        }


                    }
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);
                    var VoucherCredit = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.CreditNotiId == voucher.InvoiceId).ToList();

                    if (VoucherCredit.Count() > 0)
                    {
                        VoucherDetCredit = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == VoucherCredit.FirstOrDefault().InvoiceId).ToList();
                        VoucherCredit.FirstOrDefault().Rad = true;   
                        
                    }

                    var CustomerPayment = _TaamerProContext.CustomerPayments.Where(s => s.IsDeleted == false && s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                    if (CustomerPayment.Count() > 0)
                    {
                        CustomerPayment.FirstOrDefault().IsCanceled = true;
                    }

                    
                    
                }
                _TaamerProContext.SaveChanges();
                var VoucherDetailsV= _VoucherDetailsRepository.GetAllDetailsByInvoiceId(VoucherUpdated.InvoiceId).Result;
                var ObjDet = new List<ObjRet>();
                

                if (VoucherDetCredit.Count() > 0)
                {
                    foreach (var item in VoucherDetailsV)
                    {
                        var ObjDetInst = new ObjRet();
                        foreach (var itemC in VoucherDetCredit)
                        {
                            if (item.ServicesPriceId == itemC.ServicesPriceId)
                            {

                                item.TaxAmount =  item.TaxAmount - itemC.TaxAmount;
                                item.Amount = item.Amount - itemC.Amount;
                                item.TotalAmount = item.TotalAmount - itemC.TotalAmount;
                              
                            }
                        }
                        ObjDetInst.TaxAmount = item.TaxAmount;
                        ObjDetInst.Amount = item.Amount;
                        ObjDetInst.TotalAmount = item.TotalAmount;
                        ObjDetInst.Qty = item.Qty;
                        ObjDetInst.ServicesPriceName = item.ServicesPriceName;
                        ObjDetInst.DiscountValue_Det = item.DiscountValue_Det;
                        ObjDetInst.DiscountPercentage_Det = item.DiscountPercentage_Det;
                        ObjDetInst.InvoiceId = item.InvoiceId;
                        ObjDetInst.VoucherDetailsId = item.VoucherDetailsId;
                        ObjDetInst.Type = 4;
                        ObjDet.Add(ObjDetInst);
                    }
                }
                else
                {
                    foreach (var item in VoucherDetailsV)
                    {
                        var ObjDetInst = new ObjRet();
                        ObjDetInst.TaxAmount = item.TaxAmount;
                        ObjDetInst.Amount = item.Amount;
                        ObjDetInst.TotalAmount = item.TotalAmount;
                        ObjDetInst.Qty = item.Qty;
                        ObjDetInst.ServicesPriceName = item.ServicesPriceName;
                        ObjDetInst.DiscountValue_Det = item.DiscountValue_Det;
                        ObjDetInst.DiscountPercentage_Det = item.DiscountPercentage_Det;
                        ObjDetInst.InvoiceId = item.InvoiceId;
                        ObjDetInst.VoucherDetailsId = item.VoucherDetailsId;
                        ObjDetInst.Type = 4;
                        ObjDet.Add(ObjDetInst);
                    }
                }

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " تعديل فاتورة رقم " + voucher.InvoiceId;
                _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully,ReturnedParm=voucher.InvoiceId, ObjRetDet= ObjDet };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في تعديل الفاتورة";
                _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage ReturnNotiCreditBack(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                if (VoucherUpdated != null)
                {
                    VoucherUpdated.CreditNotiId = null;
                    var VoucherCredit = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.CreditNotiId == voucher.InvoiceId).ToList(); ;
                    if(VoucherCredit.Count() > 0)
                    {
                        var VoucherUpdatedCredit = _TaamerProContext.Invoices.Where(s => s.InvoiceId == VoucherCredit.FirstOrDefault().InvoiceId).FirstOrDefault();
                        if(VoucherUpdatedCredit!=null)
                        {
                            VoucherUpdatedCredit.IsDeleted = true;
                            VoucherUpdatedCredit.CreditNotiId = null;

                            var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == VoucherUpdatedCredit.InvoiceId).ToList();
                            var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdatedCredit.InvoiceId).ToList();

                            _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                            _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                        }
                    }
                    else
                    {
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "لا يوجد اشعار دائن للفاتورة مسبقا" };
                    }
                }
                _TaamerProContext.SaveChanges();

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " الغاء اشعار الدائن " + voucher.InvoiceId;
                _SystemAction.SaveAction("ReturnNotiCreditBack", "VoucherService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في الغاء اشعار الدائن";
                _SystemAction.SaveAction("ReturnNotiCreditBack", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage ReturnNotiDepitBack(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                if (VoucherUpdated != null)
                {
                    VoucherUpdated.DepitNotiId = null;
                    var VoucherDepit = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.DepitNotiId == voucher.InvoiceId).ToList();
                    if (VoucherDepit.Count() > 0)
                    {
                        var VoucherUpdatedDepit = _TaamerProContext.Invoices.Where(s=>s.InvoiceId== VoucherDepit.FirstOrDefault().InvoiceId).FirstOrDefault();

                        if(VoucherUpdatedDepit!=null)
                        {
                            VoucherUpdatedDepit.IsDeleted = true;
                            VoucherUpdatedDepit.DepitNotiId = null;

                            var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == VoucherUpdatedDepit.InvoiceId).ToList();
                            var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdatedDepit.InvoiceId).ToList();
                            _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                            _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                        }

                    }
                    else
                    {
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
                    }
                }
                _TaamerProContext.SaveChanges();

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " الغاء اشعار الدائن " + voucher.InvoiceId;
                _SystemAction.SaveAction("ReturnNotiCreditBack", "VoucherService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في الغاء اشعار الدائن";
                _SystemAction.SaveAction("ReturnNotiCreditBack", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveInvoiceForServicesRet_Back(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                if (VoucherUpdated != null)
                {
                    VoucherUpdated.Rad = false;
                    VoucherUpdated.InvoiceRetId = "000000";

                    //var CustomerPayment = _TaamerProContext.CustomerPayments.Where(s => s.IsDeleted == false && s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                    //if (CustomerPayment.Count() > 0)
                    //{
                    //    CustomerPayment.FirstOrDefault().IsCanceled = false;
                    //}

                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                    var v = TransactionDetails.Where(s => s.Type == 4).ToList();
                    _TaamerProContext.Transactions.RemoveRange(v);
                }
                _TaamerProContext.SaveChanges();

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " الغاء القيد العكسي " + voucher.InvoiceId;
                _SystemAction.SaveAction("SaveInvoiceForServicesRet_Back", "VoucherService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في الغاء القيد العكسي";
                _SystemAction.SaveAction("SaveInvoiceForServicesRet_Back", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SavePurchaseForServices(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {

                var ToAccount = 0;
                var CustomerAccountPaid = 0;
                var BoxBankAccountPaid = 0;
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.CashInvoicesAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureThatPurchasesAreAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatPurchasesAreAccounted };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.PurchaseDiscAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureDeductionOfPurchasesIsAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureDeductionOfPurchasesIsAccounted };

                }
                else if (Branch == null || Branch.SuspendedFundAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.EnsureTheDeductionOfPurchasesAccountedTheBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheDeductionOfPurchasesAccountedTheBranchAccounts };
                }


                foreach (var item in voucher.VoucherDetails.ToList())
                {
                    var PurchItem = _TaamerProContext.Acc_Categories.Where(s=>s.CategoryId == item.CategoryId).FirstOrDefault();
                    int AccountIdPurchItem = 0;
                    if (PurchItem != null)
                    {
                        AccountIdPurchItem = PurchItem.AccountId ?? 0;
                        if (AccountIdPurchItem == 0)
                        {
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.selectItemAccountBeforeSaving };
                        }
                    }
                    else
                    {
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                    }

                }


                int? CostId;
                voucher.TransactionDetails = new List<Transactions>();

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ فاتورة مشتريات";
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                decimal Disc = 0;
                //if (voucher.DiscountValue != null)
                //{
                //    if (Convert.ToInt32(voucher.DiscountValue) != 0)
                //    {

                //        Disc = voucher.DiscountValue ?? 0;
                //    }
                //    else
                //    {
                //        Disc = 0;

                //    }
                //}
                //else
                //{
                //    Disc = 0;
                //}
                if (voucher.InvoiceId == 0)
                {
                    if (String.IsNullOrWhiteSpace(Convert.ToString(voucher.ToAccountId)) && voucher.Type == 1)
                    {

                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote3 = "فشل في حفظ وترحيل الفاتورة";
                        _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, "يجب اختيار حساب المورد", "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                    }

                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                    voucher.YearId = yearid;
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.AddUser = UserId;
                    voucher.BranchId = BranchId;
                    voucher.AddDate = DateTime.Now;
                    voucher.ProjectId = voucher.ProjectId;
                    voucher.DunCalc = false;

                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }

                    //if (voucher.ProjectId != null)
                    //{
                    //    try
                    //    { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                    //    catch
                    //    {
                    //        try
                    //        { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                    //        catch
                    //        { CostId = null; }
                    //    }

                    //}
                    //else
                    //{
                    //    try
                    //    { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                    //    catch
                    //    { CostId = null; }
                    //}
                    //if (CostId == 0)
                    //{
                    //    try
                    //    { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                    //    catch
                    //    { CostId = null; }
                    //}
                    if (voucher.CostCenterId > 0)
                    {
                        CostId = voucher.CostCenterId;
                    }
                    else
                    {
                        try
                        { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                        catch
                        { CostId = null; }
                    }
                    voucher.CostCenterId = CostId;

                    voucher.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.SuspendedFundAccId).Select(s => s.AccountId).FirstOrDefault();

                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();

                    int? itemToAccountId = 0;
                    int? itemAccountId = 0;
                    var vouchertypename = "";
                    var vouchertypename2 = "";
                    int? itemInvoiceId = 0;
                    decimal? AcPaid = 0;
                    decimal? AcPaid2 = 0;
                    decimal? AcPaid3 = 0;

                    decimal? SVAT = 0;
                    int? itemTaxType = 0;
                    int? AccountMsrofat = 0;

                    foreach (var item in voucher.VoucherDetails.ToList())
                    {


                        //ToAccount = item.AccountId ?? 0;
                        ToAccount= voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;

                        // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                        //if (ServicesUpdated != null)
                        //{
                        //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                        //}

                        AccountMsrofat = Branch.CashInvoicesAccId;


                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AddUser = UserId;
                        item.PayType = voucher.PayType;

                        item.AddDate = DateTime.Now;

                        decimal? TotalWithoutVAT = (item.Amount);
                        decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                        SVAT = (voucher.TaxAmount);
                        //SVAT = SVAT + (item.TaxAmount * item.Qty);
                        item.CostCenterId = CostId;
                        decimal? Depit = item.Amount + item.TaxAmount;

                        decimal? VAT = item.TaxAmount;
                        item.TotalAmount = item.TotalAmount;

                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        _TaamerProContext.VoucherDetails.Add(item);
                        vouchertypename = voucher.Type == 1 ? " فاتورة" + " مشتريات" : "فاتورة";
                        vouchertypename2 = " خصم فاتورة مشتريات ";
                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 1)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote3 = "فشل في حفظ فاتورة";
                            _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }
                        itemToAccountId = item.ToAccountId;
                        itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                        itemInvoiceId = item.InvoiceId;
                        itemTaxType = item.TaxType;

                    }
                    CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                    BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                    if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                    {
                        if (itemTaxType == 2)
                        {
                            AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                            AcPaid3 = 0;
                        }
                        else
                        {
                            AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                            AcPaid3 = 0;
                        }
                    }
                    else
                    {
                        if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                        {
                            AcPaid = Convert.ToDecimal(voucher.PaidValue);
                            AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;
                            //Edit 21-01-2024 Ostaze Mohammed Req
                            //AcPaid3 = 0;
                            AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                            var CustomerData = _TaamerProContext.Acc_Suppliers.Where(s => s.SupplierId == voucher.SupplierId).FirstOrDefault();
                            CustomerAccountPaid = CustomerData.AccountId ?? 0;
                            BoxBankAccountPaid = itemToAccountId ?? 0;
                        }
                        else
                        {
                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                            }

                        }

                    }
                    if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                    {
                        var CustomerDataV = _TaamerProContext.Acc_Suppliers.Where(s => s.SupplierId == voucher.SupplierId).FirstOrDefault();
                        CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                    }
                    _TaamerProContext.SaveChanges();
                    voucher.TransactionDetails.Add(new Transactions
                    {

                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        AccountId = CustomerAccountPaid,
                        CostCenterId = CostId,
                        AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 1,
                        Depit = 0,
                        Credit = AcPaid,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        InvoiceReference = "",
                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });

                    if (voucher.DiscountValue != null)
                    {
                        if (Convert.ToInt32(voucher.DiscountValue) != 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {

                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = Branch.PurchaseDiscAccId,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.PurchaseDiscAccId)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 1,
                                Depit = 0,
                                Credit = voucher.DiscountValue,
                                YearId = yearid,
                                Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                InvoiceReference = "",

                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });

                        }

                    }
                    ////credit 


                    foreach (var item in voucher.VoucherDetails.ToList())
                    {
                        var PurchItem = _TaamerProContext.Acc_Categories.Where(s=>s.CategoryId== item.CategoryId).FirstOrDefault();
                        int AccountIdPurchItem = 0;
                        if (PurchItem!=null)
                        {
                            AccountIdPurchItem = PurchItem.AccountId??0;
                            if(AccountIdPurchItem==0)
                            {
                                //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.selectItemAccountBeforeSaving };
                            }
                        }
                        else
                        {
                            //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                        }
                        //item.AccountId =AccountIdPurchItem;
                        item.Qty = item.Qty ?? 1;
                        item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                        if (itemTaxType == 2)
                        {
                            item.Amount = (item.Amount * item.Qty) - (item.DiscountValue_Det);
                        }
                        else
                        {
                            item.Amount = item.TotalAmount - (item.TaxAmount);
                        }
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = AccountIdPurchItem,

                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == AccountIdPurchItem)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = 0,
                            Depit = item.Amount,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            VoucherDetailsId = item.VoucherDetailsId,

                        });

                    }




                    voucher.TransactionDetails.Add(new Transactions
                    {

                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        //AccountId = AccOVAT,// الضريبة
                        AccountId = Branch.SuspendedFundAccId,// الضريبة

                        CostCenterId = CostId,//item.CostCenterId,
                        AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.SuspendedFundAccId)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 3,
                        Credit = 0,
                        Depit = SVAT,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });

                    if (AcPaid3 != 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = BoxBankAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 4,
                            Depit = 0,
                            Credit = AcPaid3,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 5,
                            Depit = AcPaid3,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });


                    }


                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                    if (voucher.ServicesPriceOffer != null && voucher.ServicesPriceOffer.Count > 0)
                    {
                        foreach (var item in voucher.ServicesPriceOffer)
                        {
                            item.AddUser = UserId;
                            item.AddDate = DateTime.Now;
                            item.InvoiceId = voucher.InvoiceId;
                            _TaamerProContext.Acc_Services_PriceOffer.Add(item);
                        }
                    }

                    _TaamerProContext.SaveChanges();
                    voucher.QRCodeNum = "200010001000" + voucher.InvoiceId.ToString();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 2 && s.YearId == yearid && s.InvoiceNumber == voucher.InvoiceNumber && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucherDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                        if (TransactionDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    }
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة فاتورة جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId, InvoiceIsDeleted = voucher.IsDeleted };
                }
                else
                {
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnterNewInvoice };

                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ الفاتورة";
                _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage SavePurchaseForServicesNotiDepit(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                var ToAccount = 0;
                var CustomerAccountPaid = 0;
                var BoxBankAccountPaid = 0;
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.CashInvoicesAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في  حفظ اشعار مدين لفاتورة مشتريات";
                    _SystemAction.SaveAction("SavePurchaseForServicesNotiDepit", "VoucherService", 1, Resources.EnsureThatPurchasesAreAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatPurchasesAreAccounted };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في  حفظ اشعار مدين لفاتورة مشتريات";
                    _SystemAction.SaveAction("SavePurchaseForServicesNotiDepit", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.PurchaseDiscAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في  حفظ اشعار مدين لفاتورة مشتريات";
                    _SystemAction.SaveAction("SavePurchaseForServicesNotiDepit", "VoucherService", 1, Resources.EnsureDeductionOfPurchasesIsAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureDeductionOfPurchasesIsAccounted };

                }
                else if (Branch == null || Branch.SuspendedFundAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في  حفظ اشعار مدين لفاتورة مشتريات";
                    _SystemAction.SaveAction("SavePurchaseForServicesNotiDepit", "VoucherService", 1, Resources.EnsureTheDeductionOfPurchasesAccountedTheBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheDeductionOfPurchasesAccountedTheBranchAccounts };
                }

                foreach (var item in voucher.VoucherDetails.ToList())
                {
                    var PurchItem = _TaamerProContext.Acc_Categories.Where(s=>s.CategoryId == item.CategoryId).FirstOrDefault();
                    int AccountIdPurchItem = 0;
                    if (PurchItem != null)
                    {
                        AccountIdPurchItem = PurchItem.AccountId ?? 0;
                        if (AccountIdPurchItem == 0)
                        {
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.selectItemAccountBeforeSaving };
                        }
                    }
                    else
                    {
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                    }
                    //item.AccountId =AccountIdPurchItem;

                }


                int? CostId;
                voucher.TransactionDetails = new List<Transactions>();
                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ اشعار مدين لفاتورة مشتريات";
                    _SystemAction.SaveAction("SavePurchaseForServicesNotiDepit", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                var VoucherUpdated =  _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                var VoucherDetailsUpdated = _VoucherDetailsRepository.GetAllDetailsByInvoiceIdPurchase(voucher.InvoiceId).Result;

                decimal Disc = 0;
                //if (voucher.DiscountValue != null)
                //{
                //    if (Convert.ToInt32(voucher.DiscountValue) != 0)
                //    {

                //        Disc = voucher.DiscountValue ?? 0;
                //    }
                //    else
                //    {
                //        Disc = 0;

                //    }
                //}
                //else
                //{
                //    Disc = 0;
                //}
                //--------------------------------------------------------------------

                if (voucher.InvoiceId > 0)
                {
                    if (String.IsNullOrWhiteSpace(Convert.ToString(voucher.ToAccountId)) && voucher.Type == 1)
                    {

                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote3 = "فشل في حفظ اشعار مدين لفاتورة مشتريات";
                        _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, "يجب اختيار حساب المورد", "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                    }

                    var VoucherUpdatedDepit = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.DepitNotiId == voucher.InvoiceId).ToList();

                    if (VoucherUpdatedDepit.Count() == 0)
                    {

                        voucher.IsPost = false;
                        voucher.Rad = false;
                        voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                        voucher.YearId = yearid;
                        voucher.ToAccountId = voucher.ToAccountId;
                        voucher.AddUser = UserId;
                        voucher.BranchId = BranchId;
                        voucher.AddDate = DateTime.Now;
                        voucher.ProjectId = voucher.ProjectId;
                        voucher.DunCalc = false;
                        voucher.CreditNotiId = null;
                        voucher.DepitNotiId = voucher.InvoiceId;
                        voucher.InvoiceId = 0;
                        voucher.InvUUID = GET_UUID();


                        var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                        if (vouchercheck.Count() > 0)
                        {
                            //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumberPurchaseNotiDepit(voucher.Type, yearid, BranchId).Result;
                            var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;

                            //var NewNextInv = string.Format("{0:000000}", NextInv);

                            voucher.InvoiceNumber = NewNextInv.ToString();
                        }

                        if (voucher.CostCenterId > 0)
                        {
                            CostId = voucher.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }
                        voucher.CostCenterId = CostId;


                        voucher.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                        var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.SuspendedFundAccId).Select(s => s.AccountId).FirstOrDefault();

                        _TaamerProContext.Invoices.Add(voucher);
                        //add details
                        var ObjList = new List<object>();

                        int? itemToAccountId = 0;
                        int? itemAccountId = 0;
                        var vouchertypename = "";
                        var vouchertypename2 = "";
                        int? itemInvoiceId = 0;
                        decimal? AcPaid = 0;
                        decimal? AcPaid2 = 0;
                        decimal? AcPaid3 = 0;

                        decimal? SVAT = 0;
                        int? itemTaxType = 0;
                        int? AccountMsrofat = 0;


                        foreach (var item in voucher.VoucherDetails.ToList())
                        {


                            //ToAccount = item.AccountId ?? 0;
                            ToAccount = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;

                            // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                            //if (ServicesUpdated != null)
                            //{
                            //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                            //}


                            if (VoucherDetailsUpdated.Count()>0)
                            {

                                foreach (var Det in VoucherDetailsUpdated.ToList())
                                {
                                    if (item.CategoryId == Det.CategoryId)
                                    {
                                        var QtyDiff = Det.Qty - item.Qty;
                                        if (QtyDiff <= 0)
                                        {
                                            Det.IsRetrieve = 1;

                                        }
                                    }
                                }

                            }


                            AccountMsrofat = Branch.CashInvoicesAccId;


                            ObjList.Add(new { item.AccountId, item.CostCenterId });
                            item.AddUser = UserId;
                            item.PayType = voucher.PayType;

                            item.AddDate = DateTime.Now;

                            decimal? TotalWithoutVAT = (item.Amount);
                            decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                            SVAT = (voucher.TaxAmount);
                            //SVAT = SVAT + (item.TaxAmount * item.Qty);
                            item.CostCenterId = CostId;
                            decimal? Depit = item.Amount + item.TaxAmount;

                            decimal? VAT = item.TaxAmount;
                            item.TotalAmount = item.TotalAmount;

                            item.TFK = ConvertToWord_NEW(Depit.ToString());

                            _TaamerProContext.VoucherDetails.Add(item);
                            vouchertypename ="اشعار مدين لفاتورة";
                            vouchertypename2 = " خصم اشعار مدين لفاتورة مشتريات ";
                            if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 1)
                            {

                                //-----------------------------------------------------------------------------------------------------------------
                                string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                                string ActionNote3 = "فشل في حفظ فاتورة";
                                _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                                //-----------------------------------------------------------------------------------------------------------------

                                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                            }
                            itemToAccountId = item.ToAccountId;
                            itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                            itemInvoiceId = item.InvoiceId;
                            itemTaxType = item.TaxType;

                        }
                        CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                        BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                        if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                        {
                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = 0;
                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = 0;
                            }
                        }
                        else
                        {
                            if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                            {
                                AcPaid = Convert.ToDecimal(voucher.PaidValue);
                                AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;
                                //Edit 21-01-2024 Ostaze Mohammed Req
                                //AcPaid3 = 0;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                var CustomerData = _TaamerProContext.Acc_Suppliers.Where(s => s.SupplierId == voucher.SupplierId).FirstOrDefault();
                                CustomerAccountPaid = CustomerData.AccountId ?? 0;
                                BoxBankAccountPaid = itemToAccountId ?? 0;
                            }
                            else
                            {
                                if (itemTaxType == 2)
                                {
                                    AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                }
                                else
                                {
                                    AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                }

                            }

                        }
                        if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                        {
                            var CustomerDataV = _TaamerProContext.Acc_Suppliers.Where(s => s.SupplierId == voucher.SupplierId).FirstOrDefault();
                            CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                        }
                        _TaamerProContext.SaveChanges();
                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault().Type,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = AcPaid,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });

                        if (voucher.DiscountValue != null)
                        {
                            if (Convert.ToInt32(voucher.DiscountValue) != 0)
                            {
                                voucher.TransactionDetails.Add(new Transactions
                                {

                                    AccTransactionDate = voucher.Date,
                                    AccTransactionHijriDate = voucher.HijriDate,
                                    TransactionDate = voucher.Date,
                                    TransactionHijriDate = voucher.HijriDate,
                                    AccountId = Branch.PurchaseDiscAccId,
                                    CostCenterId = CostId,
                                    AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.PurchaseDiscAccId)?.FirstOrDefault()?.Type,
                                    Type = voucher.Type,
                                    LineNumber = 1,
                                    Depit = voucher.DiscountValue,
                                    Credit = 0,
                                    YearId = yearid,
                                    Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                    Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                    //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                    InvoiceReference = "",

                                    InvoiceId = voucher.InvoiceId,
                                    IsPost = false,
                                    BranchId = BranchId,
                                    AddDate = DateTime.Now,
                                    AddUser = UserId,
                                    IsDeleted = false,
                                });

                            }

                        }

                        foreach (var item in voucher.VoucherDetails.ToList())
                        {
                            var PurchItem = _TaamerProContext.Acc_Categories.Where(s=>s.CategoryId == item.CategoryId).FirstOrDefault();
                            int AccountIdPurchItem = 0;
                            if (PurchItem != null)
                            {
                                AccountIdPurchItem = PurchItem.AccountId ?? 0;
                                if (AccountIdPurchItem == 0)
                                {
                                    //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.selectItemAccountBeforeSaving };
                                }
                            }
                            else
                            {
                                //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                            }
                            //item.AccountId =AccountIdPurchItem;
                            item.Qty = item.Qty ?? 1;
                            item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                            if (itemTaxType == 2)
                            {
                                item.Amount = (item.Amount * item.Qty) - (item.DiscountValue_Det);
                            }
                            else
                            {
                                item.Amount = item.TotalAmount - (item.TaxAmount);
                            }
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = AccountIdPurchItem,

                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== AccountIdPurchItem)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 2,
                                Credit = item.Amount,
                                Depit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",

                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                VoucherDetailsId = item.VoucherDetailsId,

                            });

                        }



                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            //AccountId = AccOVAT,// الضريبة
                            AccountId = Branch.SuspendedFundAccId,// الضريبة

                            CostCenterId = CostId,//item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.SuspendedFundAccId)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 3,
                            Credit = SVAT,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });

                        if (AcPaid3 != 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = BoxBankAccountPaid,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 4,
                                Depit = AcPaid3,
                                Credit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = CustomerAccountPaid,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 5,
                                Depit = 0,
                                Credit = AcPaid3,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });


                        }


                        _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                        _TaamerProContext.SaveChanges();

                        if (VoucherUpdated != null)
                        {
                            VoucherUpdated.DepitNotiId = -1;

                        }

                        if (voucher.IsPost != true)
                        {
                            voucher.IsPost = true;
                            voucher.PostDate = voucher.Date;
                            voucher.PostHijriDate = voucher.HijriDate;

                            var newJournal = new Journals();
                            var JNo =  _JournalsRepository.GenerateNextJournalNumber(yearid ?? default(int), BranchId).Result;
                            if (voucher.Type == 10)
                            {
                                JNo = 1;
                            }
                            else
                            {
                                JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                            }


                            newJournal.JournalNo = JNo;
                            newJournal.YearMalia = yearid ?? default(int);
                            newJournal.VoucherId = voucher.InvoiceId;
                            newJournal.VoucherType = voucher.Type;
                            newJournal.BranchId = voucher.BranchId ?? 0;
                            newJournal.AddDate = DateTime.Now;
                            newJournal.AddUser = newJournal.UserId = UserId;
                            _TaamerProContext.Journals.Add(newJournal);
                            foreach (var trans in voucher.TransactionDetails.ToList())
                            {
                                trans.IsPost = true;
                                trans.JournalNo = newJournal.JournalNo;
                            }
                            voucher.JournalNumber = newJournal.JournalNo;
                        }

                        _TaamerProContext.SaveChanges();


                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate11 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote11 = "اضافة اشعار مدين جديد" + " برقم " + voucher.InvoiceNumber; ;
                        _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate11, UserId, BranchId, ActionNote11, 1);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId };


                    }
                    else
                    {

                        if (voucher.CostCenterId > 0)
                        {
                            CostId = voucher.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }
                        voucher.CostCenterId = CostId;


                        voucher.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                        var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.SuspendedFundAccId).Select(s => s.AccountId).FirstOrDefault();

                        var ObjList = new List<object>();


                        VoucherUpdatedDepit.FirstOrDefault().InvoiceValue = VoucherUpdatedDepit.FirstOrDefault().InvoiceValue + voucher.InvoiceValue;
                        VoucherUpdatedDepit.FirstOrDefault().TaxAmount = VoucherUpdatedDepit.FirstOrDefault().TaxAmount + voucher.TaxAmount;
                        VoucherUpdatedDepit.FirstOrDefault().TotalValue = VoucherUpdatedDepit.FirstOrDefault().TotalValue + voucher.TotalValue;

                        VoucherUpdatedDepit.FirstOrDefault().InvoiceValueText = ConvertToWord_NEW(VoucherUpdatedDepit.FirstOrDefault().TotalValue.ToString());


                        int? itemToAccountId = 0;
                        int? itemAccountId = 0;
                        var vouchertypename = "";
                        var vouchertypename2 = "";
                        int? itemInvoiceId = 0;
                        decimal? AcPaid = 0;
                        decimal? AcPaid2 = 0;
                        decimal? AcPaid3 = 0;

                        decimal? SVAT = 0;
                        int? itemTaxType = 0;
                        int? AccountMsrofat = 0;


                        foreach (var item in voucher.VoucherDetails.ToList())
                        {


                            //ToAccount = item.AccountId ?? 0;
                            ToAccount = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;

                            // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                            //if (ServicesUpdated != null)
                            //{
                            //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                            //}


                            if (VoucherDetailsUpdated.Count() > 0)
                            {

                                foreach (var Det in VoucherDetailsUpdated.ToList())
                                {
                                    if (item.CategoryId == Det.CategoryId)
                                    {
                                        var QtyDiff = Det.Qty - item.Qty;
                                        if (QtyDiff <= 0)
                                        {
                                            Det.IsRetrieve = 1;

                                        }
                                    }
                                }

                            }


                            AccountMsrofat = Branch.CashInvoicesAccId;


                            ObjList.Add(new { item.AccountId, item.CostCenterId });
                            item.AddUser = UserId;
                            item.PayType = voucher.PayType;

                            item.AddDate = DateTime.Now;

                            decimal? TotalWithoutVAT = (item.Amount);
                            decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                            SVAT = (voucher.TaxAmount);
                            //SVAT = SVAT + (item.TaxAmount * item.Qty);
                            item.CostCenterId = CostId;
                            decimal? Depit = item.Amount + item.TaxAmount;

                            decimal? VAT = item.TaxAmount;
                            item.TotalAmount = item.TotalAmount;

                            item.TFK = ConvertToWord_NEW(Depit.ToString());

                            _TaamerProContext.VoucherDetails.Add(item);
                            vouchertypename = "اشعار مدين لفاتورة";
                            vouchertypename2 = " خصم اشعار مدين لفاتورة مشتريات ";
                            if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 1)
                            {

                                //-----------------------------------------------------------------------------------------------------------------
                                string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                                string ActionNote3 = "فشل في حفظ فاتورة";
                                _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                                //-----------------------------------------------------------------------------------------------------------------

                                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                            }
                            itemToAccountId = item.ToAccountId;
                            itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                            itemInvoiceId = item.InvoiceId;
                            itemTaxType = item.TaxType;
                            item.InvoiceId = VoucherUpdatedDepit.FirstOrDefault().InvoiceId;

                        }

                        CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                        BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                        if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                        {
                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = 0;
                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = 0;
                            }
                        }
                        else
                        {
                            if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                            {
                                AcPaid = Convert.ToDecimal(voucher.PaidValue);
                                AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;
                                //Edit 21-01-2024 Ostaze Mohammed Req
                                //AcPaid3 = 0;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                var CustomerData = _TaamerProContext.Acc_Suppliers.Where(s => s.SupplierId == voucher.SupplierId).FirstOrDefault();
                                CustomerAccountPaid = CustomerData.AccountId ?? 0;
                                BoxBankAccountPaid = itemToAccountId ?? 0;
                            }
                            else
                            {
                                if (itemTaxType == 2)
                                {
                                    AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                }
                                else
                                {
                                    AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                    AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                    AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                                }

                            }

                        }
                        if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                        {
                            var CustomerDataV = _TaamerProContext.Acc_Suppliers.Where(s => s.SupplierId == voucher.SupplierId).FirstOrDefault();
                            CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                        }
                        _TaamerProContext.SaveChanges();
                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = AcPaid,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = VoucherUpdatedDepit.FirstOrDefault().InvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdatedDepit.FirstOrDefault().JournalNumber,


                        });

                        if (voucher.DiscountValue != null)
                        {
                            if (Convert.ToInt32(voucher.DiscountValue) != 0)
                            {
                                voucher.TransactionDetails.Add(new Transactions
                                {

                                    AccTransactionDate = voucher.Date,
                                    AccTransactionHijriDate = voucher.HijriDate,
                                    TransactionDate = voucher.Date,
                                    TransactionHijriDate = voucher.HijriDate,
                                    AccountId = Branch.PurchaseDiscAccId,
                                    CostCenterId = CostId,
                                    AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.PurchaseDiscAccId)?.FirstOrDefault()?.Type,
                                    Type = voucher.Type,
                                    LineNumber = 1,
                                    Depit = voucher.DiscountValue,
                                    Credit = 0,
                                    YearId = yearid,
                                    Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                    Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                    //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                    InvoiceReference = "",

                                    InvoiceId = VoucherUpdatedDepit.FirstOrDefault().InvoiceId,
                                    IsPost = true,
                                    BranchId = BranchId,
                                    AddDate = DateTime.Now,
                                    AddUser = UserId,
                                    IsDeleted = false,
                                    JournalNo = VoucherUpdatedDepit.FirstOrDefault().JournalNumber,
                                });

                            }

                        }

                        foreach (var item in voucher.VoucherDetails.ToList())
                        {
                            var PurchItem = _TaamerProContext.Acc_Categories.Where(s=>s.CategoryId == item.CategoryId).FirstOrDefault();
                            int AccountIdPurchItem = 0;
                            if (PurchItem != null)
                            {
                                AccountIdPurchItem = PurchItem.AccountId ?? 0;
                                if (AccountIdPurchItem == 0)
                                {
                                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.selectItemAccountBeforeSaving };
                                }
                            }
                            else
                            {
                                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                            }
                            //item.AccountId =AccountIdPurchItem;
                            item.Qty = item.Qty ?? 1;
                            item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                            if (itemTaxType == 2)
                            {
                                item.Amount = (item.Amount * item.Qty) - (item.DiscountValue_Det);
                            }
                            else
                            {
                                item.Amount = item.TotalAmount - (item.TaxAmount);
                            }
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = AccountIdPurchItem,

                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == AccountIdPurchItem)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 2,
                                Credit = item.Amount,
                                Depit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",

                                InvoiceId = VoucherUpdatedDepit.FirstOrDefault().InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdatedDepit.FirstOrDefault().JournalNumber,
                                VoucherDetailsId = item.VoucherDetailsId,

                            });

                        }



                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            //AccountId = AccOVAT,// الضريبة
                            AccountId = Branch.SuspendedFundAccId,// الضريبة

                            CostCenterId = CostId,//item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.SuspendedFundAccId)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 3,
                            Credit = SVAT,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = VoucherUpdatedDepit.FirstOrDefault().InvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdatedDepit.FirstOrDefault().JournalNumber,
                        });

                        if (AcPaid3 != 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = BoxBankAccountPaid,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 4,
                                Depit = AcPaid3,
                                Credit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = VoucherUpdatedDepit.FirstOrDefault().InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdatedDepit.FirstOrDefault().JournalNumber,
                            });
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = CustomerAccountPaid,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 5,
                                Depit = 0,
                                Credit = AcPaid3,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = VoucherUpdatedDepit.FirstOrDefault().InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdatedDepit.FirstOrDefault().JournalNumber,
                            });


                        }


                        _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                        _TaamerProContext.SaveChanges();

                        if (VoucherUpdated != null)
                        {
                            VoucherUpdated.DepitNotiId = -1;

                        }

                     

                    }
                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة اشعار مدين جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId };
                }
                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ اشعار مدين لفاتورة مشتريات";
                _SystemAction.SaveAction("SaveInvoiceForServices", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public  GeneralMessage SaveandPostPurchaseForServices(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {

                var ToAccount = 0;
                var CustomerAccountPaid = 0;
                var BoxBankAccountPaid = 0;
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.CashInvoicesAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote2 = "فشل في حفظ وترحيل الفاتورة";
                    _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.EnsureThatPurchasesAreAccounted, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatPurchasesAreAccounted };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ وترحيل الفاتورة";
                    _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.PurchaseDiscAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate4 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote4 = "فشل في حفظ وترحيل الفاتورة";
                    _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.EnsureDeductionOfPurchasesIsAccounted, "", "", ActionDate4, UserId, BranchId, ActionNote4, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureDeductionOfPurchasesIsAccounted };

                }
                else if (Branch == null || Branch.SuspendedFundAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate5 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote5 = "فشل في حفظ وترحيل الفاتورة";
                    _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.EnsureTheDeductionOfPurchasesAccountedTheBranchAccounts, "", "", ActionDate5, UserId, BranchId, ActionNote5, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheDeductionOfPurchasesAccountedTheBranchAccounts };
                }

                foreach (var item in voucher.VoucherDetails.ToList())
                {
                    var PurchItem = _TaamerProContext.Acc_Categories.Where(s=>s.CategoryId == item.CategoryId).FirstOrDefault();
                    int AccountIdPurchItem = 0;
                    if (PurchItem != null)
                    {
                        AccountIdPurchItem = PurchItem.AccountId ?? 0;
                        if (AccountIdPurchItem == 0)
                        {
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.selectItemAccountBeforeSaving };
                        }
                    }
                    else
                    {
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                    }

                }



                int? CostId;
                voucher.TransactionDetails = new List<Transactions>();

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate6 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote6 = "فشل في حفظ وترحيل الفاتورة";
                    _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate6, UserId, BranchId, ActionNote6, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                decimal Disc = 0;
                //if (voucher.DiscountValue != null)
                //{
                //    if (Convert.ToInt32(voucher.DiscountValue) != 0)
                //    {

                //        Disc = voucher.DiscountValue ?? 0;
                //    }
                //    else
                //    {
                //        Disc = 0;

                //    }
                //}
                //else
                //{
                //    Disc = 0;
                //}
                if (voucher.InvoiceId == 0)
                {

                    if (String.IsNullOrWhiteSpace(Convert.ToString(voucher.ToAccountId)) && voucher.Type == 1)
                    {

                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote3 = "فشل في حفظ وترحيل الفاتورة";
                        _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, "يجب اختيار حساب المورد", "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
                    }

                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                    voucher.YearId = yearid;
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.AddUser = UserId;
                    voucher.BranchId = BranchId;
                    voucher.AddDate = DateTime.Now;
                    voucher.ProjectId = voucher.ProjectId;
                    voucher.DunCalc = false;


                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                       
                    }

                    //if (voucher.ProjectId != null)
                    //{
                    //    try
                    //    { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                    //    catch
                    //    {
                    //        try
                    //        { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                    //        catch
                    //        { CostId = null; }
                    //    }

                    //}
                    //else
                    //{
                    //    try
                    //    { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                    //    catch
                    //    { CostId = null; }
                    //}
                    //if (CostId == 0)
                    //{
                    //    try
                    //    { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                    //    catch
                    //    { CostId = null; }
                    //}

                    if (voucher.CostCenterId > 0)
                    {
                        CostId = voucher.CostCenterId;
                    }
                    else
                    {
                        try
                        { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                        catch
                        { CostId = null; }
                    }

                    voucher.CostCenterId = CostId;
                    voucher.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.SuspendedFundAccId).Select(s => s.AccountId).FirstOrDefault();

                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();

                    int? itemToAccountId = 0;
                    int? itemAccountId = 0;
                    var vouchertypename = "";
                    var vouchertypename2 = "";
                    int? itemInvoiceId = 0;
                    decimal? AcPaid = 0;
                    decimal? AcPaid2 = 0;
                    decimal? AcPaid3 = 0;

                    decimal? SVAT = 0;
                    int? itemTaxType = 0;
                    int? AccountMsrofat = 0;

                    foreach (var item in voucher.VoucherDetails.ToList())
                    {


                        ToAccount = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;

                        // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                        //if (ServicesUpdated != null)
                        //{
                        //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                        //}

                        AccountMsrofat = Branch.CashInvoicesAccId;

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AddUser = UserId;
                        item.PayType = voucher.PayType;

                        item.AddDate = DateTime.Now;

                        decimal? TotalWithoutVAT = (item.Amount);
                        decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                        SVAT = (voucher.TaxAmount);
                        //SVAT = SVAT + (item.TaxAmount * item.Qty);

                        item.CostCenterId = CostId;
                        decimal? Depit = item.Amount + item.TaxAmount;

                        decimal? VAT = item.TaxAmount;
                        item.TotalAmount = item.TotalAmount;

                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        _TaamerProContext.VoucherDetails.Add(item);
                        vouchertypename = voucher.Type == 1 ? " فاتورة" + " مشتريات" : "فاتورة";
                        vouchertypename2 = " خصم فاتورة مشتريات ";

                        itemToAccountId = item.ToAccountId;
                        itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                        itemInvoiceId = item.InvoiceId;
                        itemTaxType = item.TaxType;

                    }
                    CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                    BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                    if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                    {
                        if (itemTaxType == 2)
                        {
                            AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                            AcPaid3 = 0;
                        }
                        else
                        {
                            AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                            AcPaid3 = 0;
                        }
                    }
                    else
                    {
                        if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                        {
                            AcPaid = Convert.ToDecimal(voucher.PaidValue);
                            AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;
                            //Edit 21-01-2024 Ostaze Mohammed Req
                            //AcPaid3 = 0;
                            AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                            var CustomerData = _TaamerProContext.Acc_Suppliers.Where(s => s.SupplierId == voucher.SupplierId).FirstOrDefault();
                            CustomerAccountPaid = CustomerData.AccountId ?? 0;
                            BoxBankAccountPaid = itemToAccountId ?? 0;
                        }
                        else
                        {
                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                            }

                        }

                    }
                    if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                    {
                        var CustomerDataV = _TaamerProContext.Acc_Suppliers.Where(s => s.SupplierId == voucher.SupplierId).FirstOrDefault();
                        CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                    }
                    _TaamerProContext.SaveChanges();
                    voucher.TransactionDetails.Add(new Transactions
                    {

                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        AccountId = CustomerAccountPaid,
                        CostCenterId = CostId,
                        AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == CustomerAccountPaid)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 1,
                        Depit = 0,
                        Credit = AcPaid,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        InvoiceReference = "",
                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });

                    if (voucher.DiscountValue != null)
                    {
                        if (Convert.ToInt32(voucher.DiscountValue) != 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {

                                AccTransactionDate = voucher.Date,
                                AccTransactionHijriDate = voucher.HijriDate,
                                TransactionDate = voucher.Date,
                                TransactionHijriDate = voucher.HijriDate,
                                AccountId = Branch.PurchaseDiscAccId,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.PurchaseDiscAccId)?.FirstOrDefault()?.Type,
                                Type = voucher.Type,
                                LineNumber = 1,
                                Depit = 0,
                                Credit = voucher.DiscountValue,
                                YearId = yearid,
                                Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                                InvoiceReference = "",

                                InvoiceId = voucher.InvoiceId,
                                IsPost = false,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });

                        }

                    }
                    ////credit 


                    foreach (var item in voucher.VoucherDetails.ToList())
                    {
                        var PurchItem = _TaamerProContext.Acc_Categories.Where(s=>s.CategoryId == item.CategoryId).FirstOrDefault();

                        int AccountIdPurchItem = 0;
                        if (PurchItem != null)
                        {
                            AccountIdPurchItem = PurchItem.AccountId ?? 0;
                            if (AccountIdPurchItem == 0)
                            {
                                //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.selectItemAccountBeforeSaving };
                            }
                        }
                        else
                        {
                            //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                        }
                        //item.AccountId = AccountIdPurchItem;
                        item.Qty = item.Qty ?? 1;
                        item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                        if (itemTaxType == 2)
                        {
                            item.Amount = (item.Amount * item.Qty) - (item.DiscountValue_Det);
                        }
                        else
                        {
                            item.Amount = item.TotalAmount - (item.TaxAmount);
                        }
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = AccountIdPurchItem,

                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == AccountIdPurchItem)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = 0,
                            Depit = item.Amount,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            VoucherDetailsId = item.VoucherDetailsId,

                        });

                    }


                    voucher.TransactionDetails.Add(new Transactions
                    {

                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        //AccountId = AccOVAT,// الضريبة
                        AccountId = Branch.SuspendedFundAccId,// الضريبة

                        CostCenterId = CostId,//item.CostCenterId,
                        AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.SuspendedFundAccId)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 3,
                        Credit = 0,
                        Depit = SVAT,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        InvoiceReference = "",

                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });

                    if (AcPaid3 != 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = BoxBankAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 4,
                            Depit = 0,
                            Credit = AcPaid3,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 5,
                            Depit = AcPaid3,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });


                    }

                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                    if (voucher.ServicesPriceOffer != null && voucher.ServicesPriceOffer.Count > 0)
                    {
                        foreach (var item in voucher.ServicesPriceOffer)
                        {
                            item.AddUser = UserId;
                            item.AddDate = DateTime.Now;
                            item.InvoiceId = voucher.InvoiceId;
                            _TaamerProContext.Acc_Services_PriceOffer.Add(item);
                        }
                    }

                    _TaamerProContext.SaveChanges();
                    voucher.QRCodeNum = "200010001000" + voucher.InvoiceId.ToString();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 2 && s.YearId == yearid && s.InvoiceNumber == voucher.InvoiceNumber && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucherDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                        if (TransactionDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    }

                    if (voucher.IsPost != true)
                    {
                        voucher.IsPost = true;
                        //voucher.PostDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        //voucher.PostHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));
                        voucher.PostDate = voucher.Date;
                        voucher.PostHijriDate = voucher.HijriDate;

                        var newJournal = new Journals();
                        var JNo =  _JournalsRepository.GenerateNextJournalNumber(yearid ?? default(int), BranchId).Result;
                        if (voucher.Type == 10)
                        {
                            JNo = 1;
                        }
                        else
                        {
                            JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                        }


                        newJournal.JournalNo = JNo;
                        newJournal.YearMalia = yearid ?? default(int);
                        newJournal.VoucherId = voucher.InvoiceId;
                        newJournal.VoucherType = voucher.Type;
                        newJournal.BranchId = voucher.BranchId ?? 0;
                        newJournal.AddDate = DateTime.Now;
                        newJournal.AddUser = newJournal.UserId = UserId;
                        _TaamerProContext.Journals.Add(newJournal);
                        foreach (var trans in voucher.TransactionDetails.ToList())
                        {
                            trans.IsPost = true;
                            trans.JournalNo = newJournal.JournalNo;
                        }
                        voucher.JournalNumber = newJournal.JournalNo;
                    }
                    else
                    {
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnterNewInvoice };

                    }
                }
                _TaamerProContext.SaveChanges();
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "اضافة فاتورة مشتريات جديد" + " برقم " + voucher.InvoiceNumber; ;
                _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------
                List<int> voDetIds = new List<int>();
                foreach (var itemV in voucher.VoucherDetails)
                {
                    voDetIds.Add(itemV.VoucherDetailsId);
                }
                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId, InvoiceIsDeleted = voucher.IsDeleted, voucherDetObj = voDetIds };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ وترحيل الفاتورة";
                _SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage SavePurchaseForServicesRet(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {

                var ToAccount = 0;
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.CashReturnInvoicesAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.returnsAreAccountedForInTheBranchAccounts, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.returnsAreAccountedForInTheBranchAccounts };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.PurchaseDiscAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureDeductionOfPurchasesIsAccounted, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureDeductionOfPurchasesIsAccounted };

                }
                else if (Branch == null || Branch.SuspendedFundAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureTheDeductionOfPurchasesAccountedTheBranchAccounts, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheDeductionOfPurchasesAccountedTheBranchAccounts };
                }


                int? CostId;
                voucher.TransactionDetails = new List<Transactions>();

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة مشتريات";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }


                var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                decimal Disc = 0;
                if (VoucherUpdated.DiscountValue != null)
                {
                    if (Convert.ToInt32(VoucherUpdated.DiscountValue) != 0)
                    {

                        Disc = VoucherUpdated.DiscountValue ?? 0;
                    }
                    else
                    {
                        Disc = 0;

                    }
                }
                else
                {
                    Disc = 0;
                }
                if (VoucherUpdated != null)
                {

                    VoucherUpdated.Rad = true;

                    //int nextnumber = _InvoicesRepository.GenerateNextInvoiceNumberRet(VoucherUpdated.Type, yearid, BranchId).Result ?? 0;
                    var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;

                    //var NewNextInv = string.Format("{0:000000}", nextnumber);

                    VoucherUpdated.InvoiceRetId = NewNextInv.ToString();


                    if(VoucherUpdated.CostCenterId>0)
                    {
                        CostId = VoucherUpdated.CostCenterId;
                    }
                    else
                    {
                        try
                        { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                        catch
                        { CostId = null; }
                    }

                    voucher.CostCenterId = CostId;
                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.SuspendedFundAccId).Select(s => s.AccountId).FirstOrDefault();



                    int? itemToAccountId = 0;
                    int? itemAccountId = 0;
                    var vouchertypename = "";
                    var vouchertypename2 = "";
                    int? itemInvoiceId = 0;
                    decimal? AcPaid = 0;
                    decimal? AcPaid2 = 0;
                    decimal? AcPaid3 = 0;
                    decimal? SVAT = 0;
                    int? itemTaxType = 0;
                    // add new details
                    var ObjList = new List<object>();
                    foreach (var item in VoucherUpdated.VoucherDetails.ToList())
                    {

                        ToAccount = item.AccountId ?? 0;
                        ToAccount = VoucherUpdated.VoucherDetails.FirstOrDefault().AccountId ?? 0;

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.InvoiceId = VoucherUpdated.InvoiceId;
                        item.PayType = VoucherUpdated.PayType;
                        item.AddUser = 1;
                        item.AddDate = DateTime.Now;
                        decimal? TotalWithoutVAT = (item.Amount);
                        decimal? TotalWithVAT = ((item.Amount) + item.TaxAmount);
                        //SVAT = (VoucherUpdated.TaxAmount);
                        SVAT = SVAT + (item.TaxAmount * item.Qty);
                        item.CostCenterId = CostId;
                        var VouchDa = VoucherUpdated.Date;

                        CultureInfo culture = new CultureInfo("en-US");
                        DateTime tempDate = Convert.ToDateTime(VouchDa, culture);
                        DateTime Today = DateTime.Now;
                        var diff = (Today - tempDate).TotalDays;



                        decimal? Depit = item.Amount + item.TaxAmount;
                        item.TotalAmount = item.TotalAmount;

                        item.TFK = ConvertToWord_NEW(Depit.ToString());


                        //if (Convert.ToInt32(diff) > 90)
                        //{
                        //    SVAT = 0;
                        //}

                        //// add transaction
                        vouchertypename = "مردود فاتورة مشتريات";
                        vouchertypename2 = "مردود خصم فاتورة مشتريات";

                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && VoucherUpdated.Type == 2)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote3 = "فشل في حفظ فاتورة مشتريات";
                            _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                            //-----------------------------------------------------------------------------------------------------------------
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }
                        //depit 

                        itemToAccountId = item.ToAccountId;
                        itemAccountId = item.AccountId;
                        itemInvoiceId = item.InvoiceId;
                        itemTaxType = item.TaxType;

                    }

                    if (VoucherUpdated.PaidValue == 0 || VoucherUpdated.PaidValue == null)
                    {


                        if (itemTaxType == 2)
                        {
                            AcPaid = Convert.ToDecimal(VoucherUpdated.InvoiceValue) + SVAT - Disc;
                            AcPaid2 = Convert.ToDecimal(VoucherUpdated.InvoiceValue);
                            AcPaid3 = 0;
                        }
                        else
                        {
                            AcPaid = Convert.ToDecimal(VoucherUpdated.InvoiceValue) - Disc;
                            AcPaid2 = Convert.ToDecimal(VoucherUpdated.InvoiceValue) - SVAT;
                            AcPaid3 = 0;
                        }
                    }
                    else
                    {
                        if (Convert.ToInt32(VoucherUpdated.PaidValue) == Convert.ToInt32(VoucherUpdated.TotalValue))
                        {
                            AcPaid = Convert.ToDecimal(VoucherUpdated.PaidValue);
                            AcPaid2 = Convert.ToDecimal(VoucherUpdated.PaidValue) + Disc - SVAT;
                            AcPaid3 = 0;
                        }
                        else
                        {

                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(VoucherUpdated.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(VoucherUpdated.InvoiceValue);
                                AcPaid3 = Convert.ToDecimal(VoucherUpdated.PaidValue);
                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(VoucherUpdated.InvoiceValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(VoucherUpdated.InvoiceValue) - SVAT;
                                AcPaid3 = Convert.ToDecimal(VoucherUpdated.PaidValue);
                            }
                        }
                    }
                    _TaamerProContext.SaveChanges();


                    foreach (var item in VoucherUpdated.VoucherDetails.ToList())
                    {
                        var PurchItem = _TaamerProContext.Acc_Categories.Where(s=>s.CategoryId== item.CategoryId).FirstOrDefault();

                        int AccountIdPurchItem = 0;
                        if (PurchItem != null)
                        {
                            AccountIdPurchItem = PurchItem.AccountId ?? 0;
                            if (AccountIdPurchItem == 0)
                            {
                                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.selectItemAccountBeforeSaving };
                            }
                            if(AccountIdPurchItem == Branch.CashInvoicesAccId)
                            {
                                AccountIdPurchItem = Branch.CashReturnInvoicesAccId??0;
                            }
                        }
                        else
                        {
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                        }
                        item.AccountId = AccountIdPurchItem;
                        item.Qty = item.Qty ?? 1;
                        if (itemTaxType == 2)
                        {
                            item.Amount = (item.Amount * item.Qty);
                        }
                        else
                        {
                            item.Amount = item.TotalAmount - (item.TaxAmount * item.Qty);
                        }

                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = VoucherUpdated.Date,
                            AccTransactionHijriDate = VoucherUpdated.HijriDate,
                            TransactionDate = VoucherUpdated.Date,
                            TransactionHijriDate = VoucherUpdated.HijriDate,
                            AccountId = AccountIdPurchItem,

                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == AccountIdPurchItem)?.FirstOrDefault()?.Type,
                            Type = 3,
                            LineNumber = 1,
                            Credit = item.Amount,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            InvoiceReference = "",

                            InvoiceId = itemInvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdated.JournalNumber,
                            VoucherDetailsId=item.VoucherDetailsId,

                        });

                    }

                    //credit 
                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = VoucherUpdated.Date,
                        AccTransactionHijriDate = VoucherUpdated.HijriDate,
                        TransactionDate = VoucherUpdated.Date,
                        TransactionHijriDate = VoucherUpdated.HijriDate,
                        //TransactionDate = VoucherUpdated.Date,
                        //TransactionHijriDate = VoucherUpdated.HijriDate,
                        AccountId = itemToAccountId,
                        CostCenterId = CostId,
                        AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == itemToAccountId)?.FirstOrDefault()?.Type,
                        Type = 3,
                        LineNumber = 2,
                        Credit = 0,
                        Depit = AcPaid,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                        InvoiceReference = "",
                        InvoiceId = itemInvoiceId,
                        IsPost = true,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                        JournalNo = VoucherUpdated.JournalNumber

                    });



                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = VoucherUpdated.Date,
                        AccTransactionHijriDate = VoucherUpdated.HijriDate,
                        TransactionDate = VoucherUpdated.Date,
                        TransactionHijriDate = VoucherUpdated.HijriDate,
                        AccountId = Branch.SuspendedFundAccId,// الضريبة

                        CostCenterId = CostId,
                        AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.SuspendedFundAccId)?.FirstOrDefault()?.Type,
                        Type = 3,
                        LineNumber = 2,
                        Credit = SVAT,
                        Depit = 0,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",
                        InvoiceId = itemInvoiceId,
                        IsPost = true,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                        JournalNo = VoucherUpdated.JournalNumber

                    });
                    if (VoucherUpdated.DiscountValue != null)
                    {
                        if (Convert.ToInt32(VoucherUpdated.DiscountValue) != 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = VoucherUpdated.Date,
                                AccTransactionHijriDate = VoucherUpdated.HijriDate,
                                TransactionDate = VoucherUpdated.Date,
                                TransactionHijriDate = VoucherUpdated.HijriDate,
                                AccountId = Branch.PurchaseDiscAccId,// الضريبة

                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == Branch.PurchaseDiscAccId)?.FirstOrDefault()?.Type,
                                Type = 3,
                                LineNumber = 2,
                                Credit = 0,
                                Depit = VoucherUpdated.DiscountValue,
                                YearId = yearid,
                                Notes = "" + vouchertypename2 + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename2 + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = itemInvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdated.JournalNumber

                            });

                        }
                    }

                    if (AcPaid3 != 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = VoucherUpdated.Date,
                            AccTransactionHijriDate = VoucherUpdated.HijriDate,
                            TransactionDate = VoucherUpdated.Date,
                            TransactionHijriDate = VoucherUpdated.HijriDate,
                            AccountId = ToAccount,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== ToAccount)?.FirstOrDefault()?.Type,
                            Type = 3,
                            LineNumber = 4,
                            Depit = AcPaid3,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = itemInvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdated.JournalNumber

                        });
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = VoucherUpdated.Date,
                            AccTransactionHijriDate = VoucherUpdated.HijriDate,
                            TransactionDate = VoucherUpdated.Date,
                            TransactionHijriDate = VoucherUpdated.HijriDate,
                            AccountId = itemToAccountId,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== itemToAccountId)?.FirstOrDefault()?.Type,
                            Type = 3,
                            LineNumber = 5,
                            Depit = 0,
                            Credit = AcPaid3,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = itemInvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdated.JournalNumber
                        });


                    }


                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                }
                _TaamerProContext.SaveChanges();

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " تعديل فاتورة مشتريات رقم " + voucher.InvoiceId;
                _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في تعديل الفاتورة";
                _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage SavePurchaseForServicesRetNEW_func(Invoices voucher, int UserId, int BranchId, int? yearid, string lang, string Con)
        {
            try
            {

                var ToAccount = 0;
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.CashReturnInvoicesAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.returnsAreAccountedForInTheBranchAccounts, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.returnsAreAccountedForInTheBranchAccounts };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.PurchaseDiscAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureDeductionOfPurchasesIsAccounted, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureDeductionOfPurchasesIsAccounted };

                }
                else if (Branch == null || Branch.SuspendedFundAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.EnsureTheDeductionOfPurchasesAccountedTheBranchAccounts, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheDeductionOfPurchasesAccountedTheBranchAccounts };
                }


                int? CostId;
                voucher.TransactionDetails = new List<Transactions>();

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ فاتورة مشتريات";
                    _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }


                var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                decimal Disc = 0;
                if (VoucherUpdated.DiscountValue != null)
                {
                    if (Convert.ToInt32(VoucherUpdated.DiscountValue) != 0)
                    {

                        Disc = VoucherUpdated.DiscountValue ?? 0;
                    }
                    else
                    {
                        Disc = 0;

                    }
                }
                else
                {
                    Disc = 0;
                }
                if (VoucherUpdated != null)
                {

                    VoucherUpdated.Rad = true;

                    int TotalAfter = 1;
                    if (TotalAfter == 0)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate33 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote33 = "لا يمكن رد فاتورة تم عمل اشعار مدين لها بكامل المبلغ";
                        _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate33, UserId, BranchId, ActionNote33, 0);
                        //-----------------------------------------------------------------------------------------------------------------
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = ActionNote33 };
                    }

                    //int nextnumber = _InvoicesRepository.GenerateNextInvoiceNumberRet(VoucherUpdated.Type, yearid, BranchId).Result ?? 0;
                    var NewNextInv = GenerateVoucherNumberNewPro(VoucherUpdated.Type, BranchId, yearid, 4, Con).Result;
                    //var NewNextInv = string.Format("{0:000000}", nextnumber);

                    VoucherUpdated.InvoiceRetId = NewNextInv.ToString();
                    if (VoucherUpdated.CostCenterId > 0)
                    {
                        CostId = VoucherUpdated.CostCenterId;
                    }
                    else
                    {
                        try
                        { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                        catch
                        { CostId = null; }
                    }

                    voucher.CostCenterId = CostId;
                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.SuspendedFundAccId).Select(s => s.AccountId).FirstOrDefault();



                    int? itemToAccountId = 0;
                    int? itemAccountId = 0;
                    var vouchertypename = "";
                    var vouchertypename2 = "";
                    int? itemInvoiceId = 0;
                    decimal? AcPaid = 0;
                    decimal? AcPaid2 = 0;
                    decimal? AcPaid3 = 0;
                    decimal? SVAT = 0;
                    int? itemTaxType = 0;
                    // add new details
                    var ObjList = new List<object>();
                    var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                    foreach (var item in VoucherDetails)
                    {

                        ToAccount = item.AccountId ?? 0;
                        ToAccount = VoucherDetails.FirstOrDefault().AccountId ?? 0;

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.InvoiceId = VoucherUpdated.InvoiceId;
                        item.PayType = VoucherUpdated.PayType;
                        item.AddUser = 1;
                        item.AddDate = DateTime.Now;
                        decimal? TotalWithoutVAT = (item.Amount);
                        decimal? TotalWithVAT = ((item.Amount) + item.TaxAmount);
                        //SVAT = (VoucherUpdated.TaxAmount);
                        SVAT = SVAT + (item.TaxAmount * item.Qty);
                        item.CostCenterId = CostId;
                        var VouchDa = VoucherUpdated.Date;

                        CultureInfo culture = new CultureInfo("en-US");
                        DateTime tempDate = Convert.ToDateTime(VouchDa, culture);
                        DateTime Today = DateTime.Now;
                        var diff = (Today - tempDate).TotalDays;



                        decimal? Depit = item.Amount + item.TaxAmount;
                        item.TotalAmount = item.TotalAmount;

                        item.TFK = ConvertToWord_NEW(Depit.ToString());


                        //if (Convert.ToInt32(diff) > 90)
                        //{
                        //    SVAT = 0;
                        //}

                        //// add transaction
                        vouchertypename = "مردود فاتورة مشتريات";
                        vouchertypename2 = "مردود خصم فاتورة مشتريات";

                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && VoucherUpdated.Type == 2)
                        {

                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote3 = "فشل في حفظ فاتورة مشتريات";
                            _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                            //-----------------------------------------------------------------------------------------------------------------
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }
                        //depit 

                        itemToAccountId = item.ToAccountId;
                        itemAccountId = item.AccountId;
                        itemInvoiceId = item.InvoiceId;
                        itemTaxType = item.TaxType;

                    }

                    var DataOF_InvoiceReturn = _InvoicesRepository.GetInvoiceReturnData_func(itemInvoiceId ?? 0, yearid ?? default(int), BranchId, lang, Con).Result;

                    int counter = 1;

                    foreach (var item_Ret in DataOF_InvoiceReturn)
                    {
                        if (item_Ret.Total != 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = VoucherUpdated.Date,
                                AccTransactionHijriDate = VoucherUpdated.HijriDate,
                                TransactionDate = VoucherUpdated.Date,
                                TransactionHijriDate = VoucherUpdated.HijriDate,

                                AccountId = item_Ret.AccountId,
                                CostCenterId = CostId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== item_Ret.AccountId)?.FirstOrDefault()?.Type,
                                Type = 3,
                                LineNumber = counter,
                                Depit = item_Ret.Credit,
                                Credit = item_Ret.Depit,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                InvoiceReference = "",
                                InvoiceId = itemInvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdated.JournalNumber
                            });
                            counter += 1;
                        }


                    }
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                }
                _TaamerProContext.SaveChanges();

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " تعديل فاتورة مشتريات رقم " + voucher.InvoiceId;
                _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في تعديل الفاتورة";
                _SystemAction.SaveAction("SaveInvoiceForServicesRet", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SavePayVoucherForServicesRet(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {


                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.CashReturnInvoicesAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ الفاتورة";
                    _SystemAction.SaveAction("SavePayVoucherForServicesRet", "VoucherService", 1, Resources.EnsureExpenseReturnAccountedBranchAccounts, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureExpenseReturnAccountedBranchAccounts };

                }
                //else if (Branch == null || Branch.BoxAccId == null)
                //{
                //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                //}
                //else if (Branch == null || Branch.TaxsAccId == null)
                //{
                //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                //}
                else if (Branch == null || Branch.SuspendedFundAccId == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote3 = "فشل في حفظ الفاتورة";
                    _SystemAction.SaveAction("SavePayVoucherForServicesRet", "VoucherService", 1, Resources.EnsureVATcalculatedExpensesBranchAccounts, "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureVATcalculatedExpensesBranchAccounts };
                }


                int? CostId;
                voucher.TransactionDetails = new List<Transactions>();

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate5 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote5 = "فشل في حفظ الفاتورة";
                    _SystemAction.SaveAction("SavePayVoucherForServicesRet", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate5, UserId, BranchId, ActionNote5, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }





                var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();

                //decimal Disc = 0;
                //if (VoucherUpdated.DiscountValue != null)
                //{
                //    if (Convert.ToInt32(VoucherUpdated.DiscountValue) != 0)
                //    {

                //        Disc = VoucherUpdated.DiscountValue ?? 0;
                //    }
                //    else
                //    {
                //        Disc = 0;

                //    }
                //}
                //else
                //{
                //    Disc = 0;
                //}
                if (VoucherUpdated != null)
                {

                    VoucherUpdated.Rad = true;

                   

                    try
                    {
                        CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == VoucherUpdated.ProjectId).Select(s => s.CostCenterId).FirstOrDefault();
                    }
                    catch
                    {
                        CostId = null;
                    }
                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                    var ObjList = new List<object>();

                    var voucherdetails = _TaamerProContext.VoucherDetails.Where(s => s.IsDeleted == false && s.InvoiceId == VoucherUpdated.InvoiceId).ToList();
                    foreach (var item in voucherdetails)
                    {
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.VoucherDetailsId = 0;
                        item.InvoiceId = voucher.InvoiceId;
                        item.PayType = item.PayType;
                        item.AddUser = UserId;
                        item.AddDate = DateTime.Now;
                        decimal? Depit = item.TotalAmount;
                        decimal? Credit = item.TotalAmount - item.TaxAmount;
                        decimal? SVAT = (item.TaxAmount);

                        var VouchDa = VoucherUpdated.Date;

                        CultureInfo culture = new CultureInfo("en-US");
                        DateTime tempDate = Convert.ToDateTime(VouchDa, culture);
                        DateTime Today = DateTime.Now;
                        var diff = (Today - tempDate).TotalDays;


                        //if (Convert.ToInt32(diff) > 90)
                        //{
                        //    SVAT = 0;
                        //}

                        if (item.TaxType == 2) //غير شامل الضريبة
                        {
                            Depit = VoucherUpdated.InvoiceValue + item.TaxAmount;

                        }
                        else //شامل الضريبة
                        {
                            Depit = VoucherUpdated.InvoiceValue;

                        }


                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        // decimal Credit = item.TotalAmount;
                        _TaamerProContext.VoucherDetails.Add(item);
                        //// add transaction



                        //string vouchertypename = VoucherUpdated.Type == 6 ? "سند قبض" : "سند صرف";
                        var vouchertypename = "مردود مصروفات";
                        //var vouchertypename2 = "مردود خصم مصروف";
                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && VoucherUpdated.Type == 6)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في تعديل الفاتورة";
                            _SystemAction.SaveAction("SavePayVoucherForServicesRet", "VoucherService", 2, Resources.ChooseReceiptaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.ChooseReceiptaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && VoucherUpdated.Type == 5)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في تعديل الفاتورة";
                            _SystemAction.SaveAction("SavePayVoucherForServicesRet", "VoucherService", 2, Resources.Chooseexchangeaccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.Chooseexchangeaccount };
                        }
                        else if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && VoucherUpdated.Type == 2)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في تعديل الفاتورة";
                            _SystemAction.SaveAction("SavePayVoucherForServicesRet", "VoucherService", 2, Resources.choosecustomeraccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------

                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }
                        //  _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);


                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = VoucherUpdated.Date,
                            AccTransactionHijriDate = VoucherUpdated.HijriDate,
                            TransactionDate = VoucherUpdated.Date,
                            TransactionHijriDate = VoucherUpdated.HijriDate,
                            //TransactionDate = VoucherUpdated.Date,
                            //TransactionHijriDate = VoucherUpdated.HijriDate,
                            AccountId = item.ToAccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== item.ToAccountId)?.FirstOrDefault()?.Type,
                            Type = 23,
                            LineNumber = 1,
                            Depit = Depit,
                            Credit = 0,


                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = item.InvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdated.JournalNumber

                        });
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = VoucherUpdated.Date,
                            AccTransactionHijriDate = VoucherUpdated.HijriDate,
                            TransactionDate = VoucherUpdated.Date,
                            TransactionHijriDate = VoucherUpdated.HijriDate,
                            AccountId = item.AccountId,
                            CostCenterId = item.CostCenterId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type,
                            Type = 23,
                            LineNumber = 2,
                            Credit = Credit,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                            //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = item.InvoiceId,
                            IsPost = true,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            JournalNo = VoucherUpdated.JournalNumber

                        });
                        if (Convert.ToInt32(item.TaxAmount) != 0)
                        {
                            voucher.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = VoucherUpdated.Date,
                                AccTransactionHijriDate = VoucherUpdated.HijriDate,
                                TransactionDate = VoucherUpdated.Date,
                                TransactionHijriDate = VoucherUpdated.HijriDate,
                                //AccountId = AccOVAT,// الضريبة
                                AccountId = Branch.SuspendedFundAccId,// الضريبة

                                CostCenterId = item.CostCenterId,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==(VoucherUpdated.Type == 2 ? AccOVAT : AccOVAT))?.FirstOrDefault()?.Type,
                                Type = 23,
                                LineNumber = 2,
                                Credit = SVAT,
                                Depit = 0,
                                YearId = yearid,
                                Notes = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                Details = "" + vouchertypename + " " + "رقم" + " " + VoucherUpdated.InvoiceNumber + "",
                                //InvoiceReference = voucher.InvoiceNumber.ToString(),
                                InvoiceReference = "",

                                InvoiceId = VoucherUpdated.InvoiceId,
                                IsPost = true,
                                BranchId = BranchId,
                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                                JournalNo = VoucherUpdated.JournalNumber

                            });

                        }
                    }
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                }
                _TaamerProContext.SaveChanges();
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " تعديل فاتورة رقم " + voucher.InvoiceId;
                _SystemAction.SaveAction("SavePayVoucherForServicesRet", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
            }
            catch (Exception ex)
            { 
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ الفاتورة";
                _SystemAction.SaveAction("SavePayVoucherForServicesRet", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveInvoiceForServices2(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {

                var ToAccount = 0;
                var CustomerAccountPaid = 0;
                var BoxBankAccountPaid = 0;
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.ContractsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ الفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.EnsureRevenueAccountedBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureRevenueAccountedBranchAccounts };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ الفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.SaleDiscountAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ الفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.EnsureThatTheSales, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatTheSales };

                }
                else if (Branch == null || Branch.TaxsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ الفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.EnsureTheVAT, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                }

                foreach (var item in voucher.VoucherDetails.ToList())
                {
                     var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                    int? AccountEradat = 0;
                    if (ServicesUpdated != null)
                    {
                        AccountEradat = ServicesUpdated.AccountId ?? 0;
                        if (AccountEradat == 0)
                        {
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                        }
                    }
                    else
                    {
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                    }
                }


                int? CostId;

                int payId = Convert.ToInt32(voucher.Paid);
                var CustomerPaymentsUpdated = _TaamerProContext.CustomerPayments.Where(s=>s.PaymentId== payId).FirstOrDefault();

                voucher.TransactionDetails = new List<Transactions>();

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ الفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                int? ToAccID = _CustomerRepository.GetCustomersByCustId(voucher.CustomerId).Result.Select(s => s.AccountId).FirstOrDefault();
                decimal Disc = 0;
                //if (voucher.DiscountValue != null)
                //{
                //    if (Convert.ToInt32(voucher.DiscountValue) != 0)
                //    {

                //        Disc = voucher.DiscountValue ?? 0;
                //    }
                //    else
                //    {
                //        Disc = 0;

                //    }
                //}
                //else
                //{
                //    Disc = 0;
                //}
                if (voucher.InvoiceId == 0)
                {
                    //   voucher.TotalValue = voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0);
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                    voucher.YearId = yearid;
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.AddUser = UserId;
                    voucher.BranchId = BranchId;
                    voucher.AddDate = DateTime.Now;
                    voucher.ProjectId = voucher.ProjectId;
                    voucher.DunCalc = false;


                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }
                    if (voucher.ProjectId != null)
                    {
                        var project = _TaamerProContext.Project.Where(s=>s.ProjectId== voucher.ProjectId).FirstOrDefault();
                        if (project != null)
                        {
                            project.MotionProject = 1;
                            project.MotionProjectDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            project.MotionProjectNote = "أضافة دفعة علي مشروع";
                        }
                        if (voucher.CostCenterId > 0)
                        {
                            CostId = voucher.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }
                        }
                        

                    }
                    else
                    {

                        if (voucher.CostCenterId > 0)
                        {
                            CostId = voucher.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }
                    }
                    if (CostId == 0)
                    {
                        try
                        { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                        catch
                        { CostId = null; }
                    }
                    voucher.CostCenterId = CostId;
                    //Utilities utilVoucher = new Utilities((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());
                    //voucher.InvoiceValueText = utilVoucher.GetNumberAr();
                    voucher.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                    //var AccOVAT = _TaamerProContext.Accounts.Where(w => w.Classification == 18).Select(s => s.AccountId).FirstOrDefault();
                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();

                    int? itemToAccountId = 0;
                    int? itemAccountId = 0;
                    var vouchertypename = "";
                    var vouchertypename2 = "";
                    int? itemInvoiceId = 0;
                    decimal? AcPaid = 0;
                    decimal? AcPaid2 = 0;
                    decimal? AcPaid3 = 0;

                    decimal? SVAT = 0;
                    int? itemTaxType = 0;

                    foreach (var item in voucher.VoucherDetails.ToList())
                    {

                        //ToAccount = item.AccountId ?? 0;
                        ToAccount = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;

                        // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                        //if (ServicesUpdated != null)
                        //{
                        //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                        //}

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AddUser = UserId;
                        item.PayType = voucher.PayType;

                        item.AddDate = DateTime.Now;

                        decimal? TotalWithoutVAT = (item.Amount);
                        decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                        SVAT = (voucher.TaxAmount);
                        //SVAT = SVAT + (item.TaxAmount * (item.Qty ?? 1));
                        item.CostCenterId = CostId;


                        decimal? Depit = item.Amount + item.TaxAmount;
                        decimal? Depit2 = item.Amount + item.TaxAmount;


                        decimal? VAT = item.TaxAmount;
                        item.TotalAmount = item.TotalAmount;
                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        _TaamerProContext.VoucherDetails.Add(item);
                        vouchertypename = voucher.Type == 2 ? " فاتورة" + " مبيعات" : "فاتورة";
                        vouchertypename2 = " خصم فاتورة ";

                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 2)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ الفاتورة";
                            _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }

                        itemToAccountId = item.ToAccountId;
                        itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                        itemInvoiceId = item.InvoiceId;
                        itemTaxType = item.TaxType;
                    }
                    CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                    BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                    if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                    {


                        if (itemTaxType == 2)
                        {
                            AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                            AcPaid3 = 0;

                        }
                        else
                        {
                            AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                            AcPaid3 = 0;
                        }
                    }
                    else
                    {


                        if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                        {
                            AcPaid = Convert.ToDecimal(voucher.PaidValue);
                            AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;
                            //Edit 21-01-2024 Ostaze Mohammed Req
                            //AcPaid3 = 0;
                            AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                            var CustomerData = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                            CustomerAccountPaid = CustomerData.AccountId ?? 0;
                            BoxBankAccountPaid = itemToAccountId ?? 0;

                        }
                        else
                        {
                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);

                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);

                            }

                        }
                    }

                    if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                    {
                        var CustomerDataV = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                        CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                    }
                    _TaamerProContext.SaveChanges();
                    voucher.TransactionDetails.Add(new Transactions
                    {

                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        AccountId = CustomerAccountPaid,
                        CostCenterId = CostId,
                        AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 1,
                        Depit = AcPaid,
                        Credit = 0,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });
                    if (Convert.ToInt32(voucher.DiscountValue) > 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = Branch.SaleDiscountAccId,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = voucher.DiscountValue,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });

                    }

                    //credit 

                    foreach (var item in voucher.VoucherDetails.ToList())
                    {
                         var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                        int? AccountEradat = 0;
                        if (ServicesUpdated != null)
                        {
                            AccountEradat = ServicesUpdated.AccountId ?? 0;
                            if (AccountEradat == 0)
                            {
                                //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                            }
                        }
                        else
                        {
                            //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                        }
                        item.AccountId = AccountEradat;
                        item.Qty = item.Qty ?? 1;
                        item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                        if (itemTaxType == 2)
                        {
                            item.Amount = (item.Amount * item.Qty) - (item.DiscountValue_Det);
                        }
                        else
                        {
                            item.Amount = item.TotalAmount - (item.TaxAmount);
                        }
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = AccountEradat,

                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== AccountEradat)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = item.Amount,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            VoucherDetailsId = item.VoucherDetailsId,

                        });


                    }


                   
                    
                    voucher.TransactionDetails.Add(new Transactions
                    {

                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        //AccountId = AccOVAT,// الضريبة
                        AccountId = Branch.TaxsAccId,// الضريبة

                        CostCenterId = CostId,//item.CostCenterId,
                        AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 2,
                        Credit = SVAT,
                        Depit = 0,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });

                    if (AcPaid3 != 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = BoxBankAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 4,
                            Depit = AcPaid3,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 5,
                            Depit = 0,
                            Credit = AcPaid3,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });


                    }



                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);


                    if (voucher.ServicesPriceOffer != null && voucher.ServicesPriceOffer.Count > 0)
                    {
                        foreach (var item in voucher.ServicesPriceOffer)
                        {
                            item.AddUser = UserId;
                            item.AddDate = DateTime.Now;
                            item.InvoiceId = voucher.InvoiceId;
                            _TaamerProContext.Acc_Services_PriceOffer.Add(item);
                        }
                    }

                    _TaamerProContext.SaveChanges();


                    if (CustomerPaymentsUpdated != null)
                    {
                        CustomerPaymentsUpdated.ToAccountId = ToAccID;
                        CustomerPaymentsUpdated.IsPaid = true;
                        CustomerPaymentsUpdated.InvoiceId = voucher.InvoiceId;
                        CustomerPaymentsUpdated.UpdateUser = UserId;
                        CustomerPaymentsUpdated.UpdateDate = DateTime.Now;
                    }

                    _TaamerProContext.SaveChanges();
                    voucher.QRCodeNum = "200010001000" + voucher.InvoiceId.ToString();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 2 && s.YearId == yearid && s.InvoiceNumber == voucher.InvoiceNumber && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        if (CustomerPaymentsUpdated != null) CustomerPaymentsUpdated.InvoiceId = invid2;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucherDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                        if (TransactionDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    }
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة فاتورة جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    List<int> voDetIds = new List<int>();
                    foreach (var itemV in voucher.VoucherDetails)
                    {
                        voDetIds.Add(itemV.VoucherDetailsId);
                    }
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId, InvoiceIsDeleted = voucher.IsDeleted, voucherDetObj = voDetIds };
                }
                else
                {
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnterNewInvoice };

                }
            }
            catch (Exception ex)
            { 
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ الفاتورة";
                _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage SaveandPostInvoiceForServices2(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {

                var ToAccount = 0;
                var CustomerAccountPaid = 0;
                var BoxBankAccountPaid = 0;
                var Branch = _TaamerProContext.Branch.Where(s=>s.BranchId==BranchId).FirstOrDefault();
                if (Branch == null || Branch.ContractsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ الفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.EnsureRevenueAccountedBranchAccounts, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureRevenueAccountedBranchAccounts };

                }
                else if (Branch == null || Branch.BoxAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ الفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.EnsureFundAccountAccounted, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureFundAccountAccounted };

                }
                else if (Branch == null || Branch.SaleDiscountAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ الفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.EnsureThatTheSales, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureThatTheSales };

                }
                else if (Branch == null || Branch.TaxsAccId == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ الفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.EnsureTheVAT, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnsureTheVAT };
                }


                foreach (var item in voucher.VoucherDetails.ToList())
                {
                    var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();

                    int? AccountEradat = 0;
                    if (ServicesUpdated != null)
                    {
                        AccountEradat = ServicesUpdated.AccountId ?? 0;
                        if (AccountEradat == 0)
                        {
                            //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                        }
                    }
                    else
                    {
                        //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                    }
                }


                int? CostId;

                int payId = Convert.ToInt32(voucher.Paid);
                var CustomerPaymentsUpdated = _TaamerProContext.CustomerPayments.Where(s=>s.PaymentId== payId).FirstOrDefault();

                voucher.TransactionDetails = new List<Transactions>();

                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ الفاتورة";
                    _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                int? ToAccID = _CustomerRepository.GetCustomersByCustId(voucher.CustomerId).Result.Select(s => s.AccountId).FirstOrDefault();
                decimal Disc = 0;
                //if (voucher.DiscountValue != null)
                //{
                //    if (Convert.ToInt32(voucher.DiscountValue) != 0)
                //    {

                //        Disc = voucher.DiscountValue ?? 0;
                //    }
                //    else
                //    {
                //        Disc = 0;

                //    }
                //}
                //else
                //{
                //    Disc = 0;
                //}
                if (voucher.InvoiceId == 0)
                {
                    //   voucher.TotalValue = voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0);
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = voucher.VoucherDetails.Where(s => s.TaxType == 2).FirstOrDefault() != null ? true : false;
                    voucher.YearId = yearid;
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.AddUser = UserId;
                    voucher.BranchId = BranchId;
                    voucher.AddDate = DateTime.Now;
                    voucher.ProjectId = voucher.ProjectId;
                    voucher.DunCalc = false;


                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }
                    if (voucher.ProjectId != null)
                    {
                        var project = _TaamerProContext.Project.Where(s=>s.ProjectId== voucher.ProjectId).FirstOrDefault();

                        if (project != null)
                        {
                            project.MotionProject = 1;
                            project.MotionProjectDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            project.MotionProjectNote = "أضافة دفعة علي مشروع";
                        }
                        if (voucher.CostCenterId > 0)
                        {
                            CostId = voucher.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.ProjId == voucher.ProjectId && w.IsDeleted == false).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            {
                                try
                                { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                                catch
                                { CostId = null; }
                            }

                        }

                    }
                    else
                    {
                        if (voucher.CostCenterId > 0)
                        {
                            CostId = voucher.CostCenterId;
                        }
                        else
                        {
                            try
                            { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                            catch
                            { CostId = null; }
                        }
                    }
                    if (CostId == 0)
                    {
                        try
                        { CostId = _TaamerProContext.CostCenters.Where(w => w.BranchId == BranchId && w.IsDeleted == false && w.ProjId == 0).Select(s => s.CostCenterId).FirstOrDefault(); }
                        catch
                        { CostId = null; }
                    }
                    voucher.CostCenterId = CostId;
                    //Utilities utilVoucher = new Utilities((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());
                    //voucher.InvoiceValueText = utilVoucher.GetNumberAr();
                    voucher.InvoiceValueText = ConvertToWord_NEW((voucher.InvoiceValue + voucher.TaxAmount - (voucher.DiscountValue ?? 0)).ToString());

                    //var AccOVAT = _TaamerProContext.Accounts.Where(w => w.Classification == 18).Select(s => s.AccountId).FirstOrDefault();
                    var AccOVAT = _TaamerProContext.Accounts.Where(w => w.AccountId == Branch.TaxsAccId).Select(s => s.AccountId).FirstOrDefault();

                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();

                    int? itemToAccountId = 0;
                    int? itemAccountId = 0;
                    var vouchertypename = "";
                    var vouchertypename2 = "";
                    int? itemInvoiceId = 0;
                    decimal? AcPaid = 0;
                    decimal? AcPaid2 = 0;
                    decimal? AcPaid3 = 0;

                    decimal? SVAT = 0;
                    int? itemTaxType = 0;
                    //int? AccountEradat = 0;

                    foreach (var item in voucher.VoucherDetails.ToList())
                    {

                        //ToAccount = item.AccountId ?? 0;
                        ToAccount = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                        // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                        //if (ServicesUpdated != null)
                        //{
                        //    AccountEradat = ServicesUpdated.AccountId ?? Branch.ContractsAccId;
                        //}

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AddUser = UserId;
                        item.PayType = voucher.PayType;

                        item.AddDate = DateTime.Now;

                        decimal? TotalWithoutVAT = (item.Amount);
                        decimal? TotalWithVAT = (item.Amount + item.TaxAmount);
                        SVAT = (voucher.TaxAmount);
                        //SVAT = SVAT + (item.TaxAmount * (item.Qty??1));
                        item.CostCenterId = CostId;


                        decimal? Depit = item.Amount + item.TaxAmount;
                        decimal? Depit2 = item.Amount + item.TaxAmount;


                        decimal? VAT = item.TaxAmount;
                        item.TotalAmount = item.TotalAmount;
                        //Utilities utilDetails = new Utilities(Depit.ToString());
                        //item.TFK = utilDetails.GetNumberAr();
                        item.TFK = ConvertToWord_NEW(Depit.ToString());

                        _TaamerProContext.VoucherDetails.Add(item);
                        vouchertypename = voucher.Type == 2 ? " فاتورة" + " مبيعات" : "فاتورة";
                        vouchertypename2 = " خصم فاتورة ";

                        if (String.IsNullOrWhiteSpace(Convert.ToString(item.ToAccountId)) && voucher.Type == 2)
                        {
                            //-----------------------------------------------------------------------------------------------------------------
                            string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            string ActionNote2 = "فشل في حفظ الفاتورة";
                            _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.choosecustomeraccount, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                            //-----------------------------------------------------------------------------------------------------------------
                            return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosecustomeraccount };
                        }

                        itemToAccountId = item.ToAccountId;
                        itemAccountId = voucher.VoucherDetails.FirstOrDefault().AccountId ?? 0;
                        itemInvoiceId = item.InvoiceId;
                        itemTaxType = item.TaxType;
                    }
                    CustomerAccountPaid = itemToAccountId ?? 0; //will be override if paid=invoicevalue
                    BoxBankAccountPaid = ToAccount; //will be override if paid=invoicevalue
                    if (voucher.PaidValue == 0 || voucher.PaidValue == null)
                    {


                        if (itemTaxType == 2)
                        {
                            AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                            AcPaid3 = 0;

                        }
                        else
                        {
                            AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                            AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                            AcPaid3 = 0;
                        }


                    }
                    else
                    {


                        if (Convert.ToInt32(voucher.PaidValue) == Convert.ToInt32(voucher.TotalValue))
                        {

                            AcPaid = Convert.ToDecimal(voucher.PaidValue);
                            AcPaid2 = Convert.ToDecimal(voucher.PaidValue) + Disc - SVAT;
                            //Edit 21-01-2024 Ostaze Mohammed Req
                            //AcPaid3 = 0;
                            AcPaid3 = Convert.ToDecimal(voucher.PaidValue);
                            var CustomerData = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                            CustomerAccountPaid = CustomerData.AccountId ?? 0;
                            BoxBankAccountPaid = itemToAccountId ?? 0;

                        }
                        else
                        {
                            if (itemTaxType == 2)
                            {
                                AcPaid = Convert.ToDecimal(voucher.InvoiceValue) + SVAT - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.InvoiceValue);
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);

                            }
                            else
                            {
                                AcPaid = Convert.ToDecimal(voucher.TotalValue) - Disc;
                                AcPaid2 = Convert.ToDecimal(voucher.TotalValue) - SVAT;
                                AcPaid3 = Convert.ToDecimal(voucher.PaidValue);

                            }
                        }
                    }

                    if (Convert.ToInt32(voucher.PaidValue ?? 0) < Convert.ToInt32(voucher.TotalValue ?? 0))
                    {
                        var CustomerDataV = _TaamerProContext.Customer.Where(s => s.CustomerId == voucher.CustomerId).FirstOrDefault();
                        CustomerAccountPaid = CustomerDataV.AccountId ?? 0;
                    }

                    _TaamerProContext.SaveChanges();

                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        AccountId = CustomerAccountPaid,
                        CostCenterId = CostId,
                        AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 1,
                        Depit = AcPaid,
                        Credit = 0,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });
                    if (Convert.ToInt32(voucher.DiscountValue) > 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {

                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = Branch.SaleDiscountAccId,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.SaleDiscountAccId)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 1,
                            Depit = voucher.DiscountValue,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename2 + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            //InvoiceReference = voucher.InvoiceNumber.ToString(),
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });

                    }

                    //credit 

                    foreach (var item in voucher.VoucherDetails.ToList())
                    {
                        // var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();
                        var ServicesUpdated = _TaamerProContext.Acc_Services_Price.Where(s=>s.ServicesId== item.ServicesPriceId).FirstOrDefault();

                        int? AccountEradat = 0;
                        if (ServicesUpdated != null)
                        {
                            AccountEradat = ServicesUpdated.AccountId ?? 0;
                            if (AccountEradat == 0)
                            {
                                //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.serviceAccountBeforeSaving };
                            }
                        }
                        else
                        {
                            //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.check_the_item_and_account };
                        }
                        item.AccountId = AccountEradat;
                        item.Qty = item.Qty ?? 1;
                        item.DiscountValue_Det = item.DiscountValue_Det ?? 0;

                        if (itemTaxType == 2)
                        {
                            item.Amount = (item.Amount * item.Qty) - (item.DiscountValue_Det);
                        }
                        else
                        {
                            item.Amount = item.TotalAmount - (item.TaxAmount);
                        }
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = AccountEradat,

                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== AccountEradat)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 2,
                            Credit = item.Amount,
                            Depit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",

                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                            VoucherDetailsId = item.VoucherDetailsId,

                        });


                    }


                    
                    
                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        //AccountId = AccOVAT,// الضريبة
                        AccountId = Branch.TaxsAccId,// الضريبة

                        CostCenterId = CostId,//item.CostCenterId,
                        AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== Branch.TaxsAccId)?.FirstOrDefault()?.Type,
                        Type = voucher.Type,
                        LineNumber = 2,
                        Credit = SVAT,
                        Depit = 0,
                        YearId = yearid,
                        Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        InvoiceId = voucher.InvoiceId,
                        IsPost = false,
                        BranchId = BranchId,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });

                    if (AcPaid3 != 0)
                    {
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = BoxBankAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== BoxBankAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 4,
                            Depit = AcPaid3,
                            Credit = 0,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });
                        voucher.TransactionDetails.Add(new Transactions
                        {
                            AccTransactionDate = voucher.Date,
                            AccTransactionHijriDate = voucher.HijriDate,
                            TransactionDate = voucher.Date,
                            TransactionHijriDate = voucher.HijriDate,
                            AccountId = CustomerAccountPaid,
                            CostCenterId = CostId,
                            AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== CustomerAccountPaid)?.FirstOrDefault()?.Type,
                            Type = voucher.Type,
                            LineNumber = 5,
                            Depit = 0,
                            Credit = AcPaid3,
                            YearId = yearid,
                            Notes = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            Details = "" + vouchertypename + " " + "رقم" + " " + voucher.InvoiceNumber + "",
                            InvoiceReference = "",
                            InvoiceId = voucher.InvoiceId,
                            IsPost = false,
                            BranchId = BranchId,
                            AddDate = DateTime.Now,
                            AddUser = UserId,
                            IsDeleted = false,
                        });


                    }


                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                    if (voucher.ServicesPriceOffer != null && voucher.ServicesPriceOffer.Count > 0)
                    {
                        foreach (var item in voucher.ServicesPriceOffer)
                        {
                            item.AddUser = UserId;
                            item.AddDate = DateTime.Now;
                            item.InvoiceId = voucher.InvoiceId;
                            _TaamerProContext.Acc_Services_PriceOffer.Add(item);
                        }
                    }


                    _TaamerProContext.SaveChanges();


                    if (CustomerPaymentsUpdated != null)
                    {
                        CustomerPaymentsUpdated.ToAccountId = ToAccID;
                        CustomerPaymentsUpdated.IsPaid = true;
                        CustomerPaymentsUpdated.InvoiceId = voucher.InvoiceId;
                        CustomerPaymentsUpdated.UpdateUser = UserId;
                        CustomerPaymentsUpdated.UpdateDate = DateTime.Now;
                    }

                    _TaamerProContext.SaveChanges();
                    voucher.QRCodeNum = "200010001000" + voucher.InvoiceId.ToString();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 2 && s.YearId == yearid && s.InvoiceNumber == voucher.InvoiceNumber && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        if (CustomerPaymentsUpdated != null) CustomerPaymentsUpdated.InvoiceId = invid2;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucherDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                        if (TransactionDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    }


                    if (voucher.IsPost != true)
                    {
                        voucher.IsPost = true;
                        //voucher.PostDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        //voucher.PostHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));
                        voucher.PostDate = voucher.Date;
                        voucher.PostHijriDate = voucher.HijriDate;

                        var newJournal = new Journals();
                        var JNo = _JournalsRepository.GenerateNextJournalNumber(yearid ?? default(int), BranchId).Result;
                        if (voucher.Type == 10)
                        {
                            JNo = 1;
                        }
                        else
                        {
                            JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                        }


                        newJournal.JournalNo = JNo;
                        newJournal.YearMalia = yearid ?? default(int);
                        newJournal.VoucherId = voucher.InvoiceId;
                        newJournal.VoucherType = voucher.Type;
                        newJournal.BranchId = voucher.BranchId ?? 0;
                        newJournal.AddDate = DateTime.Now;
                        newJournal.AddUser = newJournal.UserId = UserId;
                        _TaamerProContext.Journals.Add(newJournal);
                        foreach (var trans in voucher.TransactionDetails.ToList())
                        {
                            trans.IsPost = true;
                            trans.JournalNo = newJournal.JournalNo;
                        }
                        voucher.JournalNumber = newJournal.JournalNo;
                        //_TaamerProContext.SaveChanges();
                    }
                    else
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        //string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        //string ActionNote3 = "فشل في حفظ وترحيل الفاتورة";
                        //_SystemAction.SaveAction("SaveandPostInvoiceForServices", "VoucherService", 1, "مرحلة مسبقا", "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "مرحلة مسبقا" };
                    }


                    _TaamerProContext.SaveChanges();




                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة فاتورة جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    List<int> voDetIds = new List<int>();
                    foreach (var itemV in voucher.VoucherDetails)
                    {
                        voDetIds.Add(itemV.VoucherDetailsId);
                    }
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId, InvoiceIsDeleted = voucher.IsDeleted, voucherDetObj = voDetIds };
                }
               else
                {
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.EnterNewInvoice };

                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ الفاتورة";
                _SystemAction.SaveAction("SaveInvoiceForServices2", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveDailyVoucher(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                if (yearid == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ القيد";
                    _SystemAction.SaveAction("SaveDailyVoucher", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                voucher.InvoiceValueText = ConvertToWord_NEW(voucher.InvoiceValue.ToString());

                if (voucher.InvoiceId == 0)
                {
                    voucher.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = false;
                    voucher.YearId = yearid;
                    voucher.BranchId = BranchId;
                    voucher.AddUser = UserId;
                    voucher.AddDate = DateTime.Now;
                    voucher.printBankAccount = false;
                    voucher.DunCalc = voucher.DunCalc;
                    voucher.VoucherAdjustment = voucher.VoucherAdjustment;

                    voucher.JournalNumber = null;


                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }


                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.TransactionDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AccTransactionDate = voucher.Date;
                        item.AccTransactionHijriDate = voucher.HijriDate;
                        item.TransactionDate = voucher.Date;
                        item.TransactionHijriDate = voucher.HijriDate;
                        item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                        item.YearId = yearid;
                        item.IsPost = false;
                        item.CostCenterId = Convert.ToInt32(voucher.CostCenterId) > 0 ? voucher.CostCenterId : item.CostCenterId;

                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }

                        item.AddUser = UserId;
                        //item.BranchId = BranchId;
                        item.BranchId = BranchCost;

                        item.Type = voucher.Type;
                        item.AddDate = DateTime.Now;
                        _TaamerProContext.Transactions.Add(item);
                    }


                    _TaamerProContext.SaveChanges();


                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 8 && s.YearId == yearid && s.InvoiceNumber == voucher.InvoiceNumber && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransaDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucDetails);
                        if (TransaDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransaDetails);
                    }
                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة قيد يومية جديد" +" برقم "+ voucher.InvoiceNumber;
                    _SystemAction.SaveAction("SaveDailyVoucher", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId };
                }
                else
                {
                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                    if (!String.IsNullOrEmpty(Convert.ToString(VoucherUpdated.JournalNumber)) && VoucherUpdated.JournalNumber > 0)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote2 = "فشل في حفظ قيد يومية ";
                        _SystemAction.SaveAction("SaveDailyVoucher", "VoucherService", 1, Resources.canteditevoucher, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.canteditevoucher };
                    }
                    if (VoucherUpdated != null)
                    {
                        VoucherUpdated.Notes = voucher.Notes;
                        VoucherUpdated.InvoiceNotes = voucher.InvoiceNotes;
                        VoucherUpdated.CostCenterId = voucher.CostCenterId;
                        VoucherUpdated.InvoiceReference = voucher.InvoiceReference;
                        VoucherUpdated.Date = voucher.Date;
                        VoucherUpdated.HijriDate = voucher.HijriDate;
                        VoucherUpdated.IsPost = voucher.IsPost;
                        VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        VoucherUpdated.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                        VoucherUpdated.BranchId = BranchId;
                        VoucherUpdated.UpdateDate = DateTime.Now;
                        VoucherUpdated.UpdateUser = UserId;
                        VoucherUpdated.DunCalc = voucher.DunCalc??false;
                        VoucherUpdated.VoucherAdjustment = voucher.VoucherAdjustment ?? false;

                    }
                    //delete existing details 
                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                    _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    // add new details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.TransactionDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تكرار الحساب مع مركز التكلفة" };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AccTransactionDate = voucher.Date;
                        item.AccTransactionHijriDate = voucher.HijriDate;
                        item.TransactionDate = voucher.Date;
                        item.TransactionHijriDate = voucher.HijriDate;
                        item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                        item.InvoiceId = voucher.InvoiceId;
                        item.Type = voucher.Type;
                        item.IsPost = false;
                        item.YearId = yearid;
                        item.CostCenterId = Convert.ToInt32(voucher.CostCenterId) > 0 ? voucher.CostCenterId : item.CostCenterId;

                        //item.InvoiceReference = voucher.InvoiceNumber.ToString();
                        //item.InvoiceReference = "";
                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted==false && s.ProjId==0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }
                        item.AddUser = UserId;
                        //item.BranchId = BranchId;
                        item.BranchId = BranchCost;

                        item.AddDate = DateTime.Now;
                        _TaamerProContext.Transactions.Add(item);
                    }

                    _TaamerProContext.SaveChanges();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 8 && s.YearId == yearid && s.InvoiceNumber == (VoucherUpdated!.InvoiceNumber??"0") && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransaDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucDetails);
                        if (TransaDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransaDetails);
                    }
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل قيد يومية رقم " + voucher.InvoiceId;
                    _SystemAction.SaveAction("SaveDailyVoucher", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId };
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ قيد يومية";
                _SystemAction.SaveAction("SaveDailyVoucher", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage SaveandPostDailyVoucher(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                if (yearid == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ القيد";
                    _SystemAction.SaveAction("SaveandPostDailyVoucher", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }

                voucher.InvoiceValueText = ConvertToWord_NEW(voucher.InvoiceValue.ToString());

                if (voucher.InvoiceId == 0)
                {
                    voucher.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = false;
                    voucher.YearId = yearid;
                    voucher.BranchId = BranchId;
                    voucher.AddUser = UserId;
                    voucher.AddDate = DateTime.Now;
                    voucher.printBankAccount = false;
                    voucher.DunCalc = voucher.DunCalc??false;
                    voucher.VoucherAdjustment = voucher.VoucherAdjustment ?? false;

                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }

                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.TransactionDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                         item.AccTransactionDate = voucher.Date;
                        item.AccTransactionHijriDate = voucher.HijriDate;
                        item.TransactionDate = voucher.Date;
                        item.TransactionHijriDate = voucher.HijriDate;
                        item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                        item.YearId = yearid;
                        item.IsPost = false;
                        item.CostCenterId = Convert.ToInt32(voucher.CostCenterId)>0 ? voucher.CostCenterId : item.CostCenterId;
                        //item.InvoiceReference = voucher.InvoiceNumber.ToString();
                        //item.InvoiceReference = "";
                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }
                        item.AddUser = UserId;
                        //item.BranchId = BranchId;
                        item.BranchId = BranchCost;
                        item.Type = voucher.Type;
                        item.AddDate = DateTime.Now;
                        _TaamerProContext.Transactions.Add(item);
                    }


                    _TaamerProContext.SaveChanges();

                    if (voucher.IsPost != true)
                    {
                        voucher.IsPost = true;
                        voucher.PostDate = voucher.Date;
                        voucher.PostHijriDate = voucher.HijriDate;
                        //voucher.PostDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        //voucher.PostHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));

                        var newJournal = new Journals();
                        var JNo = _JournalsRepository.GenerateNextJournalNumber(yearid ?? default(int), BranchId).Result;
                        if (voucher.Type == 10)
                        {
                            JNo = 1;
                        }
                        else
                        {
                            JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                        }


                        newJournal.JournalNo = JNo;
                        newJournal.YearMalia = yearid ?? default(int);
                        newJournal.VoucherId = voucher.InvoiceId;
                        newJournal.VoucherType = voucher.Type;
                        newJournal.BranchId = voucher.BranchId ?? 0;
                        newJournal.AddDate = DateTime.Now;
                        newJournal.AddUser = newJournal.UserId = UserId;
                        _TaamerProContext.Journals.Add(newJournal);
                        foreach (var trans in voucher.TransactionDetails.ToList())
                        {
                            trans.IsPost = true;
                            trans.JournalNo = newJournal.JournalNo;
                        }
                        voucher.JournalNumber = newJournal.JournalNo;
                        //_TaamerProContext.SaveChanges();
                    }
                    else
                    {
                        ////-----------------------------------------------------------------------------------------------------------------
                        //string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        //string ActionNote3 = "فشل في حفظ القيد";
                        //_SystemAction.SaveAction("SaveandPostDailyVoucher", "VoucherService", 1, "مرحلة مسبقا", "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        ////-----------------------------------------------------------------------------------------------------------------

                        //return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "مرحلة مسبقا" };
                    }

                    _TaamerProContext.SaveChanges();
                    var ExistInvoice = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 8 && s.YearId == yearid && s.InvoiceNumber == voucher.InvoiceNumber && s.TotalValue == voucher.TotalValue && s.BranchId == BranchId).ToList();
                    if (ExistInvoice.Count() > 1)
                    {
                        var invid = ExistInvoice.FirstOrDefault().InvoiceId;
                        var invid2 = ExistInvoice.LastOrDefault().InvoiceId;
                        ExistInvoice.FirstOrDefault().IsDeleted = true;
                        ExistInvoice.FirstOrDefault().DeleteUser = 100000;
                        var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == invid).ToList();
                        var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == invid).ToList();
                        if (VoucherDetails.Count() > 0) _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                        if (TransactionDetails.Count() > 0) _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    }
                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة قيد يومية جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveandPostDailyVoucher", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId };
                }
                else
                {
                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                    if (!String.IsNullOrEmpty(Convert.ToString(VoucherUpdated.JournalNumber)) && VoucherUpdated.JournalNumber > 0)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote2 = "فشل في حفظ قيد يومية ";
                        _SystemAction.SaveAction("SaveandPostDailyVoucher", "VoucherService", 1, Resources.canteditevoucher, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.canteditevoucher };
                    }
                    if (VoucherUpdated != null)
                    {
                        VoucherUpdated.Notes = voucher.Notes;
                        VoucherUpdated.InvoiceNotes = voucher.InvoiceNotes;
                        VoucherUpdated.CostCenterId = voucher.CostCenterId;
                        VoucherUpdated.InvoiceReference = voucher.InvoiceReference;
                        VoucherUpdated.Date = voucher.Date;
                        VoucherUpdated.HijriDate = voucher.HijriDate;
                        VoucherUpdated.IsPost = voucher.IsPost;
                        VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        VoucherUpdated.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                        VoucherUpdated.BranchId = BranchId;
                        VoucherUpdated.UpdateDate = DateTime.Now;
                        VoucherUpdated.UpdateUser = UserId;
                    }
                    //delete existing details 
                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                    _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    // add new details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.TransactionDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تكرار الحساب مع مركز التكلفة" };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                         item.AccTransactionDate = voucher.Date;
                        item.AccTransactionHijriDate = voucher.HijriDate;
                        item.TransactionDate = voucher.Date;
                        item.TransactionHijriDate = voucher.HijriDate;
                        item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                        item.InvoiceId = voucher.InvoiceId;
                        item.Type = voucher.Type;
                        item.IsPost = false;
                        item.YearId = yearid;
                        //item.InvoiceReference = voucher.InvoiceNumber.ToString();
                        //item.InvoiceReference = "";
                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }
                        item.AddUser = UserId;
                        //item.BranchId = BranchId;
                        item.BranchId = BranchCost;

                        item.AddDate = DateTime.Now;
                        _TaamerProContext.Transactions.Add(item);
                    }

                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل قيد يومية رقم " + voucher.InvoiceId;
                    _SystemAction.SaveAction("SaveandPostDailyVoucher", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully, ReturnedParm = voucher.InvoiceId };
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ قيد يومية";
                _SystemAction.SaveAction("SaveandPostDailyVoucher", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveClosingVoucher(Invoices voucher, int UserId, int BranchId, int? VoucherNum, int? yearid, string Con)
        {
            try
            {
                //var year = _fiscalyearsRepository.GetCurrentYear();
                if (yearid == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ القيد";
                    _SystemAction.SaveAction("SaveClosingVoucher", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                voucher.InvoiceValueText = ConvertToWord_NEW(voucher.InvoiceValue.ToString());

                if (voucher.InvoiceId == 0)
                {

                    voucher.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = false;
                    voucher.YearId = yearid;
                    voucher.BranchId = BranchId;
                    voucher.AddUser = UserId;
                    voucher.AddDate = DateTime.Now;
                    voucher.printBankAccount = false;
                    voucher.DunCalc = false;

                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.TransactionDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AccTransactionDate = voucher.Date;
                        item.AccTransactionHijriDate = voucher.HijriDate;
                        item.TransactionDate = voucher.Date;
                        item.TransactionHijriDate = voucher.HijriDate;
                        item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                        item.YearId = yearid;
                        item.IsPost = false;
                        //item.InvoiceReference = voucher.InvoiceNumber.ToString();
                        //item.InvoiceReference = "";

                        var BranchCost = 0;
                        var CostCenterUpdated = _CostCenterRepository.GetMatching(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }


                        item.AddUser = UserId;
                        item.BranchId = BranchCost;
                        item.Type = voucher.Type;
                        item.AddDate = DateTime.Now;
                        _TaamerProContext.Transactions.Add(item);
                    }


                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة قيد يومية جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveClosingVoucher", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };


                }
                else
                {
                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                    if (!String.IsNullOrEmpty(Convert.ToString(VoucherUpdated.JournalNumber)) && VoucherUpdated.JournalNumber > 0)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote2 = "فشل في حفظ قيد الاقفال ";
                        _SystemAction.SaveAction("SaveClosingVoucher", "VoucherService", 1, Resources.canteditevoucher, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.canteditevoucher };
                    }
                    if (VoucherUpdated != null)
                    {
                        VoucherUpdated.Notes = voucher.Notes;
                        VoucherUpdated.InvoiceNotes = voucher.InvoiceNotes;
                        VoucherUpdated.CostCenterId = voucher.CostCenterId;
                        VoucherUpdated.InvoiceReference = voucher.InvoiceReference;
                        VoucherUpdated.Date = voucher.Date;
                        VoucherUpdated.HijriDate = voucher.HijriDate;
                        VoucherUpdated.IsPost = voucher.IsPost;
                        VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        VoucherUpdated.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                        VoucherUpdated.BranchId = BranchId;
                        VoucherUpdated.UpdateDate = DateTime.Now;
                        VoucherUpdated.UpdateUser = UserId;
                    }
                    //delete existing details 
                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                    _TaamerProContext.Transactions.RemoveRange(TransactionDetails);                    // add new details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.TransactionDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تكرار الحساب مع مركز التكلفة" };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                         item.AccTransactionDate = voucher.Date;
                        item.AccTransactionHijriDate = voucher.HijriDate;
                        item.TransactionDate = voucher.Date;
                        item.TransactionHijriDate = voucher.HijriDate;
                        item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                        item.InvoiceId = voucher.InvoiceId;
                        item.Type = voucher.Type;
                        item.IsPost = false;
                        item.YearId = yearid;
                        //item.InvoiceReference = voucher.InvoiceNumber.ToString();
                        //item.InvoiceReference = "";

                        var BranchCost = 0;
                        var CostCenterUpdated = _CostCenterRepository.GetMatching(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }

                        item.AddUser = UserId;
                        item.BranchId = BranchCost;
                        item.AddDate = DateTime.Now;
                        _TaamerProContext.Transactions.Add(item);
                    }

                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل قيد اقفال رقم " + voucher.InvoiceId;
                    _SystemAction.SaveAction("SaveClosingVoucher", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ قيد الاقفال";
                _SystemAction.SaveAction("SaveClosingVoucher", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveEmpVoucher(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                //var year = _fiscalyearsRepository.GetCurrentYear();
                if (yearid == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في ترحيل القيود";
                    _SystemAction.SaveAction("SaveEmpVoucher", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                voucher.InvoiceValueText = ConvertToWord_NEW(voucher.InvoiceValue.ToString());

                if (voucher.InvoiceId == 0)
                {
                    voucher.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.IsTax = false;
                    voucher.YearId = yearid;
                    voucher.BranchId = BranchId;
                    voucher.AddUser = UserId;
                    voucher.AddDate = DateTime.Now;
                    voucher.printBankAccount = false;
                    voucher.DunCalc = false;


                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }

                    _TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.TransactionDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                         item.AccTransactionDate = voucher.Date;
                        item.AccTransactionHijriDate = voucher.HijriDate;
                        item.TransactionDate = voucher.Date;
                        item.TransactionHijriDate = voucher.HijriDate;
                        item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                        item.YearId = yearid;
                        item.IsPost = false;

                        item.CostCenterId = Convert.ToInt32(voucher.CostCenterId) > 0 ? voucher.CostCenterId : item.CostCenterId;

                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }

                        item.AddUser = UserId;
                        //item.BranchId = BranchId;
                        item.BranchId = BranchCost;
                        item.Type = voucher.Type;
                        item.AddDate = DateTime.Now;
                        _TaamerProContext.Transactions.Add(item);
                    }

                    _TaamerProContext.SaveChanges();


                    var postedVoucher = _TaamerProContext.Invoices.Where(s=>s.InvoiceId== voucher.InvoiceId).FirstOrDefault();
                    if(postedVoucher!=null)
                    {
                        if (postedVoucher.IsPost != true)
                        {

                            postedVoucher.IsPost = true;
                            postedVoucher.PostDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            postedVoucher.PostHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));

                            var newJournal = new Journals();
                            var JNo = _JournalsRepository.GenerateNextJournalNumber(Convert.ToInt32(yearid), BranchId).Result;
                            if (postedVoucher.Type == 10)
                            {
                                JNo = 1;
                            }
                            else
                            {
                                JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                            }


                            newJournal.JournalNo = JNo;
                            //newJournal.JournalNo = _JournalsRepository.GenerateNextJournalNumber();
                            newJournal.YearMalia = Convert.ToInt32(yearid);
                            newJournal.VoucherId = postedVoucher.InvoiceId;
                            newJournal.VoucherType = postedVoucher.Type;
                            newJournal.BranchId = postedVoucher.BranchId ?? 0;
                            newJournal.AddDate = DateTime.Now;
                            newJournal.AddUser = newJournal.UserId = UserId;
                            _TaamerProContext.Journals.Add(newJournal);
                            var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == postedVoucher.InvoiceId).ToList();

                            foreach (var trans in TransactionDetails)
                            {
                                trans.IsPost = true;
                                trans.JournalNo = newJournal.JournalNo;
                            }
                            postedVoucher.JournalNumber = newJournal.JournalNo;
                            _TaamerProContext.SaveChanges();
                        }

                    }


                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "تم ترحيل القيود";
                    _SystemAction.SaveAction("SaveEmpVoucher", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
                else
                {
                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                    if (!String.IsNullOrEmpty(Convert.ToString(VoucherUpdated.JournalNumber)) && VoucherUpdated.JournalNumber > 0)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote2 = "فشل في ترحيل القيود ";
                        _SystemAction.SaveAction("SaveEmpVoucher", "VoucherService", 1, Resources.canteditevoucher, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.canteditevoucher };
                    }
                    if (VoucherUpdated != null)
                    {
                        VoucherUpdated.Notes = voucher.Notes;
                        VoucherUpdated.CostCenterId = voucher.CostCenterId;
                        VoucherUpdated.InvoiceReference = voucher.InvoiceReference;
                        VoucherUpdated.Date = voucher.Date;
                        VoucherUpdated.HijriDate = voucher.HijriDate;
                        VoucherUpdated.IsPost = voucher.IsPost;
                        VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        VoucherUpdated.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                        VoucherUpdated.BranchId = BranchId;
                        VoucherUpdated.UpdateDate = DateTime.Now;
                        VoucherUpdated.UpdateUser = UserId;
                    }
                    //delete existing details 
                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                    _TaamerProContext.Transactions.RemoveRange(TransactionDetails);                    // add new details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.TransactionDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تكرار الحساب مع مركز التكلفة" };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                         item.AccTransactionDate = voucher.Date;
                        item.AccTransactionHijriDate = voucher.HijriDate;
                        item.TransactionDate = voucher.Date;
                        item.TransactionHijriDate = voucher.HijriDate;
                        item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                        item.InvoiceId = voucher.InvoiceId;
                        item.Type = voucher.Type;
                        item.IsPost = false;
                        item.YearId = yearid;
                        item.CostCenterId = Convert.ToInt32(voucher.CostCenterId) > 0 ? voucher.CostCenterId : item.CostCenterId;

                        var BranchCost = 0;
                        var CostCenterUpdated = _TaamerProContext.CostCenters.Where(s => s.CostCenterId == item.CostCenterId && s.IsDeleted == false && s.ProjId == 0).FirstOrDefault();
                        if (CostCenterUpdated != null)
                        {
                            var BranchUpdated = _TaamerProContext.Branch.Where(s => s.IsDeleted == false && s.BranchId == CostCenterUpdated.BranchId).FirstOrDefault();
                            if (BranchUpdated != null)
                            {
                                BranchCost = BranchUpdated.BranchId;
                            }
                            else
                            {
                                BranchCost = BranchId;
                            }
                        }
                        else
                        {
                            BranchCost = BranchId;
                        }

                        item.AddUser = UserId;
                        //item.BranchId = BranchId;
                        item.BranchId = BranchCost;
                        item.AddDate = DateTime.Now;
                        _TaamerProContext.Transactions.Add(item);
                    }

                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل قيد رقم " + voucher.InvoiceId;
                    _SystemAction.SaveAction("SaveEmpVoucher", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ القيد ";
                _SystemAction.SaveAction("SaveEmpVoucher", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveRecycleVoucher(int SelectedYear, int UserId, int BranchId, int? yearid,string Con)
        {
            try
            {
                int nextyear = (yearid ?? 0) + 1;

                var DateString = nextyear  + "-01" + "-01";
                var DateTimee= Convert.ToDateTime(DateString, CultureInfo.CreateSpecificCulture("en"));
                var DateM = DateTimee.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                var DateH = DateTimee.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));
                //var DateM = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                //var DateH = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));

                List<Transactions> lmd = new List<Transactions>();
                
                if (yearid == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote2 = "فشل في تدوير الأرصدة تأكد من السنة المالية";
                    _SystemAction.SaveAction("SaveRecycleVoucher", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }

                int RecycleVoucherNo = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 26 && s.YearId == nextyear && s.IsPost == true).Count();
                if (RecycleVoucherNo == 0)
                {
                    int ClosedVoucherNo = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.Type == 25 && s.YearId == yearid && s.IsPost == true).Count();

                    if (ClosedVoucherNo == 1)
                    {
                        using (SqlConnection con = new SqlConnection(Con))
                        {
                            using (SqlCommand cmd = new SqlCommand("AccountRecycle", con))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;

                                cmd.Parameters.Add(new SqlParameter("@From", DBNull.Value));
                                cmd.Parameters.Add(new SqlParameter("@to", DBNull.Value));
                                cmd.Parameters.Add(new SqlParameter("@CCID", 0));
                                cmd.Parameters.Add(new SqlParameter("@AccountCode", DBNull.Value));
                                cmd.Parameters.Add(new SqlParameter("@lvl", DBNull.Value));
                                cmd.Parameters.Add(new SqlParameter("@YearId", yearid));
                                cmd.Parameters.Add(new SqlParameter("@BranchId", BranchId));
                                cmd.Parameters.Add(new SqlParameter("@dir", "rtl"));

                                con.Open();
                                //cmd.ExecuteNonQuery();


                                SqlDataAdapter a = new SqlDataAdapter(cmd);
                                DataSet ds = new DataSet();
                                a.Fill(ds);


                                DataTable dt = new DataTable();
                                dt = ds.Tables[0];
                                foreach (DataRow dr in dt.Rows)

                                // loop for adding add from dataset to list<modeldata>  
                                {
                                    string EndTotalDebit, EndTotalCredit;
                                    double PeriodDebit, PeriodCredit, DebitOP, CreditOP;
                                    try
                                    {
                                        PeriodDebit = double.Parse(dr[6].ToString());
                                    }
                                    catch (Exception)
                                    {
                                        PeriodDebit = 0;
                                    }
                                    try
                                    {
                                        PeriodCredit = double.Parse(dr[5].ToString());
                                    }
                                    catch (Exception)
                                    {
                                        PeriodCredit = 0;
                                    }

                                    try
                                    {
                                        DebitOP = double.Parse(dr[3].ToString());
                                    }
                                    catch (Exception)
                                    {
                                        DebitOP = 0;
                                    }

                                    try
                                    {
                                        CreditOP = double.Parse(dr[4].ToString());
                                    }
                                    catch (Exception)
                                    {
                                        CreditOP = 0;
                                    }

                                    var checkValueDEPIT = DebitOP + PeriodDebit;
                                    var checkValueCREDIT = CreditOP + PeriodCredit;

                                    if (checkValueDEPIT > checkValueCREDIT)
                                    {

                                        EndTotalDebit = (checkValueDEPIT - checkValueCREDIT).ToString();
                                        EndTotalCredit = "0";
                                    }
                                    else
                                    {
                                        EndTotalCredit = (checkValueCREDIT - checkValueDEPIT).ToString();
                                        EndTotalDebit = "0";

                                    }

                                    if (EndTotalDebit == "0" && EndTotalCredit == "0")
                                    {

                                    }
                                    else
                                    {
                                        lmd.Add(new Transactions
                                        {
                                            AccountId = Convert.ToInt32(dr[7]),
                                            Credit = Convert.ToDecimal(Math.Round(double.Parse(EndTotalCredit), 2)),
                                            Depit = Convert.ToDecimal(Math.Round(double.Parse(EndTotalDebit), 2)),
                                            BranchId = Convert.ToInt32(dr[9]),
                                            //Credit = Convert.ToDecimal(EndTotalCredit),
                                            //Depit = Convert.ToDecimal(EndTotalDebit),

                                        });

                                    }

                                }

                            }
                        }




                        Invoices voucherNEW;

                        foreach (var lmNEW in lmd)
                        {

                            voucherNEW = new Invoices();
                            voucherNEW.TransactionDetails = new List<Transactions>();

                            decimal TempValueCredit = 0;
                            decimal TempValueDepit = 0;
                            //if (lmNEW.Credit >= lmNEW.Depit)
                            //{
                            //    TempValueCredit = ((lmNEW.Credit ?? 0) - (lmNEW.Depit ?? 0));
                            //    TempValueDepit = 0;
                            //}
                            //else
                            //{
                            //    TempValueCredit = 0;
                            //    TempValueDepit = ((lmNEW.Depit ?? 0) - (lmNEW.Credit ?? 0));
                            //}


                            TempValueCredit = lmNEW.Credit ?? 0;
                            TempValueDepit = lmNEW.Depit??0;



                            //voucherNEW.InvoiceNumber = _InvoicesRepository.GenerateNextInvoiceNumber(26, yearid, BranchId);
                            voucherNEW.InvoiceId = 0;
                            voucherNEW.TotalValue = lmNEW.Credit>lmNEW.Depit? lmNEW.Credit: lmNEW.Depit;
                            voucherNEW.ToAccountId = lmNEW.AccountId;
                            voucherNEW.IsPost = false;
                            voucherNEW.InvoiceNumber = 1.ToString();
                            voucherNEW.Rad = false;
                            voucherNEW.IsTax = false;
                            voucherNEW.Date = DateM;
                            voucherNEW.HijriDate = DateH;
                            voucherNEW.YearId = nextyear;
                            //voucherNEW.BranchId = BranchId;
                            voucherNEW.BranchId = lmNEW.BranchId;

                            voucherNEW.AddUser = UserId;
                            voucherNEW.AddDate = DateTime.Now;
                            voucherNEW.printBankAccount = false;
                            voucherNEW.DunCalc = false;

                            voucherNEW.Type = 26;
                            voucherNEW.IsDeleted = false;
                            _TaamerProContext.Invoices.Add(voucherNEW);


                            voucherNEW.TransactionDetails.Add(new Transactions
                            {
                                AccTransactionDate = DateM,
                                AccTransactionHijriDate = DateH,
                                TransactionDate = DateM,
                                TransactionHijriDate = DateH,
                                AccountId = lmNEW.AccountId,
                                CostCenterId = 0,
                                AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==lmNEW.AccountId)?.FirstOrDefault()?.Type,
                                Type = 26,
                                LineNumber = 1,
                                Depit = TempValueDepit,
                                Credit = TempValueCredit,
                                YearId = nextyear,
                                Notes = "تدوير ارصدة السنة السابقة ك ارصدة افتتاحية ",
                                Details = "تدوير ارصدة السنة السابقة ك ارصدة افتتاحية ",
                                InvoiceReference = "",
                                //InvoiceId = voucherNEW.InvoiceId,
                                IsPost = false,
                                //BranchId = BranchId,
                                BranchId = lmNEW.BranchId,

                                AddDate = DateTime.Now,
                                AddUser = UserId,
                                IsDeleted = false,
                            });

                            _TaamerProContext.Transactions.AddRange(voucherNEW.TransactionDetails);

                            _TaamerProContext.SaveChanges();



                            //var postedVoucherNEW = _InvoicesRepository.GetById(voucherNEW.InvoiceId);
                            var postedVoucherNEW = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucherNEW.InvoiceId).FirstOrDefault();
                            if(postedVoucherNEW!=null)
                            {
                                if (postedVoucherNEW.IsPost != true)
                                {

                                    postedVoucherNEW.IsPost = true;
                                    postedVoucherNEW.PostDate = DateM;
                                    postedVoucherNEW.PostHijriDate = DateH;

                                    var newJournalNEW = new Journals();
                                    //var JNo = _JournalsRepository.GenerateNextJournalNumber(Convert.ToInt32(yearid), BranchId).Result;
                                    //if (postedVoucherNEW.Type == 10)
                                    //{
                                    //    JNo = 1;
                                    //}
                                    //else
                                    //{
                                    //    JNo = (newJournalNEW.VoucherType == 10 && JNo == 1) ? JNo : (newJournalNEW.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                                    //}
                                    var JNo = 1;

                                    newJournalNEW.JournalNo = JNo;
                                    //newJournal.JournalNo = _JournalsRepository.GenerateNextJournalNumber();
                                    newJournalNEW.YearMalia = Convert.ToInt32(yearid);
                                    newJournalNEW.VoucherId = postedVoucherNEW.InvoiceId;
                                    newJournalNEW.VoucherType = postedVoucherNEW.Type;
                                    newJournalNEW.BranchId = postedVoucherNEW.BranchId ?? 0;

                                    newJournalNEW.AddDate = DateTime.Now;
                                    newJournalNEW.AddUser = newJournalNEW.UserId = UserId;
                                    _TaamerProContext.Journals.Add(newJournalNEW);
                                    foreach (var trans in postedVoucherNEW.TransactionDetails.ToList())
                                    {
                                        trans.IsPost = true;
                                        trans.JournalNo = newJournalNEW.JournalNo;
                                    }
                                    postedVoucherNEW.JournalNumber = newJournalNEW.JournalNo;
                                    _TaamerProContext.SaveChanges();
                                }

                            }

                        }

                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote = "تم تدوير الارصدة";
                        _SystemAction.SaveAction("SaveRecycleVoucher", "VoucherService", 1, "تم تدوير الارصدة", "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                    }
                    else
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote2 = "فشل في تدوير الأرصدة تأكد من وجود قيد اقفال تم ترحيله";
                        _SystemAction.SaveAction("SaveRecycleVoucher", "VoucherService", 1, "فشل في تدوير الأرصدة تأكد من وجود قيد اقفال تم ترحيله", "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "فشل في تدوير الأرصدة تأكد من وجود قيد اقفال تم ترحيله" };
                    }
                }
                else
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote2 = "فشل في تدوير الأرصدة تم تدوير الارصدة مسبقا";
                    _SystemAction.SaveAction("SaveRecycleVoucher", "VoucherService", 1, "فشل في تدوير الأرصدة تم تدوير الارصدة مسبقا", "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "فشل في تدوير الأرصدة تم تدوير الارصدة مسبقا" };
                }


            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في تدوير الارصدة  ";
                _SystemAction.SaveAction("SaveRecycleVoucher", "VoucherService", 1, "فشل في تدوير الارصدة", "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveRecycleReturnVoucher(int SelectedYear, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                List<Transactions> lmd = new List<Transactions>();

                int nextyear = (yearid ?? 0) + 1;

                //var year = _fiscalyearsRepository.GetCurrentYear();
                if (yearid == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote2 = "فشل في استرجاع الأرصدة تأكد من السنة المالية";
                    _SystemAction.SaveAction("SaveRecycleReturnVoucher", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }

                using (SqlConnection con = new SqlConnection(Con))
                {
                    using (SqlCommand cmd = new SqlCommand("AccountReturnRecycle", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@YearToID", SqlDbType.Int).Value = nextyear;
                        con.Open();

                        SqlDataAdapter a = new SqlDataAdapter(cmd);
                        DataSet ds = new DataSet();
                        a.Fill(ds);


                        DataTable dt = new DataTable();
                        dt = ds.Tables[0];
                        foreach (DataRow dr in dt.Rows)

                        {
                            lmd.Add(new Transactions
                            {
                                InvoiceId = Convert.ToInt32(dr[0]),
                            });
                        }


                    }
                }

                Invoices voucherNEW;



                foreach (var item in lmd)
                {
                    var postedVoucher = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==item.InvoiceId).FirstOrDefault();

                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == postedVoucher.InvoiceId).ToList();

                    _TaamerProContext.Transactions.RemoveRange(TransactionDetails);


                    if (postedVoucher.IsPost == true)
                    {
                        var JournalExist = _TaamerProContext.Journals.Where(s => s.IsDeleted == false && s.VoucherId == item.InvoiceId && s.YearMalia == nextyear);
                        if (JournalExist != null && JournalExist.Count() > 0)
                        {
                            _TaamerProContext.Journals.RemoveRange(JournalExist);
                        }

                        postedVoucher.IsPost = false;
                        postedVoucher.PostDate = "";
                        postedVoucher.PostHijriDate = "";
                        postedVoucher.IsDeleted = true;

                        //foreach (var trans in postedVoucher.TransactionDetails.ToList())
                        //{
                        //    trans.IsPost = false;
                        //    trans.JournalNo = null;
                        //}

                        postedVoucher.JournalNumber = null;
                    }


                }

                _TaamerProContext.SaveChanges();

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "تم استرجاع الارصدة";
                _SystemAction.SaveAction("SaveRecycleReturnVoucher", "VoucherService", 1, "تم استرجاع الارصدة", "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };

            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في استرجاع الارصدة  ";
                _SystemAction.SaveAction("SaveRecycleReturnVoucher", "VoucherService", 1, "فشل في استرجاع الارصدة", "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveDailyVoucher2(List<Transactions> voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            var invoiceid = 0;
            var Type = 0;
            int Transactionid = 0;
            var invno = "";
            try
            {
                //var year = _fiscalyearsRepository.GetCurrentYear();
                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ القيداليومي";
                    _SystemAction.SaveAction("SaveDailyVoucher2", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }

                foreach (var item in voucher.ToList())
                {
                    invoiceid = item.InvoiceId.Value;
                    Type = item.Type.Value;
                    Transactionid = (Int32)item.TransactionId;
                    break;
                }
                if (invoiceid == 0)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ القيد اليومي";
                    _SystemAction.SaveAction("SaveDailyVoucher2", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
                }

                if (Transactionid == 0)
                {
                    //voucher.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                    //voucher.IsPost = false;
                    //voucher.Rad = false;
                    //voucher.IsTax = false;
                    //voucher.YearId = year.YearId;
                    //voucher.BranchId = BranchId;
                    //voucher.AddUser = UserId;
                    //voucher.AddDate = DateTime.Now;
                    //_TaamerProContext.Invoices.Add(voucher);
                    //add details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                         item.AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        item.AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));
                        item.TransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        item.TransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));
                        item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                        item.YearId = yearid;
                        item.IsPost = false;
                        item.AddUser = UserId;
                        item.BranchId = BranchId;
                        item.Type = Type;
                        item.AddDate = DateTime.Now;
                        _TaamerProContext.Transactions.Add(item);
                    }

                    _TaamerProContext.SaveChanges();
                    try
                    {
                        invno = _TaamerProContext.Invoices.FirstOrDefault(x => x.InvoiceId == voucher.FirstOrDefault().InvoiceId).InvoiceNumber;

                    }
                    catch
                    {

                    }
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة قيد يومي جديد" + " برقم " + invno;
                    _SystemAction.SaveAction("SaveDailyVoucher2", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };

                }
                else
                {
                    var VoucherUpdated = _TransactionsRepository.GetAllTransByVoucherId(invoiceid);
                    if (VoucherUpdated != null)
                    {
                        //VoucherUpdated.Notes = voucher.Notes;
                        //VoucherUpdated.IsPost = voucher.IsPost;
                        //VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        //VoucherUpdated.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                        //VoucherUpdated.BranchId = BranchId;
                        //VoucherUpdated.UpdateDate = DateTime.Now;
                        //VoucherUpdated.UpdateUser = UserId;

                    }
                    //_TaamerProContext.Transactions.RemoveRange(VoucherUpdated.ToList());

                    //delete existing details 
                    // add new details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تكرار الحساب مع مركز التكلفة" };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                         item.AccTransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        item.AccTransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));
                        item.TransactionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        item.TransactionHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));
                        item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                        item.InvoiceId = invoiceid;
                        item.Type = Type;
                        item.IsPost = false;
                        item.AddUser = UserId;
                        item.BranchId = BranchId;
                        item.AddDate = DateTime.Now;
                        _TaamerProContext.Transactions.Add(item);
                    }

                    _TaamerProContext.SaveChanges();
                    try
                    {
                        invno = _TaamerProContext.Invoices.FirstOrDefault(x => x.InvoiceId == voucher.FirstOrDefault().InvoiceId).InvoiceNumber;

                    }
                    catch
                    {

                    }

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل فيد يومي رقم " + invno;
                    _SystemAction.SaveAction("SaveDailyVoucher2", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ القيد اليومي";
                _SystemAction.SaveAction("SaveDailyVoucher2", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage SaveConvertVoucher(Invoices voucher, int UserId, int BranchId, int? yearid, string Con)
        {
            try
            {
                voucher.TransactionDetails = new List<Transactions>();
                //var year = _fiscalyearsRepository.GetCurrentYear();
                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ سند التحويل";
                    _SystemAction.SaveAction("SaveConvertVoucher", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }

                voucher.InvoiceValueText = ConvertToWord_NEW(voucher.InvoiceValue.ToString());

                if (voucher.InvoiceId == 0)
                {
                    voucher.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                    voucher.ToAccountId = voucher.ToAccountId;
                    voucher.IsPost = false;
                    voucher.Rad = false;
                    voucher.YearId = yearid;
                    voucher.BranchId = BranchId;
                    voucher.AddUser = UserId;
                    voucher.AddDate = DateTime.Now;
                    voucher.DunCalc = false;

                    var vouchercheck = _TaamerProContext.Invoices.Where(s => s.IsDeleted == false && s.YearId == yearid && s.Type == voucher.Type && s.BranchId == BranchId && s.InvoiceNumber == voucher.InvoiceNumber);
                    if (vouchercheck.Count() > 0)
                    {
                        //var NextInv = _InvoicesRepository.GenerateNextInvoiceNumber(voucher.Type, yearid, BranchId).Result;
                        var NewNextInv = GenerateVoucherNumberNewPro(voucher.Type, BranchId, yearid, voucher.Type, Con).Result;
                        //var NewNextInv = string.Format("{0:000000}", NextInv);

                        voucher.InvoiceNumber = NewNextInv.ToString();
                    }

                    _TaamerProContext.Invoices.Add(voucher);
                    // add transactions
                    //depit 
                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        AccountId = voucher.ToAccountId,
                        AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == voucher.ToAccountId)?.FirstOrDefault()?.Type,
                        Type = 16,
                        LineNumber = 1,
                        Depit = voucher.TotalValue,
                        Credit = 0,
                        YearId = yearid,
                        Notes = "سند تحويل فرع رقم " + voucher.InvoiceNumber + "",
                        Details = "سند تحويل فرع رقم " + voucher.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        IsPost = false,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });
                    //credit 
                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        AccountId = voucher.FromAccountId,
                        AccountType =  _TaamerProContext.Accounts.Where(s => s.AccountId == voucher.FromAccountId)?.FirstOrDefault()?.Type,
                        Type = 16,
                        LineNumber = 2,
                        Credit = voucher.TotalValue,
                        Depit = 0,
                        YearId = yearid,
                        Notes = "سند تحويل فرع رقم " + voucher.InvoiceNumber + "",
                        Details = "سند تحويل فرع رقم " + voucher.InvoiceNumber + "",
                        //InvoiceReference = voucher.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        IsPost = false,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                    _TaamerProContext.SaveChanges();
                    
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة سند تحويل جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveConvertVoucher", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
                else
                {
                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                    if (VoucherUpdated != null)
                    {
                        VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        VoucherUpdated.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                        VoucherUpdated.Notes = voucher.Notes;
                        VoucherUpdated.Date = voucher.Date;
                        VoucherUpdated.HijriDate = voucher.HijriDate;
                        VoucherUpdated.BranchId = voucher.BranchId;
                        VoucherUpdated.ToAccountId = voucher.ToAccountId;
                        VoucherUpdated.UpdateDate = DateTime.Now;
                        VoucherUpdated.UpdateUser = UserId;
                    }
                    ////  //delete existing details 
                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                    _TaamerProContext.Transactions.RemoveRange(TransactionDetails);                    // add transactions
                    //depit 
                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        AccountId = voucher.ToAccountId,
                        AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId == voucher.ToAccountId)?.FirstOrDefault()?.Type,
                        Type = 16,
                        LineNumber = 1,
                        Depit = voucher.TotalValue,
                        Credit = 0,
                        YearId = yearid,
                        Notes = "سند تحويل فرع رقم " + VoucherUpdated.InvoiceNumber + "",
                        Details = "سند تحويل فرع رقم " + VoucherUpdated.InvoiceNumber + "",
                        //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        IsPost = false,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });
                    //credit 
                    voucher.TransactionDetails.Add(new Transactions
                    {
                        AccTransactionDate = voucher.Date,
                        AccTransactionHijriDate = voucher.HijriDate,
                        TransactionDate = voucher.Date,
                        TransactionHijriDate = voucher.HijriDate,
                        AccountId = voucher.FromAccountId,
                        AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId== voucher.FromAccountId)?.FirstOrDefault()?.Type,
                        Type = 16,
                        LineNumber = 2,
                        Credit = voucher.TotalValue,
                        Depit = 0,
                        YearId = yearid,
                        Notes = "سند تحويل فرع رقم " + VoucherUpdated.InvoiceNumber + "",
                        Details = "سند تحويل فرع رقم " + VoucherUpdated.InvoiceNumber + "",
                        //InvoiceReference = VoucherUpdated.InvoiceNumber.ToString(),
                        InvoiceReference = "",

                        IsPost = false,
                        AddDate = DateTime.Now,
                        AddUser = UserId,
                        IsDeleted = false,
                    });
                    _TaamerProContext.Transactions.AddRange(voucher.TransactionDetails);

                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل سند تحويل رقم " + voucher.InvoiceId;
                    _SystemAction.SaveAction("SaveConvertVoucher", "VoucherService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ سند تحويل";
                _SystemAction.SaveAction("SaveConvertVoucher", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public async Task<InvoicesVM> GetVoucherById(int VoucherId)
        {
            return await _InvoicesRepository.GetVoucherById(VoucherId);
        }
        public async Task<InvoicesVM> GetInvoiceDateById(int VoucherId)
        {
            return await _InvoicesRepository.GetInvoiceDateById(VoucherId);
        }
        public Invoices GetVoucherByIdNotiDepit_Purchase(int VoucherId)
        {
            return _TaamerProContext.Invoices.Where(s=>s.DepitNotiId==VoucherId).FirstOrDefault();
        }
        public async Task<IEnumerable<InvoicesVM>> GetVoucherByIdNoti(int VoucherId)
        {
            return await _InvoicesRepository.GetVoucherByIdNoti(VoucherId);
        }

        public async Task<InvoicesVM> GetVoucherByIdPurchase(int VoucherId)
        {
            return await _InvoicesRepository.GetVoucherByIdPurchase(VoucherId);
        }
        public async Task<Invoices> GetInvoicesById(int VoucherId)
        {
            return await _InvoicesRepository.GetInvoicesById(VoucherId);
        }
        //heba
        public async Task<DataTable> ReceiptCashingPaying(int VoucherId, string Con)
        {
            return await _InvoicesRepository.ReceiptCashingPaying(VoucherId, Con);
        }
        public async Task<DataTable> ReceiptCashingPayingNoti(int VoucherId, string Con)
        {
            return await _InvoicesRepository.ReceiptCashingPayingNoti(VoucherId, Con);
        }
        public async Task<DataTable> ReceiptCashingPayingNotiDepit(int VoucherId, string Con)
        {
            return await _InvoicesRepository.ReceiptCashingPayingNotiDepit(VoucherId, Con);
        }
        public async Task<DataTable> ReceiptCashingPayingNotiDepitPurchase(int VoucherId, string Con)
        {
            return await _InvoicesRepository.ReceiptCashingPayingNotiDepitPurchase(VoucherId, Con);
        }
        public async Task<DataTable> DailyVoucherReport(int VoucherId,string Con)
        {
            return await _InvoicesRepository.DailyVoucherReport(VoucherId,Con);
        }

        public async Task<DataTable> OpeningVoucherReport(int VoucherId, string Con)
        {
            return await _InvoicesRepository.OpeningVoucherReport(VoucherId, Con);
        }
        
        public async Task<int?> GenerateVoucherNumber(int Type, int BranchId, int? yearid)
        {
            return await _InvoicesRepository.GenerateNextInvoiceNumber(Type, yearid, BranchId);
        }
        public async Task<string> GenerateVoucherNumberNewPro(int Type, int BranchId, int? yearid,int Status, string Con)
        {
            var codePrefix = "";
            var BranchObj = _BranchesRepository.GetById(BranchId);
            if (BranchObj.InvoiceBranchSeparated == false)
            {
                BranchId = 0;
            }
            if (Type == 2)
            {
                if (BranchObj.InvoiceStartCode != null && BranchObj.InvoiceStartCode != "")
                {
                    codePrefix = BranchObj.InvoiceStartCode;
                }
            }
            var ProList = await _InvoicesRepository.GenerateVoucherNumberNewPro(Type, yearid, BranchId, codePrefix, BranchObj.InvoiceBranchSeparated ?? false,Status, Con);
            var Value = 0;
            if (ProList.Count()>0)
            {
                Value = ProList.FirstOrDefault()!.Newinvoicenumber??1;
            }
            else
            {
                Value = 1;
            }
            var NewValue = string.Format("{0:000000}", Value);
            if (codePrefix != "")
            {
                NewValue = codePrefix + NewValue;
            }
            return (NewValue);
        }
        public async Task<int?> GenerateVoucherZatcaNumber( int BranchId, int? yearid)
        {
            return await _InvoicesRepository.GenerateVoucherZatcaNumber(yearid, BranchId);
        }
        public async Task<int?> GenerateVoucherNumberOpening(int Type, int BranchId, int? yearid)
        {
            return await _InvoicesRepository.GenerateVoucherNumberOpening(Type, yearid, BranchId);
        }
        public async Task<int?> GenerateVoucherNumberClosing(int Type, int BranchId, int? yearid)
        {
            return await _InvoicesRepository.GenerateVoucherNumberClosing(Type, yearid, BranchId);
        }

        public async Task<IEnumerable<TransactionsVM>> GetAllTransByVoucherId(int? voucherId)
        {
            return await _TransactionsRepository.GetAllTransByVoucherId(voucherId);
        }
        public GeneralMessage PostVouchers(List<Invoices> PostedList, int UserId, int BranchId, int? yearid)
        {
            try
            {
                List<string> VNo = new List<string>();
                //var year = _fiscalyearsRepository.GetCurrentYear();
                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote2 = "فشل في الترحيل";
                    _SystemAction.SaveAction("PostVouchers", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                int CountNMora7l = 0;
                int CountMora7l = 0;
                int vtype = 0;
                foreach (var item in PostedList)
                {
                    var postedVoucher = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==item.InvoiceId).FirstOrDefault();
                    if(postedVoucher!=null)
                    {
                        VNo.Add(postedVoucher.InvoiceNumber);

                        if (postedVoucher.IsPost != true)
                        {
                            CountNMora7l += 1;
                            postedVoucher.IsPost = true;
                            //postedVoucher.PostDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            //postedVoucher.PostHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));
                            postedVoucher.PostDate = postedVoucher.Date;
                            postedVoucher.PostHijriDate = postedVoucher.HijriDate;

                            var newJournal = new Journals();
                            var JNo = _JournalsRepository.GenerateNextJournalNumber(yearid ?? default(int), BranchId).Result;
                            if (postedVoucher.Type == 10)
                            {
                                JNo = 1;
                            }
                            else
                            {
                                JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                            }

                            vtype = postedVoucher.Type;
                            newJournal.JournalNo = JNo;
                            //newJournal.JournalNo = _JournalsRepository.GenerateNextJournalNumber();
                            newJournal.YearMalia = yearid ?? default(int);
                            newJournal.VoucherId = postedVoucher.InvoiceId;
                            newJournal.VoucherType = postedVoucher.Type;
                            newJournal.BranchId = postedVoucher.BranchId ?? 0;
                            newJournal.AddDate = DateTime.Now;
                            newJournal.AddUser = newJournal.UserId = UserId;
                            _TaamerProContext.Journals.Add(newJournal);
                            var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == postedVoucher.InvoiceId).ToList();

                            foreach (var trans in TransactionDetails)
                            {
                                trans.IsPost = true;
                                trans.JournalNo = newJournal.JournalNo;
                            }
                            postedVoucher.JournalNumber = newJournal.JournalNo;
                            _TaamerProContext.SaveChanges();
                        }
                        else
                        {
                            CountMora7l += 1;
                        }
                    }                  
                }
                string ReasonPhrase = "";
                if (CountMora7l == 0 && CountNMora7l > 0)
                {
                    ReasonPhrase = Resources.Deported;
                    //return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.Deported };
                }
                else if (CountMora7l > 0 && CountNMora7l == 0)
                {
                    // _TaamerProContext.SaveChanges();
                    ReasonPhrase = Resources.Restrictionspreviouslyposted;
                    //return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.Restrictionspreviouslyposted };
                }
                else if (CountMora7l > 0 && CountNMora7l > 0)
                {
                    //_TaamerProContext.SaveChanges();
                    string Msg = String.Format("Resources.posted", CountNMora7l, CountMora7l);
                    ReasonPhrase = Msg;
                    //return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Msg };
                }
                //_TaamerProContext.SaveChanges();
                ReasonPhrase = Resources.Deported;
                
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote =  "تم ترحيل  " + GetVoucherType (vtype) + VNo;
                _SystemAction.SaveAction("PostVouchers", "VoucherService", 2, ReasonPhrase, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = ReasonPhrase };
            }
            catch (Exception)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في الترحيل ";
                _SystemAction.SaveAction("PostVouchers", "VoucherService", 1, Resources.PostesFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.PostesFailed };
            }
        }

        public GeneralMessage PostVouchersCustody(List<Custody> PostedList, int UserId, int BranchId, int? yearid)
        {
            try
            {
                //var year = _fiscalyearsRepository.GetCurrentYear();
                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote2 = "فشل في الترحيل";
                    _SystemAction.SaveAction("PostVouchers", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                int CountNMora7l = 0;
                int CountMora7l = 0;
                foreach (var item in PostedList)
                {
                    var postedVoucher = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==item.InvoiceId).FirstOrDefault();
                    if(postedVoucher!=null)
                    {
                        if (postedVoucher.IsPost != true)
                        {
                            CountNMora7l += 1;
                            postedVoucher.IsPost = true;
                            //postedVoucher.PostDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            //postedVoucher.PostHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));
                            postedVoucher.PostDate = postedVoucher.Date;
                            postedVoucher.PostHijriDate = postedVoucher.HijriDate;

                            var newJournal = new Journals();
                            var JNo = _JournalsRepository.GenerateNextJournalNumber(yearid ?? default(int), BranchId).Result;
                            if (postedVoucher.Type == 10)
                            {
                                JNo = 1;
                            }
                            else
                            {
                                JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                            }


                            newJournal.JournalNo = JNo;
                            //newJournal.JournalNo = _JournalsRepository.GenerateNextJournalNumber();
                            newJournal.YearMalia = yearid ?? default(int);
                            newJournal.VoucherId = postedVoucher.InvoiceId;
                            newJournal.VoucherType = postedVoucher.Type;
                            newJournal.BranchId = postedVoucher.BranchId ?? 0;
                            newJournal.AddDate = DateTime.Now;
                            newJournal.AddUser = newJournal.UserId = UserId;
                            _TaamerProContext.Journals.Add(newJournal);
                            var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == postedVoucher.InvoiceId).ToList();

                            foreach (var trans in TransactionDetails.ToList())
                            {
                                trans.IsPost = true;
                                trans.JournalNo = newJournal.JournalNo;
                            }
                            postedVoucher.JournalNumber = newJournal.JournalNo;
                            _TaamerProContext.SaveChanges();
                        }
                        else
                        {
                            CountMora7l += 1;
                        }
                    }
                  

                }
                string ReasonPhrase = "";
                if (CountMora7l == 0 && CountNMora7l > 0)
                {
                    ReasonPhrase = Resources.Deported;
                    //return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.Deported };
                }
                else if (CountMora7l > 0 && CountNMora7l == 0)
                {
                    // _TaamerProContext.SaveChanges();
                    ReasonPhrase = Resources.Restrictionspreviouslyposted;
                    //return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.Restrictionspreviouslyposted };
                }
                else if (CountMora7l > 0 && CountNMora7l > 0)
                {
                    //_TaamerProContext.SaveChanges();
                    string Msg = String.Format(Resources.posted, CountNMora7l, CountMora7l);
                    ReasonPhrase = Msg;
                    //return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Msg };
                }
                //_TaamerProContext.SaveChanges();
                ReasonPhrase = Resources.Deported;

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "تم الترحيل العهد بنجاح"  +string.Join(", ", PostedList.Select(x => x.Item.NameAr)); 
                _SystemAction.SaveAction("PostVouchers", "VoucherService", 2, ReasonPhrase, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = ReasonPhrase };
            }
            catch (Exception)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في الترحيل ";
                _SystemAction.SaveAction("PostVouchers", "VoucherService", 1, Resources.PostesFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.PostesFailed };
            }
        }

        public  GeneralMessage PostVouchersCheckBox(List<Int32> voucherIds, int UserId, int BranchId, int? yearid)
        {
            try
            {
                //var year = _fiscalyearsRepository.GetCurrentYear();
                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote2 = "فشل في الترحيل";
                    _SystemAction.SaveAction("PostVouchers", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                int CountNMora7l = 0;
                int CountMora7l = 0;
                int Vtype = 0;
                for(int i=0;i< voucherIds.Count();i++)
                {
                    var postedVoucher = _TaamerProContext.Invoices.Where(s => s.InvoiceId == voucherIds[i]).FirstOrDefault();
                    if (postedVoucher != null)
                    {
                        if (postedVoucher.IsPost != true)
                        {
                            CountNMora7l += 1;
                            postedVoucher.IsPost = true;
                            //postedVoucher.PostDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                            //postedVoucher.PostHijriDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("ar"));

                            postedVoucher.PostDate = postedVoucher.Date;
                            postedVoucher.PostHijriDate = postedVoucher.HijriDate;

                            var newJournal = new Journals();
                            var JNo =  _JournalsRepository.GenerateNextJournalNumber(yearid ?? default(int), BranchId).Result;
                            if (postedVoucher.Type == 10)
                            {
                                JNo = 1;
                            }
                            else
                            {
                                JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                            }
                            Vtype = postedVoucher.Type;

                            newJournal.JournalNo = JNo;
                            //newJournal.JournalNo = _JournalsRepository.GenerateNextJournalNumber();
                            newJournal.YearMalia = yearid ?? default(int);
                            newJournal.VoucherId = postedVoucher.InvoiceId;
                            newJournal.VoucherType = postedVoucher.Type;
                            newJournal.BranchId = postedVoucher.BranchId ?? 0;
                            newJournal.AddDate = DateTime.Now;
                            newJournal.AddUser = newJournal.UserId = UserId;
                            _TaamerProContext.Journals.Add(newJournal);
                            var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == postedVoucher.InvoiceId).ToList();

                            foreach (var trans in TransactionDetails)
                            {
                                trans.IsPost = true;
                                trans.JournalNo = newJournal.JournalNo;
                            }
                            postedVoucher.JournalNumber = newJournal.JournalNo;
                            _TaamerProContext.SaveChanges();
                        }
                        else
                        {
                            CountMora7l += 1;
                        }
                    }
                   
                }

                string ReasonPhrase = "";
                var status = HttpStatusCode.OK;
                if (CountMora7l == 0 && CountNMora7l > 0)
                {
                    ReasonPhrase = Resources.Deported;
                     status = HttpStatusCode.OK;
                    //return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.Deported };
                }
                else if (CountMora7l > 0 && CountNMora7l == 0)
                {
                    // _TaamerProContext.SaveChanges();
                    ReasonPhrase = Resources.Restrictionspreviouslyposted;
                    status = HttpStatusCode.BadRequest;
                    //return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.Restrictionspreviouslyposted };
                }
                else if (CountMora7l > 0 && CountNMora7l > 0)
                {
                    //_TaamerProContext.SaveChanges();
                    string Msg = String.Format(Resources.posted, CountNMora7l, CountMora7l);
                    ReasonPhrase = Msg;
                    status = HttpStatusCode.OK;

                    //return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Msg };
                }
                //_TaamerProContext.SaveChanges();
                //ReasonPhrase = Resources.Deported;

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "تم ترحيل "+ GetVoucherType(Vtype) + string.Join(", ", voucherIds);
                _SystemAction.SaveAction("PostVouchers", "VoucherService", 2, ReasonPhrase, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = status, ReasonPhrase = ReasonPhrase };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في الترحيل ";
                _SystemAction.SaveAction("PostVouchers", "VoucherService", 1, Resources.PostesFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.PostesFailed };
            }
        }

        public GeneralMessage PostBackVouchers(List<Invoices> PostedList, int UserId, int BranchId, int? yearid)
        {
            try
            {
                int vtype = 0;
                List<string> VNo = new List<string>();
                //var year = _fiscalyearsRepository.GetCurrentYear();
                if (yearid == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote2 = "فشل في فك الترحيل ";
                    _SystemAction.SaveAction("PostVouchers", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                foreach (var item in PostedList)
                {
                    var postedVoucher = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==item.InvoiceId).FirstOrDefault();
                    if (postedVoucher != null)
                    {
                        VNo.Add(postedVoucher.InvoiceNumber);
                        vtype = postedVoucher.Type;
                        if (postedVoucher.IsPost == true)
                        {
                            var JournalExist = _TaamerProContext.Journals.Where(s => s.IsDeleted == false && s.VoucherId == item.InvoiceId && s.YearMalia == yearid && s.VoucherType == item.Type && s.BranchId == BranchId);
                            if (JournalExist != null && JournalExist.Count() > 0)
                            {
                                _TaamerProContext.Journals.RemoveRange(JournalExist);
                            }

                            postedVoucher.IsPost = false;
                            postedVoucher.PostDate = null;
                            postedVoucher.PostHijriDate = null;
                            postedVoucher.JournalNumber = null;

                            var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == item.InvoiceId).ToList();
                            if (TransactionDetails.Count() > 0)
                            {
                                foreach (var trans in TransactionDetails.ToList())
                                {
                                    trans.IsPost = false;
                                    trans.JournalNo = null;
                                }
                            }

                        }

                    }

                }

                _TaamerProContext.SaveChanges();

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فك ترحيل "+ GetVoucherType(vtype) + "ارقام"+ VNo;
                _SystemAction.SaveAction("PostBackVouchers", "VoucherService", 2, Resources.successfullyuninstalled, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.successfullyuninstalled };


            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote ="فشل في فك ترحيل السندات";
                _SystemAction.SaveAction("PostBackVouchers", "VoucherService", 2,  Resources.FailedPosting, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.FailedPosting };
            }
        }
        public GeneralMessage PostBackVouchersCustody(List<Custody> PostedList, int UserId, int BranchId, int? yearid)
        {
            try
            {
                int vtype = 0;
                if (yearid == null)
                {
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote2 = "فشل في الترحيل ";
                    _SystemAction.SaveAction("PostBackVouchersCustody", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }

                foreach (var item in PostedList)
                {
                    var postedVoucher = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==item.InvoiceId).FirstOrDefault();
                    vtype = postedVoucher.Type;
                    if (postedVoucher!=null)
                    {
                        if (postedVoucher.IsPost == true)
                        {
                            var JournalExist = _TaamerProContext.Journals.Where(s => s.IsDeleted == false && s.VoucherId == item.InvoiceId && s.YearMalia == yearid && s.VoucherType == item.Type && s.BranchId == BranchId);
                            if (JournalExist != null && JournalExist.Count() > 0)
                            {
                                _TaamerProContext.Journals.RemoveRange(JournalExist);
                            }

                            postedVoucher.IsPost = false;
                            postedVoucher.PostDate = null;
                            postedVoucher.PostHijriDate = null;
                            postedVoucher.JournalNumber = null;
                            var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == postedVoucher.InvoiceId).ToList();

                            if (TransactionDetails.Count() > 0)
                            {
                                foreach (var trans in TransactionDetails)
                                {
                                    trans.IsPost = false;
                                    trans.JournalNo = null;
                                }
                            }

                        }

                    }

                }

                _TaamerProContext.SaveChanges();

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فك ترحيل العهد "  ;
                _SystemAction.SaveAction("PostBackVouchersCustody", "VoucherService", 2, Resources.successfullyuninstalled, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.successfullyuninstalled };


            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = Resources.General_SavedFailed;
                _SystemAction.SaveAction("PostBackVouchersCustody", "VoucherService", 2, Resources.FailedPosting, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.FailedPosting };
            }
        }

        public GeneralMessage CancelPostVouchers(List<Invoices> PostedList, int UserId, int BranchId, int? yearid)
        {
            try
            {
                if (yearid == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote2 = Resources.General_SavedFailed;
                    _SystemAction.SaveAction("CancelPostVouchers", "VoucherService", 2, Resources.choosefinYear, "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }

                foreach (var item in PostedList)
                {
                    //var postedVoucher = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==item.InvoiceId).FirstOrDefault();
                    var postedVoucher = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==item.InvoiceId).FirstOrDefault();

                    if(postedVoucher!=null)
                    {
                        var JournalExist = _TaamerProContext.Journals.Where(s => s.IsDeleted == false && s.VoucherId == item.InvoiceId && s.YearMalia == yearid && s.VoucherType == item.Type && s.BranchId == BranchId);
                        if (JournalExist != null && JournalExist.Count() > 0)
                        {
                            _TaamerProContext.Journals.RemoveRange(JournalExist);

                        }

                        postedVoucher.IsPost = false;
                        postedVoucher.PostDate = "";
                        postedVoucher.PostHijriDate = "";
                        var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == postedVoucher.InvoiceId).ToList();

                        foreach (var trans in TransactionDetails)
                        {
                            trans.IsPost = false;
                            trans.JournalNo = null;
                        }
                        postedVoucher.JournalNumber = null;

                    }

                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = Resources.General_EditedSuccessfully;
                    _SystemAction.SaveAction("CancelPostVouchers", "VoucherService", 2, Resources.successfullyuninstalled, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                }

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.successfullyuninstalled };


            }
            catch (Exception ex)
            { //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = Resources.General_SavedFailed;
                _SystemAction.SaveAction("CancelPostVouchers", "VoucherService", 2, Resources.FailedPosting , "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.FailedPosting };
            }
        }

        public GeneralMessage DeleteVoucher(int VoucherId, int UserId, int BranchId)
        {
            try
            {
                Invoices? voucher = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==VoucherId).FirstOrDefault();

                if(voucher!=null)
                {
                    if (voucher.IsPost == true)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote2 = "فشل في حذف السند";
                        _SystemAction.SaveAction("DeleteVoucher", "VoucherService", 3, "Resources.cantposting", "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.cantposting };
                    }
                    voucher.IsDeleted = true;
                    voucher.DeleteDate = DateTime.Now;
                    voucher.DeleteUser = UserId;


                    if (voucher.Type == 8)
                    {
                        var cust = _TaamerProContext.Custody.Where(s => s.IsDeleted == false && s.InvoiceId == voucher.InvoiceId).ToList();
                        if (cust.Count() > 0)
                        {
                            cust.FirstOrDefault().InvoiceId = null;
                        }
                    }
                    var VoucherDetails = _TaamerProContext.VoucherDetails.Where(s => s.InvoiceId == voucher.InvoiceId).ToList();
                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == voucher.InvoiceId).ToList();
                    if(VoucherDetails.Count()>0)
                    {
                        _TaamerProContext.VoucherDetails.RemoveRange(VoucherDetails);
                    }
                    if (TransactionDetails.Count() > 0)
                    {
                        _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    }
                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " حذف سند  " + GetVoucherType(voucher.Type) +"رقم "+" "+voucher.InvoiceNumber;
                    _SystemAction.SaveAction("DeleteVoucher", "VoucherService", 3, Resources.General_DeletedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_DeletedSuccessfully };

                }
                else
                {
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_DeletedFailed };
                }


            }
            catch (Exception)
            { 
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " فشل في حذف سند رقم " + VoucherId;
                _SystemAction.SaveAction("DeleteVoucher", "VoucherService", 3, Resources.General_DeletedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_DeletedFailed };
            }
        }

        public GeneralMessage SaveVoucherAlarmDate(int VoucherId,string VoucherAlarmDate, int UserId, int BranchId)
        {
            try
            {
                //Invoices voucher = _InvoicesRepository.GetById(VoucherId);
                Invoices? voucher = _TaamerProContext.Invoices.Where(s=>s.InvoiceId == VoucherId)?.FirstOrDefault();
                if(voucher!=null)
                {
                    voucher.VoucherAlarmDate = VoucherAlarmDate;
                    voucher.VoucherAlarmCheck = true;
                    voucher.IsSendAlarm = 0;

                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " اضافة تذكير لاستحقاق فاتورة " + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveVoucherAlarmDate", "VoucherService", 3, "تم حفظ التذكير", "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                }
                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " فشل في اضافة تذكير لاستحقاق فاتورة ";
                _SystemAction.SaveAction("SaveVoucherAlarmDate", "VoucherService", 3, "فشل في حفظ التذكير", "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage SaveVueDate(int VoucherId, string VueDate, int UserId, int BranchId)
        {
            try
            {
                Invoices? voucher = _TaamerProContext.Invoices.Where(s => s.InvoiceId == VoucherId)?.FirstOrDefault();
                if (voucher != null)
                {
                    //voucher.VueDate = VueDate;
                    voucher.IsSendAlarm = 0;

                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " اضافة تاريخ لاستحقاق فاتورة " + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveVueDate", "VoucherService", 3, "تم حفظ تاريخ لاستحقاق فاتورة", "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                }
                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " فشل في اضافة تاريخ لاستحقاق فاتورة ";
                _SystemAction.SaveAction("SaveVueDate", "VoucherService", 3, "فشل في حفظ تاريخ لاستحقاق فاتورة", "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }


        public async Task<IEnumerable<TransactionsVM>> GetAllJournals(int? FromJournal, int? ToJournal, string FromDate, string ToDate, int BranchId, int? yearid)
        {
            //var year = _fiscalyearsRepository.GetCurrentYear();
            if (yearid != null)
            {
                return await _TransactionsRepository.GetAllJournals(FromJournal, ToJournal, FromDate, ToDate, yearid ?? default(int), BranchId);
            }
            return new List<TransactionsVM>();


        }
        public async Task<IEnumerable<TransactionsVM>> GetAllTotalJournals(int? FromJournal, int? ToJournal, string FromDate, string ToDate, int BranchId, int? yearid)
        {
            //var year = _fiscalyearsRepository.GetCurrentYear();
            if (yearid != null)
            {
                return await _TransactionsRepository.GetAllTotalJournals(FromJournal, ToJournal, FromDate, ToDate, yearid ?? default(int), BranchId);
            }
            return new List<TransactionsVM>();
        }


        public async Task<IEnumerable<InvoicesVM>> GetProjectManagerRevene(int? ManagerId, string dateFrom, string dateTo, int BranchId, int? yearid)
        {
            //var year = _fiscalyearsRepository.GetCurrentYear();
            if (yearid != null)
            {
                return await _InvoicesRepository.GetProjectManagerRevene(ManagerId, dateFrom, dateTo, yearid ?? default(int), BranchId);
            }
            return new List<InvoicesVM>();
        }

        public GeneralMessage SaveOpeningVoucher(Invoices voucher, int UserId, int BranchId, int? VoucherNum, int? yearid, string Con)
        {
            try
            {
                //var year = _fiscalyearsRepository.GetCurrentYear();
                if (yearid == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ قيد افتتاحي ";
                    _SystemAction.SaveAction("SaveOpeningVoucher", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }
                voucher.InvoiceValueText = ConvertToWord_NEW(voucher.InvoiceValue.ToString());

                if (voucher.InvoiceId == 0)
                {
                    if (VoucherNum > 1)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote2 = "فشل في حفظ قيد افتتاحي ";
                        _SystemAction.SaveAction("SaveOpeningVoucher", "VoucherService", 1, "Resources.cantcreatemanyopening", "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.cantcreatemanyopening };
                    }
                    else
                    {
                        int costcenterBranch = 0;

                        voucher.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                        voucher.IsPost = false;
                        voucher.Rad = false;
                        voucher.IsTax = false;
                        voucher.YearId = yearid;
                        voucher.BranchId = BranchId;
                        voucher.AddUser = UserId;
                        voucher.AddDate = DateTime.Now;
                        voucher.printBankAccount = false;
                        voucher.DunCalc = false;
                        _TaamerProContext.Invoices.Add(voucher);
                        //add details
                        var ObjList = new List<object>();
                        foreach (var item in voucher.TransactionDetails.ToList())
                        {
                            //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                            //{
                            //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
                            //}
                            ObjList.Add(new { item.AccountId, item.CostCenterId });
                             item.AccTransactionDate = voucher.Date;
                            item.AccTransactionHijriDate = voucher.HijriDate;
                            item.TransactionDate = voucher.Date;
                            item.TransactionHijriDate = voucher.HijriDate;
                            item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                            item.YearId = yearid;
                            item.IsPost = false;
                            //item.InvoiceReference = voucher.InvoiceNumber.ToString();
                            item.InvoiceReference = "";

                            item.AddUser = UserId;
                            
                            if (item.CostCenterId != null)
                            {
                                try
                                {
                                    costcenterBranch = _TaamerProContext.CostCenters.Where(s=>s.CostCenterId==item.CostCenterId)?.FirstOrDefault()?.BranchId ?? 0;
                                }
                                catch (Exception)
                                {
                                    costcenterBranch = BranchId;
                                }
                            }
                            else
                            {
                                costcenterBranch = BranchId;
                            }
                           
                           

                            item.BranchId = costcenterBranch;
                            item.Type = voucher.Type;
                            item.AddDate = DateTime.Now;
                            _TaamerProContext.Transactions.Add(item);
                        }
                    }

                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة قيد افتتاحي جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveOpeningVoucher", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
                else
                {
                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                    if (!String.IsNullOrEmpty(Convert.ToString(VoucherUpdated.JournalNumber)) && VoucherUpdated.JournalNumber > 0)
                    {

                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote2 = " فشل في تعديل قيد افتتاحي برقم " + voucher.InvoiceId;
                        _SystemAction.SaveAction("SaveOpeningVoucher", "VoucherService", 2, "Resources.canteditepostboun", "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.canteditepostboun };
                    }
                    if (VoucherUpdated != null)
                    {
                        VoucherUpdated.Notes = voucher.Notes;
                        VoucherUpdated.IsPost = voucher.IsPost;
                        VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        VoucherUpdated.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);



                        VoucherUpdated.BranchId = BranchId;
                       //VoucherUpdated.UpdateDate = DateTime.Now;
                        // VoucherUpdated.UpdateDate = voucher.Date;
                        VoucherUpdated.HijriDate = voucher.HijriDate;
                        VoucherUpdated.Date = voucher.Date;
                        VoucherUpdated.CostCenterId = voucher.CostCenterId;

                        VoucherUpdated.UpdateUser = UserId;
                       
                    }
                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                    //delete existing details 
                    _TaamerProContext.Transactions.RemoveRange(TransactionDetails);
                    // add new details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.TransactionDetails.ToList())
                    {

                        int costcenterBranch = 0;

                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AccTransactionDate = voucher.Date;
                        item.AccTransactionHijriDate = voucher.HijriDate;
                        item.TransactionDate = voucher.Date;
                        item.TransactionHijriDate = voucher.HijriDate;
                        item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                        item.InvoiceId = voucher.InvoiceId;
                        item.Type = voucher.Type;
                        item.IsPost = false;
                        item.YearId = yearid;
                        //item.InvoiceReference = voucher.InvoiceNumber.ToString();
                        item.InvoiceReference = "";

                        item.AddUser = UserId;

                        if (item.CostCenterId != null)
                        {
                            try
                            {
                                costcenterBranch = _TaamerProContext.CostCenters.Where(s=>s.CostCenterId==item.CostCenterId)?.FirstOrDefault()?.BranchId ?? 0;
                            }
                            catch (Exception)
                            {
                                costcenterBranch = BranchId;
                            }
                        }
                        else
                        {
                            costcenterBranch = BranchId;
                        }

                        item.BranchId = costcenterBranch;
                        item.AddDate = DateTime.Now;
                        _TaamerProContext.Transactions.Add(item);
                    }

                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل قيدافتتاحي رقم " + voucher.InvoiceId;
                    _SystemAction.SaveAction("SaveOpeningVoucher", "VoucherService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ قيدافتتاحي";
                _SystemAction.SaveAction("SaveOpeningVoucher", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public  GeneralMessage SaveandPostOpeningVoucher(Invoices voucher, int UserId, int BranchId, int? VoucherNum, int? yearid, string Con)
        {
            try
            {
                //var year = _fiscalyearsRepository.GetCurrentYear();
                if (yearid == null)
                {

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "فشل في حفظ قيد افتتاحي ";
                    _SystemAction.SaveAction("SaveandPostOpeningVoucher", "VoucherService", 1, Resources.choosefinYear, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.choosefinYear };
                }

                voucher.InvoiceValueText = ConvertToWord_NEW(voucher.InvoiceValue.ToString());

                if (voucher.InvoiceId == 0)
                {
                    if (VoucherNum > 1)
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote2 = "فشل في حفظ قيد افتتاحي ";
                        _SystemAction.SaveAction("SaveandPostOpeningVoucher", "VoucherService", 1, "Resources.cantcreatemanyopening", "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.cantcreatemanyopening };
                    }
                    else
                    {
                        voucher.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                        voucher.IsPost = false;
                        voucher.Rad = false;
                        voucher.IsTax = false;
                        voucher.YearId = yearid;
                        voucher.BranchId = BranchId;
                        voucher.AddUser = UserId;
                        voucher.AddDate = DateTime.Now;
                        
                        voucher.Date = voucher.Date;
                        voucher.HijriDate = voucher.HijriDate;
                        voucher.printBankAccount = false;
                        voucher.DunCalc = false;

                        _TaamerProContext.Invoices.Add(voucher);
                        //add details
                        var ObjList = new List<object>();
                        foreach (var item in voucher.TransactionDetails.ToList())
                        {

                            int costcenterBranch = 0;

                            ObjList.Add(new { item.AccountId, item.CostCenterId });
                            item.AccTransactionDate = voucher.Date;
                            item.AccTransactionHijriDate = voucher.HijriDate;
                            item.TransactionDate = voucher.Date;
                            item.TransactionHijriDate = voucher.HijriDate;
                            item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                            item.YearId = yearid;
                            item.IsPost = false;
                            //item.InvoiceReference = voucher.InvoiceNumber.ToString();
                            item.InvoiceReference = "";

                            item.AddUser = UserId;

                            if (item.CostCenterId != null)
                            {
                                try
                                {
                                    costcenterBranch = _TaamerProContext.CostCenters.Where(s=>s.CostCenterId==item.CostCenterId)?.FirstOrDefault()?.BranchId ?? 0;
                                }
                                catch (Exception)
                                {
                                    costcenterBranch = BranchId;
                                }
                            }
                            else
                            {
                                costcenterBranch = BranchId;
                            }

                            item.BranchId = costcenterBranch;
                            item.Type = voucher.Type;
                            item.AddDate = DateTime.Now;
                            _TaamerProContext.Transactions.Add(item);
                        }
                    }

                    _TaamerProContext.SaveChanges();



                    if (voucher.IsPost != true)
                    {
                        voucher.IsPost = true;
                        voucher.PostDate = voucher.Date;
                        voucher.PostHijriDate = voucher.HijriDate;

                        var newJournal = new Journals();
                        var JNo = _JournalsRepository.GenerateNextJournalNumber(yearid ?? default(int), BranchId).Result;
                        if (voucher.Type == 10)
                        {
                            JNo = 1;
                        }
                        else
                        {
                            JNo = (newJournal.VoucherType == 10 && JNo == 1) ? JNo : (newJournal.VoucherType != 10 && JNo == 1) ? JNo + 1 : JNo;
                        }


                        newJournal.JournalNo = JNo;
                        newJournal.YearMalia = yearid ?? default(int);
                        newJournal.VoucherId = voucher.InvoiceId;
                        newJournal.VoucherType = voucher.Type;
                        newJournal.BranchId = voucher.BranchId ?? 0;
                        newJournal.AddDate = DateTime.Now;
                        newJournal.AddUser = newJournal.UserId = UserId;
                        _TaamerProContext.Journals.Add(newJournal);
                        foreach (var trans in voucher.TransactionDetails.ToList())
                        {
                            trans.IsPost = true;
                            trans.JournalNo = newJournal.JournalNo;
                        }
                        voucher.JournalNumber = newJournal.JournalNo;
                        //_TaamerProContext.SaveChanges();
                    }
                    else
                    {
                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate3 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote3 = "فشل في حفظ السند";
                        _SystemAction.SaveAction("SaveandPostOpeningVoucher", "VoucherService", 1, "مرحلة مسبقا", "", "", ActionDate3, UserId, BranchId, ActionNote3, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
                    }

                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة قيد افتتاحي جديد" + " برقم " + voucher.InvoiceNumber; ;
                    _SystemAction.SaveAction("SaveandPostOpeningVoucher", "VoucherService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
                else
                {
                    int costcenterBranch = 0;

                    var VoucherUpdated = _TaamerProContext.Invoices.Where(s=>s.InvoiceId==voucher.InvoiceId)?.FirstOrDefault();
                    if (!String.IsNullOrEmpty(Convert.ToString(VoucherUpdated.JournalNumber)) && VoucherUpdated.JournalNumber > 0)
                    {

                        //-----------------------------------------------------------------------------------------------------------------
                        string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        string ActionNote2 = " فشل في تعديل قيد افتتاحي برقم " + voucher.InvoiceId;
                        _SystemAction.SaveAction("SaveandPostOpeningVoucher", "VoucherService", 2, "Resources.canteditepostboun", "", "", ActionDate2, UserId, BranchId, ActionNote2, 0);
                        //-----------------------------------------------------------------------------------------------------------------

                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.canteditepostboun };
                    }
                    if (VoucherUpdated != null)
                    {
                        VoucherUpdated.Notes = voucher.Notes;
                        VoucherUpdated.IsPost = voucher.IsPost;
                        VoucherUpdated.InvoiceValue = voucher.InvoiceValue;
                        VoucherUpdated.TotalValue = voucher.InvoiceValue - (voucher.DiscountValue ?? 0);
                        VoucherUpdated.BranchId = BranchId;
                        VoucherUpdated.UpdateDate = DateTime.Now;
                        VoucherUpdated.UpdateUser = UserId;
                    }
                    //delete existing details 
                    var TransactionDetails = _TaamerProContext.Transactions.Where(s => s.InvoiceId == VoucherUpdated.InvoiceId).ToList();

                    _TaamerProContext.Transactions.RemoveRange(TransactionDetails.ToList());
                    // add new details
                    var ObjList = new List<object>();
                    foreach (var item in voucher.TransactionDetails.ToList())
                    {
                        //if (ObjList.Contains(new { item.AccountId, item.CostCenterId }))
                        //{
                        //    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تكرار الحساب مع مركز التكلفة" };
                        //}
                        ObjList.Add(new { item.AccountId, item.CostCenterId });
                        item.AccTransactionDate = voucher.Date;
                        item.AccTransactionHijriDate = voucher.HijriDate;
                        item.TransactionDate = voucher.Date;
                        item.TransactionHijriDate = voucher.HijriDate;
                        item.AccountType =  _TaamerProContext.Accounts.Where(s=>s.AccountId==item.AccountId)?.FirstOrDefault()?.Type;
                        item.InvoiceId = voucher.InvoiceId;
                        item.Type = voucher.Type;
                        item.IsPost = false;
                        item.YearId = yearid;
                        //item.InvoiceReference = voucher.InvoiceNumber.ToString();
                        item.InvoiceReference = "";

                        item.AddUser = UserId;

                        if (item.CostCenterId != null)
                        {
                            try
                            {
                                costcenterBranch = _TaamerProContext.CostCenters.Where(s=>s.CostCenterId==item.CostCenterId)?.FirstOrDefault()?.BranchId ?? 0;
                            }
                            catch (Exception)
                            {
                                costcenterBranch = BranchId;
                            }
                        }
                        else
                        {
                            costcenterBranch = BranchId;
                        }


                        item.BranchId = costcenterBranch;
                        item.AddDate = DateTime.Now;
                        _TaamerProContext.Transactions.Add(item);
                        
                    }

                    _TaamerProContext.SaveChanges();
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " تعديل قيدافتتاحي رقم " + voucher.InvoiceId;
                    _SystemAction.SaveAction("SaveandPostOpeningVoucher", "VoucherService", 2, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ قيدافتتاحي";
                _SystemAction.SaveAction("SaveandPostOpeningVoucher", "VoucherService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public async Task<IEnumerable<TransactionsVM>> GetAllJournalsByInvID(int? invId, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await  _TransactionsRepository.GetAllJournalsByInvID(invId, yearid, BranchId);
            }
            return new List<TransactionsVM>();
        }
        public async Task<IEnumerable<TransactionsVM>> GetAllJournalsByInvIDPurchase(int? invId, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _TransactionsRepository.GetAllJournalsByInvIDPurchase(invId, yearid, BranchId);
            }
            return new List<TransactionsVM>();
        }

        public async Task<IEnumerable<TransactionsVM>> GetAllJournalsByReVoucherID(int? invId, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _TransactionsRepository.GetAllJournalsByReVoucherID(invId, yearid, BranchId);
            }
            return new List<TransactionsVM>();


        }
        public async Task<IEnumerable<TransactionsVM>> GetAllJournalsByPayVoucherID(int? invId, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _TransactionsRepository.GetAllJournalsByPayVoucherID(invId, yearid, BranchId);
            }
            return new List<TransactionsVM>();

        }
        public async Task<IEnumerable<TransactionsVM>> GetAllJournalsByDailyID(int? invId, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _TransactionsRepository.GetAllJournalsByDailyID(invId, yearid, BranchId);
            }
            return new List<TransactionsVM>();


        }
        public async Task<IEnumerable<TransactionsVM>> GetAllJournalsByDailyID_Custody(int? invId, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _TransactionsRepository.GetAllJournalsByDailyID_Custody(invId, yearid, BranchId);
            }
            return new List<TransactionsVM>();


        }
        public async Task<IEnumerable<TransactionsVM>> GetAllJournalsByClosingID(int? invId, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _TransactionsRepository.GetAllJournalsByClosingID(invId, yearid, BranchId);
            }
            return new List<TransactionsVM>();


        }

        public async Task<IEnumerable<TransactionsVM>> GetAllJournalsByInvIDRet(int? invId, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _TransactionsRepository.GetAllJournalsByInvIDRet(invId, yearid, BranchId);
            }
            return new List<TransactionsVM>();


        }
        public async Task<IEnumerable<TransactionsVM>> GetAllJournalsByInvIDCreditDepitNoti(int? invId, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _TransactionsRepository.GetAllJournalsByInvIDCreditDepitNoti(invId, yearid, BranchId);
            }
            return new List<TransactionsVM>();
        }
        public async Task<IEnumerable<TransactionsVM>> GetAllJournalsByInvIDRetPurchase(int? invId, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _TransactionsRepository.GetAllJournalsByInvIDRetPurchase(invId, yearid, BranchId);
            }
            return new List<TransactionsVM>();


        }
        public async Task<IEnumerable<TransactionsVM>> GetAllPayJournalsByInvIDRet(int? invId, int BranchId, int? yearid)
        {
            if (yearid != null)
            {
                return await _TransactionsRepository.GetAllPayJournalsByInvIDRet(invId, yearid, BranchId);
            }
            return new List<TransactionsVM>();


        }


        public string ConvertDateCalendar(DateTime DateConv, string Calendar, string DateLangCulture)
        {
            DateLangCulture = DateLangCulture.ToLower();
            string formattedDate = DateConv.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string DAtee = GregToHijri2(formattedDate);
            return (DAtee);
        }
        private string[] allFormats ={"yyyy/MM/dd","yyyy/M/d",
           "dd/MM/yyyy","d/M/yyyy",
            "dd/M/yyyy","d/MM/yyyy","yyyy-MM-dd",
            "yyyy-M-d","dd-MM-yyyy","d-M-yyyy",
            "dd-M-yyyy","d-MM-yyyy","yyyy MM dd",
            "yyyy M d","dd MM yyyy","d M yyyy",
            "dd M yyyy","d MM yyyy"
        };
        public string GregToHijri2(string Greg)
        {
            CultureInfo arCul = new CultureInfo("ar-SA");
            CultureInfo enCul = new CultureInfo("en");
            DateTime tempDate = DateTime.ParseExact(Greg, allFormats, enCul.DateTimeFormat, DateTimeStyles.AllowWhiteSpaces);
            return tempDate.ToString("yyyy-MM-dd", arCul.DateTimeFormat);
        }
        public async Task<Tuple<decimal, decimal>> GetAllCreditNotiTotalValue(int invoiceid)
        {

            decimal TotalValueCredit = 0;
            decimal TotalValueDepit = 0;

            try
            {
                var Voucher = await _InvoicesRepository.GetVoucherByIdNoti(invoiceid);
                if (Voucher.Count() > 0)
                {
                    foreach (var item in Voucher)
                    {
                        if (item.Type == 29)
                        {
                            TotalValueCredit += item.CreditNotiTotal ?? 0;
                        }
                        if (item.Type == 30)
                        {
                            TotalValueDepit += item.DepitNotiTotal ?? 0;
                        }

                    }
                }
                else
                {
                    TotalValueCredit = 0;
                    TotalValueDepit = 0;
                }
            }
            catch (Exception ex)
            {
                TotalValueCredit = 0;
                TotalValueDepit = 0;
            }
            return Tuple.Create(TotalValueCredit, TotalValueDepit);
        }

        public string GET_UUID()
        {
            System.Guid guid = System.Guid.NewGuid();
            string UUIDStr = guid.ToString();
            return UUIDStr;
        }


        public Task<List<InvoicesVM>> GetFinancialfollowup(string Con, FinancialfollowupVM _financialfollowupVM)
        {
            var Invoices = _InvoicesRepository.GetFinancialfollowup(Con, _financialfollowupVM);
            return Invoices!;
        }
        public async Task<List<InvoicesVM>> GetInvoiceByCustomer(int CustomerId,int YearId)
        {
           return await _InvoicesRepository.GetInvoiceByCustomer(CustomerId, YearId);
          
        }

        public async Task<InvoicesVM> GetInvoiceByNo(string VocherNo, int YearId)
        {

            return await _InvoicesRepository.GetInvoiceByNo(VocherNo, YearId);


        }
        public async Task<InvoicesVM> GetInvoiceByNo_purches(string VocherNo, int YearId)
        {

            return await _InvoicesRepository.GetInvoiceByNo_purches(VocherNo, YearId);


        }


        public string GetVoucherType(int vouchertype)
        {
            string TypeName = "";
            if (vouchertype == 1)
            {
                TypeName = "فاتورة مشتريات";
            }
            else if (vouchertype == 2)
            {
                TypeName = "فاتورة مبيعات";
            }
            else if (vouchertype == 3)
            {
                TypeName = "مردود مشتريات";
            }
            else if (vouchertype == 4)
            {
                TypeName = "مردود المبيعات";
            }
            else if (vouchertype == 5)
            {
                TypeName = "سند صرف";
            }
            else if (vouchertype == 6)
            {
                TypeName = "سند قبض";
            }
            else if (vouchertype == 7)
            {
                TypeName = "سند جرد";
            }
            else if (vouchertype == 8)
            {
                TypeName = "سند يومية";
            }
            else if (vouchertype == 9)
            {
                TypeName = "سند قبض فاتورة مبيعات معلقة";
            }
            else if (vouchertype == 10)
            {
                TypeName = "سند افتتاحي";
            }
            else if (vouchertype == 11)
            {
                TypeName = "سند نقل";
            }
            else if (vouchertype == 12)
            {
                TypeName = "عقد";
            }
            else if (vouchertype == 13)
            {
                TypeName = "مصروفات متوقعه";
            }
            else if (vouchertype == 14)
            {
                TypeName = "ايرادات متوقعه";
            }
            else if (vouchertype == 15)
            {
                TypeName = "ضمان بنكي";
            }
            else if (vouchertype == 16)
            {
                TypeName = "سند تحويل";
            }
            else if (vouchertype == 17)
            {
                TypeName = "ضريبه";
            }
            else if (vouchertype == 18)
            {
                TypeName = "دفعة عقد";
            }
            else if (vouchertype == 19)
            {
                TypeName = "رصيد افتتاحي";
            }
            else if (vouchertype == 20)
            {
                TypeName = "مكافأة";
            }
            else if (vouchertype == 21)
            {
                TypeName = "خصم";
            }
            else if (vouchertype == 22)
            {
                TypeName = "سلفة";
            }
            else if (vouchertype == 23)
            {
                TypeName = "مردود المصروفات";
            }
            else if (vouchertype == 24)
            {
                TypeName = "سندات المسير";
            }
            else if (vouchertype == 25)
            {
                TypeName = "قيد اقفال";
            }
            else if (vouchertype == 26)
            {
                TypeName = "قيد تدوير افتتاحي";
            }
            else if (vouchertype == 27)
            {
                TypeName = "قيد صرف مشروع";
            }
            else if (vouchertype == 28)
            {
                TypeName = "حركة سيارة";
            }
            else if (vouchertype == 29)
            {
                TypeName = "إشعار دائن";
            }
            else if (vouchertype == 30)
            {
                TypeName = "إشعار مدين";
            }
            else if (vouchertype == 31)
            {
                TypeName = "رصيد حساب افتتاحي";
            }
            else if (vouchertype == 32)
            {
                TypeName = "إشعار دائن";
            }
            else if (vouchertype == 33)
            {
                TypeName = "إشعار مدين";
            }
            return TypeName;
        }
    }
}

