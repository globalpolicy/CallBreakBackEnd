namespace CallBreakBackEnd.Models.DTO.Output
{
    // It is crucial that the members of this class be public PROPERTIES and not FIELDS. Else, the response will be empty.
    public class BaseJsonResponse
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
    }
}
