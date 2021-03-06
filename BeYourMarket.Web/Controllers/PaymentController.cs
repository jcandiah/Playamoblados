﻿using System;
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
using PayPal;
using Twilio;
//using Plugin.Payment.PayPal;
//using MvcApplication1.Utilities;
using PayPal.Api;

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
        private readonly IDetailBedService _detailBedService;

        private readonly DataCacheService _dataCacheService;
        private readonly SqlDbService _sqlDbService;

        private readonly IUnitOfWorkAsync _unitOfWorkAsync;

        private readonly IPluginFinder _pluginFinder;
        private readonly IListingPriceService _listingPriceService;

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
            IAspNetRoleService aspNetRoleService,
            IDetailBedService detailBedService,
            IListingPriceService listingPriceService)
        {
            _settingService = settingService;
            _settingDictionaryService = settingDictionaryService;

            _categoryService = categoryService;

            _listingService = listingService;
            _pictureService = pictureService;
            _listingPictureservice = ListingPictureservice;
            _listingStatservice = listingStatservice;
            _listingReviewService = listingReviewService;
            _detailBedService = detailBedService;

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
            _listingPriceService = listingPriceService;
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
        public async Task<ActionResult> Order(Model.Models.Order order)
        {
            var listing = await _listingService.FindAsync(order.ListingID);
            var ordersListing = await _orderService.Query(x => x.ListingID == order.ListingID).SelectAsync();

            if ((order.ToDate.Value - order.FromDate.Value).Days < 2)
            {
                TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
                TempData[TempDataKeys.UserMessage] = "[[[The minium quantity for order is two nights]]]";

                return RedirectToAction("Listing", "Listing", new { id = order.ListingID });
            }
			if (order.ToDate < order.FromDate)
			{
				TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
				TempData[TempDataKeys.UserMessage] = "[[[The date of check out can't be less than the check in date]]]";

				return RedirectToAction("Listing", "Listing", new { id = order.ListingID });
			}
			if ((order.Children + order.Adults) > listing.Max_Capacity)
			{
				TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
				TempData[TempDataKeys.UserMessage] = string.Format("La maxima capacidad de pasajeros es de  {0}",listing.Max_Capacity);

				return RedirectToAction("Listing", "Listing", new { id = order.ListingID });
			}

            if (listing == null)
                return new HttpNotFoundResult();

            // Redirect if not authenticated
            if (!User.Identity.IsAuthenticated)
            {
                if (Session["fechas"] != null)
                {
                    SearchListingModel fecha = (SearchListingModel)Session["fechas"];
                    fecha.FromDate = order.FromDate;
                    fecha.ToDate = order.ToDate;
                   
                    fecha.Niños = order.Children;
                    fecha.Passengers = order.Adults;

                    Session["fechas"] = fecha;
                }
                return RedirectToAction("Login", "Account", new { ReturnUrl = Url.Action("Listing", "Listing", new { id = order.ListingID }), FromDate = order.FromDate, ToDate = order.ToDate, Adults = order.Adults, Children = order.Children });
            }
            var userCurrent = User.Identity.User();

            //validar que los dias no esten reservados
            List<DateTime> FechasCocinadas = new List<DateTime>();
            for (DateTime date = order.FromDate.Value; date < order.ToDate.Value; date = date.Date.AddDays(1))
            {
                FechasCocinadas.Add(date);

            }
            foreach (Model.Models.Order ordenesArrendadas in ordersListing.Where(x => x.Status != (int)Enum_OrderStatus.Cancelled))
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
                        order.Status = (int)Enum_OrderStatus.Created;
                        order.UserProvider = listing.UserID;
                        order.UserReceiver = userCurrent.Id;
                        order.ListingTypeID = order.ListingTypeID;
                        order.Currency = listing.Currency;
                        order.OrderType = 3;
                        order.Price = 0;
						order.Quantity = 0;
                        var listingPrice = await _listingPriceService.Query(x => x.ListingID == order.ListingID).SelectAsync();
                        var precioEnero = listingPrice.FirstOrDefault(x=>x.FromDate.Month.Equals(01));
                        var precioFebrero = listingPrice.FirstOrDefault(x=>x.FromDate.Month.Equals(02));

                        for (DateTime date = order.FromDate.Value; date <= order.ToDate.Value.AddDays(-1); date = date.Date.AddDays(1))
                        {
                            if (date >= precioEnero.FromDate && date <= precioEnero.ToDate)
                            {
                                order.Price = order.Price + precioEnero.Price;
								order.Quantity = order.Quantity + 1;
                            }
                            else if (date >= precioFebrero.FromDate && date <= precioFebrero.ToDate)
                            {
                                order.Price = order.Price + precioFebrero.Price;
								order.Quantity = order.Quantity + 1;
							}
                            else
                            {
                                order.Price = order.Price + listing.Price;
								order.Quantity = order.Quantity + 1;
							}
                        }

						var servicio = order.Price * 0.04;
						order.Total = order.Price + listing.CleanlinessPrice + servicio;

						order.Description = HttpContext.ParseAndTranslate(
                            string.Format("{0} #{1} ([[[From]]] {2} [[[To]]] {3})",
                            listing.Title,
                            listing.ID,
                            order.FromDate.Value.ToShortDateString(),
                            order.ToDate.Value.ToShortDateString()));                          

                        _orderService.Insert(order);

                        var provider = await _aspNetUserService.FindAsync(order.UserProvider);

                        //await EnviarCorreo(confirmacion, provider.Email);
                    }
                    await _unitOfWorkAsync.SaveChangesAsync();

                    ClearCache();
                    return RedirectToAction("ConfirmOrder", "Payment", new { id = order.ID });
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
        public async Task<ActionResult> PropertyOrder(Model.Models.Order order)
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
            foreach (Model.Models.Order ordenesArrendadas in ordersListing.Where(x => x.Status != (int)Enum_OrderStatus.Cancelled))
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

			//Envio de correo a propietario 
			var emailTemplateQuery = await _emailTemplateService.Query(x => x.Slug.ToLower() == "bloqueopropietario").SelectAsync();
			var emailTemplate = emailTemplateQuery.Single();

			dynamic email = new Postal.Email("Email");
			email.To = userCurrent.Email;
			email.From = CacheHelper.Settings.EmailAddress;
			email.Subject = emailTemplate.Subject;
			email.Body = emailTemplate.Body;
			email.Id = order.ListingID;
			email.Name = userCurrent.FullName;
			email.FromDate = order.FromDate.Value.ToShortDateString();
			email.ToDate = order.ToDate.Value.ToShortDateString();
			EmailHelper.SendEmail(email);

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

			//Aqui enviamos el correo al pasajero
			#region Correo Pasajero    
			var administrator = _aspNetUserService.Query(x => x.AspNetRoles.Any(y => y.Name.Equals("Administrator"))).Select().FirstOrDefault();
			var emailTemplateQuery = await _emailTemplateService.Query(x => x.Slug.ToLower() == "confirmorder").SelectAsync();
			var emailTemplate = emailTemplateQuery.Single();

			if (emailTemplate != null)
			{
				dynamic email = new Postal.Email("Email");
				email.To = user.Email;
				email.From = CacheHelper.Settings.EmailAddress;
				email.Subject = emailTemplate.Subject;
				email.Body = emailTemplate.Body;
				email.Id = model.Id;
				email.Name = model.Name;
				email.FromDate = model.FromDate;
				email.ToDate = model.ToDate;
				email.Adults = model.Adults;
				email.Children = model.Children;
				email.Rent = model.Rent;
				email.Service = model.Service;
				email.CleanlinessPrice = model.CleanlinessPrice;
				email.Total = model.Rent + model.CleanlinessPrice + model.Service;
				email.ShortDescription = model.ShortDescription;
				email.Description = model.Description;
				email.Condominium = model.Condominium;
				email.TypeOfProperty = model.TypeOfProperty;
				email.Capacity = model.Capacity;
				email.Rooms = model.Rooms;
				email.Beds = model.Beds;
				email.SuiteRooms = model.SuiteRooms;
				email.Bathrooms = model.Bathrooms;
				email.Dishwasher = model.Dishwasher;
				email.Washer = model.Washer;
				email.Grill = model.Grill;
				email.TvCable = model.TvCable;
				email.Wifi = model.Wifi;
				email.Elevator = model.Elevator;
				email.FloorNumber = model.FloorNumber;
				email.Stay = model.Stay;
				email.ConditionHouse = model.ConditionHouse;
				EmailHelper.SendEmail(email);
			}
			#endregion

			//Con esto se envia el correo a la administracion y a PM
			#region Correo PM y Admin
			var emailorderquery = await _emailTemplateService.Query(x => x.Slug.ToLower() == "blockedproperty").SelectAsync();
			var templateorder = emailorderquery.Single();
			var admin = await _aspNetUserService.Query(x => x.AspNetRoles.Any(z => z.Name == "Administrator")).SelectAsync();

			dynamic emailorder = new Postal.Email("Email");
			foreach (var administradores in admin)
			{
				emailorder.To = administradores.Email;
				emailorder.From = CacheHelper.Settings.EmailAddress;
				emailorder.Subject = templateorder.Subject;
				emailorder.Body = templateorder.Body;
				emailorder.Name = administradores.FullName;
				emailorder.FromDate = model.FromDate;
				emailorder.ToDate = model.ToDate;
				emailorder.Id = model.Id;
				EmailHelper.SendEmail(emailorder);
			}
			#endregion

			//Aqui enviamos el correo y sms al propietario
			#region Correo Propietario
			var emailOwnerQuery = await _emailTemplateService.Query(x => x.Slug.ToLower() == "emailowner").SelectAsync();
			var emailOwner = emailOwnerQuery.Single();
			var propietario = _aspNetUserService.Query(x => x.Email.Equals(correoPropietario)).Select().FirstOrDefault();
			var order = _orderService.Query(x => x.ID == model.OrderId).Select().FirstOrDefault();
			var propiedad = await _listingService.FindAsync(order.ListingID);
			dynamic ownermail = new Postal.Email("Email");
			ownermail.To = propietario.Email;
			ownermail.From = CacheHelper.Settings.EmailAddress;
			ownermail.Subject = emailOwner.Subject;
			ownermail.Body = emailOwner.Body;
			ownermail.Name = propietario.FullName;
			ownermail.FromDate = model.FromDate;
			ownermail.ToDate = model.ToDate;
			ownermail.Tarifa = propiedad.Price;
			ownermail.Total = order.Price;
			ownermail.Passengers = order.Adults + order.Children;
			ownermail.Id = model.Id;
			EmailHelper.SendEmail(ownermail);
			//if(prop.PhoneNumberConfirmed)
			SMSHelper.SendSMS(propietario.PhoneNumber, string.Format("Estimado {0} hemos recibido una reserva por su propiedad desde {1} hasta {2} Mayores detalles en su correo", propietario.FullName, model.FromDate, model.ToDate));
			#endregion
			return RedirectToAction("Payment", "Payment", new { id = model.Id });
		}

		public async Task<ActionResult> ConfirmOrder(int id)
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

		[HttpPost]
		[AllowAnonymous]
		public async Task<ActionResult> ConfirmOrderAction(int id, int adultos, int niños)
		{
			var selectQuery = await _orderService.Query(x => x.ID == id)
				.Include(x => x.Listing)
				.Include(x => x.Listing.ListingType)
				.Include(x => x.Listing.ListingPictures)
				.SelectAsync();

			var order = selectQuery.FirstOrDefault();

			if (order == null)
				return new HttpNotFoundResult();

			order.Status = (int)Enum_OrderStatus.Pending;

			_orderService.Update(order);

			var userCurrent = User.Identity.User();
			var propietario = _aspNetUserService.Query(x => x.Id == order.UserProvider.ToString()).Select().FirstOrDefault();
			var condominium = _categoryService.Query(x => x.ID == order.Listing.CategoryID).Select().FirstOrDefault();

			ConfirmOrder confirmacion = new ConfirmOrder();
			confirmacion.Id = order.ListingID;
			confirmacion.Name = userCurrent.FirstName + " " + userCurrent.LastName;
			confirmacion.FromDate = order.FromDate.Value.ToString("dd-MM-yyyy");
			confirmacion.ToDate = order.ToDate.Value.ToString("dd-MM-yyyy");
			confirmacion.Email = userCurrent.Email;
			confirmacion.Rent = order.Price;
			confirmacion.Service = order.Price * 0.04;
			confirmacion.CleanlinessPrice = order.Listing.CleanlinessPrice;
			confirmacion.Total = confirmacion.Rent + confirmacion.Service + confirmacion.CleanlinessPrice;
			confirmacion.Condominium = condominium.Name;
			confirmacion.TypeOfProperty = order.Listing.TypeOfProperty;
			confirmacion.Capacity = order.Listing.Max_Capacity;
			confirmacion.Rooms = order.Listing.Rooms;
			confirmacion.Beds = order.Listing.Beds;
			confirmacion.SuiteRooms = order.Listing.Suite;
			confirmacion.Bathrooms = order.Listing.Bathrooms;
			confirmacion.FloorNumber = order.Listing.FloorNumber;
			confirmacion.Adults = adultos;
			confirmacion.Children = niños;
			confirmacion.OrderId = order.ID;
			confirmacion.Dishwasher = order.Listing.Dishwasher ? "Si" : "No";
			confirmacion.Washer = order.Listing.Washer ? "Si" : "No";
			confirmacion.Grill = order.Listing.Grill ? "Si" : "No";
			confirmacion.TvCable = order.Listing.TV_cable ? "Si" : "No";
			confirmacion.Wifi = order.Listing.Wifi ? "Si" : "No";
			confirmacion.Elevator = order.Listing.Elevator ? "Si" : "No";

			confirmacion.Stay = order.Listing.Stay;
			confirmacion.ConditionHouse = order.Listing.ConditionHouse;
			confirmacion.Description = order.Listing.Description;
			confirmacion.ShortDescription = order.Listing.ShortDescription;
			await EnviarCorreo(confirmacion, propietario.Email);

			SMSHelper.SendSMS(propietario.PhoneNumber,
				string.Format("Estimado {0}. Hemos recibido una reserva por su propiedad desde {1} hasta {2}", propietario.FullName, order.FromDate.Value.ToShortDateString(), order.ToDate.Value.ToShortDateString()));

			 await _unitOfWorkAsync.SaveChangesAsync();

            return View("~/Views/Payment/Congratulations.cshtml");
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> CancelOrderAction(int id)
        {
            var selectQuery = await _orderService.Query(x => x.ID == id)
                .Include(x => x.Listing)
                .Include(x => x.Listing.ListingType)
                .Include(x => x.Listing.ListingPictures)
                .SelectAsync();

            var order = selectQuery.FirstOrDefault();

            if (order == null)
                return new HttpNotFoundResult();

            order.Status = (int)Enum_OrderStatus.Cancelled;

            _orderService.Update(order);

            await _unitOfWorkAsync.SaveChangesAsync();

            //return View("~/Views/Listing/Listing.cshtml", itemModel);
            return RedirectToAction("Listing", "Listing", new { id = order.ListingID });
        }

        [HttpPost]
        public async Task<JsonResult> DeleteOrder(int id)
        {
            var order = _orderService.Query(x => x.ID == id).Select().FirstOrDefault();
            _orderService.Delete(order);
            await _unitOfWorkAsync.SaveChangesAsync();
            return Json(true);
        }

		public ActionResult Congratulations()
		{
			return View();
		}

		public async Task<JsonResult> PayPalPayment(int id)
		{
			var selectQuery = await _orderService.Query(x => x.ID == id)
			.Include(x => x.Listing)
			.Include(x => x.Listing.ListingType)
			.Include(x => x.Listing.ListingPictures)
			.SelectAsync();

			var order = selectQuery.FirstOrDefault();

			//if (order == null)
			//	return new HttpNotFoundResult();

			order.Status = (int)Enum_OrderStatus.PayFor;

			_orderService.Update(order);

			//Aqui hay que enviar un correo

			await _unitOfWorkAsync.SaveChangesAsync();

			return Json(true, JsonRequestBehavior.AllowGet);
		}

		//public void CreatePayment(int id)
		//{
		//	// ### Api Context
		//	// Pass in a `APIContext` object to authenticate 
		//	// the call and to send a unique request id 
		//	// (that ensures idempotency). The SDK generates
		//	// a request id if you do not pass one explicitly. 
		//	// See [Configuration.cs](/Source/Configuration.html) to know more about APIContext.
		//	var apiContext = Configuration.GetAPIContext();
			
		//	// A transaction defines the contract of a payment - what is the payment for and who is fulfilling it. 
		//	var transaction = new Transaction()
		//	{
		//		amount = new Amount()
		//		{
		//			currency = "USD",
		//			total = "7",
		//			details = new Details()
		//			{
		//				shipping = "1",
		//				subtotal = "5",
		//				tax = "1"
		//			}
		//		},
		//		description = "This is the payment transaction description.",
		//		item_list = new ItemList()
		//		{
		//			items = new List<Item>()
		//			{
		//				new Item()
		//				{
		//					name = "Item Name",
		//					currency = "USD",
		//					price = "1",
		//					quantity = "5",
		//					sku = "sku"
		//				}
		//			},
		//			shipping_address = new ShippingAddress
		//			{
		//				city = "Johnstown",
		//				country_code = "US",
		//				line1 = "52 N Main ST",
		//				postal_code = "43210",
		//				state = "OH",
		//				recipient_name = "Joe Buyer"
		//			}
		//		},
		//		invoice_number = Common.GetRandomInvoiceNumber()
		//	};

		//	// A resource representing a Payer that funds a payment.
		//	var payer = new Payer()
		//	{
		//		payment_method = "credit_card",
		//		funding_instruments = new List<FundingInstrument>()
		//		{
		//			new FundingInstrument()
		//			{
		//				credit_card = new CreditCard()
		//				{
		//					billing_address = new PayPal.Api.Address()
		//					{
		//						city = "Johnstown",
		//						country_code = "US",
		//						line1 = "52 N Main ST",
		//						postal_code = "43210",
		//						state = "OH"
		//					},
		//					cvv2 = "874",
		//					expire_month = 11,
		//					expire_year = 2018,
		//					first_name = "Joe",
		//					last_name = "Shopper",
		//					number = "4877274905927862",
		//					type = "visa"
		//				}
		//			}
		//		},
		//		payer_info = new PayerInfo
		//		{
		//			email = "test@email.com"
		//		}
		//	};

		//	// A Payment resource; create one using the above types and intent as `sale` or `authorize`
		//	var payment = new Payment()
		//	{
		//		intent = "sale",
		//		payer = payer,
		//		transactions = new List<Transaction>() { transaction }
		//	};

		//	// ^ Ignore workflow code segment
		//	#region Track Workflow
		//	//this.flow.AddNewRequest("Create credit card payment", payment);
		//	#endregion

		//	// Create a payment using a valid APIContext
		//	var createdPayment = payment.Create(apiContext);

		//	// ^ Ignore workflow code segment
		//	#region Track Workflow
		//	//this.flow.RecordResponse(createdPayment);
		//	#endregion

		//	// For more information, please visit [PayPal Developer REST API Reference](https://developer.paypal.com/docs/api/).
		//}
		#endregion
	}
}