namespace UXAV.AVnet.Core.Models
{
    public interface IGenericItem
    {
        /// <summary>
        ///     The unique ID of the <see cref="IGenericItem" />
        /// </summary>
        uint Id { get; }

        /// <summary>
        ///     The name of the <see cref="IGenericItem" />
        /// </summary>
        string Name { get; }
    }
}