using System;
using System.ComponentModel.DataAnnotations;

namespace BaGet.Azure
{
    /// <summary>  
    /// Configuration options for connecting to Azure Search services.  
    /// </summary>  
    public class AzureSearchOptions
    {
        /// <summary>  
        /// Gets or sets the name of the Azure Search service.  
        /// </summary>  
        [Required(ErrorMessage = "The AccountName is required.")]
        public string AccountName { get; set; }

        /// <summary>  
        /// Gets or sets the API key for accessing the Azure Search service.  
        /// </summary>  
        [Required(ErrorMessage = "The ApiKey is required.")]
        public string ApiKey { get; set; }

        /// <summary>  
        /// Gets or sets the endpoint URL for the Azure Search service.  
        /// </summary>  
        [Required(ErrorMessage = "The Endpoint is required.")]
        public Uri Endpoint { get; set; }

        public string IndexName { get; set; }
    }
}
