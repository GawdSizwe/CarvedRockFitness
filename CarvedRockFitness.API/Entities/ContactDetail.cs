using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarvedRockFitness.API.Entities;

public class ContactDetail
{
    [Key]
    public int Id { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? WhatsApp { get; set; }
    public string? BookingUrl { get; set; }

    public int SuggestionId { get; set; }
    [ForeignKey("SuggestionId")]
    public Suggestion? Suggestion { get; set; }
}
