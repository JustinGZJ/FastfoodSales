using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Globalization;

namespace DAQ
{
    public class MCDAddressValidationRule : ValidationRule
    {
        public string Area { get; set; }
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var regex = new Regex(@"D(\d{1,6})");
            return !regex.IsMatch(value.ToString())
              ? new ValidationResult(false, "请输入D区地址，例如D100.")
              : ValidationResult.ValidResult;
        }

    }
}
