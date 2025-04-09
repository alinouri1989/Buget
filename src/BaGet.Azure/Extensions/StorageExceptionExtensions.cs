using System.Net;
using Azure;
using Azure.Data.Tables;

namespace BaGet.Azure
{
    internal static class StorageExceptionExtensions
    {
        /// <summary>  
        /// Checks if the exception indicates that the resource already exists.  
        /// </summary>  
        public static bool IsAlreadyExistsException(this RequestFailedException e)
        {
            return e.Status == (int)HttpStatusCode.Conflict;
        }

        /// <summary>  
        /// Checks if the exception indicates that the resource was not found.  
        /// </summary>  
        public static bool IsNotFoundException(this RequestFailedException e)
        {
            return e.Status == (int)HttpStatusCode.NotFound;
        }

        /// <summary>  
        /// Checks if the exception indicates that the resource already exists.  
        /// </summary>  
        public static bool IsAlreadyExistsException(this TableTransactionFailedException e)
        {
            return e.Status == (int)HttpStatusCode.Conflict;
        }

        /// <summary>  
        /// Checks if the exception indicates that a precondition for the operation failed.  
        /// </summary>  
        public static bool IsPreconditionFailedException(this RequestFailedException e)
        {
            return e.Status == (int)HttpStatusCode.PreconditionFailed;
        }
    }
}
