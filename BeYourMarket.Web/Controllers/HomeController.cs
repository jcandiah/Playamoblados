using BeYourMarket.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BeYourMarket.Web.Extensions;
using BeYourMarket.Web.Utilities;
using System.Threading.Tasks;
using BeYourMarket.Model.Models;
using BeYourMarket.Web.Models;
using PagedList;
using BeYourMarket.Web.Models.Grids;
using i18n;
using i18n.Helpers;
using System.Data.Entity;
using BeYourMarket.Core.Web;

namespace BeYourMarket.Web.Controllers
{
    public class HomeController : Controller
    {
        #region Fields
        private readonly ICategoryService _categoryService;
        private readonly IListingService _listingService;
        private readonly IContentPageService _contentPageService;
        #endregion

        #region Constructor
        public HomeController(
            ICategoryService categoryService,
            IListingService listingService,
            IContentPageService contentPageService)
        {
            _categoryService = categoryService;
            _listingService = listingService;
            _contentPageService = contentPageService;
        }
        #endregion

        #region Methods
        public async Task<ActionResult> Index(string id)
        {
            if (!string.IsNullOrEmpty(id))
                return RedirectToAction("ContentPage", "Home", new { id = id.ToLowerInvariant() });

            var model = new SearchListingModel();
            model.ListingTypeID = CacheHelper.ListingTypes.Select(x => x.ID).ToList();
            //await GetSearchResult(model);

            return View(model);
        }
        public async Task<ActionResult> ContentPage(string id)
        {
            if (string.IsNullOrEmpty(id))
                return RedirectToAction("Index", "Home");

            var slug = id.ToLowerInvariant();
            var contentPageQuery = await _contentPageService.Query(x => x.Slug == slug && x.Published).SelectAsync();
            var contentPage = contentPageQuery.FirstOrDefault();

            if (contentPage == null)
            {
                return new HttpNotFoundResult();
            }

            return View(contentPage);
        }
        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }
        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            var model = new ContactModel();

            if (User.Identity.IsAuthenticated)
            {
                model.Email = User.Identity.User().Email;
            }

            return View(model);
        }
        public async Task<ActionResult> Search(SearchListingModel model)
        {
            await GetSearchResult(model);
            //Si las listas vienen vacias
            if (model.ListingsPageList.Count() == 0 && model.ListingsPageList2.Count() == 0)
            {
                TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
                TempData[TempDataKeys.UserMessage] = "[[[La busqueda no arrojo ningún resultado, por favor verifique los campos]]]";
                return View("~/Views/Listing/Listings.cshtml", model);
            }

           if(!string.IsNullOrEmpty(model.string_fromdate))
            {
                model.FromDate = Convert.ToDateTime(model.string_fromdate);
            }
           else
            {
                model.ToDate = null;
            }
                
            if(!string.IsNullOrEmpty(model.string_todate))
            {
                model.ToDate = Convert.ToDateTime(model.string_todate);
            }
            else
            {
                model.ToDate = null;
            }
                Session.Add("fechas", model);
            

            return View("~/Views/Listing/Listings.cshtml", model);
        }
        private async Task GetSearchResult(SearchListingModel model)
        {
            IEnumerable<Listing> items = null;
            IEnumerable<Listing> items2 = null;
            if (model.Niños == null)
                model.Niños = 0;
            // Search Text
            #region Busqueda Texto
            if (!string.IsNullOrEmpty(model.SearchText))
            {
                model.SearchText = model.SearchText.ToLower();

                // Search by title, description, location
                if (items != null)
                {
                    items = items.Where(x =>
                        //x.Title.ToLower().Contains(model.SearchText) ||
                        x.Description.ToLower().Contains(model.SearchText) ||
                        x.Location.ToLower().Contains(model.SearchText));
                }
                else
                {
                    items = await _listingService.Query(
                            x => x.Title.ToLower().Contains(model.SearchText.ToLower()) ||
                            x.Description.ToLower().Contains(model.SearchText.ToLower()) ||
                            x.Location.ToLower().Contains(model.SearchText.ToLower()))
                            .Include(x => x.ListingPictures)
                            .Include(x => x.Category)
                            .Include(x => x.AspNetUser)
                            .Include(x => x.Orders)
                            .Include(x => x.ListingReviews)
                            .SelectAsync();
                }
            }
            #endregion    
                 
            // Search dates
            #region Busqueda Fecha
            if (!string.IsNullOrEmpty(model.FromDate.ToString()) && !string.IsNullOrEmpty(model.ToDate.ToString()))
            {
                    var Todate2 = model.ToDate.Value.AddDays(2);
                    var FromDate2 = model.FromDate.Value.AddDays(-2);

                    if (items != null)
                    {
                        //TODO: Generar las condiciones por fecha
                        items = items.Where(x => x.Orders.Where(y => y.FromDate <= model.ToDate && y.ToDate >= model.FromDate).Count() == 0);
                        items2 = await _listingService.Query(
                                x => x.Orders.Any(y => y.FromDate >= FromDate2 && y.FromDate < model.FromDate ||
                                                        y.ToDate > model.ToDate && y.ToDate <= Todate2))
                                .Include(x => x.ListingPictures)
                                .Include(x => x.Category)
                                .Include(x => x.AspNetUser)
                                .Include(x => x.ListingReviews)
                                .Include(x => x.Orders)
                                .SelectAsync();
                    }
                    else
                    {
                        //Si los campos fecha vienen con datos, genera condicion por FROMDATE y TODATE
                        items = await _listingService.Query(
                                x => x.Orders.Where(y => y.FromDate <= model.ToDate && y.ToDate >= model.FromDate).Count() == 0)
                                .Include(x => x.ListingPictures)
                                .Include(x => x.Category)
                                .Include(x => x.AspNetUser)
                                .Include(x => x.ListingReviews)
                                .Include(x => x.Orders)
                                .SelectAsync();

                        items2 = await _listingService.Query(
                                x => x.Orders.Any(y => y.FromDate >= FromDate2 && y.FromDate < model.FromDate ||
                                                        y.ToDate > model.ToDate && y.ToDate <= Todate2 ||
                                                        y.FromDate <= Todate2 && y.FromDate > model.ToDate ||
                                                        y.ToDate < model.FromDate && y.ToDate >= FromDate2))
                                .Include(x => x.ListingPictures)
                                .Include(x => x.Category)
                                .Include(x => x.AspNetUser)
                                .Include(x => x.ListingReviews)
                                .Include(x => x.Orders)
                                .SelectAsync();
                    }                
            }
            #endregion               
            //Niños
            if (model.Niños > 0)
            {
                items = await _listingService.Query(x => x.Children == true)
                    .Include(x => x.ListingPictures)
                    .Include(x => x.Category)
                    .Include(x => x.AspNetUser)
                    .Include(x => x.ListingReviews)
                    .Include(x => x.Orders).SelectAsync();
                items2 = await _listingService.Query(x => x.Children == true)
                    .Include(x => x.ListingPictures)
                    .Include(x => x.Category)
                    .Include(x => x.AspNetUser)
                    .Include(x => x.ListingReviews)
                    .SelectAsync();
            }

            // Category
            #region Busqueda Condominio
            if (model.CategoryID != 0)
            {
                items = await _listingService.Query(x => x.CategoryID == model.CategoryID)
                    .Include(x => x.ListingPictures)
                    .Include(x => x.Category)
                    .Include(x => x.ListingType)
                    .Include(x => x.AspNetUser)
                    .Include(x => x.Orders)
                    .Include(x => x.ListingReviews)
                    .SelectAsync();

                // Set listing types
                model.ListingTypes = CacheHelper.ListingTypes.Where(x => x.CategoryListingTypes.Any(y => y.CategoryID == model.CategoryID)).ToList();
            }
            else
            {
                model.ListingTypes = CacheHelper.ListingTypes;
            }
            #endregion
            // Set default Listing Type if it's not set or listing type is not set
            if (model.ListingTypes.Count > 0 &&
                (model.ListingTypeID == null || !model.ListingTypes.Any(x => model.ListingTypeID.Contains(x.ID))))
            {
                model.ListingTypeID = new List<int>();
                model.ListingTypeID.Add(model.ListingTypes.FirstOrDefault().ID);
            }
            //Search Passengers
            #region Busqueda Pasajeros
            if (!string.IsNullOrEmpty(model.Passengers.ToString()))
            {
                if (items != null)
                {
                    items = items.Where(x => x.Max_Capacity >= model.Passengers + model.Niños);
                }
                else
                {
                    items = await _listingService.Query(
                            x => x.Max_Capacity >= model.Passengers + model.Niños)
                            .Include(x => x.ListingPictures)
                            .Include(x => x.Category)
                            .Include(x => x.AspNetUser)
                            .Include(x => x.Orders)
                            .Include(x => x.ListingReviews)
                            .SelectAsync();
                }
                if (items2 != null)
                {
                    items2 = items2.Where(x => x.Max_Capacity >= model.Passengers + model.Niños);
                }
            }
            #endregion
            //Search Type of Property
            #region Busqueda por tipo de propiedad
            if (!string.IsNullOrEmpty(model.TypeOfProperty))
            {
                if (items != null)
                {
                    items = items.Where(x => x.TypeOfProperty == model.TypeOfProperty);
                }
                else
                {
                    items = await _listingService.Query(
                            x => x.TypeOfProperty == model.TypeOfProperty)
                            .Include(x => x.ListingPictures)
                            .Include(x => x.Category)
                            .Include(x => x.AspNetUser)
                            .Include(x => x.Orders)
                            .Include(x => x.ListingReviews)
                            .SelectAsync();
                }
            }
            #endregion
            //Search ParkingLot
            #region Busqueda Estacionamientos
            if (!string.IsNullOrEmpty(model.ParkingLot.ToString()))
            {
                if (items != null)
                {
                    items = items.Where(x => x.ParkingLot == model.ParkingLot);
                }
                else
                {
                    items = await _listingService.Query(
                            x => x.ParkingLot == model.ParkingLot)
                            .Include(x => x.ListingPictures)
                            .Include(x => x.Category)
                            .Include(x => x.AspNetUser)
                            .Include(x => x.Orders)
                            .Include(x => x.ListingReviews)
                            .SelectAsync();
                }
            }
            #endregion
            //Search BathRooms
            #region Busqueda Baños
            if (!string.IsNullOrEmpty(model.Bathrooms.ToString()))
            {
                if (items != null)
                {
                    items = items.Where(x => x.Bathrooms == model.Bathrooms);
                }
                else
                {
                    items = await _listingService.Query(
                            x => x.Bathrooms == model.Bathrooms)
                            .Include(x => x.ListingPictures)
                            .Include(x => x.Category)
                            .Include(x => x.AspNetUser)
                            .Include(x => x.Orders)
                            .Include(x => x.ListingReviews)
                            .SelectAsync();
                }
            }
            #endregion
            //Search Rooms
            #region Busqueda Habitaciones
            if (!string.IsNullOrEmpty(model.Rooms.ToString()))
            {
                if (items != null)
                {
                    items = items.Where(x => x.Rooms == model.Rooms);
                }
                else
                {
                    items = await _listingService.Query(
                            x => x.Rooms == model.Rooms)
                            .Include(x => x.ListingPictures)
                            .Include(x => x.Category)
                            .Include(x => x.AspNetUser)
                            .Include(x => x.Orders)
                            .Include(x => x.ListingReviews)
                            .SelectAsync();
                }
            }
            #endregion
            //Search M2
            #region Busqueda M2
            if (!string.IsNullOrEmpty(model.M2From.ToString()))
            {
                if (!string.IsNullOrEmpty(model.M2To.ToString()))
                {
                    if (items != null)
                    {
                        items = items.Where(x => x.M2 >= model.M2From && x.M2 <= model.M2To);
                    }
                    else
                    {
                        items = await _listingService.Query(
                                x => x.M2 >= model.M2From && x.M2 <= model.M2To)
                                .Include(x => x.ListingPictures)
                                .Include(x => x.Category)
                                .Include(x => x.AspNetUser)
                                .Include(x => x.Orders)
                                .Include(x => x.ListingReviews)
                                .SelectAsync();
                    }
                }
                else
                {
                    if (items != null)
                    {
                        items = items.Where(x => x.M2 >= model.M2From);
                    }
                    else
                    {
                        items = await _listingService.Query(
                                x => x.M2 >= model.M2From)
                                .Include(x => x.ListingPictures)
                                .Include(x => x.Category)
                                .Include(x => x.AspNetUser)
                                .Include(x => x.Orders)
                                .Include(x => x.ListingReviews)
                                .SelectAsync();
                    }
                }
            }
            #endregion
            // Latest
            #region BusquedaLatest
            if (items == null)
            {
                items = await _listingService.Query().OrderBy(x => x.OrderByDescending(y => y.Created))
                    .Include(x => x.ListingPictures)
                    .Include(x => x.Category)
                    .Include(x => x.AspNetUser)
                    .Include(x => x.Orders)
                    .Include(x => x.ListingReviews)
                    .SelectAsync();
            }
            #endregion
            // Filter items by Listing Type
            #region Busqueda ListingType
            //items = items.Where(x => model.ListingTypeID.Contains(x.ListingTypeID));
            #endregion
            // Price
            #region Busqueda Precio
            if (model.PriceFrom.HasValue)
                items = items.Where(x => x.Price >= model.PriceFrom.Value);
            if (model.PriceTo.HasValue)
                items = items.Where(x => x.Price <= model.PriceTo.Value);
            #endregion
            // Property
            #region Busqueda Propiedad
            if (model.Property > 0)
                items = items.Where(x => x.ID == model.Property);
            #endregion

            // Show active and enabled only
            var itemsModelList = new List<ListingItemModel>();
            var itemsModelList2 = new List<ListingItemModel>();

			if (items2 != null)
			{
				foreach (var varitem in items)
				{
					//if(items2.Contains(varitem))
					//{
					//    items2.ToList().Remove(varitem);
					//}
					items2 = items2.Where(x => x.ID != varitem.ID);
				}
			}
            
            foreach (var item in items.Where(x => x.Active && x.Enabled).OrderBy(x => x.Price))
            {
                itemsModelList.Add(new ListingItemModel()
                {
                    ListingCurrent = item,
                    UrlPicture = item.ListingPictures.Count == 0 ? ImageHelper.GetListingImagePath(0) : ImageHelper.GetListingImagePath(item.ListingPictures.OrderBy(x => x.Ordering).FirstOrDefault().PictureID)
                });
            }
            if (items2 != null)
            {
                foreach (var item2 in items2.Where(x => x.Active && x.Enabled).OrderBy(x => x.Price))
                {
                    itemsModelList2.Add(new ListingItemModel()
                    {
                        ListingCurrent = item2,
                        UrlPicture = item2.ListingPictures.Count == 0 ? ImageHelper.GetListingImagePath(0) : ImageHelper.GetListingImagePath(item2.ListingPictures.OrderBy(x => x.Ordering).FirstOrDefault().PictureID)
                    });
                }
            }            
            var breadCrumb = GetParents(model.CategoryID).Reverse().ToList();

            model.BreadCrumb = breadCrumb;
            model.Categories = CacheHelper.Categories;

            model.Listings = itemsModelList;
            model.Listings2 = itemsModelList2;
            model.ListingsPageList = itemsModelList.ToPagedList(model.PageNumber, model.PageSize);
            model.ListingsPageList2 = itemsModelList2.ToPagedList(model.PageNumber, model.PageSize);
            model.Grid = new ListingModelGrid(model.ListingsPageList.AsQueryable());
            model.Grid = new ListingModelGrid(model.ListingsPageList2.AsQueryable());
        }
        IEnumerable<Category> GetParents(int categoryId)
        {
            Category category = _categoryService.Find(categoryId);
            while (category != null && category.Parent != category.ID)
            {
                yield return category;
                category = _categoryService.Find(category.Parent);
            }
        }
        [ChildActionOnly]
        public ActionResult NavigationSide()
        {
            var rootId = 0;
            var categories = CacheHelper.Categories.ToList();

            var categoryTree = categories.OrderBy(x => x.Parent).ThenBy(x => x.Ordering).ToList().GenerateTree(x => x.ID, x => x.Parent, rootId);

            var contentPages = CacheHelper.ContentPages.Where(x => x.Published).OrderBy(x => x.Ordering);

            var model = new NavigationSideModel()
            {
                CategoryTree = categoryTree,
                ContentPages = contentPages
            };

            return View("_NavigationSide", model);
        }
        [ChildActionOnly]
        public ActionResult LanguageSelector()
        {
            //var languages = i18n.LanguageHelpers.GetAppLanguages();
            var languages = LanguageHelper.AvailableLanguges.Languages;
            var languageCurrent = ControllerContext.RequestContext.HttpContext.GetPrincipalAppLanguageForRequest();

            var model = new LanguageSelectorModel();
            model.Culture = languageCurrent.GetLanguage();
            model.DisplayName = languageCurrent.GetCultureInfo().NativeName;

            foreach (var language in languages)
            {
                if (language.Culture != languageCurrent.GetLanguage() && language.Enabled)
                {
                    model.LanguageList.Add(new LanguageSelectorModel()
                    {
                        Culture = language.Culture,
                        DisplayName = language.LanguageTag.CultureInfo.NativeName
                    });
                }
            }

            return PartialView("_LanguageSelector", model);
        }
        [AllowAnonymous]
        public ActionResult SetLanguage(string langtag, string returnUrl)
        {
            // If valid 'langtag' passed.
            i18n.LanguageTag lt = i18n.LanguageTag.GetCachedInstance(langtag);
            if (lt.IsValid())
            {
                // Set persistent cookie in the client to remember the language choice.
                Response.Cookies.Add(new HttpCookie("i18n.langtag")
                {
                    Value = lt.ToString(),
                    HttpOnly = true,
                    Expires = DateTime.UtcNow.AddYears(1)
                });
            }
            // Owise...delete any 'language' cookie in the client.
            else
            {
                var cookie = Response.Cookies["i18n.langtag"];
                if (cookie != null)
                {
                    cookie.Value = null;
                    cookie.Expires = DateTime.UtcNow.AddMonths(-1);
                }
            }
            // Update PAL setting so that new language is reflected in any URL patched in the 
            // response (Late URL Localization).
            HttpContext.SetPrincipalAppLanguageForRequest(lt);
            // Patch in the new langtag into any return URL.
            if (returnUrl.IsSet())
            {
                returnUrl = LocalizedApplication.Current.UrlLocalizerForApp.SetLangTagInUrlPath(HttpContext, returnUrl, UriKind.RelativeOrAbsolute, lt == null ? null : lt.ToString()).ToString();
            }
            //Redirect user agent as approp.
            return this.Redirect(returnUrl);
        }
        #endregion
    }
}