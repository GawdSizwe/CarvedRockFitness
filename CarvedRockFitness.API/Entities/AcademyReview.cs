namespace CarvedRockFitness.API.Entities;

public class AcademyReview
{
    public int      Id            { get; set; }
    public string   ReviewerName  { get; set; } = "Anonymous";
    public string?  ReviewerRole  { get; set; } // "Parent", "Player", "Coach", etc.
    public int      Stars         { get; set; }  // 1–5
    public string   Comment       { get; set; } = string.Empty;
    public bool     Approved      { get; set; } = false;
    public bool     IsDemoData    { get; set; } = false;
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;
}
