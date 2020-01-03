using System.Windows.Controls;
using System.Globalization;

namespace DAQ
{
    public class NotEmptyValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return string.IsNullOrWhiteSpace((value ?? "").ToString())
              ? new ValidationResult(false, "Field is required.")
              : ValidationResult.ValidResult;
        }
    }
}
