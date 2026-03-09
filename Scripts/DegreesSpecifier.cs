namespace ZarkowTurretDefense.Scripts
{
    /// <summary>
    /// class describing degrees in Y (X/Z) space and X (Y/Z) space
    /// Will return contain info about angles singled, un-singed, and flags for above, below, right and left.
    /// Use the chosen properties to extract the info in the format wanted
    /// </summary>
    public class DegreesSpecifier
    {
        public float DegreesY { get; set; }

        public float DegreesX { get; set; }

        public float Distance { get; set; }
    }
}
