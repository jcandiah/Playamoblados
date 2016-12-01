using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using BeYourMarket.Web.Models;
using BeYourMarket.Model.Models;
using BeYourMarket.Web.Utilities;
using BeYourMarket.Service;
using Repository.Pattern.UnitOfWork;
using System.Collections.Generic;
using BeYourMarket.Model.Enum;
using BeYourMarket.Web.Models.Grids;
using RestSharp;
using BeYourMarket.Core.Web;
using BeYourMarket.Core.Plugins;
using BeYourMarket.Core;
using Microsoft.Practices.Unity;
using BeYourMarket.Core.Controllers;
using BeYourMarket.Service.Models;
using BeYourMarket.Web.Extensions;
using i18n;

namespace BeYourMarket.Web.Controllers
{
    [Authorize]
    public class PaymentController : Controller
    {
        #region Fields
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        private readonly ISettingService _settingService;
        private readonly ISettingDictionaryService _settingDictionaryService;
        private readonly ICategoryService _categoryService;
        private readonly IListingService _listingService;
        private readonly IListingStatService _listingStatservice;
        private readonly IListingPictureService _listingPictureservice;
        private readonly IListingReviewService _listingReviewService;
        private readonly IPictureService _pictureService;
        private readonly IOrderService _orderService;
        private readonly ICustomFieldService _customFieldService;
        private readonly ICustomFieldCategoryService _customFieldCategoryService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ICustomFieldListingService _customFieldListingService;
        private readonly IAspNetUserService _aspNetUserService;
        private readonly IAspNetRoleService _aspNetRoleService;

        private readonly DataCacheService _dataCacheService;
        private readonly SqlDbService _sqlDbService;

        private readonly IUnitOfWorkAsync _unitOfWorkAsync;

        private readonly IPluginFinder _pluginFinder;

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }
        #endregion

        #region Constructors
        public PaymentController(
            IUnitOfWorkAsync unitOfWorkAsync,
            ISettingService settingService,
            ICategoryService categoryService,
            IListingService listingService,
            IPictureService pictureService,
            IListingPictureService ListingPictureservice,
            IOrderService orderService,
            ICustomFieldService customFieldService,
            ICustomFieldCategoryService customFieldCategoryService,
            ICustomFieldListingService customFieldListingService,
            ISettingDictionaryService settingDictionaryService,
            IListingStatService listingStatservice,
            IListingReviewService listingReviewService,
            DataCacheService dataCacheService,
            SqlDbService sqlDbService,
            IEmailTemplateService emailTemplateService,
            IPluginFinder pluginFinder,
            IAspNetUserService aspNetUserService,
            IAspNetRoleService aspNetRoleService)
        {
            _settingService = settingService;
            _settingDictionaryService = settingDictionaryService;

            _categoryService = categoryService;

            _listingService = listingService;
            _pictureService = pictureService;
            _listingPictureservice = ListingPictureservice;
            _listingStatservice = listingStatservice;
            _listingReviewService = listingReviewService;

            _orderService = orderService;
            _customFieldService = customFieldService;
            _customFieldCategoryService = customFieldCategoryService;
            _customFieldListingService = customFieldListingService;
            _aspNetUserService = aspNetUserService;
            _aspNetRoleService = aspNetRoleService;

            _dataCacheService = dataCacheService;
            _sqlDbService = sqlDbService;

            _pluginFinder = pluginFinder;

            _unitOfWorkAsync = unitOfWorkAsync;
            _emailTemplateService = emailTemplateService;
        }
        #endregion

        #region Methods
        [HttpPost]
        public async Task<ActionResult> OrderAction(int id, int status)
        {
            var orderQuery = await _orderService.Query(x => x.ID == id).Include(x => x.Listing).SelectAsync();
            var order = orderQuery.FirstOrDefault();

            if (order == null)
                return new HttpNotFoundResult();

            var currentUserId = User.Identity.GetUserId();
            // Unauthorized access
            if (order.UserProvider != currentUserId && order.UserReceiver != currentUserId)
                return new HttpUnauthorizedResult();

            var descriptor = _pluginFinder.GetPluginDescriptorBySystemName<IHookPlugin>(order.PaymentPlugin);
            if (descriptor == null)
                return new HttpNotFoundResult("Not found");

            var controllerType = descriptor.Instance<IHookPlugin>().GetControllerType();
            var controller = ContainerManager.GetConfiguredContainer().Resolve(controllerType) as IPaymentController;

            string message = string.Empty;
            var orderResult = controller.OrderAction(id, status, out message);

            var orderStatus = (Enum_OrderStatus)status;
            var orderStatusText = string.Empty;

            switch (orderStatus)
            {
                case Enum_OrderStatus.Created:
                case Enum_OrderStatus.Pending:
                    orderStatusText = "[[[Pending]]]";
                    break;
                case Enum_OrderStatus.Confirmed:
                    orderStatusText = "[[[Confirmed]]]";
                    break;
                case Enum_OrderStatus.Cancelled:
                    orderStatusText = "[[[Cancelled]]]";
                    break;
                default:
                    orderStatusText = orderStatus.ToString();
                    break;
            }

            var result = new
            {
                Success = orderResult,
                Message = message
            };

            if (orderResult)
            {
                // Send message to the user
                var messageSend = new MessageSendModel()
                {
                    UserFrom = currentUserId,
                    UserTo = order.UserProvider == currentUserId ? order.UserReceiver : order.UserProvider,
                    ListingID = order.ListingID,
                    Subject = order.Listing.Title,
                    Body = HttpContext.ParseAndTranslate(string.Format(
                    "[[[Order %0 - %1 - Total Price %2 %3. <a href=\"%4\">See Details</a>|||{0}|||{1}|||{2}|||{3}|||{4}]]]",
                    HttpContext.ParseAndTranslate(orderStatusText),
                    HttpContext.ParseAndTranslate(order.Description),
                    order.Price,
                    order.Currency,
                    Url.Action("Orders")))
                };

                await MessageHelper.SendMessage(messageSend);
            }

            TempData[TempDataKeys.UserMessage] = string.Format("[[[The order is %0.|||{0}]]]", orderStatus);

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> Orders()
        {
            var userId = User.Identity.GetUserId();

            var orders = await _orderService
                .Query(x => (x.UserProvider == userId || x.UserReceiver == userId))
                .Include(x => x.Listing)
                .Include(x => x.AspNetUserProvider)
                .Include(x => x.AspNetUserReceiver)
                .Include(x => x.ListingReviews)
                .SelectAsync();

            var grid = new OrdersGrid(orders.AsQueryable().OrderByDescending(x => x.Created));

            var model = new OrderModel()
            {
                Grid = grid
            };

            return View(model);
        }

        public ActionResult PaymentSetting()
        {
            return View();
        }

        public async Task<ActionResult> Payment(int id)
        {
            var selectQuery = await _orderService.Query(x => x.ID == id)
                .Include(x => x.Listing)
                .Include(x => x.Listing.ListingType)
                .Include(x => x.Listing.ListingPictures)
                .SelectAsync();

            var order = selectQuery.FirstOrDefault();

            if (order == null)
                return new HttpNotFoundResult();

            var model = new PaymentModel()
            {
                ListingOrder = order
            };

            ClearCache();

            return View(model);
        }

        public async Task<ActionResult> Transaction()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> Order(Order order)
        {
            var listing = await _listingService.FindAsync(order.ListingID);
            var ordersListing = await _orderService.Query(x => x.ListingID == order.ListingID).SelectAsync();

            if (order.FromDate == order.ToDate)
            {
                TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
                TempData[TempDataKeys.UserMessage] = "[[[You cant book just to one day, minimun two day.]]]";

                return RedirectToAction("Listing", "Listing", new { id = order.ListingID });
            }

            if (listing == null)
                return new HttpNotFoundResult();

            // Redirect if not authenticated
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account", new { ReturnUrl = Url.Action("Listing", "Listing", new { id = order.ListingID }) });

            var userCurrent = User.Identity.User();

            ConfirmOrder confirmacion = new ConfirmOrder();
            confirmacion.Id = order.ListingID;
            confirmacion.Name = userCurrent.FirstName + " " + userCurrent.LastName;
            confirmacion.FromDate = order.FromDate.Value.ToString("dd-MM-yyyy");
            confirmacion.ToDate = order.ToDate.Value.ToString("dd-MM-yyyy");
            confirmacion.Email = userCurrent.Email;

            //validar que los dias no esten reservados
            List<DateTime> FechasCocinadas = new List<DateTime>();
            for (DateTime date = order.FromDate.Value; date < order.ToDate.Value; date = date.Date.AddDays(1))
            {
                FechasCocinadas.Add(date);

            }
            foreach (Order ordenesArrendadas in ordersListing.Where(x => x.Status != (int)Enum_OrderStatus.Cancelled))
            {
                for (DateTime date = ordenesArrendadas.FromDate.Value; date < ordenesArrendadas.ToDate.Value; date = date.Date.AddDays(1))
                {
                    if (FechasCocinadas.Contains(date))
                    {
                        TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
                        TempData[TempDataKeys.UserMessage] = "[[[You can not book with these selected dates!]]]";
                        return RedirectToAction("Listing", "Listing", new { id = listing.ID });
                    }
                }

            }

            // Check if payment method is setup on user or the platform
            var descriptors = _pluginFinder.GetPluginDescriptors<IHookPlugin>(LoadPluginsMode.InstalledOnly, "Payment").Where(x => x.Enabled);
            if (descriptors.Count() == 0)
            {
                TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
                TempData[TempDataKeys.UserMessage] = "[[[The provider has not setup the payment option yet, please contact the provider.]]]";

                return RedirectToAction("Listing", "Listing", new { id = order.ListingID });
            }

            if (!User.IsInRole("Administrator"))
            {
                if (listing.UserID != userCurrent.Id)
                {
                    //foreach (var descriptor in descriptors)
                    //{
                    //    var controllerType = descriptor.Instance<IHookPlugin>().GetControllerType();
                    //    var controller = ContainerManager.GetConfiguredContainer().Resolve(controllerType) as IPaymentController;

                    //    if (!controller.HasPaymentMethod(listing.UserID))
                    //    {
                    //        TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
                    //        TempData[TempDataKeys.UserMessage] = string.Format("[[[The provider has not setup the payment option for {0} yet, please contact the provider.]]]", descriptor.FriendlyName);

                    //        return RedirectToAction("Listing", "Listing", new { id = order.ListingID });
                    //    }
                    //}       
                    if (order.ID == 0)
                    {
                        order.ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added;
                        order.Created = DateTime.Now;
                        order.Modified = DateTime.Now;
                        order.Status = (int)Enum_OrderStatus.Pending;
                        order.UserProvider = listing.UserID;
                        order.UserReceiver = userCurrent.Id;
                        order.ListingTypeID = order.ListingTypeID;
                        order.Currency = listing.Currency;
                        order.OrderType = 3;
                        if (order.ToDate.HasValue && order.FromDate.HasValue)
                        {
                            order.Description = HttpContext.ParseAndTranslate(
                                string.Format("{0} #{1} ([[[From]]] {2} [[[To]]] {3})",
                                listing.Title,
                                listing.ID,
                                order.FromDate.Value.ToShortDateString(),
                                order.ToDate.Value.ToShortDateString()));

                            order.Quantity = order.ToDate.Value.Subtract(order.FromDate.Value.Date).Days;
                            order.Price = order.Quantity * listing.Price;                            
                        }
                        else if (order.Quantity.HasValue)
                        {
                            order.Description = string.Format("{0} #{1}", listing.Title, listing.ID);
                            order.Quantity = order.Quantity.Value - 1;
                            order.Price = listing.Price;
                        }
                        else
                        {
                            // Default
                            order.Description = string.Format("{0} #{1}", listing.Title, listing.ID);
                            order.Quantity = 1;
                            order.Price = listing.Price;
                        }
                        _orderService.Insert(order);

                        var provider = await _aspNetUserService.FindAsync(order.UserProvider);

                        await EnviarCorreo(confirmacion, provider.Email);
                    }
                    await _unitOfWorkAsync.SaveChangesAsync();

                    ClearCache();
                    return RedirectToAction("Payment", "Payment", new { id = order.ID });
                }
                else
                {
                    order.OrderType = 2;
                    if (order.ID == 0)
                    {
                        order.ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added;
                        order.Created = DateTime.Now;
                        order.Modified = DateTime.Now;
                        order.Status = (int)Enum_OrderStatus.Pending;
                        order.UserProvider = listing.UserID;
                        order.UserReceiver = userCurrent.Id;
                        order.ListingTypeID = order.ListingTypeID;
                        order.Currency = listing.Currency;

                        if (order.ToDate.HasValue && order.FromDate.HasValue)
                        {
                            order.Description = HttpContext.ParseAndTranslate(
                                string.Format("{0} #{1} ([[[From]]] {2} [[[To]]] {3})",
                                listing.Title,
                                listing.ID,
                                order.FromDate.Value.ToShortDateString(),
                                order.ToDate.Value.ToShortDateString()));

                            order.Quantity = order.ToDate.Value.Subtract(order.FromDate.Value.Date).Days;
                            order.Price = 0;
                        }
                        else if (order.Quantity.HasValue)
                        {
                            order.Description = string.Format("{0} #{1}", listing.Title, listing.ID);
                            order.Quantity = order.Quantity.Value - 1;
                            order.Price = 0;
                        }
                        else
                        {
                            // Default
                            order.Description = string.Format("{0} #{1}", listing.Title, listing.ID);
                            order.Quantity = 1;
                            order.Price = 0;
                        }

                        _orderService.Insert(order);
                    }

                    await _unitOfWorkAsync.SaveChangesAsync();

                    ClearCache();
                    TempData[TempDataKeys.UserMessage] = string.Format("[[[Has bloqueado correctamenter tu propiedad entre las fechas {0} y {1}]]]", order.FromDate.Value.ToString("dd-MM-yyyy"), order.ToDate.Value.ToString("dd-MM-yyyy"));
                    return RedirectToAction("Listing", "Listing", new { id = listing.ID });
                }
            }
            else
            {
                order.OrderType = 1;
                if (order.ID == 0)
                {
                    order.ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added;
                    order.Created = DateTime.Now;
                    order.Modified = DateTime.Now;
                    order.Status = (int)Enum_OrderStatus.Pending;
                    order.UserProvider = listing.UserID;
                    order.UserReceiver = userCurrent.Id;
                    order.ListingTypeID = order.ListingTypeID;
                    order.Currency = listing.Currency;

                    if (order.ToDate.HasValue && order.FromDate.HasValue)
                    {
                        order.Description = HttpContext.ParseAndTranslate(
                            string.Format("{0} #{1} ([[[From]]] {2} [[[To]]] {3})",
                            listing.Title,
                            listing.ID,
                            order.FromDate.Value.ToShortDateString(),
                            order.ToDate.Value.ToShortDateString()));

                        order.Quantity = order.ToDate.Value.Subtract(order.FromDate.Value.Date).Days;
                        order.Price = 0;
                    }
                    else if (order.Quantity.HasValue)
                    {
                        order.Description = string.Format("{0} #{1}", listing.Title, listing.ID);
                        order.Quantity = order.Quantity.Value - 1;
                        order.Price = 0;
                    }
                    else
                    {
                        // Default
                        order.Description = string.Format("{0} #{1}", listing.Title, listing.ID);
                        order.Quantity = 1;
                        order.Price = 0;
                    }

                    _orderService.Insert(order);
                }

                await _unitOfWorkAsync.SaveChangesAsync();

                ClearCache();
                TempData[TempDataKeys.UserMessage] = "[[[The maintenance was scheduled successfully!]]]";
                return RedirectToAction("Listing", "Listing", new { id = listing.ID });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> PropertyOrder(Order order)
        {
            var listing = await _listingService.FindAsync(order.ListingID);
            var ordersListing = await _orderService.Query(x => x.ListingID == order.ListingID).SelectAsync();

            if (order.FromDate == order.ToDate)
            {
                TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
                TempData[TempDataKeys.UserMessage] = "[[[You cant book just to one day, minimun two day.]]]";

                return RedirectToAction("Listing", "Manage", new { id = order.ListingID });
            }

            if (listing == null)
                return new HttpNotFoundResult();

            // Redirect if not authenticated
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login", "Account", new { ReturnUrl = Url.Action("Listing", "Manage", new { id = order.ListingID }) });

            var userCurrent = User.Identity.User();

            //validar que los dias no esten reservados
            List<DateTime> FechasCocinadas = new List<DateTime>();
            for (DateTime date = order.FromDate.Value; date <= order.ToDate.Value; date = date.Date.AddDays(1))
            {
                FechasCocinadas.Add(date);

            }
            foreach (Order ordenesArrendadas in ordersListing.Where(x => x.Status != (int)Enum_OrderStatus.Cancelled))
            {
                for (DateTime date = ordenesArrendadas.FromDate.Value; date < ordenesArrendadas.ToDate.Value; date = date.Date.AddDays(1))
                {
                    if (FechasCocinadas.Contains(date))
                    {
                        TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
                        TempData[TempDataKeys.UserMessage] = "[[[You can not book with these selected dates!]]]";
                        return RedirectToAction("Listing", "Manage", new { id = listing.ID });
                    }
                }

            }

            // Check if payment method is setup on user or the platform
            var descriptors = _pluginFinder.GetPluginDescriptors<IHookPlugin>(LoadPluginsMode.InstalledOnly, "Payment").Where(x => x.Enabled);
            if (descriptors.Count() == 0)
            {
                TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
                TempData[TempDataKeys.UserMessage] = "[[[The provider has not setup the payment option yet, please contact the provider.]]]";

                return RedirectToAction("Listing", "Manage", new { id = order.ListingID });
            }


            
            if (order.ID == 0)
            {
                order.ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added;
                order.Created = DateTime.Now;
                order.Modified = DateTime.Now;
                order.Status = (int)Enum_OrderStatus.Pending;
                order.UserProvider = listing.UserID;
                order.UserReceiver = userCurrent.Id;
                order.ListingTypeID = order.ListingTypeID;
                order.Currency = listing.Currency;
                order.OrderType = 2;
                if (order.ToDate.HasValue && order.FromDate.HasValue)
                {
                    order.Description = HttpContext.ParseAndTranslate(
                        string.Format("{0} #{1} ([[[From]]] {2} [[[To]]] {3})",
                        listing.Title,
                        listing.ID,
                        order.FromDate.Value.ToShortDateString(),
                        order.ToDate.Value.ToShortDateString()));

                    order.Quantity = order.ToDate.Value.Date.AddDays(1).Subtract(order.FromDate.Value.Date).Days;
                    order.Price = 0;
                }
                else if (order.Quantity.HasValue)
                {
                    order.Description = string.Format("{0} #{1}", listing.Title, listing.ID);
                    order.Quantity = order.Quantity.Value;
                    order.Price = 0;
                }
                else
                {
                    // Default
                    order.Description = string.Format("{0} #{1}", listing.Title, listing.ID);
                    order.Quantity = 1;
                    order.Price = 0;
                }

                _orderService.Insert(order);
            }

            await _unitOfWorkAsync.SaveChangesAsync();

            ClearCache();
            TempData[TempDataKeys.UserMessage] = string.Format("[[[Has bloqueado correctamenter tu propiedad entre las fechas {0} y {1}]]]", order.FromDate.Value.ToString("dd-MM-yyyy"), order.ToDate.Value.ToString("dd-MM-yyyy"));
            return RedirectToAction("Listing", "Manage", new { id = listing.ID });
        }

        private void ClearCache()
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddHours(-1));
            Response.Cache.SetNoStore();
        }

        //[HttpPost]
        //[AllowAnonymous]
        //public async Task<ActionResult> ConfirmarPago(Order order)
        //{
        //    var listing = await _listingService.FindAsync(order.ListingID);
        //    var ordersListing = await _orderService.Query(x => x.ListingID == order.ListingID).SelectAsync();
        //    var userCurrent = User.Identity.User();

        //    order.OrderType = 3;


        //    _orderService.Insert(order);            

        //    await _unitOfWorkAsync.SaveChangesAsync();

        //    ClearCache();

        //    ClearCache();
        //    TempData[TempDataKeys.UserMessage] = "[[[You booked your stay correctly!]]]";
        //    return RedirectToAction("Listing", "Listing", new { id = listing.ID });

        //}

        public async Task<ActionResult> EnviarCorreo(ConfirmOrder model, string correoPropietario)
        {
            var user = await UserManager.FindByNameAsync(model.Email);

            var emailTemplateQuery = await _emailTemplateService.Query(x => x.Slug.ToLower() == "confirmorder").SelectAsync();
            var emailTemplate = emailTemplateQuery.Single();

            //Aqui se envia el correo al cliente confirmando la orden.
            dynamic email = new Postal.Email("Email");
            email.To = user.Email;
            email.From = CacheHelper.Settings.EmailAddress;
            email.Subject = emailTemplate.Subject;
            email.Body = emailTemplate.Body;
            email.Name = model.Name;
            email.FromDate = model.FromDate;
            email.ToDate = model.ToDate;
            email.Id = model.Id;
            EmailHelper.SendEmail(email);

            var emailorderquery = await _emailTemplateService.Query(x => x.Slug.ToLower() == "orderhome").SelectAsync();
            var templateorder = emailorderquery.Single();
            var admin = await _aspNetUserService.Query(x => x.AspNetRoles.Any(z => z.Name == "Administrator")).SelectAsync();

            IList<EmailModel> personas = new List<EmailModel>();
            foreach (var recorrer in admin)
            {
                EmailModel persona = new EmailModel()
                {
                    Id = recorrer.Id,
                    Nombre = recorrer.FullName,
                    Email = recorrer.Email
                };
                personas.Add(persona);
            }

            var listaprop = await _aspNetUserService.Query(x => x.Email.Equals(correoPropietario)).SelectAsync();
            var prop = listaprop.FirstOrDefault(x => x.Email == correoPropietario);

            EmailModel propietario = new EmailModel()
            {
                Id = "1",
                Nombre = prop.FullName,
                Email = correoPropietario
            };
            personas.Add(propietario);

            EmailModel playamoblados = new EmailModel()
            {
                Id = "1",
                Nombre = "PlayaMoblados",
                Email = "playamoblados@ratio.cl"
            };
            personas.Add(playamoblados);

            //Con esto se envia el correo al propietario, a la administracion y a PM
            dynamic emailorder = new Postal.Email("Email");                        
            foreach (var administradores in personas)
            {
                emailorder.To = administradores.Email;
                emailorder.From = CacheHelper.Settings.EmailAddress;
                emailorder.Subject = templateorder.Subject;
                emailorder.Body = templateorder.Body;
                emailorder.Name = administradores.Nombre;
                emailorder.FromDate = model.FromDate;
                emailorder.ToDate = model.ToDate;
                emailorder.Id = model.Id;
                EmailHelper.SendEmail(emailorder);
            }
            

            return RedirectToAction("Payment", "Payment", new { id = model.Id });
        }       
        #endregion
    }
}