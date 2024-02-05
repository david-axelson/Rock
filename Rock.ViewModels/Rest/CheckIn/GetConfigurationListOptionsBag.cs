using System;

namespace Rock.ViewModels.Rest.CheckIn
{
    /// <summary>
    /// The request parameters to use when generating the list of valid
    /// check-in configurations.
    /// </summary>
    public class GetConfigurationListOptionsBag
    {
        /// <summary>
        /// Gets or sets the kiosk unique identifier to use when filtering
        /// the configuration list.
        /// </summary>
        /// <value>The kiosk unique identifier.</value>
        public Guid? Kiosk { get; set; }
    }
}
