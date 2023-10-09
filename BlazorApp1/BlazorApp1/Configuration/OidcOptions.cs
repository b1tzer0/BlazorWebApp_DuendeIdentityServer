namespace BlazorApp1.Configuration
{
    public record OidcOptions
    {
        /// <summary>
        /// Gets or sets the authority.
        /// </summary>
        /// <value>The authority.</value>
        public string? Authority { get; set; }

        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        /// <value>The client identifier.</value>
        public string? ClientId { get; set; }

        /// <summary>
        /// Gets or sets the client secret.
        /// </summary>
        /// <value>The client secret.</value>
        public string? ClientSecret { get; set; }
    }
}
