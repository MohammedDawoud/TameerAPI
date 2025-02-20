using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models.Common;
using TaamerProject.Models.DBContext;
using TaamerProject.Models;
using TaamerProject.Repository.Interfaces;
using TaamerProject.Service.IGeneric;
using TaamerProject.Service.Interfaces;
using TaamerProject.Service.LocalResources;
using System.Net.Mail;
using System.Net.Mime;
using TaamerProject.Repository.Repositories;

namespace TaamerProject.Service.Services
{
    public class Pro_DestinationsService : IPro_DestinationsService
    {
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly ISystemAction _SystemAction;
        private readonly IPro_DestinationsRepository _Pro_DestinationsRepository;
        private readonly IEmailSettingRepository _EmailSettingRepository;
        private readonly IUsersRepository _IUsersRepository;
        private readonly INotificationService _notificationService;
        public Pro_DestinationsService(IPro_DestinationsRepository pro_DestinationsRepository
            , TaamerProjectContext dataContext, ISystemAction systemAction, IEmailSettingRepository emailSettingRepository, IUsersRepository iUsersRepository, INotificationService notificationService)
        {
            _TaamerProContext = dataContext; _SystemAction = systemAction;
            _Pro_DestinationsRepository = pro_DestinationsRepository;
            _EmailSettingRepository = emailSettingRepository;
            _IUsersRepository = iUsersRepository;
            _notificationService = notificationService;
        }

        public Task<IEnumerable<Pro_DestinationsVM>> GetAllDestinations(int BranchId)
        {
            var Destinations = _Pro_DestinationsRepository.GetAllDestinations(BranchId);
            return Destinations;
        }
        public Task<Pro_DestinationsVM> GetDestinationByProjectId(int projectId)
        {
            var Destinations = _Pro_DestinationsRepository.GetDestinationByProjectId(projectId);
            return Destinations;
        }
        public Task<Pro_DestinationsVM> GetDestinationByProjectIdToReplay(int projectId)
        {
            var Destinations = _Pro_DestinationsRepository.GetDestinationByProjectIdToReplay(projectId);
            return Destinations;
        }
        public GeneralMessage SaveDestination(Pro_Destinations Destination, int UserId, int BranchId, OrganizationsVM Organization, string Url, string ImgUrl)
        {
            try
            {

                var settings = _TaamerProContext.SystemSettings.Where(s => s.IsDeleted == false).FirstOrDefault();

                if (settings.DestinationCheckCode == Destination.Checkcode)
                {
                    settings.DestinationCheckCode = null;
                }
                else
                {
                    return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "كود التفعيل غير صحيح" };
                }


                if (Destination.DestinationId==0)
                {
                    Destination.BranchId = BranchId;
                    Destination.Status = 1;
                    Destination.AddUser = UserId;
                    Destination.AddDate = DateTime.Now;

                    var DATE = (Destination.AddFileDate ?? new DateTime()).ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    DateTime AccDate = DateTime.ParseExact(DATE + " " + Destination.AddFileDateTime, "yyyy-MM-dd h:mm tt", CultureInfo.InvariantCulture);
                    Destination.AddFileDate = AccDate;


                    _TaamerProContext.Pro_Destinations.Add(Destination);
                    _TaamerProContext.SaveChanges();
                    var Project = _TaamerProContext.Project.Where(s => s.ProjectId == Destination.ProjectId).FirstOrDefault();
                    if (Project != null)
                    {
                        Project.DestinationsUpload = Destination.DestinationId;
                    }
                    _TaamerProContext.SaveChanges();

                    #region  sending email and notifications
                    var strbody = "";
                    var distinationtype = _TaamerProContext.Pro_DestinationTypes.Where(x => x.DestinationTypeId == Destination.DestinationTypeId).FirstOrDefault();

                    var headertxt = "تم رفع المشروع الي  " + distinationtype.NameAr;
                    var subject = "رفع مشروع الي " + distinationtype.NameAr;
                    var customer = _TaamerProContext.Customer.Where(x => x.CustomerId == Project.CustomerId).FirstOrDefault();
                    var Manager = _TaamerProContext.Users.Where(x => x.UserId == Project.MangerId).FirstOrDefault();
                    var branch = _TaamerProContext.Branch.Where(x => x.BranchId == Project.BranchId).FirstOrDefault();

                    var notitxt = "تم رفع مشروع رقم " + Project.ProjectNo + "الي " + distinationtype.NameAr;
                    //var listusers = new List<int>();
                    var listusers = _TaamerProContext.ProjectWorkers.Where(x => x.IsDeleted == false && x.ProjectId == Project.ProjectId).ToList().Select(x => x.UserId).Distinct().ToList();
                    var tasksuser = _TaamerProContext.ProjectPhasesTasks.Where(x => x.IsDeleted == false && x.ProjectId == Project.ProjectId && x.Type==3 && x.Status !=4).Distinct().ToList().Select(x=>x.UserId).ToList();
                    strbody = @"<!DOCTYPE html><html lang = '' ><head>
                                                <style>
                                                .square {
                                                    height: 35px;width: 35px;background-color: #ffffff;border: ridge;
                                                    text-align: center;align-content: center;font-size: 28px;}
                                                </style>
                                                </ head >
                            <body>                  
                            <h3 style = 'text-align:center;' > " + headertxt + @"</h3>
                     
                            <table align = 'center' border = '1' ><tr> <td>  رقم المشروع</td><td>" + Project.ProjectNo + @"</td> </tr> <tr> <td> اسم العميل  </td> <td>" + customer.CustomerNameAr + @"</td>
                             </tr>  <tr> <td> الفرع</td> <td>" + branch.NameAr + @"</td></tr><tr> <td> مدير المشروع</td> <td>" + Manager.FullNameAr + @"</td></tr><tr> <td> إسم الجهة الخارجية</td> <td>" + distinationtype.NameAr + @"</td></tr></table> <h7> مع تحيات قسم ادارة المشاريع</h7>                         
                            </ body ></ html > ";
                    listusers.AddRange(tasksuser);
                    if (listusers != null && listusers.Count() > 0)
                    {
                        foreach (var usr in listusers)
                        {


                            var issent = SendMail_Destination(Organization, branch, UserId, usr.Value, subject, strbody, Url, ImgUrl, 1, true).Result;

                            var ListOfPrivNotify = new List<Notification>();
                            ListOfPrivNotify.Add(new Notification
                            {
                                ReceiveUserId = usr.Value,
                                Name = subject,
                                Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CreateSpecificCulture("en")),
                                HijriDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CreateSpecificCulture("ar")),
                                SendUserId = 1,
                                Type = 1,
                                Description = notitxt,
                                AllUsers = false,
                                SendDate = DateTime.Now,
                                ProjectId = Project.ProjectId,
                                TaskId = 0,
                                AddUser = UserId,
                                BranchId = branch.BranchId,
                                AddDate = DateTime.Now,
                                IsHidden = false,
                                NextTime = null,
                            });
                            _TaamerProContext.Notification.AddRange(ListOfPrivNotify);
                            _TaamerProContext.SaveChanges();
                            _notificationService.sendmobilenotification(usr.Value, subject, notitxt);

                        }
                    }
                    #endregion
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote2 = "رفع لجهه حكومية";
                    _SystemAction.SaveAction("SaveDestination", "Pro_DestinationsService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate2, UserId, BranchId, ActionNote2, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }

                //var DestinationsUpdated = _TaamerProContext.Pro_Destinations.Where(s => s.ProjectId == Destination.ProjectId).FirstOrDefault();
                //if (DestinationsUpdated == null)
                //{
                //    Destination.DestinationId = 0;
                //    Destination.BranchId = BranchId;
                //    Destination.Status = 1;
                //    Destination.AddUser = UserId;
                //    Destination.AddDate = DateTime.Now;

                //    var DATE = (Destination.AddFileDate??new DateTime()).ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                //    DateTime AccDate = DateTime.ParseExact(DATE + " " + Destination.AddFileDateTime, "yyyy-MM-dd h:mm tt", CultureInfo.InvariantCulture);
                //    Destination.AddFileDate = AccDate;


                //    _TaamerProContext.Pro_Destinations.Add(Destination);
                //    _TaamerProContext.SaveChanges();
                //    var Project = _TaamerProContext.Project.Where(s => s.ProjectId == Destination.ProjectId).FirstOrDefault();
                //    if (Project != null)
                //    {
                //        Project.DestinationsUpload = Destination.DestinationId;
                //    }
                //    _TaamerProContext.SaveChanges();
                //    //-----------------------------------------------------------------------------------------------------------------
                //    string ActionDate2 = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                //    string ActionNote2 = "رفع لجهه حكومية";
                //    _SystemAction.SaveAction("SaveDestination", "Pro_DestinationsService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate2, UserId, BranchId, ActionNote2, 1);
                //    //-----------------------------------------------------------------------------------------------------------------
                //    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                //}
                //else
                //{
                //    DestinationsUpdated.ProjectId = Destination.ProjectId;
                //    if (Destination.TransactionNumber != null)
                //        DestinationsUpdated.TransactionNumber = Destination.TransactionNumber;
                //    if (Destination.DestinationTypeId != null)
                //        DestinationsUpdated.DestinationTypeId = Destination.DestinationTypeId;
                //    if (Destination.DepartmentId != null)
                //        DestinationsUpdated.DepartmentId = Destination.DepartmentId;
                //    if (Destination.FileName != null)
                //        DestinationsUpdated.FileName = Destination.FileName;
                //    if (Destination.UserId != null)
                //        DestinationsUpdated.UserId = Destination.UserId;
                //    DestinationsUpdated.Counter = (DestinationsUpdated.Counter ?? 0) + 1;
                //    if (Destination.FileReasonId != null)
                //        DestinationsUpdated.FileReasonId = Destination.FileReasonId;
                //    if (Destination.Notes != null)
                //        DestinationsUpdated.Notes = Destination.Notes;
                //    DestinationsUpdated.BranchId = BranchId;
                //    DestinationsUpdated.UpdateUser = UserId;
                //    DestinationsUpdated.UpdateDate = DateTime.Now;

                //    if (Destination.AddFileDate != null)
                //    {
                //        var DATE = (Destination.AddFileDate ?? new DateTime()).ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                //        DateTime AccDate = DateTime.ParseExact(DATE + " " + Destination.AddFileDateTime, "yyyy-MM-dd h:mm tt", CultureInfo.InvariantCulture);
                //        Destination.AddFileDate = AccDate;
                //    }


                //}
                //_TaamerProContext.SaveChanges();
                ////-----------------------------------------------------------------------------------------------------------------
                //string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                //string ActionNote = " تعديل ملف لجهه حكومية رقم " + Destination.DestinationId;
                //_SystemAction.SaveAction("SaveDestination", "Pro_DestinationsService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                ////-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };

            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في رفع ملف لجهة حكومية";
                _SystemAction.SaveAction("SaveDestination", "Pro_DestinationsService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
        public GeneralMessage SaveDestinationReplay(Pro_Destinations Destination, int UserId, int BranchId, OrganizationsVM Organization, string Url, string ImgUrl)
        {
            try
            {

                var DestinationsUpdated = _TaamerProContext.Pro_Destinations.Where(s => s.IsDeleted==false && s.ProjectId == Destination.ProjectId && (s.Status==1 || s.Status == 0 || s.Status == null)).FirstOrDefault();
                if (DestinationsUpdated != null)
                {
                    DestinationsUpdated.ProjectId = Destination.ProjectId;

                    if (Destination.UserIdRec != null)
                        DestinationsUpdated.UserIdRec = Destination.UserIdRec;
                    DestinationsUpdated.CounterRec = (DestinationsUpdated.CounterRec ?? 0) + 1;

                    if (Destination.NotesRec != null)
                        DestinationsUpdated.NotesRec = Destination.NotesRec;
                    if (Destination.Status != null)
                        DestinationsUpdated.Status = Destination.Status??1;
                    DestinationsUpdated.BranchId = BranchId;
                    DestinationsUpdated.UpdateUser = UserId;
                    DestinationsUpdated.UpdateDate = DateTime.Now;


                    if (Destination.AddFileDateRec != null)
                    {
                        var DATE = (Destination.AddFileDateRec ?? new DateTime()).ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                        DateTime AccDate = DateTime.ParseExact(DATE + " " + Destination.AddFileDateRecTime, "yyyy-MM-dd h:mm tt", CultureInfo.InvariantCulture);
                        DestinationsUpdated.AddFileDateRec = AccDate;
                    }


                    if (Destination.AddFileDateRec <= DestinationsUpdated.AddFileDate) {
                        return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = "تأكد من ان يكون تاريخ الاستلام بعد تاريخ الرفع "+ DestinationsUpdated.AddFileDate };
                    }


                    var Proj = _TaamerProContext.Project.Where(s => s.ProjectId == Destination.ProjectId).FirstOrDefault();
                    if (Proj != null)
                    {
                        var Days = Math.Ceiling(((DestinationsUpdated.AddFileDateRec ?? new DateTime()) - (DestinationsUpdated.AddFileDate ?? new DateTime())).TotalDays);
                        //var AccDays = Math.Ceiling(Days);
                        var ProDate = Convert.ToDateTime(Proj.ProjectExpireDate).AddDays(Days);
                        var AccProDate = ProDate.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));

                        Proj.ProjectExpireDate = AccProDate;
                        Proj.FirstProjectExpireDate = AccProDate;
                        Proj.DestinationsUpload = null;
                    }
                    _TaamerProContext.SaveChanges();

                    #region  sending email and notifications
                    var strbody = "";
                    var distinationtype = _TaamerProContext.Pro_DestinationTypes.Where(x => x.DestinationTypeId == DestinationsUpdated.DestinationTypeId).FirstOrDefault();

                    var headertxt = "تم رد  " + distinationtype.NameAr;
                    var subject = "رد" + distinationtype.NameAr + " علي المشروع";
                    var customer = _TaamerProContext.Customer.Where(x => x.CustomerId == Proj.CustomerId).FirstOrDefault();
                    var Manager = _TaamerProContext.Users.Where(x => x.UserId == Proj.MangerId).FirstOrDefault();
                    var branch = _TaamerProContext.Branch.Where(x => x.BranchId == Proj.BranchId).FirstOrDefault();
                    //var listusers = new List<int>();
                    var listusers = _TaamerProContext.ProjectWorkers.Where(x => x.IsDeleted == false && x.ProjectId == Proj.ProjectId).ToList().Select(x => x.UserId).Distinct().ToList();
                    var tasksuser = _TaamerProContext.ProjectPhasesTasks.Where(x => x.IsDeleted == false && x.ProjectId == Proj.ProjectId && x.Type == 3 && x.Status != 4).Distinct().ToList().Select(x => x.UserId).ToList();

                    var status = DestinationsUpdated.Status == 2 ? "موافقة" : "رفض";
                    var notitxt = " تم رد  " + distinationtype.NameAr + "بال " + status + "علي مشروع رقم" + Proj.ProjectNo;

                    strbody = @"<!DOCTYPE html><html lang = '' ><head>
                                                <style>
                                                .square {
                                                    height: 35px;width: 35px;background-color: #ffffff;border: ridge;
                                                    text-align: center;align-content: center;font-size: 28px;}
                                                </style>
                                                </ head >
                            <body>                  
                            <h3 style = 'text-align:center;' > " + notitxt + @"</h3>
                     
                            <table align = 'center' border = '1' ><tr> <td>  رقم المشروع</td><td>" + Proj.ProjectNo + @"</td> </tr> <tr> <td> اسم العميل  </td> <td>" + customer.CustomerNameAr + @"</td>
                             </tr>  <tr> <td> الفرع</td> <td>" + branch.NameAr + @"</td></tr><tr> <td> مدير المشروع</td> <td>" + Manager.FullNameAr + @"</td></tr><tr> <td> إسم الجهة الخارجية</td> <td>" + distinationtype.NameAr + @"</td></tr><tr> <td> حالة الرد </td> <td>" + status + @"</td></tr></table> <h7> مع تحيات قسم ادارة المشاريع</h7>                         
                            </ body ></ html > ";
                    listusers.AddRange(tasksuser);

                    if (listusers != null && listusers.Count() > 0)
                    {
                        foreach (var usr in listusers)
                        {


                            var issent = SendMail_Destination(Organization, branch, UserId, usr.Value, subject, strbody, Url, ImgUrl, 1, true).Result;

                            //var ListOfPrivNotify = new List<Notification>();
                            //ListOfPrivNotify.Add(new Notification
                            //{
                            //    ReceiveUserId = usr.Value,
                            //    Name = subject,
                            //    Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CreateSpecificCulture("en")),
                            //    HijriDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CreateSpecificCulture("ar")),
                            //    SendUserId = 1,
                            //    Type = 1,
                            //    Description = notitxt,
                            //    AllUsers = false,
                            //    SendDate = DateTime.Now,
                            //    ProjectId = Proj.ProjectId,
                            //    TaskId = 0,
                            //    AddUser = UserId,
                            //    BranchId = branch.BranchId,
                            //    AddDate = DateTime.Now,
                            //    IsHidden = false,
                            //    NextTime = null,
                            //});
                            //_TaamerProContext.Notification.AddRange(ListOfPrivNotify);
                           // _TaamerProContext.SaveChanges();
                           // _notificationService.sendmobilenotification(usr.Value, subject, notitxt);

                        }
                    }
                    #endregion

                }

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " تعديل ملف لجهه حكومية رقم " + Destination.DestinationId;
                _SystemAction.SaveAction("SaveDestination", "Pro_DestinationsService", 2, Resources.General_EditedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };

            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في رفع ملف لجهة حكومية";
                _SystemAction.SaveAction("SaveDestination", "Pro_DestinationsService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }

        public GeneralMessage DeleteDestination(int DestinationId, int UserId, int BranchId)
        {
            try
            {
                Pro_Destinations? Destination = _TaamerProContext.Pro_Destinations.Where(s => s.DestinationId == DestinationId).FirstOrDefault();
                if (Destination != null)
                {
                    Destination.IsDeleted = true;
                    Destination.DeleteDate = DateTime.Now;
                    Destination.DeleteUser = UserId;

                    var DestinationsUpdated = _TaamerProContext.Pro_Destinations.Where(s => s.IsDeleted==false && s.ProjectId == Destination.ProjectId && (s.Status == 1 || s.Status == 0 || s.Status == null)).FirstOrDefault();
                    if(DestinationsUpdated==null)
                    {
                        var proj = _TaamerProContext.Project.Where(s => s.ProjectId == Destination.ProjectId).FirstOrDefault();
                        if (proj != null)
                        {
                            proj.DestinationsUpload = null;
                        }
                    }
                  
                    _TaamerProContext.SaveChanges();

                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = " حذف ملف لجهه حكومية رقم " + DestinationId;
                    _SystemAction.SaveAction("DeleteDestination", "Pro_DestinationsService", 3, Resources.General_DeletedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------

                }
                return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_DeletedSuccessfully };

            }
            catch (Exception ex)
            {

                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = " فشل في رفع ملف لجهه حكومية رقم " + DestinationId; ;
                _SystemAction.SaveAction("DeleteDestination", "Pro_DestinationsService", 3, Resources.General_DeletedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------

                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_DeletedFailed };
            }
        }


        public async Task<bool> SendMail_Destination(OrganizationsVM org, Branch branch, int UserId, int ReceivedUser, string Subject, string textBody, string Url, string ImgUrl, int type, bool IsBodyHtml = false)
        {
            try
            {
                string formattedDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                var mail = new MailMessage();
                var email = _EmailSettingRepository.GetEmailSetting(branch?.OrganizationId ?? 0).Result.SenderEmail;
                var loginInfo = new NetworkCredential(_EmailSettingRepository.GetEmailSetting(branch?.OrganizationId ?? 0).Result.SenderEmail, _EmailSettingRepository.GetEmailSetting(branch?.OrganizationId ?? 0).Result.Password);
                // mail.From = new MailAddress(_EmailSettingRepository.GetEmailSetting(branch).SenderEmail);
                if (_EmailSettingRepository.GetEmailSetting(branch?.OrganizationId ?? 0).Result.DisplayName != null)
                {
                    mail.From = new MailAddress(email, _EmailSettingRepository.GetEmailSetting(branch?.OrganizationId ?? 0).Result.DisplayName);
                }
                else
                {
                    mail.From = new MailAddress(email, "لديك اشعار من نظام تعمير السحابي");
                }
                var title = "لقد طلبت رفع ملف لجهة خارجية";
                var body = "";
                title = "";
                body = PopulateBody(textBody, _IUsersRepository.GetUserById(ReceivedUser, "rtl").Result.FullName, title, "مع تمنياتنا لكم بالتوفيق", Url, org.NameAr);


                LinkedResource logo = new LinkedResource(ImgUrl);
                logo.ContentId = "companylogo";
                AlternateView av1 = AlternateView.CreateAlternateViewFromString(body.Replace("{Header}", title), null, MediaTypeNames.Text.Html);
                av1.LinkedResources.Add(logo);
                mail.AlternateViews.Add(av1);


                var userReciver = _TaamerProContext.Users.Where(s => s.UserId == ReceivedUser).FirstOrDefault();
                mail.To.Add(new MailAddress(userReciver?.Email ?? ""));

                mail.Subject = Subject;

                mail.Body = textBody;
                mail.IsBodyHtml = IsBodyHtml;
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var smtpClient = new SmtpClient(_EmailSettingRepository.GetEmailSetting(branch?.OrganizationId ?? 0).Result.Host);
                smtpClient.EnableSsl = true;
                smtpClient.UseDefaultCredentials = false;
                //smtpClient.Port = 587;
                smtpClient.Port = Convert.ToInt32(_EmailSettingRepository.GetEmailSetting(branch?.OrganizationId ?? 0).Result.Port);

                smtpClient.Credentials = loginInfo;
                smtpClient.Send(mail);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }



        public string PopulateBody(string bodytxt, string fullname, string header, string footer, string url, string orgname)
        {
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(url))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{FullName}", fullname);
            body = body.Replace("{Body}", bodytxt);
            body = body.Replace("{Header}", header);
            body = body.Replace("{Footer}", footer);
            body = body.Replace("{orgname}", orgname);
            return body;
        }

    }
}
