using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaamerProject.Models.Common;
using TaamerProject.Models.DomainObjects;
using TaamerProject.Models.ViewModels;

namespace TaamerProject.Service.Interfaces
{
    public interface IContactListsService
    {
        Task<IEnumerable<ContactListVM>> GetContactLists(int Id, int Type);
        GeneralMessage SaveContact(ContactList contact, int UserId, int BranchId);

    }
}
