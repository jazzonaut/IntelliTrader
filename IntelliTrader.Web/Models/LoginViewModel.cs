using System.ComponentModel.DataAnnotations;

namespace IntelliTrader.Web.Models
{
    public class LoginViewModel : BaseViewModel
    {
        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
