using Azure.Search.Documents.Indexes.Models;

namespace BaGet.Azure
{
    /// <summary>  
    /// A custom analyzer for case insensitive exact match.  
    /// </summary>  
    public static class ExactMatchCustomAnalyzer
    {
        public const string Name = "baget-exact-match-analyzer";

        /// <summary>  
        /// Gets the instance of the custom analyzer.  
        /// </summary>  
        public static CustomAnalyzer Instance => new CustomAnalyzer(Name, LexicalTokenizerName.Keyword);
    }
}
