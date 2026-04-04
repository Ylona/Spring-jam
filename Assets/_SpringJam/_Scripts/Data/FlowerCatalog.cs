using System;
using System.Collections.Generic;

namespace SpringJam.Data.Flowers
{
    public enum FlowerTypeId
    {
        Hellebore,
        Crocus,
        Primrose,
        Hyacinth,
        Tulip,
        Bluebell,
    }

    public enum FlowerBloomWindow
    {
        EarlySpring,
        MidSpring,
        LateSpring,
    }

    public enum FlowerSunPreference
    {
        Shade,
        PartialSun,
        FullSun,
    }

    public enum BeeSpecies
    {
        HoneyBee,
        BumbleBee,
        MasonBee,
    }

    public sealed class FlowerTypeDefinition
    {
        public FlowerTypeDefinition(
            FlowerTypeId flowerTypeId,
            string displayName,
            int bloomSequenceOrder,
            FlowerBloomWindow bloomWindow,
            FlowerSunPreference sunPreference,
            BeeSpecies preferredPollinator,
            bool isSpringMealIngredient,
            string description)
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("Flower display name is required.", nameof(displayName));
            }

            if (bloomSequenceOrder <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bloomSequenceOrder), "Bloom sequence order must be greater than zero.");
            }

            if (string.IsNullOrWhiteSpace(description))
            {
                throw new ArgumentException("Flower description is required.", nameof(description));
            }

            FlowerTypeId = flowerTypeId;
            DisplayName = displayName.Trim();
            BloomSequenceOrder = bloomSequenceOrder;
            BloomWindow = bloomWindow;
            SunPreference = sunPreference;
            PreferredPollinator = preferredPollinator;
            IsSpringMealIngredient = isSpringMealIngredient;
            Description = description.Trim();
        }

        public FlowerTypeId FlowerTypeId { get; }
        public string DisplayName { get; }
        public int BloomSequenceOrder { get; }
        public FlowerBloomWindow BloomWindow { get; }
        public FlowerSunPreference SunPreference { get; }
        public BeeSpecies PreferredPollinator { get; }
        public bool IsSpringMealIngredient { get; }
        public string Description { get; }
    }

    public sealed class FlowerTypeCatalog
    {
        private readonly List<FlowerTypeDefinition> flowerTypes;
        private readonly Dictionary<FlowerTypeId, FlowerTypeDefinition> flowerLookup;

        public FlowerTypeCatalog(IEnumerable<FlowerTypeDefinition> flowerTypes)
        {
            if (flowerTypes == null)
            {
                throw new ArgumentNullException(nameof(flowerTypes));
            }

            this.flowerTypes = new List<FlowerTypeDefinition>();
            flowerLookup = new Dictionary<FlowerTypeId, FlowerTypeDefinition>();
            HashSet<int> bloomOrders = new HashSet<int>();

            foreach (FlowerTypeDefinition flowerType in flowerTypes)
            {
                if (flowerType == null)
                {
                    continue;
                }

                if (flowerLookup.ContainsKey(flowerType.FlowerTypeId))
                {
                    throw new ArgumentException(
                        string.Format("Duplicate flower type '{0}' found.", flowerType.FlowerTypeId),
                        nameof(flowerTypes));
                }

                if (!bloomOrders.Add(flowerType.BloomSequenceOrder))
                {
                    throw new ArgumentException(
                        string.Format("Duplicate bloom sequence order '{0}' found.", flowerType.BloomSequenceOrder),
                        nameof(flowerTypes));
                }

                this.flowerTypes.Add(flowerType);
                flowerLookup.Add(flowerType.FlowerTypeId, flowerType);
            }

            if (this.flowerTypes.Count == 0)
            {
                throw new ArgumentException("At least one flower type definition is required.", nameof(flowerTypes));
            }

            this.flowerTypes.Sort((left, right) => left.BloomSequenceOrder.CompareTo(right.BloomSequenceOrder));
        }

        public IReadOnlyList<FlowerTypeDefinition> FlowerTypes => flowerTypes.AsReadOnly();

        public bool TryGet(FlowerTypeId flowerTypeId, out FlowerTypeDefinition flowerTypeDefinition)
        {
            return flowerLookup.TryGetValue(flowerTypeId, out flowerTypeDefinition);
        }

        public FlowerTypeDefinition Get(FlowerTypeId flowerTypeId)
        {
            if (!TryGet(flowerTypeId, out FlowerTypeDefinition flowerTypeDefinition))
            {
                throw new KeyNotFoundException(string.Format("Flower type '{0}' is not defined in this catalog.", flowerTypeId));
            }

            return flowerTypeDefinition;
        }

        public static FlowerTypeCatalog CreateDefaultSpringPuzzleCatalog()
        {
            return new FlowerTypeCatalog(new[]
            {
                new FlowerTypeDefinition(
                    FlowerTypeId.Hellebore,
                    "Hellebore",
                    1,
                    FlowerBloomWindow.EarlySpring,
                    FlowerSunPreference.Shade,
                    BeeSpecies.BumbleBee,
                    false,
                    "A dusky early bloom in sage and plum tones that opens the garden sequence without leaving the palette."),
                new FlowerTypeDefinition(
                    FlowerTypeId.Crocus,
                    "Crocus",
                    2,
                    FlowerBloomWindow.EarlySpring,
                    FlowerSunPreference.FullSun,
                    BeeSpecies.MasonBee,
                    false,
                    "A low purple spring flower that works well as the player's first bee-routing lesson."),
                new FlowerTypeDefinition(
                    FlowerTypeId.Primrose,
                    "Primrose",
                    3,
                    FlowerBloomWindow.EarlySpring,
                    FlowerSunPreference.Shade,
                    BeeSpecies.HoneyBee,
                    true,
                    "A rosy woodland bloom whose petals can also support the spring meal."),
                new FlowerTypeDefinition(
                    FlowerTypeId.Hyacinth,
                    "Hyacinth",
                    4,
                    FlowerBloomWindow.MidSpring,
                    FlowerSunPreference.PartialSun,
                    BeeSpecies.BumbleBee,
                    false,
                    "A clustered lilac bloom that marks the middle of the chain and fits the palette far better than daffodil yellow."),
                new FlowerTypeDefinition(
                    FlowerTypeId.Tulip,
                    "Tulip",
                    5,
                    FlowerBloomWindow.MidSpring,
                    FlowerSunPreference.FullSun,
                    BeeSpecies.HoneyBee,
                    true,
                    "A warm pink cup bloom that can feed both the puzzle and the final meal."),
                new FlowerTypeDefinition(
                    FlowerTypeId.Bluebell,
                    "Bluebell",
                    6,
                    FlowerBloomWindow.LateSpring,
                    FlowerSunPreference.Shade,
                    BeeSpecies.MasonBee,
                    false,
                    "A late woodland bloom that finishes the loop's full flower sequence."),
            });
        }
    }

    public static class FlowerCatalogs
    {
        public static FlowerTypeCatalog SpringPuzzle { get; } = FlowerTypeCatalog.CreateDefaultSpringPuzzleCatalog();
    }
}
