namespace Rock.Tv.Classes
{
    /// <summary>
    /// POCO for a Roku TV Application Settings.
    /// </summary>
    public class RokuTvApplicationSettings : ApplicationSettings
    {
        /// <summary>
        /// Gets or sets the API key.
        /// </summary>
        /// <value>
        /// The API key.
        /// </value>
        public int? ApiKeyId { get; set; }
    }
}
