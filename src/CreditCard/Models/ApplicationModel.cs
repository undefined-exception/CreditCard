using System.ComponentModel.DataAnnotations;

namespace CreditCard.Models
{
    public class ApplicationModel
    {
        [Key]
        public Guid Id { get; set; }

        public ApplicationStatus Status { get; set; }

        [MaxLength(128)]
        public string SSN { get; set; }

        [MaxLength(128)]
        public string FirstName { get; set; }

        [MaxLength(128)]
        public string LastName { get; set; }

        [MaxLength(128)]
        public DateTime DateOfBirth { get; set; }
        
        public decimal AnnualIncome { get; set; }

        [MaxLength(128)]
        public string EmploymentStatus { get; set; }

        [MaxLength(128)]
        public string HousingStatus { get; set; }
      
        public decimal MonthlyRentOrMortgage { get; set; }

        public string? ExternalApplicationId { get; set; }
    }
}
