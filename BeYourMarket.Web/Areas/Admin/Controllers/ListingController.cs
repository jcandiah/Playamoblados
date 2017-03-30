using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using BeYourMarket.Service;
using System.Threading.Tasks;
using BeYourMarket.Model.Models;
using Repository.Pattern.UnitOfWork;
using BeYourMarket.Web.Extensions;
using BeYourMarket.Web.Models.Grids;
using BeYourMarket.Web.Models;
using BeYourMarket.Web.Utilities;
using ImageProcessor.Imaging.Formats;
using System.Drawing;
using ImageProcessor;
using System.IO;
using System.Text;
using BeYourMarket.Model.Enum;
using RestSharp;
using BeYourMarket.Web.Areas.Admin.Models;
using Postal;
using System.Net.Mail;
using System.Net;
using BeYourMarket.Service.Models;
using BeYourMarket.Core.Web;
using Newtonsoft.Json;

namespace BeYourMarket.Web.Areas.Admin.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class ListingController : Controller
    {
        #region Fields
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;
        private ApplicationRoleManager _roleManager;

        private readonly ISettingService _settingService;
        private readonly ISettingDictionaryService _settingDictionaryService;

        private readonly ICategoryService _categoryService;
        private readonly ICategoryListingTypeService _categoryListingTypeService;

        private readonly IListingService _listingService;
        private readonly IListingTypeService _listingTypeService;
        private readonly IListingReviewService _listingReviewService;
        private readonly IListingObservationService _listingObservationService;

        private readonly ICustomFieldService _customFieldService;
        private readonly ICustomFieldCategoryService _customFieldCategoryService;
        private readonly ICustomFieldListingService _customFieldListingService;

        private readonly IContentPageService _contentPageService;

        private readonly IOrderService _orderService;

        private readonly IListingPictureService _listingPictureservice;
        private readonly IPictureService _pictureService;

        private readonly IEmailTemplateService _emailTemplateService;

        private readonly DataCacheService _dataCacheService;
        private readonly SqlDbService _sqlDbService;

        private readonly IUnitOfWorkAsync _unitOfWorkAsync;

        private readonly IDetailBedService _detailBedService;
        private readonly ITypeOfBedService _typeOfBedService;

        private readonly IAspNetUserService _aspNetUserService;
        #endregion

        #region Properties
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

        public ApplicationRoleManager RoleManager
        {
            get
            {
                return _roleManager ?? HttpContext.GetOwinContext().Get<ApplicationRoleManager>();
            }
            private set
            {
                _roleManager = value;
            }
        }
        #endregion

        #region Constructor
        public ListingController(
            IUnitOfWorkAsync unitOfWorkAsync,
            ISettingService settingService,
            ICategoryService categoryService,
            ICategoryListingTypeService categoryListingTypeService,
            IListingService listingService,
            IListingTypeService listingTypeService,
            ICustomFieldService customFieldService,
            ICustomFieldCategoryService customFieldCategoryService,
            ICustomFieldListingService customFieldListingService,
            IContentPageService contentPageService,
            IOrderService orderService,
           IDetailBedService detailBedService,
           ITypeOfBedService typeOfBedService,
            ISettingDictionaryService settingDictionaryService,
            IEmailTemplateService emailTemplateService,
            IPictureService pictureService,
            IListingPictureService listingPictureservice,
            IListingReviewService listingReviewService,
            DataCacheService dataCacheService,
            SqlDbService sqlDbService,
            AspNetUserService aspNetUserService,
            IListingObservationService listingObservationService)
        {
            _settingService = settingService;
            _settingDictionaryService = settingDictionaryService;

            _categoryService = categoryService;
            _categoryListingTypeService = categoryListingTypeService;

            _listingService = listingService;
            _listingTypeService = listingTypeService;

            _pictureService = pictureService;
            
            _listingPictureservice = listingPictureservice;
            _listingReviewService = listingReviewService;

            _customFieldService = customFieldService;
            _customFieldCategoryService = customFieldCategoryService;
            _customFieldListingService = customFieldListingService;

            _orderService = orderService;

            _emailTemplateService = emailTemplateService;
            _contentPageService = contentPageService;
            _unitOfWorkAsync = unitOfWorkAsync;
            _dataCacheService = dataCacheService;
            _detailBedService = detailBedService;
            _typeOfBedService = typeOfBedService;
            _sqlDbService = sqlDbService;
            _aspNetUserService = aspNetUserService;
            _listingObservationService = listingObservationService;
        }
        #endregion

        #region Methods
        public ActionResult Categories()
        {
            var categories = _categoryService.Queryable().OrderBy(x => x.Parent).ThenBy(x => x.Ordering).ToList().GenerateTree(x => x.ID, x => x.Parent);

            return View(categories);
        }


        public class CategoryJson
        {
            public int id { get; set; }

            public CategoryJson[] children { get; set; }
        }

        [HttpPost]
        public async Task<ActionResult> CategoriesUpdate(string JsonCategories)
        {
            var result = JsonConvert.DeserializeObject<CategoryJson[]>(JsonCategories);

            await SetCategoryOrdering(0, result);

            await _unitOfWorkAsync.SaveChangesAsync();

            _dataCacheService.RemoveCachedItem(CacheKeys.ListingTypes);
            _dataCacheService.RemoveCachedItem(CacheKeys.Categories);

            return RedirectToAction("Categories");
        }

        /// <summary>
        /// Recursive method to set category ordering
        /// </summary>
        /// <param name="json"></param>
        public async Task SetCategoryOrdering(int parent, CategoryJson[] json)
        {
            int sortOrder = 0;
            foreach (var item in json)
            {
                var category = await _categoryService.FindAsync(item.id);
                category.Parent = parent;
                category.Ordering = sortOrder;
                category.ObjectState = Repository.Pattern.Infrastructure.ObjectState.Modified;

                _categoryService.Update(category);

                sortOrder++;

                if (item.children != null)
                {
                    await SetCategoryOrdering(item.id, item.children);
                }
            }
        }

        public async Task<ActionResult> CategoryUpdate(int? id)
        {
            Category category;

            var categoryModel = new CategoryModel();

            var ListingTypes = await _listingTypeService.Query().SelectAsync();
            categoryModel.ListingTypes = ListingTypes.ToList();

            if (id.HasValue)
            {
                category = await _categoryService.FindAsync(id);
                var categoryListingTypes = await _categoryListingTypeService.Query(x => x.CategoryID == id.Value).SelectAsync();
                categoryModel.CategoryListingTypeID = categoryListingTypes.Select(x => x.ListingTypeID).ToList();
            }
            else
                category = new Category() { Enabled = true };

            categoryModel.Category = category;

            return View(categoryModel);
        }

        [HttpPost]
        public async Task<ActionResult> CategoryUpdate(CategoryModel model)
        {
            if (model.Category.ID == 0)
            {
                model.Category.ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added;
                model.Category.Parent = 0;

                _categoryService.Insert(model.Category);
            }
            else
            {
                var categoryExisting = await _categoryService.FindAsync(model.Category.ID);

                categoryExisting.Name = model.Category.Name;
                categoryExisting.Description = model.Category.Description;
                categoryExisting.Enabled = model.Category.Enabled;

                categoryExisting.ObjectState = Repository.Pattern.Infrastructure.ObjectState.Modified;

                _categoryService.Update(categoryExisting);
            }

            await _unitOfWorkAsync.SaveChangesAsync();

            // Delete existing category item types
            var ListingTypes = _categoryListingTypeService.Queryable().Where(x => x.CategoryID == model.Category.ID).Select(x => x.ID).ToList();
            foreach (var ListingTypeID in ListingTypes)
            {
                await _categoryListingTypeService.DeleteAsync(ListingTypeID);
            }

            // Insert category item types
            foreach (var categoryListingTypeID in model.CategoryListingTypeID)
            {
                _categoryListingTypeService.Insert(new CategoryListingType()
                {
                    CategoryID = model.Category.ID,
                    ListingTypeID = categoryListingTypeID,
                    ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added
                });
            }

            await _unitOfWorkAsync.SaveChangesAsync();

            _dataCacheService.RemoveCachedItem(CacheKeys.ListingTypes);
            _dataCacheService.RemoveCachedItem(CacheKeys.Categories);

            return RedirectToAction("Categories");
        }

        public ActionResult CustomFields()
        {
            var grid = new CustomFieldsGrid(_customFieldService.Query().Include(x => x.MetaCategories.Select(s => s.Category)).Select().OrderByDescending(x => x.ID).AsQueryable());

            return View(grid);
        }

        public async Task<ActionResult> CustomFieldUpdate(int? id)
        {
            MetaField field;

            if (id.HasValue)
            {
                var fieldQuery = await _customFieldService.Query(x => x.ID == id).Include(x => x.MetaCategories).SelectAsync();
                field = fieldQuery.FirstOrDefault();
            }
            else
                field = new MetaField()
                {
                    Required = false,
                    Searchable = false
                };

            var categories = await _categoryService.Query().SelectAsync();

            var model = new MetaFieldModel()
            {
                MetaField = field,
                Categories = categories.ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> CustomFieldUpdate(MetaField metaField, List<int> Categories)
        {
            if (metaField.ID == 0)
            {
                metaField.ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added;

                _customFieldService.Insert(metaField);
            }
            else
            {
                var metaFieldExistingQuery = await _customFieldService.Query(x => x.ID == metaField.ID).Include(x => x.MetaCategories).SelectAsync();
                var metaFieldExisting = metaFieldExistingQuery.FirstOrDefault();

                metaFieldExisting.Name = metaField.Name;
                metaFieldExisting.ControlTypeID = metaField.ControlTypeID;
                metaFieldExisting.Options = metaField.Options;
                metaFieldExisting.Required = metaField.Required;
                metaFieldExisting.Searchable = metaField.Searchable;

                metaFieldExisting.ObjectState = Repository.Pattern.Infrastructure.ObjectState.Modified;

                _customFieldService.Update(metaFieldExisting);

                // Delete existing
                foreach (var category in metaFieldExisting.MetaCategories)
                {
                    await _customFieldCategoryService.DeleteAsync(category.ID);
                }
            }

            if (Categories != null)
            {
                // Insert meta categories
                var metaCategories = Categories.Select(x => new MetaCategory()
                {
                    CategoryID = x,
                    FieldID = metaField.ID,
                    ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added
                }).ToList();

                _customFieldCategoryService.InsertRange(metaCategories);
            }

            await _unitOfWorkAsync.SaveChangesAsync();

            return RedirectToAction("CustomFields");
        }

        public ActionResult Listings()
        {
            var grid = new ListingsGrid(_listingService.Query().Include(x => x.ListingType).Select().OrderByDescending(x => x.Created).AsQueryable());
            var categories = CacheHelper.Categories;

            var model = new ListingModel()
            {
                Categories = categories,
                Grid = grid
            };

            return View(model);
        }

        public async Task<ActionResult> ListingUpdate(int? id)
        {
            if (CacheHelper.Categories.Count == 0)
            {
                TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
                TempData[TempDataKeys.UserMessage] = "[[[There are not categories available yet.]]]";
            }

            Listing item;

            var model = new ListingUpdateModel()
            {
                Categories = CacheHelper.Categories,
                TypesOfBeds = CacheHelper.TypesOfBeds
            };

            if (id.HasValue)
            {
                item = await _listingService.FindAsync(id);

                if (item == null)
                    return new HttpNotFoundResult();

                // Pictures
                var pictures = await _listingPictureservice.Query(x => x.ListingID == id).SelectAsync();

                var picturesModel = pictures.Select(x =>
                    new PictureModel()
                    {
                        ID = x.PictureID,
                        Url = ImageHelper.GetListingImagePath(x.PictureID),
                        ListingID = x.ListingID,
                        Ordering = x.Ordering
                    }).OrderBy(x => x.Ordering).ToList();

                model.Pictures = picturesModel;
                var detallecama = await _detailBedService.Query(x => x.ListingID == id).Include(x => x.TypeOfBed).SelectAsync();
                model.DetailBeds = detallecama.ToList();
            }
            else
                item = new Listing()
                {
                    CategoryID = CacheHelper.Categories.Any() ? CacheHelper.Categories.FirstOrDefault().ID : 0,
                    Created = DateTime.Now.Date,
                    LastUpdated = DateTime.Now.Date,
                    Expiration = DateTime.Now.AddDays(30),
                    Enabled = true,
                    Active = true,
                };

            // Item
            model.ListingItem = item;

            // Custom fields
            var customFieldCategoryQuery = await _customFieldCategoryService.Query(x => x.CategoryID == item.CategoryID).Include(x => x.MetaField.ListingMetas).SelectAsync();
            var customFieldCategories = customFieldCategoryQuery.ToList();
            var customFieldModel = new CustomFieldListingModel()
            {
                ListingID = item.ID,
                MetaCategories = customFieldCategories
            };

            model.CustomFields = customFieldModel;
            model.Users = UserManager.Users.ToList();
            model.UserID = item.UserID;
            model.CategoryID = item.CategoryID;
            model.ListingTypeID = item.ListingTypeID;

            // Listing types
            model.ListingTypes = CacheHelper.ListingTypes.Where(x => x.CategoryListingTypes.Any(y => y.CategoryID == model.CategoryID)).ToList();

            return View(model);
        }

        //http://stackoverflow.com/questions/11774741/load-partial-view-depending-on-dropdown-selection-in-mvc3
        public async Task<ActionResult> ListingPartial(int categoryID)
        {
            // Custom fields
            var customFieldCategoryQuery = await _customFieldCategoryService.Query(x => x.CategoryID == categoryID).Include(x => x.MetaField.ListingMetas).SelectAsync();
            var customFieldCategories = customFieldCategoryQuery.ToList();
            var customFieldModel = new CustomFieldListingModel()
            {
                MetaCategories = customFieldCategories
            };

            return PartialView("_CategoryCustomFields", customFieldModel);
        }


        public async Task<ActionResult> ListingCalendar(int id)
        {
            var itemQuery = await _listingService.Query(x => x.ID == id)
                                            .Include(x => x.Category)
                                            .Include(x => x.ListingMetas)
                                            .Include(x => x.ListingMetas.Select(y => y.MetaField))
                                            .Include(x => x.ListingStats)
                                            .Include(x => x.ListingType)
                                            .Include(x => x.Orders)
                                            .SelectAsync();

            var item = itemQuery.FirstOrDefault();

            if (item == null)
                return new HttpNotFoundResult();

            var orders = _orderService.Queryable()
                .Where(x => x.ListingID == id
                    && (x.Status != (int)Enum_OrderStatus.Pending || x.Status != (int)Enum_OrderStatus.Confirmed)
                    && (x.FromDate.HasValue && x.ToDate.HasValue)
                    && (x.FromDate >= DateTime.Now || x.ToDate >= DateTime.Now))
                    .ToList();



            List<DateTime> datesBooked = new List<DateTime>();
            foreach (var order in orders)
            {

                for (DateTime date = order.FromDate.Value; date <= order.ToDate.Value; date = date.Date.AddDays(1))
                {
                    datesBooked.Add(date);
                }
            }

            var reviews = await _listingReviewService
                .Query(x => x.UserTo == item.UserID)
                .Include(x => x.AspNetUserFrom)
                .SelectAsync();

            var user = await UserManager.FindByIdAsync(item.UserID);
            var usuarios = UserManager.Users.ToList();
            var itemModel = new ListingItemModel()
            {
                ListingCurrent = item,
                DatesBooked = datesBooked,
                ListUsers = usuarios.ToList(),
                ListingReviews = reviews.ToList(),
                ListOrder = orders,

            };



            return View(itemModel);
        }

        public async Task<ActionResult> ListingTypesPartial(int categoryID, int listingID)
        {
            var model = new ListingUpdateModel();
            model.ListingTypes = CacheHelper.ListingTypes.Where(x => x.CategoryListingTypes.Any(y => y.CategoryID == categoryID)).ToList();
            model.ListingItem = new Listing();

            if (listingID > 0)
                model.ListingItem = await _listingService.FindAsync(listingID);

            model.ListingTypeID = model.ListingItem.ListingTypeID;

            return PartialView("_ListingTypes", model);
        }

        [HttpPost]
        public async Task<ActionResult> ListingUpdate(Listing listing, FormCollection form, IEnumerable<HttpPostedFileBase> files, int[] idcama, int[] cantidad)
        {
            if (CacheHelper.Categories.Count == 0)
            {
                TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
                TempData[TempDataKeys.UserMessage] = "[[[There are not categories available yet.]]]";

                return RedirectToAction("Listing", new { id = listing.ID });
            }
            int actualiza = 0;
            bool updateCount = false;            
            int nextPictureOrderId = 0;

            var user = await _aspNetUserService.FindAsync(listing.UserID);

            // Add new listing
            if (listing.ID == 0)
            {
                listing.ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added;
                listing.IP = Request.GetVisitorIP();
                listing.ListingTypeID = 1;
                listing.Title = "";
                listing.Expiration = DateTime.MaxValue.AddDays(-1);
                listing.ContactName = user.FullName;
                listing.ContactEmail = user.Email;
                listing.ShowPhone = false;
                listing.ShowEmail = false;
                listing.UserID = user.Id;                
                updateCount = true;
                _listingService.Insert(listing);
                listing.Currency = "CLP";
            }
            else
            {
                // Update listing
                var listingExisting = await _listingService.FindAsync(listing.ID);

                listingExisting.Title = listing.ID.ToString();
                listingExisting.Description = listing.Description;
                listingExisting.CategoryID = listing.CategoryID;

                listingExisting.Enabled = listing.Enabled;
                listingExisting.Active = listing.Active;
                listingExisting.Premium = listing.Premium;

                var propietario = UserManager.FindById(listing.UserID);
                listingExisting.ContactEmail = propietario.Email;
                listingExisting.ContactName = propietario.FullName;
                listingExisting.ContactPhone = propietario.PhoneNumber;  

                listingExisting.Latitude = listing.Latitude;
                listingExisting.Longitude = listing.Longitude;
                listingExisting.Location = listing.Location;

                listingExisting.ShowPhone = listing.ShowPhone;
                listingExisting.ShowEmail = listing.ShowEmail;

                listingExisting.Beds = listing.Beds;
                listingExisting.Rooms = listing.Rooms;
                listingExisting.Children = listing.Children;


                listingExisting.UserID = listing.UserID;

                listingExisting.Price = listing.Price;
                listingExisting.Currency = listing.Currency;

                listingExisting.Tv = listing.Tv;
                listingExisting.TV_cable = listing.TV_cable;
                listingExisting.Wifi = listing.Wifi;
                listingExisting.Grill = listing.Grill;
                listingExisting.Dishwasher = listing.Dishwasher;
                listingExisting.Elevator = listing.Elevator;
                listingExisting.ShortDescription = listing.ShortDescription;
                listingExisting.SafetyMesh = listing.SafetyMesh;
                listingExisting.Smoker = listing.Smoker;
                listingExisting.Pets = listing.Pets;
                listingExisting.Terrace = listing.Terrace;
                listingExisting.DescribeCondominium = listing.DescribeCondominium;
                listingExisting.FirstLine = listing.FirstLine;
                listingExisting.FloorNumber = listing.FloorNumber;
                listingExisting.M2 = listing.M2;
                listingExisting.Max_Capacity = listing.Max_Capacity;
                listingExisting.NroOfParking = listing.NroOfParking;
                listingExisting.Suite = listing.Suite;
                listingExisting.Washer = listing.Washer;
				listingExisting.Warranty = listing.Warranty;
				listingExisting.CleanlinessPrice = listing.CleanlinessPrice;
				listingExisting.Address = listing.Address;

				//listingExisting.ListingTypeID = listing.ListingTypeID;                

				listingExisting.ObjectState = Repository.Pattern.Infrastructure.ObjectState.Modified;

                _listingService.Update(listingExisting);
                actualiza = 1;
            }

            // Delete existing fields on item
            var customFieldItemQuery = await _customFieldListingService.Query(x => x.ListingID == listing.ID).SelectAsync();
            var customFieldIds = customFieldItemQuery.Select(x => x.ID).ToList();
            foreach (var customFieldId in customFieldIds)
            {
                await _customFieldListingService.DeleteAsync(customFieldId);
            }

            // Elimina las camas

            var listacama = await _detailBedService.Query(x => x.ListingID == listing.ID).SelectAsync();
            var lista = listacama.Select(x => x.ID).ToList();
            foreach (var id in lista)
            {
                await _detailBedService.DeleteAsync(id);
            }


            // Get custom fields
            var customFieldCategoryQuery = await _customFieldCategoryService.Query(x => x.CategoryID == listing.CategoryID).Include(x => x.MetaField.ListingMetas).SelectAsync();
            var customFieldCategories = customFieldCategoryQuery.ToList();

            // Update custom fields
            foreach (var metaCategory in customFieldCategories)
            {
                var field = metaCategory.MetaField;
                var controlType = (BeYourMarket.Model.Enum.Enum_MetaFieldControlType)field.ControlTypeID;

                string controlId = string.Format("customfield_{0}_{1}_{2}", metaCategory.ID, metaCategory.CategoryID, metaCategory.FieldID);

                var formValue = form[controlId];

                if (string.IsNullOrEmpty(formValue))
                    continue;

                formValue = formValue.ToString();

                var itemMeta = new ListingMeta()
                {
                    ListingID = listing.ID,
                    Value = formValue,
                    FieldID = field.ID,
                    ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added
                };

                _customFieldListingService.Insert(itemMeta);
            }

            await _unitOfWorkAsync.SaveChangesAsync();


            // Update photos
            if (Request.Files.Count > 0)
            {
                var itemPictureQuery = _listingPictureservice.Queryable().Where(x => x.ListingID == listing.ID);
                if (itemPictureQuery.Count() > 0)
                    nextPictureOrderId = itemPictureQuery.Max(x => x.Ordering);
            }

            if (files != null && files.Count() > 0)
            {
                foreach (HttpPostedFileBase file in files)
                {
                    if ((file != null) && (file.ContentLength > 0) && !string.IsNullOrEmpty(file.FileName))
                    {
                        // Picture picture and get id
                        var picture = new Picture();
                        picture.MimeType = "image/jpeg";
                        _pictureService.Insert(picture);
                        await _unitOfWorkAsync.SaveChangesAsync();

                        // Format is automatically detected though can be changed.
                        ISupportedImageFormat format = new JpegFormat { Quality = 90 };
                        Size size = new Size(500, 0);

                        //https://naimhamadi.wordpress.com/2014/06/25/processing-images-in-c-easily-using-imageprocessor/
                        // Initialize the ImageFactory using the overload to preserve EXIF metadata.
                        using (ImageFactory imageFactory = new ImageFactory(preserveExifData: true))
                        {
                            var path = Path.Combine(Server.MapPath("~/images/listing"), string.Format("{0}.{1}", picture.ID.ToString("00000000"), "jpg"));

                            // Load, resize, set the format and quality and save an image.
                            imageFactory.Load(file.InputStream)
                                        .Resize(size)
                                        .Format(format)
                                        .Save(path);
                        }

                        var itemPicture = new ListingPicture();
                        itemPicture.ListingID = listing.ID;
                        itemPicture.PictureID = picture.ID;
                        itemPicture.Ordering = nextPictureOrderId;

                        _listingPictureservice.Insert(itemPicture);

                        nextPictureOrderId++;
                    }
                }
            }
            //INSERTANDO CAMAS
            if (idcama != null)
            {
                List<int> listaid = new List<int>();
                for (int i = 0; i < idcama.Length; i++)
                {
                    if (listaid.Count != 0) //entra aca cuando no es la primera recorrida
                    {
                        if (!listaid.Contains(idcama[i]))
                        {
                            DetailBed detallecama = new DetailBed();
                            detallecama.TypeOfBedID = idcama[i];
                            detallecama.Quantity = cantidad[i];
                            detallecama.ListingID = listing.ID;
                            _detailBedService.Insert(detallecama);
                            listaid.Add(idcama[i]);
                        }
                    }
                    else //entra primero por no tener ninguna cama en la listaid 
                    {
                        DetailBed detallecama = new DetailBed();
                        detallecama.TypeOfBedID = idcama[i];
                        detallecama.Quantity = cantidad[i];
                        detallecama.ListingID = listing.ID;
                        listaid.Add(idcama[i]);
                        _detailBedService.Insert(detallecama);
                    }
                }
            }   //fin insertando cama

            await _unitOfWorkAsync.SaveChangesAsync();

            if (actualiza == 0)
            {
                listing.Title = listing.ID.ToString();
                _listingService.Update(listing);
                await _unitOfWorkAsync.SaveChangesAsync();
            }

            // Update statistics count
            if (updateCount)
            {
                _sqlDbService.UpdateCategoryItemCount(listing.CategoryID);
                _dataCacheService.RemoveCachedItem(CacheKeys.Statistics);
            }

            return RedirectToAction("Listings");
        }

        [HttpPost]
        public async Task<ActionResult> ListingDelete(int id)
        {
            var item = await _listingService.FindAsync(id);
            var orderQuery = await _orderService.Query(x => x.ListingID == id).SelectAsync();

            // Delete item if no orders associated with it
            if (item.Orders.Count > 0)
            {
                TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
                TempData[TempDataKeys.UserMessage] = "[[[You cannot delete item with orders! You can deactivate it instead.]]]";

                return RedirectToAction("Listings");
            }

            await _listingService.DeleteAsync(id);

            await _unitOfWorkAsync.SaveChangesAsync();

            TempData[TempDataKeys.UserMessage] = "[[[The listing has been deleted.]]]";

            return RedirectToAction("Listings");
        }

        [HttpPost]
        public async Task<ActionResult> ListingTypeDelete(int id)
        {
            if (CacheHelper.ListingTypes.Count <= 1)
            {
                TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
                TempData[TempDataKeys.UserMessage] = "[[[You cannot the listing type. There should be at least one listing type.]]]";

                return RedirectToAction("ListingTypes");
            }

            var item = await _listingTypeService.FindAsync(id);                       

            await _listingTypeService.DeleteAsync(id);

            await _unitOfWorkAsync.SaveChangesAsync();

            TempData[TempDataKeys.UserMessage] = "[[[The listing type has been deleted.]]]";

            _dataCacheService.RemoveCachedItem(CacheKeys.ListingTypes);

            return RedirectToAction("ListingTypes");
        }

        [HttpPost]
        public async Task<ActionResult> CategoryDelete(int id)
        {
            var item = await _categoryService.FindAsync(id);

            await _categoryService.DeleteAsync(id);

            await _unitOfWorkAsync.SaveChangesAsync();

            TempData[TempDataKeys.UserMessage] = "[[[The category has been deleted.]]]";

            _dataCacheService.RemoveCachedItem(CacheKeys.Categories);

            return RedirectToAction("Categories");
        }

        public async Task<ActionResult> ListingPhotoDelete(int id)
        {
            try
            {
                await _pictureService.DeleteAsync(id);
                var itemPicture = _listingPictureservice.Query(x => x.PictureID == id).Select().FirstOrDefault();

                if (itemPicture != null)
                    await _listingPictureservice.DeleteAsync(itemPicture.ID);

                await _unitOfWorkAsync.SaveChangesAsync();

                var path = Path.Combine(Server.MapPath("~/images/listing"), string.Format("{0}.{1}", id.ToString("00000000"), "jpg"));

                System.IO.File.Delete(path);

                var result = new { Success = "true", Message = "" };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                var result = new { Success = "false", Message = ex.Message };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult ListingTypes()
        {
            var grid = new ListingTypesGrid(_listingTypeService.Queryable().OrderBy(x => x.ID));


            var model = new ListingTypeModel()
            {
                Grid = grid
            };

            return View(model);
        }

        public async Task<ActionResult> ListingTypeUpdate(int? id)
        {
            ListingType ListingType = new ListingType();

            if (id.HasValue)
            {
                ListingType = await _listingTypeService.FindAsync(id);

                if (ListingType == null)
                    return new HttpNotFoundResult();
            }

            return View(ListingType);
        }

        [HttpPost]
        public async Task<ActionResult> ListingTypeUpdate(ListingType model)
        {
            var isNew = false;

            if (model.ID == 0)
            {
                model.ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added;

                isNew = true;
                _listingTypeService.Insert(model);
            }
            else
            {
                var listingTypeExisting = await _listingTypeService.FindAsync(model.ID);

                listingTypeExisting.Name = model.Name;
                listingTypeExisting.ButtonLabel = model.ButtonLabel;
                listingTypeExisting.PriceUnitLabel = model.PriceUnitLabel;
                listingTypeExisting.ShippingEnabled = model.ShippingEnabled;

                listingTypeExisting.PriceEnabled = model.PriceEnabled;
                listingTypeExisting.PaymentEnabled = model.PaymentEnabled;
                if (model.PaymentEnabled)
                {
                    listingTypeExisting.OrderTypeID = model.OrderTypeID;
                    listingTypeExisting.OrderTypeLabel = model.OrderTypeLabel;
                }

                listingTypeExisting.ObjectState = Repository.Pattern.Infrastructure.ObjectState.Modified;

                _listingTypeService.Update(listingTypeExisting);
            }

            await _unitOfWorkAsync.SaveChangesAsync();

            // Add item type to each category
            if (isNew)
            {
                foreach (var category in CacheHelper.Categories)
                {
                    _categoryListingTypeService.Insert(new CategoryListingType()
                    {
                        CategoryID = category.ID,
                        ListingTypeID = model.ID,
                        ObjectState = Repository.Pattern.Infrastructure.ObjectState.Added
                    });
                }
                await _unitOfWorkAsync.SaveChangesAsync();
            }

            _dataCacheService.RemoveCachedItem(CacheKeys.ListingTypes);

            return RedirectToAction("ListingTypes");
        }

        [HttpPost]
        public async Task<ActionResult> ListingBlock(int id, string descripcion)
        {
            var listingExisting = await _listingService.FindAsync(id);

            listingExisting.Active = false;

            _listingService.Update(listingExisting);

            ListingObservation observaciones = new ListingObservation() {
                Type = "Desactivar",
                Description = descripcion,
                Created = DateTime.Now,
                ListingID = id
            };
            _listingObservationService.Insert(observaciones);

            await _unitOfWorkAsync.SaveChangesAsync();

            TempData[TempDataKeys.UserMessage] = "[[[The listing has been blocked.]]]";

            return RedirectToAction("Listings");
        }

        [HttpPost]
        public async Task<ActionResult> ListingUnlock(int id, string descripcion)
        {
            var listingExisting = await _listingService.FindAsync(id);

            listingExisting.Active = true;

            _listingService.Update(listingExisting);

            ListingObservation observaciones = new ListingObservation()
            {
                Type = "Activar",
                Description = descripcion,
                Created = DateTime.Now,
                ListingID = id
            };
            _listingObservationService.Insert(observaciones);

            await _unitOfWorkAsync.SaveChangesAsync();

            TempData[TempDataKeys.UserMessage] = "[[[The listing has been unlock.]]]";

            return RedirectToAction("Listings");
        }
		#endregion

		//Exportar Propiedades a CSV
		public async Task<ActionResult> ExportarPropiedades()
		{
			IEnumerable<Listing> lista = await _listingService.Query().SelectAsync();
			if (lista.Count() > 0)
			{
				string header = @"""ID"";""Titulo"";""Descripcion Corta"";""Direccion"";""Precio"";""Condominio"";""Tipo de propiedad"";""Capacidad"";""Habitaciones"";""Banos"";""Cantidad de estacionamientos"";""Numeracion de estacionamiento"";""M2"";""Cantidad de camas"";""Piezas Suite"";""Lavavajillas"";""Lavadora"";""Parrilla"";""TV cable"";""Wi fi"";""Ascensor"";""Malla de seguridad""";
                StringBuilder sb = new StringBuilder();
				sb.AppendLine(header);

				foreach (var i in lista)
				{
					var condominium = _categoryService.Query(x => x.ID == i.CategoryID).Select().FirstOrDefault();
					sb.AppendLine(string.Join(";",
						string.Format(@"""{0}""", i.ID),
						string.Format(@"""{0}""", i.Title),
						string.Format(@"""{0}""", i.ShortDescription),
						string.Format(@"""{0}""", i.Address),
						string.Format(@"""{0}""", i.Price),
						string.Format(@"""{0}""", condominium.Name),
						string.Format(@"""{0}""", i.TypeOfProperty),
						string.Format(@"""{0}""", i.Max_Capacity),
						string.Format(@"""{0}""", i.Rooms),
						string.Format(@"""{0}""", i.Bathrooms),
						string.Format(@"""{0}""", i.ParkingLot),
						string.Format(@"""{0}""", i.NroOfParking),
						string.Format(@"""{0}""", i.M2),
						string.Format(@"""{0}""", i.Beds),
						string.Format(@"""{0}""", i.Suite),
						string.Format(@"""{0}""", i.Dishwasher ? "Si" : "No"),
						string.Format(@"""{0}""", i.Washer ? "Si" : "No"),
						string.Format(@"""{0}""", i.Grill ? "Si" : "No"),
						string.Format(@"""{0}""", i.TV_cable ? "Si" : "No"),
						string.Format(@"""{0}""", i.Wifi ? "Si" : "No"),
						string.Format(@"""{0}""", i.Elevator ? "Si" : "No"),
						string.Format(@"""{0}""", i.SafetyMesh ? "Si" : "No")));
				}

				// Download Here
				HttpContext context = System.Web.HttpContext.Current;
				context.Response.Write(sb.ToString());
				context.Response.ContentType = "text/csv";
				context.Response.AddHeader("Content-Disposition", "attachment; filename= Propiedades.csv");
				context.Response.End();
			}
			else
			{
				TempData[TempDataKeys.UserMessageAlertState] = "bg-danger";
				TempData[TempDataKeys.UserMessage] = "[[[No items to export.]]]";
				return RedirectToAction("Users", "Manage");
			}

			return RedirectToAction("Users", "Manage");
		}
	}
}