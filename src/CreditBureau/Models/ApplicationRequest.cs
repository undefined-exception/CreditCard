namespace CreditBureau.Models
{
    public class ApplicationRequest
    {
        public string SSN { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public decimal AnnualIncome { get; set; }
        public string EmploymentStatus { get; set; }
        public string HousingStatus { get; set; }
        public decimal MonthlyRentOrMortgage { get; set; }
    }
}
