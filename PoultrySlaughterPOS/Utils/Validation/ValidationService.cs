using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace PoultrySlaughterPOS.Utils.Validation
{
    /// <summary>
    /// Enterprise-grade validation service providing comprehensive input validation and business rule enforcement
    /// Implements sophisticated validation patterns with detailed error reporting and performance optimization
    /// </summary>
    public class ValidationService
    {
        private readonly ILogger<ValidationService> _logger;

        public ValidationService(ILogger<ValidationService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Validates object using DataAnnotations with comprehensive error collection
        /// Provides detailed validation results with localized error messages
        /// </summary>
        /// <param name="obj">Object to validate</param>
        /// <returns>Validation result containing success status and error details</returns>
        public BusinessValidationResult ValidateObject(object obj)
        {
            try
            {
                var validationContext = new ValidationContext(obj);
                var validationResults = new List<ValidationResult>();

                bool isValid = Validator.TryValidateObject(obj, validationContext, validationResults, validateAllProperties: true);

                var errorMessages = validationResults
                    .Where(vr => !string.IsNullOrEmpty(vr.ErrorMessage))
                    .Select(vr => vr.ErrorMessage!)
                    .ToList();

                return new BusinessValidationResult
                {
                    IsValid = isValid,
                    ErrorMessages = errorMessages,
                    ValidationDetails = validationResults.ToDictionary(
                        vr => vr.MemberNames.FirstOrDefault() ?? "General",
                        vr => vr.ErrorMessage ?? "Validation failed"
                    )
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during object validation for type: {ObjectType}", obj?.GetType().Name);

                return new BusinessValidationResult
                {
                    IsValid = false,
                    ErrorMessages = new List<string> { "Validation system error occurred" },
                    ValidationDetails = new Dictionary<string, string> { { "System", "Validation infrastructure failure" } }
                };
            }
        }

        /// <summary>
        /// Validates decimal input with business rule enforcement and range checking
        /// Implements sophisticated numeric validation with Arabic numeral support
        /// </summary>
        /// <param name="value">String representation of decimal value</param>
        /// <param name="min">Minimum allowable value</param>
        /// <param name="max">Maximum allowable value</param>
        /// <param name="allowEmpty">Whether empty/null values are permitted</param>
        /// <returns>Validation success indicator</returns>
        public bool IsValidDecimal(string value, decimal min = 0, decimal max = decimal.MaxValue, bool allowEmpty = false)
        {
            try
            {
                // Handle empty/null input according to business rules
                if (string.IsNullOrWhiteSpace(value))
                {
                    return allowEmpty;
                }

                // Normalize Arabic numerals to Western numerals for parsing
                var normalizedValue = NormalizeNumericInput(value);

                if (!decimal.TryParse(normalizedValue, out decimal parsedValue))
                {
                    _logger.LogDebug("Decimal parsing failed for input: {Value}", value);
                    return false;
                }

                bool isInRange = parsedValue >= min && parsedValue <= max;

                if (!isInRange)
                {
                    _logger.LogDebug("Decimal value {Value} outside acceptable range [{Min}, {Max}]", parsedValue, min, max);
                }

                return isInRange;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating decimal input: {Value}", value);
                return false;
            }
        }

        /// <summary>
        /// Validates phone number format with international and local pattern support
        /// Implements comprehensive telecommunication number validation
        /// </summary>
        /// <param name="phoneNumber">Phone number string to validate</param>
        /// <returns>Validation success indicator</returns>
        public bool IsValidPhoneNumber(string phoneNumber)
        {
            try
            {
                // Empty phone numbers are acceptable for optional fields
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return true;
                }

                var cleanedNumber = phoneNumber.Trim();

                // Comprehensive phone number pattern supporting international formats
                var phonePattern = @"^[\d\s\-\+\(\)]{7,15}$";
                bool matchesPattern = System.Text.RegularExpressions.Regex.IsMatch(cleanedNumber, phonePattern);

                if (!matchesPattern)
                {
                    _logger.LogDebug("Phone number validation failed for: {PhoneNumber}", phoneNumber);
                }

                return matchesPattern;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating phone number: {PhoneNumber}", phoneNumber);
                return false;
            }
        }

        /// <summary>
        /// Validates business entity with custom rule evaluation
        /// Implements domain-specific validation logic for POS entities
        /// </summary>
        /// <typeparam name="T">Entity type for validation</typeparam>
        /// <param name="entity">Entity instance to validate</param>
        /// <returns>Comprehensive validation result with business context</returns>
        public BusinessValidationResult ValidateBusinessEntity<T>(T entity) where T : class
        {
            try
            {
                var result = ValidateObject(entity);

                // Apply domain-specific business rules based on entity type
                result = ApplyBusinessRules(entity, result);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Business entity validation failed for type: {EntityType}", typeof(T).Name);
                return new BusinessValidationResult
                {
                    IsValid = false,
                    ErrorMessages = new List<string> { "Business validation system error" }
                };
            }
        }

        /// <summary>
        /// Normalizes numeric input for consistent parsing across different locales
        /// Handles Arabic numerals and various formatting conventions
        /// </summary>
        /// <param name="input">Raw numeric input string</param>
        /// <returns>Normalized numeric string for parsing</returns>
        private static string NormalizeNumericInput(string input)
        {
            // Arabic to Western numeral mapping for international support
            var arabicToWestern = new Dictionary<char, char>
            {
                {'٠', '0'}, {'١', '1'}, {'٢', '2'}, {'٣', '3'}, {'٤', '4'},
                {'٥', '5'}, {'٦', '6'}, {'٧', '7'}, {'٨', '8'}, {'٩', '9'}
            };

            var normalized = input.ToCharArray();
            for (int i = 0; i < normalized.Length; i++)
            {
                if (arabicToWestern.ContainsKey(normalized[i]))
                {
                    normalized[i] = arabicToWestern[normalized[i]];
                }
            }

            return new string(normalized);
        }

        /// <summary>
        /// Applies domain-specific business rules for entity validation
        /// Implements sophisticated business logic validation patterns
        /// </summary>
        /// <typeparam name="T">Entity type for rule application</typeparam>
        /// <param name="entity">Entity instance for rule evaluation</param>
        /// <param name="baseResult">Base validation result for enhancement</param>
        /// <returns>Enhanced validation result with business rule evaluation</returns>
        private BusinessValidationResult ApplyBusinessRules<T>(T entity, BusinessValidationResult baseResult) where T : class
        {
            // Implementation would include specific business rules for different entity types
            // This pattern allows for extensible business rule validation

            switch (entity)
            {
                case Models.DTOs.CreateInvoiceDto invoice:
                    return ValidateInvoiceBusinessRules(invoice, baseResult);

                case Models.DTOs.CreateCustomerDto customer:
                    return ValidateCustomerBusinessRules(customer, baseResult);

                default:
                    return baseResult;
            }
        }

        /// <summary>
        /// Validates invoice-specific business rules with comprehensive logic
        /// Implements financial transaction validation patterns
        /// </summary>
        /// <param name="invoice">Invoice DTO for validation</param>
        /// <param name="baseResult">Base validation result for enhancement</param>
        /// <returns>Enhanced validation result with invoice-specific rules</returns>
        private BusinessValidationResult ValidateInvoiceBusinessRules(Models.DTOs.CreateInvoiceDto invoice, BusinessValidationResult baseResult)
        {
            var enhancedResult = new BusinessValidationResult(baseResult);

            // Business rule: Net weight must be positive
            var netWeight = invoice.GrossWeight - invoice.CagesWeight;
            if (netWeight <= 0)
            {
                enhancedResult.AddError("NetWeight", "Net weight must be greater than zero after subtracting cage weight");
            }

            // Business rule: Reasonable weight per cage ratio
            if (invoice.CagesCount > 0)
            {
                var weightPerCage = netWeight / invoice.CagesCount;
                if (weightPerCage < 0.5m || weightPerCage > 50m)
                {
                    enhancedResult.AddError("WeightPerCage", "Weight per cage appears unreasonable (should be between 0.5 and 50 kg)");
                }
            }

            return enhancedResult;
        }

        /// <summary>
        /// Validates customer-specific business rules with relationship constraints
        /// Implements customer relationship management validation patterns
        /// </summary>
        /// <param name="customer">Customer DTO for validation</param>
        /// <param name="baseResult">Base validation result for enhancement</param>
        /// <returns>Enhanced validation result with customer-specific rules</returns>
        private BusinessValidationResult ValidateCustomerBusinessRules(Models.DTOs.CreateCustomerDto customer, BusinessValidationResult baseResult)
        {
            var enhancedResult = new BusinessValidationResult(baseResult);

            // Business rule: Credit limit should be reasonable for business size
            if (customer.CreditLimit > 100000m)
            {
                enhancedResult.AddWarning("CreditLimit", "Credit limit exceeds typical business thresholds - please verify");
            }

            return enhancedResult;
        }
    }

    /// <summary>
    /// Business validation result containing comprehensive validation feedback
    /// Implements sophisticated result patterns with detailed error context
    /// </summary>
    public class BusinessValidationResult
    {
        public bool IsValid { get; set; } = true;
        public List<string> ErrorMessages { get; set; } = new();
        public List<string> WarningMessages { get; set; } = new();
        public Dictionary<string, string> ValidationDetails { get; set; } = new();

        public BusinessValidationResult() { }

        public BusinessValidationResult(BusinessValidationResult other)
        {
            IsValid = other.IsValid;
            ErrorMessages = new List<string>(other.ErrorMessages);
            WarningMessages = new List<string>(other.WarningMessages);
            ValidationDetails = new Dictionary<string, string>(other.ValidationDetails);
        }

        /// <summary>
        /// Adds validation error with property context
        /// </summary>
        /// <param name="propertyName">Property name for error context</param>
        /// <param name="errorMessage">Detailed error message</param>
        public void AddError(string propertyName, string errorMessage)
        {
            IsValid = false;
            ErrorMessages.Add(errorMessage);
            ValidationDetails[propertyName] = errorMessage;
        }

        /// <summary>
        /// Adds validation warning with property context
        /// </summary>
        /// <param name="propertyName">Property name for warning context</param>
        /// <param name="warningMessage">Detailed warning message</param>
        public void AddWarning(string propertyName, string warningMessage)
        {
            WarningMessages.Add(warningMessage);
            if (!ValidationDetails.ContainsKey(propertyName))
            {
                ValidationDetails[propertyName] = warningMessage;
            }
        }
    }
}