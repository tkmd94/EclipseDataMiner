namespace EclipseDataMiner.Models
{
    /// <summary>
    /// Regular expression cheat sheet snippet item.
    /// </summary>
    public class RegexSnippetItem
    {
        /// <summary>
        /// Regular expression pattern string.
        /// </summary>
        public string Pattern { get; set; }

        /// <summary>
        /// Snippet title (e.g. Starts with).
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Description of the snippet pattern.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Example clinical contour IDs that match this pattern.
        /// </summary>
        public string Example { get; set; }
    }
}
