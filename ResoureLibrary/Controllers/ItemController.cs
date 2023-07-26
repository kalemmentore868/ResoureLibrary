/*
' Copyright (c) 2023 Christoc.com
'  All rights reserved.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
' TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
' THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
' CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
' DEALINGS IN THE SOFTWARE.
' 
*/

using Christoc.Modules.ResoureLibrary.Components;
using Christoc.Modules.ResoureLibrary.Models;
using DotNetNuke.Entities.Users;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Web.Mvc.Framework.ActionFilters;
using DotNetNuke.Web.Mvc.Framework.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Christoc.Modules.ResoureLibrary.Controllers
{
    [DnnHandleError]
    public class ItemController : DnnController
    {

        private List<SelectListItem> GetResourceTypes()
        {
            // You can replace these with your desired resource types or add more as needed
            var resourceTypes = new List<string>
    {
        "pdf", "word doc", "external article", "internal article"
    };

            var selectList = resourceTypes.Select(type => new SelectListItem
            {
                Text = type,
                Value = type
            }).ToList();

            return selectList;
        }

        public ActionResult Delete(int itemId)
        {
            ItemManager.Instance.DeleteItem(itemId, ModuleContext.ModuleId);
            return RedirectToDefaultRoute();
        }

        public ActionResult Edit(int itemId = -1)
        {
            DotNetNuke.Framework.JavaScriptLibraries.JavaScript.RequestRegistration(CommonJs.DnnPlugins);

            var userlist = UserController.GetUsers(PortalSettings.PortalId);
            var users = from user in userlist.Cast<UserInfo>().ToList()
                        select new SelectListItem { Text = user.DisplayName, Value = user.UserID.ToString() };

            ViewBag.Users = users;
            ViewBag.ResourceTypes = GetResourceTypes();

            var item = (itemId == -1)
                 ? new Item { ModuleId = ModuleContext.ModuleId }
                 : ItemManager.Instance.GetItem(itemId, ModuleContext.ModuleId);

            return View(item);
        }

        [HttpPost]
        [DotNetNuke.Web.Mvc.Framework.ActionFilters.ValidateAntiForgeryToken]
        public ActionResult Edit(Item item)
        {
            if (item.ItemId == -1)
            {
                item.CreatedByUserId = User.UserID;
                item.CreatedOnDate = DateTime.UtcNow;
                item.LastModifiedByUserId = User.UserID;
                item.LastModifiedOnDate = DateTime.UtcNow;

                ItemManager.Instance.CreateItem(item);
            }
            else
            {
                var existingItem = ItemManager.Instance.GetItem(item.ItemId, item.ModuleId);
                existingItem.LastModifiedByUserId = User.UserID;
                existingItem.LastModifiedOnDate = DateTime.UtcNow;
                existingItem.ItemName = item.ItemName;
                existingItem.ItemDescription = item.ItemDescription;
                existingItem.AssignedUserId = item.AssignedUserId;

                // Update the new properties
                existingItem.Categories = item.Categories;
                existingItem.Tags = item.Tags;
                existingItem.ImgUrl = item.ImgUrl;
                existingItem.FileUrl = item.FileUrl;
                existingItem.ItemLongDescription = item.ItemLongDescription;
                existingItem.ResourceType = item.ResourceType;

                ItemManager.Instance.UpdateItem(existingItem);
            }

            ViewBag.ResourceTypes = GetResourceTypes();

            return RedirectToDefaultRoute();
        }


        [ModuleAction(ControlKey = "Edit", TitleKey = "AddItem")]
        public ActionResult Index(string searchTerm, string mediaType = "", string sortBy = "relevance")
        {
            var items = ItemManager.Instance.GetItems(ModuleContext.ModuleId);

            // Filter items based on the mediaType
            if (!string.IsNullOrEmpty(mediaType))
            {
                items = items.Where(item => item.ResourceType.Equals(mediaType, StringComparison.OrdinalIgnoreCase));
            }

            // Search items based on the searchTerm
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLowerInvariant();
                items = items.Where(item =>
                    item.ItemName.ToLowerInvariant().Contains(searchTerm) ||
                    item.Categories.ToLowerInvariant().Contains(searchTerm) ||
                    item.Tags.ToLowerInvariant().Contains(searchTerm)
                );
            }

            // Apply sorting based on the sortBy value
            if (sortBy == "newest")
            {
                items = items.OrderByDescending(item => item.CreatedOnDate);
            }
            else if (sortBy == "oldest")
            {
                items = items.OrderBy(item => item.CreatedOnDate);
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.MediaType = mediaType;
            ViewBag.SortBy = sortBy;

            return View(items);
        }

        public ActionResult Details(int itemId)
        {
            var item = ItemManager.Instance.GetItem(itemId, ModuleContext.ModuleId);
            if (item == null)
            {
                // Item not found, handle the error or redirect to a 404 page
                return HttpNotFound();
            }

            return View(item);
        }







    }
}
