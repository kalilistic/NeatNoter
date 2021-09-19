using System.Numerics;

namespace NeatNoter
{
    /// <summary>
    /// Note category.
    /// </summary>
    public class Category : UniqueDocument
    {
        /// <summary>
        /// Gets or sets category color.
        /// </summary>
        public Vector3 Color { get; set; }
    }
}
