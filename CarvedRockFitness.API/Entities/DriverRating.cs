namespace CarvedRockFitness.API.Entities;

public class DriverRating
{
    public int      Id          { get; set; }
    public int      DriverId    { get; set; }
    public string   ReviewerName { get; set; } = "Anonymous";
    public int      Stars       { get; set; }  // 1–5
    public string?  Comment     { get; set; }
    public bool     IsDemoData  { get; set; } = false;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;
}
