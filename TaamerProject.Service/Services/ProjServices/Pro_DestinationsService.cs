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

namespace TaamerProject.Service.Services
{
    public class Pro_DestinationsService : IPro_DestinationsService
    {
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly ISystemAction _SystemAction;
        private readonly IPro_DestinationsRepository _Pro_DestinationsRepository;
        public Pro_DestinationsService(IPro_DestinationsRepository pro_DestinationsRepository
            , TaamerProjectContext dataContext, ISystemAction systemAction)
        {
            _TaamerProContext = dataContext; _SystemAction = systemAction;
            _Pro_DestinationsRepository = pro_DestinationsRepository;
        }

        public Task<IEnumerable<Pro_DestinationsVM>> GetAllDestinations()
        {
            var Destinations = _Pro_DestinationsRepository.GetAllDestinations();
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
        public GeneralMessage SaveDestination(Pro_Destinations Destination, int UserId, int BranchId)
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
        public GeneralMessage SaveDestinationReplay(Pro_Destinations Destination, int UserId, int BranchId)
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

                }

                _TaamerProContext.SaveChanges();
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
    }
}
