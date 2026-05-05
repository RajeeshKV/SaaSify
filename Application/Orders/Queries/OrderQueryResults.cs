namespace Application.Orders.Queries
{
    public class HandleStripeCancelQueryResult
    {
        public bool Success { get; set; }
        public string RedirectUrl { get; set; }
        public string Error { get; set; }
    }

    public class HandleStripeSuccessQueryResult
    {
        public bool Success { get; set; }
        public string RedirectUrl { get; set; }
        public string Error { get; set; }
    }
}
