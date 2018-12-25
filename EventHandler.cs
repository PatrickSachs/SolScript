namespace PSUtility
{
    /// <summary>
    /// An event handler with a type safe sender.
    /// </summary>
    /// <typeparam name="TSender">The sender type.</typeparam>
    /// <typeparam name="TArgs">The event type.</typeparam>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The event.</param>
    public delegate void EventHandler<in TSender, in TArgs>(TSender sender, TArgs e);
}