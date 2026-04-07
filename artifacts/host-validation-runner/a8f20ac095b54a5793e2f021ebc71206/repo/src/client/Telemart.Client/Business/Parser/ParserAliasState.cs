namespace Telemart.Client.Business.Parser
{
    public enum ParserAliasState
    {
        /// <summary>
        /// The none (0)
        /// </summary>
        None = 0,

        /// <summary>
        /// The not assosiated (1)
        /// </summary>
        NotAssosiated = 1,

        /// <summary>
        /// The associated automatic (2)
        /// </summary>
        AssociatedAuto = 2,

        /// <summary>
        /// The associated (3)
        /// </summary>
        Associated = 3,

        /// <summary>
        /// The postponed (4)
        /// </summary>
        Postponed = 4,

        /// <summary>
        /// The ignored (5)
        /// </summary>
        Ignored = 5
    }
}