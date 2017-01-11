using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BeYourMarket.Web.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required]
        [Display(Name = "Email")]
        public string Email { get; set; }
    }

    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }

    public class SendCodeViewModel
    {
        public string SelectedProvider { get; set; }
        public ICollection<System.Web.Mvc.SelectListItem> Providers { get; set; }
        public string ReturnUrl { get; set; }
        public bool RememberMe { get; set; }
    }

    public class VerifyCodeViewModel
    {
        [Required]
        public string Provider { get; set; }

        [Required]
        [Display(Name = "[[[Code]]]")]
        public string Code { get; set; }
        public string ReturnUrl { get; set; }

        [Display(Name = "[[[Remember this browser?]]]")]
        public bool RememberBrowser { get; set; }

        public bool RememberMe { get; set; }
    }

    public class ForgotViewModel
    {
        [Required]
        [Display(Name = "[[[Email]]]")]
        public string Email { get; set; }
    }

    public class LoginViewModel
    {
        [Required]
        [Display(Name = "[[[Email]]]")]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "[[[Password]]]")]
        public string Password { get; set; }

        [Display(Name = "[[[Remember me?]]]")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "[[[Email]]]")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "[[[The {0} must be at least {2} characters long.]]]", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "[[[Password]]]")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "[[[Confirm password]]]")]
        [Compare("Password", ErrorMessage = "[[[The password and confirmation password do not match.]]]")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "[[[First Name]]]")]
        public string FirstName { get; set; }

        [Display(Name = "[[[Last Name]]]")]
        public string LastName { get; set; }

        [Display(Name = "[[[Gender]]]")]
        public string Gender { get; set; }
    }

    public class ResetPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "[[[Email]]]")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "[[[The {0} must be at least {2} characters long.]]]", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "[[[Password]]]")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "[[[Confirm password]]]")]
        [Compare("Password", ErrorMessage = "[[[The password and confirmation password do not match.]]]")]
        public string ConfirmPassword { get; set; }

        public string Code { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "[[[Email]]]")]
        public string Email { get; set; }
    }

    public class ConfirmOrder
    {
        [Required]
        [EmailAddress]
        [Display(Name = "[[[Email]]]")]
        public string Email { get; set; }

        [Display(Name = "[[[Name]]]")]
        public string Name { get; set; }

        [Display(Name = "[[[ID]]]")]
        public int Id { get; set; }

        [Display(Name = "[[[FromDate]]]")]
        public string FromDate { get; set; }

        [Display(Name = "[[[ToDate]]]")]
        public string ToDate { get; set; }

        [Display(Name = "[[[Adults]]]")]
        public int? Adults { get; set; }

        [Display(Name = "[[[Children]]]")]
        public int? Children { get; set; }

        [Display(Name = "[[[Rent]]]")]
        public double? Rent { get; set; }

        [Display(Name = "[[[Service]]]")]
        public double? Service { get; set; }

        [Display(Name = "[[[CleanlinessPrice]]]")]
        public double? CleanlinessPrice { get; set; }

        [Display(Name = "[[[Total]]]")]
        public double? Total { get; set; }

        [Display(Name = "[[[Description]]]")]
        public string Description { get; set; }

        [Display(Name = "[[[ShortDescription]]]")]
        public string ShortDescription { get; set; }

        [Display(Name = "[[[Condominium]]]")]
        public string Condominium { get; set; }

        [Display(Name = "[[[TypeOfProperty]]]")]
        public string TypeOfProperty { get; set; }

        [Display(Name = "[[[Capacity]]]")]
        public int Capacity { get; set; }

        [Display(Name = "[[[Rooms]]]")]
        public int? Rooms { get; set; }

        [Display(Name = "[[[Beds]]]")]
        public int? Beds { get; set; }

        [Display(Name = "[[[SuiteRooms]]]")]
        public int? SuiteRooms { get; set; }

        [Display(Name = "[[[Bathrooms]]]")]
        public int? Bathrooms { get; set; }

        [Display(Name = "[[[Dishwasher]]]")]
        public string Dishwasher { get; set; }

        [Display(Name = "[[[Washer]]]")]
        public string Washer { get; set; }

        [Display(Name = "[[[Grill]]]")]
        public string Grill { get; set; }

        [Display(Name = "[[[TvCable]]]")]
        public string TvCable { get; set; }

        [Display(Name = "[[[Wifi]]]")]
        public string Wifi { get; set; }

        [Display(Name = "[[[Elevator]]]")]
        public string Elevator { get; set; }

        [Display(Name = "[[[FloorNumber]]]")]
        public int? FloorNumber { get; set; }

        [Display(Name = "[[[Stay]]]")]
        public string Stay { get; set; }

        [Display(Name = "[[[ConditionHouse]]]")]
        public string ConditionHouse { get; set; }

		[Display(Name = "[[[OrderId]]]")]
		public int OrderId { get; set; }
	}
}
