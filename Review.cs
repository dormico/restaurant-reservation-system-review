namespace Review;
# nullable enable
public class Review
{
    public string? id { get; set; }
    public string? Date { get; set; }
    public int Rating { get; set; }
    public string? Text { get; set; }
    public string? Answer { get; set; }
}

public class RestaurantItem
{
    public string? partitionKey { get; set; }
    public string? id { get; set; }
    public Review[]? Reviews { get; set; }
    public int Rating { get; set; }
}