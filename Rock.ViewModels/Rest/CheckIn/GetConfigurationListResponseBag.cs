using System.Collections.Generic;

using Rock.ViewModels.CheckIn;

namespace Rock.ViewModels.Rest.CheckIn
{
    /// <summary>
    /// The configurations that are valid for use with check-in.
    /// </summary>
    public class GetConfigurationListResponseBag
    {
        /// <summary>
        /// Gets or sets the configurations that are valid for use with check-in.
        /// </summary>
        /// <value>The configuration items.</value>
        public List<ConfigurationItemSummaryBag> Configurations { get; set; }

        /// <summary>
        /// Gets or sets the areas that are valid for use with check-in.
        /// </summary>
        /// <value>The areas items.</value>
        public List<AreaItemSummaryBag> Areas { get; set; }
    }
}
