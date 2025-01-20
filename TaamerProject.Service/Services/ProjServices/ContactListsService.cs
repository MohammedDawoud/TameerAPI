using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models.Common;
using TaamerProject.Models;
using TaamerProject.Models.DBContext;
using TaamerProject.Models.ViewModels;
using TaamerProject.Repository.Interfaces;
using TaamerProject.Repository.Repositories;
using TaamerProject.Service.IGeneric;
using TaamerProject.Service.Interfaces;
using TaamerProject.Service.LocalResources;
using TaamerProject.Models.DomainObjects;

namespace TaamerProject.Service.Services.ProjServices
{
    public class ContactListsService : IContactListsService
    {
        private readonly TaamerProjectContext _TaamerProContext;
        private readonly ISystemAction _SystemAction;
        private readonly IContactListRepository _contactList;



        public ContactListsService(TaamerProjectContext dataContext, ISystemAction systemAction, IContactListRepository contactList)
        {
            _TaamerProContext = dataContext;
            _SystemAction = systemAction;
            _contactList = contactList;

        }
        public async Task<IEnumerable<ContactListVM>> GetContactLists(int Id, int Type)
        {
            return await _contactList.GetContactLists(Id, Type);
        }
        public GeneralMessage SaveContact(ContactList contact, int UserId, int BranchId)
        {
            try
            {
                if (contact !=null)
                {
                    contact.AddUser = UserId;
                    contact.AddDate = DateTime.Now;
                    contact.UserId = UserId;
                    contact.ContactDate= DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss:tt", CultureInfo.CreateSpecificCulture("en"));
                    _TaamerProContext.ContactLists.Add(contact);
                    //-----------------------------------------------------------------------------------------------------------------
                    string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                    string ActionNote = "اضافة  تعليق";
                    _SystemAction.SaveAction("SaveContact", "ContactService", 1, Resources.General_SavedSuccessfully, "", "", ActionDate, UserId, BranchId, ActionNote, 1);
                    //-----------------------------------------------------------------------------------------------------------------
                    return new GeneralMessage { StatusCode = HttpStatusCode.OK, ReasonPhrase = Resources.General_SavedSuccessfully };
                }
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };


            }
            catch (Exception ex)
            {
                //-----------------------------------------------------------------------------------------------------------------
                string ActionDate = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.CreateSpecificCulture("en"));
                string ActionNote = "فشل في حفظ تعليق";
                _SystemAction.SaveAction("SaveContact", "ContactService", 1, Resources.General_SavedFailed, "", "", ActionDate, UserId, BranchId, ActionNote, 0);
                //-----------------------------------------------------------------------------------------------------------------
                return new GeneralMessage { StatusCode = HttpStatusCode.BadRequest, ReasonPhrase = Resources.General_SavedFailed };
            }
        }
    }
}
