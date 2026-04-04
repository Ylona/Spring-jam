using System;
using System.Linq;
using NUnit.Framework;
using SpringJam.Data.Flowers;

namespace SpringJam.Tests.EditMode
{
    public sealed class FlowerTypeCatalogTests
    {
        [Test]
        public void DefaultSpringCatalog_ContainsExpectedFlowerOrder()
        {
            FlowerTypeCatalog catalog = FlowerCatalogs.SpringPuzzle;

            FlowerTypeId[] order = catalog.FlowerTypes.Select(flower => flower.FlowerTypeId).ToArray();

            Assert.That(order, Is.EqualTo(new[]
            {
                FlowerTypeId.Hellebore,
                FlowerTypeId.Crocus,
                FlowerTypeId.Primrose,
                FlowerTypeId.Hyacinth,
                FlowerTypeId.Tulip,
                FlowerTypeId.Bluebell,
            }));
        }

        [Test]
        public void DefaultSpringCatalog_ExposesMealFlowers()
        {
            FlowerTypeCatalog catalog = FlowerCatalogs.SpringPuzzle;

            FlowerTypeId[] mealFlowers = catalog.FlowerTypes
                .Where(flower => flower.IsSpringMealIngredient)
                .Select(flower => flower.FlowerTypeId)
                .ToArray();

            Assert.That(mealFlowers, Is.EqualTo(new[]
            {
                FlowerTypeId.Primrose,
                FlowerTypeId.Tulip,
            }));
        }

        [Test]
        public void Catalog_ThrowsWhenFlowerTypeIdsRepeat()
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() => new FlowerTypeCatalog(new[]
            {
                CreateFlower(FlowerTypeId.Hellebore, 1),
                CreateFlower(FlowerTypeId.Hellebore, 2),
            }));

            Assert.That(exception.Message, Does.Contain("Duplicate flower type"));
        }

        [Test]
        public void Catalog_ThrowsWhenBloomOrderRepeats()
        {
            ArgumentException exception = Assert.Throws<ArgumentException>(() => new FlowerTypeCatalog(new[]
            {
                CreateFlower(FlowerTypeId.Hellebore, 1),
                CreateFlower(FlowerTypeId.Crocus, 1),
            }));

            Assert.That(exception.Message, Does.Contain("Duplicate bloom sequence order"));
        }

        [Test]
        public void TryGet_ReturnsKnownFlowerType()
        {
            bool found = FlowerCatalogs.SpringPuzzle.TryGet(FlowerTypeId.Hyacinth, out FlowerTypeDefinition flower);

            Assert.That(found, Is.True);
            Assert.That(flower.DisplayName, Is.EqualTo("Hyacinth"));
            Assert.That(flower.PreferredPollinator, Is.EqualTo(BeeSpecies.BumbleBee));
        }

        private static FlowerTypeDefinition CreateFlower(FlowerTypeId flowerTypeId, int bloomSequenceOrder)
        {
            return new FlowerTypeDefinition(
                flowerTypeId,
                flowerTypeId.ToString(),
                bloomSequenceOrder,
                FlowerBloomWindow.EarlySpring,
                FlowerSunPreference.FullSun,
                BeeSpecies.HoneyBee,
                false,
                "Test flower definition.");
        }
    }
}
