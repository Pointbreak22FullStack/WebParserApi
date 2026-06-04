namespace Domain.Entities
{
    public class ParsingResult
    {
        public int Id { get; set; }
        public Guid ParserTaskId { get; set; }
        public bool IsSuccess { get; set; }
        public TimeSpan Duration { get; set; }
        public int AccountId { get; set; }
        public string Data { get; set; }
    }
}
