namespace Movies.Api.Auth
{
    public static class IdentityExtensions
    {
        public static Guid? GetUserId(this HttpContext context)
        {
            var userId = context.User.Claims.SingleOrDefault(x => x.Type == "user");
            
            if (Guid.TryParse(userId?.Value, out var parseId))
            {
                return parseId;
            }

            return null;
        }
    }
}
