namespace Chat.Web.Models
{
    public class Enums
    {
       public enum ResponseType
        {
            success,
            error,
            warning,
            info
        }

        public enum HttpMethod
        {
            GET,
            POST,
            UPDATE,
            DELETE
        }

    }
}
